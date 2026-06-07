using System.IO.Compression;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

namespace MaIN.Docs.Api.Services;

public class ArtifactService
{
    private readonly IAmazonS3 _s3;
    private readonly string _bucket;

    public ArtifactService(IConfiguration config)
    {
        _bucket = config["AWS_S3_BUCKET"] ?? throw new InvalidOperationException("AWS_S3_BUCKET is not configured.");
        var region = RegionEndpoint.GetBySystemName(config["AWS_REGION"] ?? "us-east-1");
        _s3 = new AmazonS3Client(
            config["AWS_ACCESS_KEY_ID"],
            config["AWS_SECRET_ACCESS_KEY"],
            region);
    }

    public async Task<string> CreateZipAndPresign(string archiveName, IList<(string Path, string Content)> files)
    {
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in files)
            {
                var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
                await using var writer = new StreamWriter(entry.Open());
                await writer.WriteAsync(content);
            }
        }

        ms.Position = 0;
        var key = $"artifacts/{Guid.NewGuid():N}/{archiveName}";

        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = ms,
            ContentType = "application/zip"
        });

        return _s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucket,
            Key = key,
            Expires = DateTime.UtcNow.AddHours(1),
            Verb = HttpVerb.GET
        });
    }
}
