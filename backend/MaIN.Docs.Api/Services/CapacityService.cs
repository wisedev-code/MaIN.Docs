using MaIN.Docs.Api.Models;
using Microsoft.Extensions.Options;

namespace MaIN.Docs.Api.Services;

public class CapacityService(IOptions<CapacitySettings> options, CapacityStateStore store, ILogger<CapacityService> logger)
{
    private readonly CapacitySettings _settings = options.Value;

    private static readonly TimeSpan FlushInterval = TimeSpan.FromMinutes(5);

    private static int _tier = 1;
    private static long _tokensInCurrentWindow = 0;
    private static DateTime? _tier1ExhaustedAt = null;
    private static DateTime? _tier2ExhaustedAt = null;
    private static int _ollamaKeyIndex = 0;
    private static bool _dirty = false;
    private static DateTime _lastFlushUtc = DateTime.UtcNow;
    private static readonly object _lock = new();

    // Restores tier/usage state persisted from a previous run (if any). Call once at
    // startup, before the app starts accepting requests.
    public async Task LoadStateAsync(CancellationToken ct = default)
    {
        var state = await store.LoadAsync(ct);
        if (state is null)
            return;

        lock (_lock)
        {
            _tier = state.Tier;
            _tokensInCurrentWindow = state.TokensInCurrentWindow;
            _tier1ExhaustedAt = state.Tier1ExhaustedAt;
            _tier2ExhaustedAt = state.Tier2ExhaustedAt;
            _ollamaKeyIndex = state.OllamaKeyIndex;
            _lastFlushUtc = DateTime.UtcNow;
        }

        logger.LogInformation("[Capacity] Restored persisted state — tier {Tier}, {Tokens} tokens in window, Ollama key #{KeyIndex}",
            state.Tier, state.TokensInCurrentWindow, state.OllamaKeyIndex + 1);
    }

    // Persists current state to S3 if it has changed since the last save. Called on
    // graceful shutdown as a final safety net — normal flushes happen inline from
    // RecordTokenUsage/GetCurrentTier, no background timer involved.
    public async Task FlushIfDirtyAsync(CancellationToken ct = default)
    {
        CapacityState? snapshot = null;
        lock (_lock)
        {
            if (!_dirty)
                return;

            snapshot = new CapacityState(_tier, _tokensInCurrentWindow, _tier1ExhaustedAt, _tier2ExhaustedAt, _ollamaKeyIndex);
            _dirty = false;
            _lastFlushUtc = DateTime.UtcNow;
        }

        await store.SaveAsync(snapshot, ct);
    }

    public int GetCurrentTier()
    {
        CapacityState? snapshot = null;
        int tier;
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (_tier == 2 && _tier1ExhaustedAt.HasValue &&
                now - _tier1ExhaustedAt.Value > TimeSpan.FromMinutes(_settings.Tier1.CooldownMinutes))
            {
                logger.LogInformation("[Capacity] Tier 1 cooldown expired — resetting to Tier 1");
                _tier = 1;
                _tokensInCurrentWindow = 0;
                _tier1ExhaustedAt = null;
                snapshot = new CapacityState(_tier, _tokensInCurrentWindow, _tier1ExhaustedAt, _tier2ExhaustedAt, _ollamaKeyIndex);
                _dirty = false;
                _lastFlushUtc = now;
            }

            if (_tier == 3 && _tier2ExhaustedAt.HasValue &&
                now - _tier2ExhaustedAt.Value > TimeSpan.FromMinutes(_settings.Tier2.CooldownMinutes))
            {
                logger.LogInformation("[Capacity] Tier 2 cooldown expired — resetting to Tier 2");
                _tier = 2;
                _tokensInCurrentWindow = 0;
                _tier2ExhaustedAt = null;
                snapshot = new CapacityState(_tier, _tokensInCurrentWindow, _tier1ExhaustedAt, _tier2ExhaustedAt, _ollamaKeyIndex);
                _dirty = false;
                _lastFlushUtc = now;
            }

            tier = _tier;
        }

        if (snapshot is not null)
            _ = store.SaveAsync(snapshot);

        return tier;
    }

    public string GetCapacityLevel() => GetCurrentTier() switch
    {
        1 => "normal",
        2 => "low",
        _ => "very-low"
    };

    public CapacityStatus GetStatus()
    {
        lock (_lock)
        {
            var tier = GetCurrentTier();

            long? limit = tier switch
            {
                1 => _settings.Tier1.TokenLimit,
                2 => _settings.Tier2.TokenLimit,
                _ => null
            };

            long? remaining = limit.HasValue
                ? Math.Max(0, limit.Value - _tokensInCurrentWindow)
                : null;

            DateTime? resetsAt = tier switch
            {
                2 => _tier1ExhaustedAt?.AddMinutes(_settings.Tier1.CooldownMinutes),
                3 => _tier2ExhaustedAt?.AddMinutes(_settings.Tier2.CooldownMinutes),
                _ => null
            };

            return new CapacityStatus(tier, GetCapacityLevel(), _tokensInCurrentWindow, limit, remaining, resetsAt);
        }
    }

    public (bool TierChanged, int NewTier) RecordTokenUsage(int tokens)
    {
        CapacityState? snapshot = null;
        bool tierChanged;
        int newTier;
        lock (_lock)
        {
            _tokensInCurrentWindow += tokens;
            _dirty = true;

            if (_tier == 1 && _tokensInCurrentWindow >= _settings.Tier1.TokenLimit)
            {
                _tier1ExhaustedAt = DateTime.UtcNow;
                _tier = 2;
                _tokensInCurrentWindow = 0;
                logger.LogWarning("[Capacity] Tier 1 exhausted ({Limit} tokens) — switching to Tier 2 (low)", _settings.Tier1.TokenLimit);
                tierChanged = true;
            }
            else if (_tier == 2 && _tokensInCurrentWindow >= _settings.Tier2.TokenLimit)
            {
                _tier2ExhaustedAt = DateTime.UtcNow;
                _tier = 3;
                _tokensInCurrentWindow = 0;
                logger.LogWarning("[Capacity] Tier 2 exhausted ({Limit} tokens) — switching to Tier 3 (very-low)", _settings.Tier2.TokenLimit);
                tierChanged = true;
            }
            else
            {
                tierChanged = false;
            }

            newTier = _tier;

            // Flush immediately on a tier transition (rare, must not be lost), or
            // opportunistically piggyback on this request if it's been a while since the
            // last flush — no background timer needed to keep state roughly up to date.
            var now = DateTime.UtcNow;
            if (tierChanged || now - _lastFlushUtc >= FlushInterval)
            {
                snapshot = new CapacityState(_tier, _tokensInCurrentWindow, _tier1ExhaustedAt, _tier2ExhaustedAt, _ollamaKeyIndex);
                _dirty = false;
                _lastFlushUtc = now;
            }
        }

        if (snapshot is not null)
            _ = store.SaveAsync(snapshot);

        return (tierChanged, newTier);
    }

    public int GetOllamaKeyIndex()
    {
        lock (_lock) return _ollamaKeyIndex;
    }

    // Called when a Tier 3 (Ollama) request fails in a way that suggests the active
    // key's quota is exhausted. Switches to the secondary key (if configured and not
    // already active) so the next request transparently retries on it. Returns true
    // if a switch happened.
    public bool MarkOllamaKeyExhausted()
    {
        CapacityState? snapshot = null;
        bool switched;
        lock (_lock)
        {
            if (_ollamaKeyIndex == 0 && !string.IsNullOrEmpty(_settings.Tier3.OllamaKey2))
            {
                _ollamaKeyIndex = 1;
                logger.LogWarning("[Capacity] Tier 3 Ollama primary key appears exhausted — switching to secondary key");
                snapshot = new CapacityState(_tier, _tokensInCurrentWindow, _tier1ExhaustedAt, _tier2ExhaustedAt, _ollamaKeyIndex);
                _dirty = false;
                _lastFlushUtc = DateTime.UtcNow;
                switched = true;
            }
            else
            {
                switched = false;
            }
        }

        if (snapshot is not null)
            _ = store.SaveAsync(snapshot);

        return switched;
    }
}
