using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using server.Interfaces;
using server.Models;
using System.Security.Claims;

namespace server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class FlockController : Controller
    {
        private readonly IFirestoreService _firestoreService;


        public FlockController(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        [HttpPost("create-flock-async")]
        public async Task<IActionResult> CreateFlockAsync([FromBody] FlockModel request)
        {
            try
            {
                await _firestoreService.CreateNewFlock(request.userId, request.flockName);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok();
        }

        [HttpGet("get-rules/{flockId}")]
        public async Task<IActionResult> GetRules(string flockId)
        {
            try
            {
                string rules = await _firestoreService.GetFlockRules(flockId);

                return Ok(rules);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("update-rules-async")]
        public async Task UpdateRules([FromBody] FlockRulesModel model)
        {
            try
            {
                await _firestoreService.UpdateFlockRules(model.flockId, model.newRules);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("add-role-async")]
        public async Task AddRole([FromBody] FlockRoleModel model)
        {
            try
            {
                await _firestoreService.AddRoleToFlock(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("update-role-async")]
        public async Task UpdateRole([FromBody] FlockRoleModel model)
        {
            try
            {
                await _firestoreService.UpdateFlockRole(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpDelete("delete-role-async")]
        public async Task DeleteRole([FromBody] FlockRoleModel model)
        {
            try
            {
                await _firestoreService.DeleteFlockRole(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [HttpPost("send-invite-async")]
        public async Task<IActionResult> Invite(string otherUid, string flockId)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                List<object> invitesSentList = await _firestoreService.LoadSentInvites(flockId);
                List<FlockInviteReceivedModel> invitesReceivedList = await _firestoreService.LoadReceivedInvites(otherUid);
                List<string> userList = await _firestoreService.LoadFlockUsers(flockId);

                if (invitesSentList.Contains(otherUid))
                {
                    return Conflict("Already sent an invite.");
                }
                if (invitesReceivedList.Any(invite => invite.Uid == flockId))
                {
                    return Conflict("Already exists in invites");
                }
                if (userList.Contains(otherUid))
                {
                    return Conflict("Already a member");
                }
                if (otherUid.Equals(currentUserUid))
                {
                    return Conflict("Cannot invite self");
                }

                await _firestoreService.AddUserToSentListAsync(otherUid, flockId);
                await _firestoreService.AddFlockToReceivedList(otherUid, flockId);

                return Ok("Sent invite");
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

        [HttpPost("accept-invite-async")]
        public async Task<IActionResult> AcceptInvite(string otherUid, string flockId)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                List<FlockInviteReceivedModel> invitesReceivedList = await _firestoreService.LoadReceivedInvites(otherUid);

                if (!invitesReceivedList.Any(invite => invite.Uid == flockId))
                {
                    return Conflict("Flock not found in received invites");
                }

                await _firestoreService.DeleteFlockFromReceivedInvites(otherUid, flockId);
                await _firestoreService.DeleteUserFromSentList(otherUid, flockId);
                await _firestoreService.AddUserToFlock(otherUid, flockId);

                return Ok("Added to flock");
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

        [HttpDelete("remove-invite-async")]
        public async Task<IActionResult> CancelInvite(string otherUid, string flockId)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;

                List<object> invitesSentList = await _firestoreService.LoadSentInvites(flockId);

                if (!invitesSentList.Contains(otherUid))
                {
                    return Conflict("Did not send invite");
                }

                await _firestoreService.DeleteFlockFromReceivedInvites(otherUid, flockId);
                await _firestoreService.DeleteUserFromSentList(otherUid, flockId);

                return Ok("Cancelled invite");
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


        [HttpPost("create-new-channel-async")]
        public async Task<IActionResult> CreateNewChannel(string flockId, string name)
        {
            try
            {
                string currentUserUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
                await _firestoreService.CreateNewChannel(flockId, name, currentUserUid);
            }
            catch (UnauthorizedAccessException)
            {
                return StatusCode(404, "Error unauthorized");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            return Ok("Created channel");
        }
    }
}
