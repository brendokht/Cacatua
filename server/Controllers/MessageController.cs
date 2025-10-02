using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Google.Cloud.Storage.V1;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;
using server.Models;
using server.Interfaces;
using Microsoft.AspNetCore.Authorization;


namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MessageController : Controller
    {
        private readonly IFirestoreService _firestoreService;


        public MessageController(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        [HttpPost("send-message-async")]
        public async Task<IActionResult> SendMessageAsync([FromBody] MessageModel request)
        {
            Console.WriteLine("send-message-async hit");
            var stopwatch = Stopwatch.StartNew();
            try
            {
                await _firestoreService.SendMessage(request);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
            stopwatch.Stop();
            Console.WriteLine($"Firestore SendMessage Time: {stopwatch.Elapsed.Milliseconds}ms");
            return Ok();
        }
    }
}
