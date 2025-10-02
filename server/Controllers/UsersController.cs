using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using server.Models;
using Microsoft.Extensions.FileProviders;
using Firebase.Storage;
using server.Interfaces;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Google.Cloud.Firestore;
using Firebase.Auth;
using UserInfo = server.Models.UserInfo;
using FirebaseAuthException = Firebase.Auth.FirebaseAuthException;
using System.Collections;
using FireSharp.Extensions;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IFirebaseStorageService _firebaseStorageService;
        private readonly IFirestoreService _firestoreService;


        public UsersController(IConfiguration configuration, IEmailService emailService, IFirebaseStorageService firebaseStorageService, IFirestoreService firestoreService)
        {
            _configuration = configuration;
            _emailService = emailService;
            _firebaseStorageService = firebaseStorageService;
            _firestoreService = firestoreService;
        }

        [HttpGet("get-users")]
        public async Task<IActionResult> GetAllUsers()
        {
            try
            {
                List<object> users = new List<object>();
                var enumerator = FirebaseAuth.DefaultInstance.ListUsersAsync(null).GetAsyncEnumerator();

                while (await enumerator.MoveNextAsync())
                {
                    var user = enumerator.Current;

                    // Create a list of account/user objects with the following properties
                    users.Add(new
                    {
                        user.Uid,
                        user.Email,
                        user.DisplayName,
                        user.PhoneNumber,
                        user.PhotoUrl,
                        user.EmailVerified,
                        user.Disabled,
                        user.TokensValidAfterTimestamp
                    });
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpDelete("delete-user")]
        public async Task<IActionResult> DeleteUser()
        {
            string email = "NULL";
            try
            {
                //FOR DEBUGGING ONLY
                /////////////
                Process.Start("powershell", $"-Command Clear-Content -Path ../bin/Debug/net8.0/jwt-token.txt -Force");
                /////////////

                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);
                email = userRecord.Email;
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(userRecord.Uid);
                await _firestoreService.DeleteAccountDb(userRecord.Uid);
                await _firebaseStorageService.DeleteUserAccountPics(userRecord.Uid);
                await _emailService.SendAccountDeletionEmailAsync(email, userRecord.DisplayName);
                return Ok($"Successfully deleted user: {userRecord.Email}.");
            }
            catch (FirebaseAuthException ex)
            {
                return StatusCode(404, $"User with email {email} does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting user: {ex.Message}");
            }
        }

        [HttpGet("get-user-email")]
        public async Task<IActionResult> GetUserByEmail(string email)
        {
            try
            {
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
                return Ok(userRecord);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpGet("get-profile-data")]
        public async Task<IActionResult> GetProfileByUid(string uid)
        {
            try
            {
                var requesterUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;


                UserInfo userInfo = await _firestoreService.GetUserFromDbByUidAsync(uid);


                //Get users friends list
                List<UserWithDateTime> usersFriendsList = await _firestoreService.GetFriendActivityByCollection(userInfo.Uid, "friend_list");

                //User is viewing their own profile page
                if (userInfo.Uid == requesterUid)
                {
                    //Get the users friend request received list
                    List<FlockInviteReceivedModel> usersReceivedFlockInvites =
                        await _firestoreService.LoadReceivedInvites(userInfo.Uid);
                    List<UserWithDateTime> usersFriendRequestReceived = await _firestoreService.GetFriendActivityByCollection(userInfo.Uid, "received_friend_requests");
                    return Ok(new { Photo = userInfo.PhotoUrl, userInfo.DisplayName, Friends = usersFriendsList, FriendRequests = usersFriendRequestReceived, FlockInvites = usersReceivedFlockInvites });
                }

                //List of friend requests received
                List<UserWithDateTime> userList = await _firestoreService.GetFriendActivityByCollection(userInfo.Uid, "received_friend_requests");

                //Check to see if the user is already friends
                bool alreadyFriends = usersFriendsList.Any(user => user.UserInfo.Uid == requesterUid);

                //Find if the user already sent a friend request
                bool friendRequestAlreadySent = userList.Any(user => user.UserInfo.Uid == requesterUid);


                //Get the users friend request received list
                List<UserWithDateTime> requesterFriendRequestReceived = await _firestoreService.GetFriendActivityByCollection(requesterUid, "received_friend_requests");
                //Find if the user has received a friend request from the user
                bool receivedFriendrequest = requesterFriendRequestReceived.Any(user => user.UserInfo.Uid == userInfo.Uid);
                for (int i = 0; i < requesterFriendRequestReceived.Count; i++)
                {
                    Console.WriteLine(requesterFriendRequestReceived[i].UserInfo.Uid);
                }
                return Ok(new
                {
                    Photo = userInfo.PhotoUrl,
                    userInfo.DisplayName,
                    FriendRequestAlreadySent = friendRequestAlreadySent,
                    Friends = usersFriendsList,
                    AlreadyFriends = alreadyFriends,
                    receivedFriendrequest
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        [HttpGet("get-user-uid")]
        public async Task<IActionResult> GetUserByUid(string uid)
        {
            try
            {
                UserInfo userInfo = await _firestoreService.GetUserFromDbByUidAsync(uid);

                return Ok(new { Photo = userInfo.PhotoUrl, userInfo.DisplayName });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"General error: {ex.Message}" });
            }
        }

        //[HttpGet("get-friends-uid")]
        //public async Task<IActionResult> GetFriendsByUid(string uid)
        //{
        //    try
        //    {
        //        List<UserInfo> userInfo = await _firestoreService.GetUsersListByDbAsync(uid);

        //        return Ok(new { Message = userInfo.Email, userInfo.PhotoUrl, userInfo.DisplayName });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { Message = $"General error: {ex.Message}" });
        //    }
        //}

        [HttpPut("update-email")]
        public async Task<IActionResult> UpdateEmail(string newEmail)
        {
            try
            {
                //Get the current logged in user
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                // Fetch the user by the current email
                UserInfo curretUserInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);

                // Check if the new email is not the same as the current one
                if (newEmail == curretUserInfo.Email)
                {
                    return BadRequest("The new email cannot be the same as the current email.");
                }

                //Check if the email is already in use
                if (FirebaseAuth.DefaultInstance.GetUserByEmailAsync(newEmail) != null)
                {
                    return BadRequest("The email is already in use.");
                }

                // Generate a unique verification token
                string verificationToken = Guid.NewGuid().ToString();

                // Build the verification link using configuration
                string verificationLink = $"{_configuration["UrlList:EmailVerificationUrl"]}{verificationToken}";

                // Send an email for verification to the new email
                await _emailService.SendVerificationEmailAsync(newEmail, curretUserInfo.DisplayName, verificationLink);

                //Change the email to the new one
                curretUserInfo.Email = newEmail;

                await _firestoreService.SaveUserPreUpdateAsync(curretUserInfo, verificationToken);

                // Return a success message with updated user details
                return Ok(new
                {
                    Message = $"A verification update email request has been sent to {curretUserInfo.Email}."
                });

            }
            catch (FirebaseAuthException ex)
            {
                return NotFound($"Current user does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating user: {ex.Message}");
            }
        }

        [HttpPut("update-display-name")]
        public async Task<IActionResult> UpdateDisplayName(string newDisplayName)
        {
            //string whatEmailNotFound = "NULL";
            try
            {

                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);

                //whatEmailNotFound = userRecord.Email;

                UserRecordArgs args = new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    DisplayName = newDisplayName ?? userRecord.DisplayName,
                };

                //Update the user in Firebase
                UserRecord updatedUser = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

                //Update the user in Firestore
                UserInfo updatedUserInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);

                updatedUserInfo.DisplayName = newDisplayName;

                //Update the user in Firestore
                await _firestoreService.SaveUserToDatabaseAsync(updatedUserInfo);

                return Ok(new
                {
                    Message = "User successfully updated.",
                    updatedUser.Uid,
                    updatedUser.Email,
                    updatedUser.DisplayName,
                    updatedUser.PhoneNumber,
                    updatedUser.PhotoUrl,
                    updatedUser.EmailVerified,
                    updatedUser.Disabled,
                });
            }
            catch (FirebaseAuthException ex)
            {
                return NotFound($"Current user does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating user: {ex.Message}");
            }
        }


        [HttpPut("update-user")]
        public async Task<IActionResult> UpdateUser(string? newEmail = null, string? newDisplayName = null, IFormFile? file = null)
        {
            if (string.IsNullOrEmpty(newEmail) && string.IsNullOrEmpty(newDisplayName) && file == null)
            {
                return BadRequest("No input to update");
            }

            try
            {
                // Get the account based on the current logged-in email
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);
                string tempPhotoUrl = userRecord.PhotoUrl;
                string tempDisplayName = newDisplayName ?? userRecord.DisplayName;

                // Check if the new email is not the same as the current one
                if (newEmail == userRecord.Email)
                {
                    return BadRequest("The new email cannot be the same as the current email.");
                }

                //Check if the email is already in use
                if (FirebaseAuth.DefaultInstance.GetUserByEmailAsync(newEmail) != null)
                {
                    return BadRequest("The email is already in use.");
                }


                if (file != null)
                {
                    using (var stream = file.OpenReadStream())
                    {
                        // Upload the file and update the photo URL in Firebase
                        var uploadResult = await _firebaseStorageService.UploadPfpAsync(file, currentUserUid);

                        if (uploadResult != null)
                        {
                            tempPhotoUrl = uploadResult.ToString();  // Update the user's photo URL
                        }
                        else
                        {
                            return BadRequest("Failed to upload profile picture.");
                        }
                    }
                }

                // Prepare user update argument for firebase auth, only updating if new data is provided
                UserRecordArgs args = new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    DisplayName = tempDisplayName,  // If displayName is null, keep the existing displayName
                    PhotoUrl = tempPhotoUrl,  // If file is null, keep the existing photoUrl
                };

                // Fetch the user by the current email
                UserInfo userInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);


                if (!string.IsNullOrEmpty(newEmail))
                {
                    // Generate a unique verification token
                    string verificationToken = Guid.NewGuid().ToString();

                    // Build the verification link using configuration
                    string verificationLink = $"{_configuration["UrlList:EmailVerificationUrl"]}{verificationToken}";

                    // Send an email for verification to the new email
                    await _emailService.SendVerificationEmailAsync(newEmail, userRecord.DisplayName, verificationLink);

                    // Change the email to the new one
                    userInfo.Email = newEmail ?? userInfo.Email;
                    userInfo.DisplayName = tempDisplayName;
                    userInfo.PhotoUrl = tempPhotoUrl;

                    await _firestoreService.SaveUserPreUpdateAsync(userInfo, verificationToken);

                    return Ok(new
                    {
                        Message = $"A verification update email request has been sent to {newEmail}.",
                    });
                }

                // Update the user in Firebase
                UserRecord updatedUser = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

                // Change the email to the new one
                userInfo.Email = userInfo.Email;
                userInfo.DisplayName = tempDisplayName;
                userInfo.PhotoUrl = tempPhotoUrl;

                await _firestoreService.SaveUserToDatabaseAsync(userInfo);

                return Ok(new
                {
                    Message = "User successfully updated.",
                    CollectionDb = new
                    {
                        userInfo.Uid,
                        userInfo.Email,
                        userInfo.DisplayName,
                        userInfo.PhoneNumber,
                        userInfo.PhotoUrl,
                        userInfo.Role,
                        userInfo.FirstName,
                        userInfo.LastName,
                        userInfo.DateRegistered,
                    },
                    Firebaseauth = new
                    {
                        updatedUser.Uid,
                        updatedUser.Email,
                        updatedUser.DisplayName,
                        updatedUser.PhoneNumber,
                        updatedUser.PhotoUrl,
                        updatedUser.Disabled,
                        updatedUser.EmailVerified,
                    }
                });

            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                return NotFound($"Current user does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating user: {ex.Message}");
            }
        }

        [HttpPost("update-pfp")]
        public async Task<IActionResult> UploadPfpAsync([FromBody] FileUploadRequest request)
        {
            try
            {
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);
                UserRecordArgs args = new UserRecordArgs
                {
                    Uid = userRecord.Uid,
                    PhotoUrl = request.File,
                };

                //Update the photoUrl in Firebase Firestore
                UserInfo updatedUserInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);
                updatedUserInfo.PhotoUrl = args.PhotoUrl;
                await _firestoreService.SaveUserToDatabaseAsync(updatedUserInfo);

                return Ok(new
                {
                    Message = "Profile Picture Updated",
                    Uploaded = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return BadRequest(new { Message = $"Error during file upload: {ex.Message}", Uploaded = false });
            }
        }

        //[HttpPost("upload-pfp")]
        //public async Task<IActionResult> UploadPfpAsync(IFormFile file)
        //{
        //    try
        //    {
        //        var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        //        UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);
        //        UserRecordArgs args = new UserRecordArgs
        //        {
        //            Uid = userRecord.Uid,
        //            PhotoUrl = userRecord.PhotoUrl,
        //        };

        //        using (var stream = file.OpenReadStream())
        //        {
        //            // Upload the file and update the photo URL in Firebase
        //            var uploadResult = await _firebaseStorageService.UploadPfpAsync(file, currentUserUid);

        //            if (uploadResult != null)
        //            {
        //                args.PhotoUrl = uploadResult.ToString();  // Update the user's photo URL
        //            }
        //            else
        //            {
        //                return BadRequest("Failed to upload profile picture.");
        //            }
        //        }

        //        // Update the user in FirebaseAuth
        //        UserRecord updatedUser = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);

        //        //Update the photoUrl in Firebase Firestore
        //        UserInfo updatedUserInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);
        //        updatedUserInfo.PhotoUrl = args.PhotoUrl;
        //        await _firestoreService.SaveUserToDatabaseAsync(updatedUserInfo);

        //        return Ok(new
        //        {
        //            Message = "User successfully updated.",
        //            updatedUser.Uid,
        //            updatedUser.Email,
        //            updatedUser.DisplayName,
        //            updatedUser.PhoneNumber,
        //            updatedUser.PhotoUrl,
        //            updatedUser.EmailVerified,
        //            updatedUser.Disabled
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return BadRequest(new { Message = $"Error during file upload {ex.Message}" });
        //    }
        //}

        [HttpDelete("upload-default-pfp")]
        public async Task<IActionResult> ResetDefaultPfp()
        {
            try
            {
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                // Reset Profile Picture in Firestore
                await _firestoreService.ResetDefaultPfp(currentUserUid);

                // Fetch the current user from Firebase Authentication (Admin SDK)
                UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);

                // Set the PhotoUrl to the default profile picture URL in Firebase Authentication
                string defaultProfilePictureUrl = _configuration["Default_ProfilePicture_Url"];

                // Update the user's photoUrl in Firebase Authentication
                var updateRequest = new UserRecordArgs
                {
                    Uid = currentUserUid,
                    PhotoUrl = defaultProfilePictureUrl
                };

                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateRequest);

                return Ok(new
                {
                    Message = "Profile picture reset to default.",
                    userRecord.Uid,
                    userRecord.Email,
                    userRecord.DisplayName,
                    userRecord.PhoneNumber,
                    userRecord.PhotoUrl,
                    userRecord.EmailVerified,
                    userRecord.Disabled
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = $"Error during file upload: {ex.Message}" });
            }
        }

        [HttpGet("get_current_user")]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                var currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                UserRecord currentUserAuth = await FirebaseAuth.DefaultInstance.GetUserAsync(currentUserUid);
                // Fetch the user by the current email
                UserInfo currentUserInfo = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);

                return Ok(new
                {
                    Message = "Current logged-in user account information:",
                    CollectionDb = new
                    {
                        currentUserInfo.Uid,
                        currentUserInfo.Email,
                        currentUserInfo.DisplayName,
                        currentUserInfo.PhoneNumber,
                        currentUserInfo.PhotoUrl,
                        currentUserInfo.Role,
                        currentUserInfo.FirstName,
                        currentUserInfo.LastName,
                        currentUserInfo.DateRegistered,
                    },
                    Firebaseauth = new
                    {
                        currentUserAuth.Uid,
                        currentUserAuth.Email,
                        currentUserAuth.DisplayName,
                        currentUserAuth.PhoneNumber,
                        currentUserAuth.PhotoUrl,
                        currentUserAuth.Disabled,
                        currentUserAuth.EmailVerified,
                    }
                });
            }
            catch (FirebaseAdmin.Auth.FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.UserNotFound)
            {
                return NotFound($"Current user does not exist.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error updating user: {ex.Message}");
            }

        }
    }
}