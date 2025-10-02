using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc;
using server.Interfaces;
using server.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using FireSharp.Extensions;

namespace server.Controllers
{
    public class UserWithDateTime
    {
        public UserInfo UserInfo { get; set; }
        public DateTime RequestDate { get; set; }
    }


    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FriendController : Controller
    {
        private readonly IFirestoreService _firestoreService;

        public FriendController(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }



        // Send friend request
        [HttpPost("friend-request")]
        public async Task<IActionResult> FriendRequest(FriendUid friendUid)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                Dictionary<string, object> friendSentReqList = await _firestoreService.LoadFriendsFromCollectionAsync(currentUserUid, "sent_friend_requests");
                Dictionary<string, object> friendReceivedReqList = await _firestoreService.LoadFriendsFromCollectionAsync(currentUserUid, "received_friend_requests");
                Dictionary<string, object> friendList = await _firestoreService.LoadFriendsFromCollectionAsync(currentUserUid, "friend_list");

                if (friendSentReqList.ContainsKey(friendUid.Uid))
                {
                    return Conflict("Already sent a friend request.");
                }
                if (friendReceivedReqList.ContainsKey(friendUid.Uid))
                {
                    return Conflict("Already exists in friend request");
                }
                if (friendList.ContainsKey(friendUid.Uid))
                {
                    return Conflict("Already a friend");
                }
                if (friendUid.Uid.Equals(currentUserUid))
                {
                    return Conflict("Cannot add self as friend");
                }


                var sentUser = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);
                var receivedUser = await _firestoreService.GetUserFromDbByUidAsync(friendUid.Uid);

                if (sentUser != null && receivedUser != null)
                {
                    await _firestoreService.AddFriendToCollectionAsync(receivedUser.Uid, sentUser.Uid, "received_friend_requests");
                    await _firestoreService.AddFriendToCollectionAsync(sentUser.Uid, receivedUser.Uid, "sent_friend_requests");

                    return Ok(true);
                }
                return BadRequest(false);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(404, "Error unauthorized");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("accept-friend-profile")]
        public async Task<IActionResult> AcceptFriendRequestProfile(FriendRequestProfileModel data)
        {
            Dictionary<string, object> friendReceivedList = await _firestoreService.LoadFriendsFromCollectionAsync(data.Uid, "received_friend_requests");

            //Check to see if the friend request from the profile the users visiting is in the logged in users friend requests
            if (!friendReceivedList.ContainsKey(data.FriendUid))
            {
                return Conflict("Friend not found in friend request");
            }

            var sentUser = await _firestoreService.GetUserFromDbByUidAsync(data.Uid);
            var receivedUser = await _firestoreService.GetUserFromDbByUidAsync(data.FriendUid);

            if (sentUser != null && receivedUser != null)
            {
                // add friend to current user
                await _firestoreService.DeleteFriendFromCollectionAsync(sentUser.Uid, receivedUser.Uid, "received_friend_requests");
                await _firestoreService.AddFriendToCollectionAsync(sentUser.Uid, receivedUser.Uid, "friend_list");

                // add current user on friend
                await _firestoreService.DeleteFriendFromCollectionAsync(receivedUser.Uid, sentUser.Uid, "sent_friend_requests");
                await _firestoreService.AddFriendToCollectionAsync(receivedUser.Uid, sentUser.Uid, "friend_list");
                return Ok(new { sentUser.Uid, sentUser.DisplayName, Photo = sentUser.PhotoUrl });
            }

            return BadRequest(new { Message = "Something went wrong when adding a friend from their profile" });
        }


        [HttpPost("accept-friend-request")]
        public async Task<IActionResult> AcceptFriendRequest(FriendUid friendUid)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                Dictionary<string, object> friendReceivedList = await _firestoreService.LoadFriendsFromCollectionAsync(currentUserUid, "received_friend_requests");

                if (!friendReceivedList.ContainsKey(friendUid.Uid))
                {
                    Console.WriteLine("Does not contain uid");
                    return Conflict(new { Message = "Friend not found in friend request" });
                }

                var sentUser = await _firestoreService.GetUserFromDbByUidAsync(currentUserUid);
                var receivedUser = await _firestoreService.GetUserFromDbByUidAsync(friendUid.Uid);

                if (sentUser != null && receivedUser != null)
                {
                    // add friend to current user
                    await _firestoreService.DeleteFriendFromCollectionAsync(sentUser.Uid, receivedUser.Uid, "received_friend_requests");
                    await _firestoreService.AddFriendToCollectionAsync(sentUser.Uid, receivedUser.Uid, "friend_list");

                    // add current user on friend
                    await _firestoreService.DeleteFriendFromCollectionAsync(receivedUser.Uid, sentUser.Uid, "sent_friend_requests");
                    await _firestoreService.AddFriendToCollectionAsync(receivedUser.Uid, sentUser.Uid, "friend_list");

                    await _firestoreService.CreateDmDocument(receivedUser.Uid, sentUser.Uid);
                    return Ok(true);
                }

                return BadRequest(false);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(404, "Error unauthorized");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("remove-friend-requests")]
        public async Task<IActionResult> RemoveFriendRequest(FriendUid friendUid)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                Dictionary<string, object> friendRequestList = await _firestoreService.LoadFriendsFromCollectionAsync(currentUserUid, "sent_friend_requests");

                if (!friendRequestList.ContainsKey(friendUid.Uid))
                {
                    return Conflict(new { Message = "Did not sent friend request" });
                }

                await _firestoreService.DeleteFriendFromCollectionAsync(currentUserUid, friendUid.Uid, "sent_friend_requests");

                await _firestoreService.DeleteFriendFromCollectionAsync(friendUid.Uid, currentUserUid, "received_friend_requests");

                return Ok(new { Message = "Cancelled friend request" });
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(404, "Error unauthorized");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpDelete("remove-friend")]
        public async Task<IActionResult> RemoveFriend(FriendRequestProfileModel data)
        {
            try
            {
                Dictionary<string, object> friendRequestList = await _firestoreService.LoadFriendsFromCollectionAsync(data.Uid, "friend_list");

                if (!friendRequestList.ContainsKey(data.FriendUid))
                {
                    Console.WriteLine("Did not delete friend");
                    return Conflict(new { Message = "Did not delete friend" });
                }

                await _firestoreService.DeleteFriendFromCollectionAsync(data.Uid, data.FriendUid, "friend_list");
                await _firestoreService.DeleteFriendFromCollectionAsync(data.FriendUid, data.Uid, "friend_list");

                Console.WriteLine("Deleting friend");
                return Ok(true);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(404, "Error unauthorized");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-all-friend-requests")]
        public async Task<IActionResult> GetAllFriendRequests()
        {
            return await GetFriendActivityByCollection("sent_friend_requests");
        }

        [HttpGet("get-all-friend-received")]
        public async Task<IActionResult> GetAllFriendReceived()
        {
            return await GetFriendActivityByCollection("received_friend_requests");
        }

        [HttpGet("get-all-friend")]
        public async Task<IActionResult> GetAllFriend()
        {
            return await GetFriendActivityByCollection("friend_list");
        }

        [NonAction]
        public async Task<IActionResult> GetFriendActivityByCollection(string collectionName)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                if (currentUserUid == null)
                {
                    return NotFound("Current user not found");
                }

                List<UserWithDateTime> userList = await _firestoreService.GetFriendActivityByCollection(currentUserUid, collectionName);

                // Extract photoUrl and displayName from the userList
                var extractedData = userList.Select(user => new
                {
                    user.UserInfo.Uid,
                    user.UserInfo.DisplayName,
                    user.UserInfo.PhotoUrl
                }).ToList();

                return Ok(extractedData);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(401, "Error: Unauthorized access");
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}
