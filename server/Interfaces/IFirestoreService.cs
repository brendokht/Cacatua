using FirebaseAdmin.Auth;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using server.Models;
using Google.Cloud.Firestore;
using Microsoft.AspNetCore.Mvc;
using server.Controllers;

namespace server.Interfaces
{
    public interface IFirestoreService
    {
        Task<string> GetUserFieldAsync(string userId, string fieldName);
        Task<bool> CheckEmailExists(string email);
        Task SaveUserPreVerificationAsync(RegisterRequestModel request, string verificationToken);
        Task SaveUserPreUpdateAsync(UserInfo user, string verificationToken);
        Task<VerificationResultModel> GetVerificationDataAsync(string token);
        Task DeleteVerificationDataAsync(string token);
        Task SaveUserToDatabaseAsync(UserInfo user);
        Task<UserInfo> GetUserFromDbByUidAsync(string uid);
        Task DeleteAccountDb(string userId);
        Task CreateAccountDb(string token, VerificationResultModel request = null);
        Task AddFriendToCollectionAsync(string userId, string friendId, string collectionName);
        Task DeleteFriendFromCollectionAsync(string userId, string friendId, string collectionName);
        Task<Dictionary<string, object>> LoadFriendsFromCollectionAsync(string userId, string collectionName);
        Task SendMessage(MessageModel model);
        Task CreateNewFlock(string userId, string flockName);
        Task<string> GetFlockRules(string flockId);
        Task UpdateFlockRules(string flockId, string newRules);
        Task AddRoleToFlock(FlockRoleModel model);
        Task UpdateFlockRole(FlockRoleModel model);
        Task DeleteFlockRole(FlockRoleModel model);
        Task SaveRefreshTokenToFirestore(string userId, string refreshToken);
        Task<Dictionary<string, object?>> ValidateRefreshToken(string refreshToken);
        Task RotateRefreshToken(string userId, string oldRefreshToken, string newRefreshToken);
        Task RemoveRefreshToken(string userId);
        Task ResetDefaultPfp(string userId);
        Task<List<UserWithDateTime>> GetFriendActivityByCollection(string userId, string collectionName);
        Task<List<object>> LoadSentInvites(string flockId);
        Task<List<FlockInviteReceivedModel>> LoadReceivedInvites(string otherUid);
        Task<List<string>> LoadFlockUsers(string flockId);
        Task AddUserToSentListAsync(string otherUid, string flockId);
        Task AddFlockToReceivedList(string otherUid, string flockId);
        Task DeleteFlockFromReceivedInvites(string otherUid, string flockId);
        Task DeleteUserFromSentList(string otherUid, string flockId);
        Task AddUserToFlock(string otherUid, string flockId);
        Task CreateDmDocument(string user1, string user2);
        Task CreateNewChannel(string flockId, string name, string userId);
    }
}
