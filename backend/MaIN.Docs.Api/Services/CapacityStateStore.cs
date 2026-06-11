using System.Net;
using System.Text;
using System.Text.Json;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using MaIN.Docs.Api.Models;

namespace MaIN.Docs.Api.Services;

// Persists CapacityService's tier/usage state to S3 so it survives container
// restarts (e.g. serverless scale-to-zero) instead of always resetting to Tier 1.
public class CapacityStateStore
{
    private const string Key = "state/capacity.json";

    private readonly IAmazonS3 _s3;
    private readonly string _bucket;
    private readonly ILogger<CapacityStateStore> _logger;

    public CapacityStateStore(IConfiguration config, ILogger<CapacityStateStore> logger)
    {
        _logger = logger;
        _bucket = config["AWS_S3_BUCKET"] ?? throw new InvalidOperationException("AWS_S3_BUCKET is not configured.");
        var region = RegionEndpoint.GetBySystemName(config["AWS_REGION"] ?? "us-east-1");
        _s3 = new AmazonS3Client(
            config["AWS_ACCESS_KEY_ID"],
            config["AWS_SECRET_ACCESS_KEY"],
            region);
    }

    public async Task<CapacityState?> LoadAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _s3.GetObjectAsync(_bucket, Key, ct);
            using var reader = new StreamReader(response.ResponseStream);
            var json = await reader.ReadToEndAsync(ct);
            return JsonSerializer.Deserialize<CapacityState>(json);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("[Capacity] No persisted state found in S3 — starting fresh at Tier 1");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Capacity] Failed to load persisted state from S3 — starting fresh at Tier 1");
            return null;
        }
    }

    public async Task SaveAsync(CapacityState state, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(state);
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = Key,
                InputStream = ms,
                ContentType = "application/json"
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Capacity] Failed to persist state to S3");
        }
    }
}
