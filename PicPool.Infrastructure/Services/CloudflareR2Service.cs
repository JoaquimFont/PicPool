using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace PicPool.Infrastructure.Services
{
    public class CloudflareR2Service
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        public CloudflareR2Service(IConfiguration configuration)
        {
            var accessKey = configuration["CloudflareR2:AccessKey"];
            var secretKey = configuration["CloudflareR2:SecretKey"];
            var serviceUrl = configuration["CloudflareR2:ServiceUrl"];

            _bucketName = configuration["CloudflareR2:BucketName"]!;
            _publicUrl = configuration["CloudflareR2:PublicUrl"]!;

            var config = new AmazonS3Config
            {
                ServiceURL = serviceUrl,
                ForcePathStyle = true
            };

            _s3Client = new AmazonS3Client(
                accessKey,
                secretKey,
                config
            );
        }

        public async Task<string> PujarFitxerAsync(
            Stream stream,
            string objectKey,
            string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = stream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request);

            return $"{_publicUrl}/{objectKey}";
        }

        public async Task CopiarFitxerAStreamAsync(string objectKey, Stream destinationStream, CancellationToken cancellationToken = default)
        {
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            using var response = await _s3Client.GetObjectAsync(
                request,
                cancellationToken
            );

            await response.ResponseStream.CopyToAsync(
                destinationStream,
                cancellationToken
            );
        }

        public async Task EliminarFitxerAsync(string objectKey, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(objectKey))
            {
                return;
            }

            var request = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);
        }
    }
}