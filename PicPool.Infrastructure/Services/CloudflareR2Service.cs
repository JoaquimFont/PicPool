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

        /// <summary>
        /// Explicació: inicialitza el client S3 compatible amb Cloudflare R2 a partir de la configuració.
        /// Precondicions: la configuració CloudflareR2:* ha d'incloure credencials, URL del servei, bucket i URL pública.
        /// Postcondicions: el servei queda preparat per pujar, copiar i eliminar objectes del bucket configurat.
        /// </summary>
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

        /// <summary>
        /// Explicació: puja un fitxer al bucket R2 i construeix la seva URL pública.
        /// Precondicions: l'stream ha de ser llegible, l'objectKey ha d'identificar la ruta de destí i el contentType ha d'estar informat.
        /// Postcondicions: l'objecte queda pujat al bucket i es retorna la URL pública esperada.
        /// </summary>
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

        /// <summary>
        /// Explicació: llegeix un objecte de R2 i el copia a un stream de destinació.
        /// Precondicions: l'objectKey ha d'existir al bucket i el stream de destinació ha de permetre escriptura.
        /// Postcondicions: el contingut de l'objecte queda copiat al stream proporcionat.
        /// </summary>
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

        /// <summary>
        /// Explicació: elimina un objecte del bucket R2 si la clau està informada.
        /// Precondicions: l'objectKey ha d'identificar l'objecte a eliminar; si és buit, el mètode no fa cap operació.
        /// Postcondicions: l'objecte queda eliminat del bucket quan la clau és vàlida i l'operació remota finalitza correctament.
        /// </summary>
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
