using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;


namespace ProductManagement.Application.Services
{
    public class MinioFileService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public MinioFileService(IConfiguration configuration)
        {
            var minioConfig = configuration.GetSection("Minio");
            _bucketName = minioConfig["BucketName"];

            var config = new AmazonS3Config
            {
                ServiceURL = $"http://{minioConfig["Endpoint"]}",
                ForcePathStyle = true // MinIO için gerekli
            };
            _s3Client = new AmazonS3Client(minioConfig["AccessKey"], minioConfig["SecretKey"], config);
            if (!_s3Client.DoesS3BucketExistAsync(_bucketName).Result)
            {
                _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                }).Wait();
            }
        }

        public async Task<string> UploadFileAsync(IFormFile file)
        {
            var key = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            using (var stream = file.OpenReadStream())
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = _bucketName,
                    Key = key,
                    InputStream = stream,
                    ContentType = file.ContentType
                };

                await _s3Client.PutObjectAsync(putRequest);
            }

            return key;
        }

        public async Task<Stream> GetFileAsync(string key)
        {
            var getRequest = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectAsync(getRequest);
            var memoryStream = new MemoryStream();
            await response.ResponseStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public string GetFileUrl(string key)
        {
            // MinIO'da dosyaları genel erişilebilir yapmak için presigned URL kullanabilirsiniz
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.AddHours(1)
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
