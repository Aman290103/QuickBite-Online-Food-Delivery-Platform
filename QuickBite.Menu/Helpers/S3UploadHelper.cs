using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.AspNetCore.Http;

namespace QuickBite.Menu.Helpers
{
    public interface IS3UploadHelper
    {
        Task<string> UploadImageAsync(IFormFile file, string folderName);
    }

    public class S3UploadHelper : IS3UploadHelper
    {
        private readonly IConfiguration _config;
        private readonly IAmazonS3 _s3Client;

        public S3UploadHelper(IConfiguration config, IAmazonS3 s3Client)
        {
            _config = config;
            _s3Client = s3Client;
        }

        public async Task<string> UploadImageAsync(IFormFile file, string folderName)
        {
            var bucketName = _config["AWS:BucketName"];
            if (string.IsNullOrEmpty(bucketName))
            {
                // FALLBACK: Return a dummy URL if S3 is not configured
                return $"https://cdn.quickbite.com/uploads/{folderName}/{Guid.NewGuid()}_{file.FileName}";
            }

            using var newStream = new MemoryStream();
            await file.CopyToAsync(newStream);

            var fileTransferUtility = new TransferUtility(_s3Client);
            var key = $"{folderName}/{Guid.NewGuid()}_{file.FileName}";

            await fileTransferUtility.UploadAsync(newStream, bucketName, key);

            return $"https://{bucketName}.s3.amazonaws.com/{key}";
        }
    }
}
