using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Storage.V1;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Google.Apis.Auth.OAuth2;
using System.Security.AccessControl;
using Auth0.ManagementApi.Models.AttackProtection;
using Google.Apis.Storage.v1.Data;
using server.Interfaces;
using server.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FilesController : Controller
    {
        private readonly IFirebaseStorageService _firebaseStorageService;

        public FilesController(IFirebaseStorageService firebaseStorageService)
        {
            _firebaseStorageService = firebaseStorageService;
        }



        [HttpPost("upload-pic")]
        public async Task<IActionResult> UploadPicAsync(string base64, string fileName)
        {
            try
            {
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                //await _firebaseStorageService.UploadFileAsync(file, currentUserUid);
                //string publicUrl = await _firebaseStorageService.UploadFileAsync(file, currentUserUid);
                string pathPrefix = $"{currentUserUid}/ProfilePicture";
                string publicUrl = await _firebaseStorageService.UploadPfpToFirebase(base64, fileName, currentUserUid);

                return Ok(new { Message = $"File has been uploaded successfully: {publicUrl}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error during file upload {ex.Message}" });
            }
        }
    }
}