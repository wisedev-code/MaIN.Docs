using MaIN.Docs.Api.Models;
using Microsoft.Extensions.Options;

namespace MaIN.Docs.Api.Services;

public class CapacityService(IOptions<CapacitySettings> options, ILogger<CapacityService> logger)
{
    private readonly CapacitySettings _settings = options.Value;

    private static int _tier = 1;
    private static long _tokensInCurrentWindow = 0;
    private static DateTime? _tier1ExhaustedAt = null;
    private static DateTime? _tier2ExhaustedAt = null;
    private static readonly object _lock = new();

    public int GetCurrentTier()
    {
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
            }

            if (_tier == 3 && _tier2ExhaustedAt.HasValue &&
                now - _tier2ExhaustedAt.Value > TimeSpan.FromMinutes(_settings.Tier2.CooldownMinutes))
            {
                logger.LogInformation("[Capacity] Tier 2 cooldown expired — resetting to Tier 2");
                _tier = 2;
                _tokensInCurrentWindow = 0;
                _tier2ExhaustedAt = null;
            }

            return _tier;
        }
    }

    public string GetCapacityLevel() => GetCurrentTier() switch
    {
        1 => "normal",
        2 => "low",
        _ => "very-low"
    };

    public (bool TierChanged, int NewTier) RecordTokenUsage(int tokens)
    {
        lock (_lock)
        {
            _tokensInCurrentWindow += tokens;

            if (_tier == 1 && _tokensInCurrentWindow >= _settings.Tier1.TokenLimit)
            {
                _tier1ExhaustedAt = DateTime.UtcNow;
                _tier = 2;
                _tokensInCurrentWindow = 0;
                logger.LogWarning("[Capacity] Tier 1 exhausted ({Limit} tokens) — switching to Tier 2 (low)", _settings.Tier1.TokenLimit);
                return (true, 2);
            }

            if (_tier == 2 && _tokensInCurrentWindow >= _settings.Tier2.TokenLimit)
            {
                _tier2ExhaustedAt = DateTime.UtcNow;
                _tier = 3;
                _tokensInCurrentWindow = 0;
                logger.LogWarning("[Capacity] Tier 2 exhausted ({Limit} tokens) — switching to Tier 3 (very-low)", _settings.Tier2.TokenLimit);
                return (true, 3);
            }

            return (false, _tier);
        }
    }
}
