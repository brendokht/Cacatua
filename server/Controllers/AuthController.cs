using Microsoft.AspNetCore.Mvc;
using FirebaseAdmin.Auth;
using server.Models;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using server.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using server.Utilities;
using Microsoft.AspNetCore.Authorization;

namespace server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IFirestoreService _firestoreService;
        private readonly HttpClient _httpClient;
        private readonly Middlewares _middleware;
        private readonly IEmailService _emailService;
        private readonly ITokenService _tokenService;

        public AuthController(IConfiguration configuration, IEmailService emailService, IFirestoreService firestoreService, ITokenService tokenService)
        {
            _configuration = configuration;
            _firestoreService = firestoreService;
            _httpClient = new HttpClient();
            _middleware = new Middlewares(_configuration);
            _emailService = emailService;
            _tokenService = tokenService;

            if (FirebaseApp.DefaultInstance == null)
            {
                var serviceAccount = new JObject
                {
                    { "type", _configuration["type"] },
                    { "project_id", _configuration["project_id"] },
                    { "private_key_id", _configuration["private_key_id"] },
                    { "private_key", _configuration["private_key"] },
                    { "client_email", _configuration["client_email"] },
                    { "client_id", _configuration["client_id"] },
                    { "auth_uri", _configuration["auth_uri"] },
                    { "token_uri", _configuration["token_uri"] },
                    { "auth_provider_x509_cert_url", _configuration["auth_provider_x509_cert_url"] },
                    { "client_x509_cert_url", _configuration["client_x509_cert_url"] },
                    { "universe_domain", _configuration["universe_domain"] }
                };

                string jsonCredentials = serviceAccount.ToString();

                GoogleCredential credential = GoogleCredential.FromJson(jsonCredentials);

                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential
                });
            }
        }

        [HttpPost("pre-register")]
        [AllowAnonymous]
        public async Task<IActionResult> PreRegister([FromBody] RegisterRequestModel request)
        {
            try
            {
                if (await _firestoreService.CheckEmailExists(request.Email))
                {
                    return Conflict(new { Message = "Email already exists. Please use different email." });
                }

                string verificationToken = Guid.NewGuid().ToString();

                await _firestoreService.SaveUserPreVerificationAsync(request, verificationToken);

                string verificationLink = $"{_configuration["UrlList:EmailVerificationUrl"]}{verificationToken}";

                // Send verification email
                string email = request.Email;
                string displayName = request.DisplayName;
                await _emailService.SendVerificationEmailAsync(email, displayName, verificationLink);

                //return Ok(new { Message = $"Verification email has been sent to {request.Email}. Please verify to complete registration." });
                //ONLY FOR DEBUGGING (test-registration.ps1)
                return Ok(new { Message = $"Verification email has been sent to {request.Email}. Please verify to complete registration.", VerificationToken = verificationToken });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpGet("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token)
        {
            try
            {
                var request = await _firestoreService.GetVerificationDataAsync(token);
                if (request.UserData == null)
                {
                    return BadRequest(new { Message = "Invalid token." });
                }

                if (request.IsTokenExpired)
                {
                    return BadRequest(new { Message = "Token expired." });
                }

                await _firestoreService.CreateAccountDb(token, request);

                //return Ok(new { Message = $"Account is verified successfully." });
                return Ok(new { Message = $"Account {request.UserData.Email} is verified successfully." });
            }
            catch (Firebase.Auth.FirebaseAuthException ex)
            {
                return BadRequest(new { Message = $"Error creating user: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestModel request)
        {
            try
            {
                var loginPayload = new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                };

                string firebaseAuthUrl = _configuration["UrlList:LoginAuthUrl"];
                HttpResponseMessage response = await _httpClient.PostAsJsonAsync(firebaseAuthUrl, loginPayload);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var loginResponse = JsonConvert.DeserializeObject<FirebaseLoginResponseModel>(responseContent);

                    var user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(request.Email);

                    var jwtToken = _middleware.GenerateJwtToken(user.Uid);

                    var refreshToken = _tokenService.GenerateRefreshToken();

                    await _firestoreService.SaveRefreshTokenToFirestore(user.Uid, refreshToken);

                    return Ok(new { Message = "Login successful", User = user, JWT = jwtToken, Refresh = refreshToken });
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    return BadRequest(new { Message = $"Login failed: {errorResponse}" });
                }
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex)
            {
                return Unauthorized(new { Message = $"Authentication error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> SignOut([FromBody] string userId)
        {
            try
            {
                await _firestoreService.RemoveRefreshToken(userId);

                return Ok(new { Message = "Logout successful. Please remove the token on the client-side." });

            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error during logout: {ex.Message}" });
            }
        }


        [HttpGet("check-jwt")]
        [AllowAnonymous]
        public IActionResult VerifyJWT([FromQuery] string jwtToken)
        {
            try
            {
                bool isValid = _middleware.ValidateJwtToken(jwtToken);
                if (isValid)
                {
                    return Ok(new { Message = "JWT is Valid :)" });
                }
                return Unauthorized(new { Message = $"JWT is Invalid :((((((((" });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { Message = $"Authentication error: {ex.Message}" });
            }
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] string token)
        {
            try
            {
                var tokenData = await _tokenService.ValidateRefreshToken(token);
                if (tokenData == null)
                {
                    return Unauthorized(new { Message = "Invalid or expired refresh token." });
                }

                string userId = tokenData["user_uid"].ToString();

                var newJwtToken = _middleware.GenerateJwtToken(userId);

                var newRefreshToken = _tokenService.GenerateRefreshToken();

                await _tokenService.RotateRefreshToken(userId, token, newRefreshToken);

                return Ok(new
                {
                    JWT = newJwtToken,
                    RefreshToken = newRefreshToken
                });
            }
            catch (Exception ex)
            {
                // Handle exceptions and return an appropriate error response
                return BadRequest(new { Message = $"Error refreshing token: {ex.Message}" });
            }
        }
    }
}