using Auth0.ManagementApi.Models;
using Firebase.Auth;
using Firebase.Storage;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Storage.v1.Data;
using Google.Cloud.Firestore;
using Google.Cloud.Storage.V1;
using server.Interfaces;
using System.IO;

namespace server.Services
{
    public class FirebaseStorageService : IFirebaseStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly StorageClient _storageClient;
        private readonly string _bucketName;
        private readonly GoogleCredential _credential;

        public FirebaseStorageService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
            _bucketName = _configuration["FirebaseConfig:Bucket"];

            // Read credentials file path from configuration or GOOGLE_APPLICATION_CREDENTIALS env var.
            // Do not hard-code credential filenames in source.
            var credentialsPath = _configuration["FirebaseConfig:CredentialsFile"] ?? Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (string.IsNullOrWhiteSpace(credentialsPath) || !File.Exists(credentialsPath))
            {
                throw new FileNotFoundException($"Firebase credentials file not found. Set FirebaseConfig:CredentialsFile in configuration or set GOOGLE_APPLICATION_CREDENTIALS environment variable to the service account json path.", credentialsPath);
            }

            _credential = GoogleCredential.FromFile(credentialsPath);
            _storageClient = StorageClient.Create(_credential);

            try
            {
                MakeBucketPublic(_bucketName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error making bucket public: {ex.Message}");
            }
        }

        public async Task DeleteUserAccountPics(string userId)
        {
            string prefix = $"{userId}/"; // Prefix to specify the user's folder

            try
            {
                // List all objects within the user's folder
                var objects = _storageClient.ListObjects(_bucketName, prefix);

                // Delete each object in the user's folder
                foreach (var storageObject in objects)
                {
                    await _storageClient.DeleteObjectAsync(_bucketName, storageObject.Name);
                    Console.WriteLine($"Deleted: {storageObject.Name}");
                }

                Console.WriteLine($"All files in {prefix} have been deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting user files for {userId}: {ex.Message}");
            }
        }


        //From: https://cloud.google.com/storage/docs/access-control/making-data-public
        //And https://stackoverflow.com/a/76453702
        public void MakeBucketPublic(string bucketName)
        {
            var policy = _storageClient.GetBucketIamPolicy(bucketName);
            policy.Bindings.Add(new Policy.BindingsData
            {
                Role = "roles/storage.objectViewer",
                Members = new List<string> { "allUsers" }
            });

            _storageClient.SetBucketIamPolicy(bucketName, policy);
            Console.WriteLine($"{bucketName} is now public");
        }

        public async Task<string> UploadFileAsync(IFormFile file, string userId)
        {
            return await UploadFileToFirebase(file, $"{userId}/images");
        }

        public async Task<string> UploadPfpAsync(IFormFile file, string userId)
        {
            //return await UploadFileToFirebase(file, $"{userId}/ProfilePicture");
            return await UploadFileToFirebase(file, $"{userId}/ProfilePicture");
        }

        //TODO: Change this to IActionResult
        public async Task<string> UploadFileToFirebase(IFormFile file, string pathPrefix)
        {
            string fileName = file.FileName;
            var filePath = await SaveFileLocallyAsync(file, fileName);

            if (filePath.StartsWith("Error")) return null;

            try
            {
                string firebasePath = $"{pathPrefix}/{fileName}";
                string contentType = await GetContentType(fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Open))
                {
                    await _storageClient.UploadObjectAsync(_bucketName, firebasePath, contentType, fileStream,
                        new UploadObjectOptions { PredefinedAcl = PredefinedObjectAcl.PublicRead });
                }

                UrlSigner urlSigner = UrlSigner.FromCredential(_credential);
                string publicUrl = urlSigner.Sign(_bucketName, firebasePath, TimeSpan.FromDays(7), HttpMethod.Get);

                //On Firebase Storage, I am getting "Error creating access token". Access token is generated by the Firebase. Unfortunately, I am not able to generate it manually.
                //To remedy that, I added a rule to the Firebase Storage to allow public access to the files. It's better than nothing. -Garen
                //string publicUrl = $"https://storage.googleapis.com/{_bucketName}/{firebasePath}";

                return publicUrl;
            }
            catch (Exception ex)
            {
                return $"Error during file upload: {ex.Message}";
            }
            finally
            {
                try { if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath); }
                catch (Exception deleteEx) { Console.WriteLine($"Error deleting local file: {deleteEx.Message}"); }
            }
        }

        //public async Task<string> UploadToFirebase(IFormFile file, string pathPrefix)
        public async Task<string> UploadPfpToFirebase(string base64Image, string fileName, string pathPrefix)
        {
            //Console.WriteLine("Upload Pfp method: ",base64Image,fileName,pathPrefix);
            try
            {
                string firebasePath = $"{pathPrefix}/{fileName}";
                string contentType = await GetContentType(fileName);
                
                byte[] imageBytes = Convert.FromBase64String(base64Image);

                var task = new FirebaseStorage(_bucketName)
                     .Child(firebasePath)
                     .Child(fileName)
                     .PutAsync(new MemoryStream(imageBytes));

                // Track progress of the upload
                task.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");

                //UrlSigner urlSigner = UrlSigner.FromCredential(_credential);
                //string publicUrl = urlSigner.Sign(_bucketName, firebasePath, TimeSpan.FromDays(7), HttpMethod.Get);

                //On Firebase Storage, I am getting "Error creating access token". Access token is generated by the Firebase. Unfortunately, I am not able to generate it manually.
                //To remedy that, I added a rule to the Firebase Storage to allow public access to the files. It's better than nothing. -Garen
                //string publicUrl = $"https://storage.googleapis.com/{_bucketName}/{firebasePath}";
                
                // Await the task to wait until upload is completed and get the download url
                var publicUrl = await task;
                Console.WriteLine("Url: " +  publicUrl);
                return publicUrl;
            }
            catch (Exception ex)
            {
                return $"Error during file upload: {ex.Message}";
            }
            finally
            {
                ////try { if (System.IO.File.Exists(filePath)) System.IO.File.Delete(filePath); }
                ////catch (Exception deleteEx) { Console.WriteLine($"Error deleting local file: {deleteEx.Message}"); }
            }
        }

        public async Task<string> SaveFileLocallyAsync(IFormFile img, string imgName, string folderName = "images")
        {
            if (img == null || img.Length == 0)
            {
                return "File is invalid";
            }

            // Generate the folder path to store the image temporarily
            var folderPath = Path.Combine(_env.WebRootPath, folderName);

            // Ensure the directory exists
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Full path for the file
            var imgPath = Path.Combine(folderPath, imgName);

            try
            {
                // Save the file locally
                using (var stream = new FileStream(imgPath, FileMode.Create))
                {
                    await img.CopyToAsync(stream);
                }
            }
            catch (Exception ex)
            {
                return $"Error saving file: {ex.Message}";
            }

            return imgPath; // Return the file path if everything is successful
        }

        public async Task<string> GetContentType(string imgName)
        {
            string contentType;
            switch (Path.GetExtension(imgName).ToLower())
            {
                case ".jpg":
                case ".jpeg":
                    contentType = "image/jpeg";
                    break;
                case ".png":
                    contentType = "image/png";
                    break;
                case ".gif":
                    contentType = "image/gif";
                    break;
                default:
                    contentType = "application/octet-stream"; // Fallback to generic binary content
                    break;
            }

            return contentType;
        }
    }
}
