using Microsoft.AspNetCore.Mvc;

namespace server.Interfaces
{
    public interface IFirebaseStorageService
    {
        Task<string> UploadFileToFirebase(IFormFile file, string pathPrefix);
        Task<string> UploadFileAsync(IFormFile file, string userId);
        Task<string> UploadPfpAsync(IFormFile file, string userId);
        Task DeleteUserAccountPics(string userId);
        Task<string> SaveFileLocallyAsync(IFormFile img, string imgName, string folderName = "images");
        Task<string> GetContentType(string imgName);
        Task<string> UploadPfpToFirebase(string base64Image, string fileName, string pathPrefix);
    }
}
