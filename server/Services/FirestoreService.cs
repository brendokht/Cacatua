using Firebase.Auth;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FireSharp.Extensions;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using Org.BouncyCastle.Asn1.Ocsp;
using server.Controllers;
using server.Interfaces;
using server.Models;
using System.Diagnostics;
using System.Text.Json.Serialization;
using static Google.Rpc.Context.AttributeContext.Types;
using UserInfo = server.Models.UserInfo;

namespace server.Services
{
    public class FirestoreService : IFirestoreService
    {
        private readonly FirestoreDb _db;
        private readonly CollectionReference _userCollection;
        private readonly CollectionReference _dmsCollection;
        private readonly CollectionReference _flocksCollection;
        private readonly IConfiguration _configuration;

        public FirestoreService(IConfiguration configuration, FirebaseApp app)
        {
            _configuration = configuration;
            var credentials = app.Options.Credential;
            var channelCreds = credentials.ToChannelCredentials();
            var builder = new FirestoreClientBuilder()
            {
                ChannelCredentials = channelCreds
            };
            var firestoreClient = builder.Build();
            _db = FirestoreDb.Create(configuration["FirebaseConfig:ProjectId"], firestoreClient);
            _userCollection = _db.Collection("users");
            _flocksCollection = _db.Collection("flocks");
            _dmsCollection = _db.Collection("dms");
        }

        public async Task<bool> CheckEmailExists(string email)
        {
            var usersCollection = _db.Collection("users");
            Query query = usersCollection.WhereEqualTo("email", email);
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
            if (querySnapshot.Documents.Count == 0)
            {
                return false;
            }

            return true;
        }

        private async Task ClearVerificationDoc(string email, CollectionReference preVerifCollection)
        {
            Query query = preVerifCollection.WhereEqualTo("email", email);
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            //Delete pre-existing verification documents as they are meant to
            //be deleted by the time the account becomes verified
            if (querySnapshot.Documents.Count > 0)
            {
                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    await documentSnapshot.Reference.DeleteAsync();
                }
            }
        }

        public async Task ResetDefaultPfp(string userId)
        {
            try
            {
                // Get the default profile picture URL from Firebase Storage configuration
                string defaultProfilePictureUrl = _configuration["Default_ProfilePicture_Url"];

                // Update the user's profile picture URL in Firestore
                var userDoc = _db.Collection("users").Document(userId);
                await userDoc.UpdateAsync(new Dictionary<string, object>
            {
                { "photoUrl", defaultProfilePictureUrl }
            });
                Console.WriteLine($"Profile picture URL reset to default in Firestore for user {userId}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error resetting profile picture for user {userId}: {ex.Message}");
            }
        }

        public async Task<List<UserWithDateTime>> GetFriendActivityByCollection(string userId, string collectionName)
        {
            Dictionary<string, object> friendCollectionList = await LoadFriendsFromCollectionAsync(userId, collectionName);

            List<UserWithDateTime> userList = new List<UserWithDateTime>();

            foreach (var friendRequest in friendCollectionList)
            {
                string friendUid = friendRequest.Key;

                UserInfo userInfo = await GetUserFromDbByUidAsync(friendUid);

                DateTime requestDate = DateTime.UtcNow;
                if (friendRequest.Value is Dictionary<string, object> friendData &&
                    friendData.TryGetValue("date", out object dateObj) &&
                    dateObj is DateTime dateTime)
                {
                    requestDate = dateTime;
                }

                userList.Add(new UserWithDateTime
                {
                    UserInfo = userInfo,
                    RequestDate = requestDate
                });
            }

            return userList;
        }

        public async Task SaveUserPreVerificationAsync(RegisterRequestModel request, string verificationToken)
        {
            var preVerifCollection = _db.Collection("pre_verification");
            var userDoc = preVerifCollection.Document(verificationToken);

            await ClearVerificationDoc(request.Email, preVerifCollection);

            //For registering a new account, use the RegisterRequestModel model and save it in pre_verification
            //document
            var userData = new Dictionary<string, object>
        {
            { "email", request.Email },
            { "password", request.Password },
            { "displayName", request.DisplayName },
            { "firstName", request.FirstName },
            { "lastName", request.LastName },
            { "validDate", DateTime.UtcNow.AddMinutes(10) },
            { "token",  verificationToken },
        };

            await userDoc.SetAsync(userData);
        }

        public async Task SaveUserPreUpdateAsync(UserInfo user, string verificationToken)
        {
            var preVerifCollection = _db.Collection("pre_verification");
            var userDoc = preVerifCollection.Document(verificationToken);

            await ClearVerificationDoc(user.Email, preVerifCollection);

            //For updating an account, use the UserInfo model and save it in pre_verification
            //document
            var userData = new Dictionary<string, object>
        {
            { "uid", user.Uid },
            { "email", user.Email },
            { "displayName", user.DisplayName },
            { "firstName", user.FirstName },
           // { "middleName", user.MiddleName },
            { "lastName", user.LastName },
            { "phoneNumber", user.PhoneNumber },
            { "photoUrl", user.PhotoUrl },
            { "dateRegistered", user.DateRegistered },
            { "validDate", DateTime.UtcNow.AddMinutes(10) },
            { "role", user.Role },
        };

            await userDoc.SetAsync(userData);
        }

        public async Task<VerificationResultModel> GetVerificationDataAsync(string token)
        {
            var preVerifCollection = _db.Collection("pre_verification");
            var userDoc = await preVerifCollection.Document(token).GetSnapshotAsync();

            if (!userDoc.Exists)
            {
                return new VerificationResultModel
                {
                    IsTokenValid = false,
                    IsTokenExpired = false,
                    UserData = null
                };
            }

            DateTime validDate = userDoc.GetValue<DateTime>("validDate");
            if (validDate < DateTime.UtcNow)
            {
                return new VerificationResultModel
                {
                    IsTokenValid = true,
                    IsTokenExpired = true,
                    UserData = null
                };
            }

            //This is for creating a new account
            if (userDoc.ContainsField("token"))
            {
                RegisterRequestModel request = new RegisterRequestModel
                {
                    DisplayName = userDoc.GetValue<string>("displayName"),
                    Email = userDoc.GetValue<string>("email"),
                    FirstName = userDoc.GetValue<string>("firstName"),
                    LastName = userDoc.GetValue<string>("lastName"),
                    Password = userDoc.GetValue<string>("password"),
                };

                return new VerificationResultModel
                {
                    IsTokenValid = true,
                    IsTokenExpired = false,
                    UserData = request,
                };
            }
            else //This is for updating the account
            {
                UserInfo request = new UserInfo
                {
                    DateRegistered = userDoc.GetValue<DateTime>("dateRegistered"),
                    DisplayName = userDoc.GetValue<string>("displayName"),
                    Email = userDoc.GetValue<string>("email"),
                    FirstName = userDoc.GetValue<string>("firstName"),
                    LastName = userDoc.GetValue<string>("lastName"),
                    //  MiddleName = userDoc.GetValue<string>("middleName"),
                    PhoneNumber = userDoc.GetValue<string>("phoneNumber"),
                    PhotoUrl = userDoc.GetValue<string>("photoUrl"),
                    Uid = userDoc.GetValue<string>("uid"),
                    Role = userDoc.GetValue<string>("role"),
                };

                return new VerificationResultModel
                {
                    IsTokenValid = true,
                    IsTokenExpired = false,
                    UserData = request,
                };
            }
        }

        public async Task DeleteVerificationDataAsync(string token)
        {
            var preVerifCollection = _db.Collection("pre_verification");
            var userDoc = preVerifCollection.Document(token);
            await userDoc.DeleteAsync();
        }

        public async Task SaveUserToDatabaseAsync(UserInfo user)
        {
            DocumentReference docRef = _db.Collection("users").Document(user.Uid);
            var userDict = new Dictionary<string, object>
        {
            { "uid", user.Uid },
            { "displayName", user.DisplayName },
            { "firstName", user.FirstName },
            //{ "middleName", user.MiddleName },
            { "lastName", user.LastName },
            { "email", user.Email },
            { "phoneNumber", user.PhoneNumber },
            { "photoUrl", user.PhotoUrl },
            { "dateRegistered", user.DateRegistered },
            { "role", user.Role },
        };
            await docRef.SetAsync(userDict);
        }

        public async Task<UserInfo> GetUserFromDbByEmailAsync(string email)
        {
            // Retrieve the Firebase Auth user record using the email
            var userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);

            // Get the Firestore document corresponding to the Firebase Auth user's Uid
            var usersCollection = _db.Collection("users").Document(userRecord.Uid);
            var userDoc = await usersCollection.GetSnapshotAsync();

            // Check if the document exists in Firestore
            if (userDoc.Exists)
            {
                // Map Firestore document data to the UserInfo object
                var userInfo = new UserInfo
                {
                    Uid = userDoc.Id, // Firestore document ID, which should be the Uid
                    DisplayName = userDoc.GetValue<string>("displayName"),
                    FirstName = userDoc.GetValue<string>("firstName"),
                    //   MiddleName = userDoc.GetValue<string>("middleName"),
                    LastName = userDoc.GetValue<string>("lastName"),
                    Email = userDoc.GetValue<string>("email"),
                    PhoneNumber = userDoc.GetValue<string>("phoneNumber"),
                    PhotoUrl = userDoc.GetValue<string>("photoUrl"),
                    DateRegistered = userDoc.GetValue<DateTime>("dateRegistered"),
                };

                return userInfo;
            }
            else
            {
                // Handle the case where the document does not exist
                Console.WriteLine($"No document found for user with UID: {userRecord.Uid}");
                return null;
            }
        }

        public async Task<string> GetUserFieldAsync(string userId, string fieldName)
        {
            try
            {
                // Reference the "users" collection
                DocumentReference docRef = _db.Collection("users").Document(userId);

                // Get the document snapshot
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    // Retrieve a specific field from the document
                    if (snapshot.TryGetValue(fieldName, out string fieldValue))
                    {
                        return fieldValue;
                    }
                    else
                    {
                        return $"Field '{fieldName}' not found in user document.";
                    }
                }
                else
                {
                    return $"User with ID '{userId}' does not exist.";
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                return $"Error retrieving user field: {ex.Message}";
            }
        }

        public async Task<UserInfo> GetUserFromDbByUidAsync(string uid)
        {
            // Retrieve the Firebase Auth user record using the email
            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);

            // Get the Firestore document corresponding to the Firebase Auth user's Uid
            var usersCollection = _db.Collection("users").Document(userRecord.Uid);
            var userDoc = await usersCollection.GetSnapshotAsync();

            // Check if the document exists in Firestore
            if (userDoc.Exists)
            {
                // Map Firestore document data to the UserInfo object
                var userInfo = new UserInfo
                {
                    Uid = userDoc.Id,  // Firestore document ID, which should be the Uid
                    DisplayName = userDoc.GetValue<string>("displayName"),
                    FirstName = userDoc.GetValue<string>("firstName"),
                    // MiddleName = userDoc.GetValue<string>("middleName"),
                    LastName = userDoc.GetValue<string>("lastName"),
                    Email = userDoc.GetValue<string>("email"),
                    PhoneNumber = userDoc.GetValue<string>("phoneNumber"),
                    PhotoUrl = userDoc.GetValue<string>("photoUrl"),
                    DateRegistered = userDoc.GetValue<DateTime>("dateRegistered"),
                    Role = userDoc.GetValue<string>("role"),
                };



                return userInfo;
            }
            else
            {
                // Handle the case where the document does not exist
                Console.WriteLine($"No document found for user with UID: {userRecord.Uid}");
                return null;
            }
        }

        public async Task DeleteAccountDb(string userId)
        {
            try
            {
                // Reference the document with the specified UID
                var userDocument = _db.Collection("users").Document(userId);

                // Get the document snapshot to check if it exists
                DocumentSnapshot snapshot = await userDocument.GetSnapshotAsync();

                if (snapshot.Exists)
                {
                    // Delete the document if it exists
                    await userDocument.DeleteAsync();
                    Console.WriteLine($"Document with UID '{userId}' deleted.");
                }
                else
                {
                    Console.WriteLine($"No document found with UID '{userId}'.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document with UID '{userId}': {ex.Message}");
            }
        }

        //Create or update the user's account in Firebase Auth and Firestore via Email verification
        public async Task CreateAccountDb(string token, VerificationResultModel request)
        {
            UserRecord userRecord;
            UserInfo userInfo;

            // Check if the user does not exist in Firebase Auth
            // If the VerificationResult.UserData has a non-empty ID attribute, 
            // it means that the user already exists, and we need to update data
            if (string.IsNullOrEmpty(request.UserData.Uid)) //USER DOES NOT EXIST
            {
                // If the user does not exist, create a new user in Firebase Auth
                UserRecordArgs userRecordArgs = new UserRecordArgs()
                {
                    Email = request.UserData.Email,
                    EmailVerified = true,
                    DisplayName = request.UserData.DisplayName,
                    PhotoUrl = _configuration["Default_ProfilePicture_Url"],
                    Disabled = false,
                    Password = request.UserData.Password,
                };

                // Create the user and get the UserRecord which contains the generated Uid
                userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(userRecordArgs);

                // DEBUG: Retrieve the generated Uid
                string newUserId = userRecord.Uid;

                // Create a UserInfo object from the updated user data
                //(PhneNumber and PhotoUrl will be updated in their separate methods)
                userInfo = new UserInfo()
                {
                    DateRegistered = DateTime.UtcNow,
                    Uid = userRecord.Uid, // Use the Uid from FirebaseAuth
                    DisplayName = request.UserData.DisplayName,
                    FirstName = request.UserData.FirstName,
                    LastName = request.UserData.LastName,
                    Email = request.UserData.Email,
                    PhoneNumber = userRecord.PhoneNumber,
                    PhotoUrl = userRecord.PhotoUrl,
                    Role = RoleModel.SubscriptionRole.Free.ToString(),
                };
            }
            else //USER EXISTS
            {
                // If user exists, fetch their record from Firebase Auth
                userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(request.UserData.Uid);

                // Update the user's profile
                UserRecordArgs updateArgs = new UserRecordArgs()
                {
                    Uid = userRecord.Uid,
                    Email = request.UserData.Email,
                    EmailVerified = true,
                    DisplayName = request.UserData.DisplayName,
                    PhotoUrl = request.UserData.PhotoUrl,
                };

                // Update the existing user
                await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);

                //// Create a UserInfo object from the updated user data
                //PhneNumber and PhotoUrl are updated in a separate method
                userInfo = new UserInfo()
                {
                    DateRegistered = request.UserData.DateRegistered,
                    Uid = userRecord.Uid, // Use the Uid from FirebaseAuth
                    DisplayName = request.UserData.DisplayName,
                    FirstName = request.UserData.FirstName,
                    LastName = request.UserData.LastName,
                    // MiddleName = request.UserData.MiddleName,
                    Email = request.UserData.Email,
                    PhoneNumber = userRecord.PhoneNumber,
                    PhotoUrl = userRecord.PhotoUrl,
                    Role = request.UserData.Role.ToString(),
                };
            }

            // Delete verification data after processing
            await DeleteVerificationDataAsync(token);


            // Save the user data to Firestore or update if already exists
            await SaveUserToDatabaseAsync(userInfo);
        }

        public async Task SendMessage(MessageModel model)
        {
            // Cached collection references
            var createdBy = _userCollection.Document(model.Uid);
            var createdAt = DateTime.UtcNow;

            // Prepare message data
            var messageData = new Dictionary<string, object>
            {
                { "text", model.Text },
                { "createdAt", createdAt },
                { "createdBy", createdBy },
            };

            // Determine target collection
            var messages = model.ServerUid == null
                ? _dmsCollection.Document(model.ChatUid).Collection("messages")
                : _flocksCollection.Document(model.ServerUid).Collection(model.ChatUid);

            // Send message
            await messages.AddAsync(messageData);
        }

        public async Task AddFriendToCollectionAsync(string userId, string friendId, string collectionName)
        {
            var collectionRef = _db.Collection("users").Document(userId).Collection(collectionName);
            DocumentReference docRef = collectionRef.Document(friendId);

            var friendData = new Dictionary<string, object>
            {
                { "date", DateTime.UtcNow }
            };
            await docRef.SetAsync(friendData);
        }

        public async Task DeleteFriendFromCollectionAsync(string userId, string friendId, string collectionName)
        {
            // Reference the specific friend document in the specified collection
            var collectionRef = _db.Collection("users").Document(userId).Collection(collectionName);
            DocumentReference docRef = collectionRef.Document(friendId);

            // Delete the document
            await docRef.DeleteAsync();
        }

        public async Task<Dictionary<string, object>> LoadFriendsFromCollectionAsync(string userId,
            string collectionName)
        {
            CollectionReference collectionRef = _db.Collection("users").Document(userId).Collection(collectionName);
            var querySnapshot = await collectionRef.GetSnapshotAsync();

            Dictionary<string, object> friendsList = new Dictionary<string, object>();

            foreach (var document in querySnapshot.Documents)
            {
                // Using the document ID as friendId
                string friendId = document.Id;

                // Creating a dictionary of field data for each friend document
                var friendData = document.ToDictionary();

                // Add friendId as key and friend data dictionary as value
                friendsList.Add(friendId, friendData);
            }

            return friendsList;
        }

        public async Task CreateNewFlock(string userId, string flockName)
        {
            var flockCollectionRef = _db.Collection("flocks");

            DocumentReference newFlock = flockCollectionRef.Document();
            DocumentReference user = _db.Collection("users").Document(userId);

            var flockData = new Dictionary<string, object>
            {
                { "name", flockName },
                { "channels", new List<string> { "general" } },
                { "roles", new List<object>() },
                { "users", new List<DocumentReference> { user } },
                { "owner", user },
                { "rules", "No rules set yet, but please be respectful!" }
            };

            await newFlock.SetAsync(flockData);

            CollectionReference generalCollection = newFlock.Collection("general");

            await generalCollection.Document().SetAsync(new Dictionary<string, object>
            {
                { "text", "This is a message!" },
                { "createdAt", DateTime.UtcNow },
                { "createdBy", user }
            });
        }

        public async Task<string> GetFlockRules(string flockId)
        {
            try
            {
                var flockRef = _db.Collection("flocks").Document(flockId);
                var snapshot = await flockRef.GetSnapshotAsync();

                if (snapshot != null && snapshot.Exists)
                {
                    var flockData = snapshot.ToDictionary();

                    if (flockData.TryGetValue("rules", out var rulesObj))
                    {
                        return rulesObj?.ToString() ?? string.Empty;
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return "No rules set yet, but please be respectful!";
        }

        public async Task UpdateFlockRules(string flockId, string newRules)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            await flockRef.UpdateAsync("rules", newRules);
        }

        public async Task SaveRefreshTokenToFirestore(string userId, string refreshToken)
        {
            var refreshTokenCollectionRef = _db.Collection("refresh_tokens");

            var refreshTokenData = new Dictionary<string, object>
            {
                { "user_uid", userId },
                { "refresh_token", refreshToken },
                { "created_at", DateTime.UtcNow },
                { "expires_at", DateTime.UtcNow.AddDays(30) },
                { "is_revoked", false },
            };

            await refreshTokenCollectionRef.AddAsync(refreshTokenData);
        }

        public async Task<Dictionary<string, object?>> ValidateRefreshToken(string refreshToken)
        {
            try
            {
                var query = _db.Collection("refresh_tokens").WhereEqualTo("refresh_token", refreshToken).Limit(1);
                var querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count == 0)
                {
                    return null;
                }

                var tokenDoc = querySnapshot.Documents.First();
                var tokenData = tokenDoc.ToDictionary();

                if (tokenData.ContainsKey("expires_at") && tokenData["expires_at"] is Timestamp expiryTimestamp)
                {
                    if (expiryTimestamp.ToDateTime() < DateTime.UtcNow)
                    {
                        return null; // Token is expired
                    }
                }

                if (tokenData.ContainsKey("is_revoked") && (bool)tokenData["is_revoked"])
                {
                    return null; // Token is revoked
                }

                return tokenData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error validating refresh token: {ex.Message}");
                throw new Exception("Failed to validate refresh token");
            }
        }

        public async Task RotateRefreshToken(string userId, string oldRefreshToken, string newRefreshToken)
        {
            try
            {
                var refreshTokenCollectionRef = _db.Collection("refresh_tokens");
                var query = refreshTokenCollectionRef.WhereEqualTo("refresh_token", oldRefreshToken).Limit(1);
                var querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count == 0)
                {
                    throw new Exception("Old refresh token not found.");
                }

                var batch = _db.StartBatch();

                var oldTokenDoc = querySnapshot.Documents.First().Reference;

                batch.Delete(oldTokenDoc);

                var tokenData = new Dictionary<string, object>
                {
                    { "user_uid", userId },
                    { "refresh_token", newRefreshToken },
                    { "expires_at", DateTime.UtcNow.AddDays(30) },
                    { "created_at", DateTime.UtcNow },
                    { "is_revoked", false }
                };

                batch.Create(refreshTokenCollectionRef.Document(), tokenData);

                await batch.CommitAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error rotating refresh token: {ex.Message}");
                throw new Exception("Failed to rotate refresh token");
            }
        }

        public async Task RemoveRefreshToken(string userId)
        {
            var query = _db.Collection("refresh_tokens").WhereEqualTo("user_uid", userId).Limit(1);
            var querySnapshot = await query.GetSnapshotAsync();
            await querySnapshot.Documents.First().Reference.DeleteAsync();
        }

        public async Task AddRoleToFlock(FlockRoleModel model)
        {
            var flockRef = _db.Collection("flocks").Document(model.flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("roles", out var rolesObj) || rolesObj is not List<object> rolesList)
            {
                throw new Exception("Roles array not found.");
            }

            var roles = rolesList.Cast<Dictionary<string, object>>().ToList(); // Convert to a usable structure

            List<DocumentReference> userRefs = new List<DocumentReference>();

            if (model.userIds != null)
            {
                foreach (var userId in model.userIds)
                {
                    userRefs.Add(_db.Collection("users").Document(userId));
                }
            }

            var roletoAdd = new Dictionary<string, object>
            {
                { "id", Guid.NewGuid().ToString() },
                { "role", model.roleName! },
                { "users", userRefs }
            };

            roles.Add(roletoAdd);

            await flockRef.UpdateAsync("roles", roles);
        }

        public async Task UpdateFlockRole(FlockRoleModel model)
        {
            var flockRef = _db.Collection("flocks").Document(model.flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("roles", out var rolesObj) || rolesObj is not List<object> rolesList)
            {
                throw new Exception("Roles array not found.");
            }

            var roles = rolesList.Cast<Dictionary<string, object>>().ToList(); // Convert to a usable structure
            var roleToUpdate = roles.FirstOrDefault(role => role["id"].ToString() == model.roleId);
            if (roleToUpdate == null)
            {
                throw new Exception("Role not found.");
            }

            List<DocumentReference> userRefs = new List<DocumentReference>();

            if (model.userIds != null)
            {
                foreach (var userId in model.userIds)
                {
                    userRefs.Add(_db.Collection("users").Document(userId));
                }
                roleToUpdate["users"] = userRefs;
            }

            if (model.roleName != null)
                roleToUpdate["role"] = model.roleName;


            await flockRef.UpdateAsync("roles", roles);
        }

        public async Task DeleteFlockRole(FlockRoleModel model)
        {
            var flockRef = _db.Collection("flocks").Document(model.flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("roles", out var rolesObj) || rolesObj is not List<object> rolesList)
            {
                throw new Exception("Roles array not found.");
            }

            var roles = rolesList.Cast<Dictionary<string, object>>().ToList(); // Convert to a usable structure
            var roleToDelete = roles.FirstOrDefault(role => role["id"].ToString() == model.roleId);

            if (roleToDelete == null)
            {
                throw new Exception("Role not found.");
            }

            roles.Remove(roleToDelete);

            await flockRef.UpdateAsync("roles", roles);
        }

        public async Task<List<object>> LoadSentInvites(string flockId)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("sentInvites", out var sentInvitesObj) || sentInvitesObj is not List<object> sentInvitesList)
            {
                sentInvitesList = new List<object>();
                flockData["sentInvites"] = sentInvitesList;
                await flockRef.UpdateAsync(flockData);
            }

            return sentInvitesList;
        }

        public async Task<List<FlockInviteReceivedModel>> LoadReceivedInvites(string otherUid)
        {
            var userInvitesRef = _db.Collection("users").Document(otherUid).Collection("invites-received");

            var querySnapshot = await userInvitesRef.GetSnapshotAsync();
            if (querySnapshot == null || querySnapshot.Count == 0)
            {
                return new List<FlockInviteReceivedModel>();
            }

            var invitesData = new List<FlockInviteReceivedModel>();

            foreach (var document in querySnapshot.Documents)
            {
                var flockRef = _db.Collection("flocks").Document(document.Id);
                var flockQuerySnapshot = await flockRef.GetSnapshotAsync();
                var flockData = flockQuerySnapshot.ToDictionary();

                invitesData.Add(new FlockInviteReceivedModel
                {
                    Uid = document.Id,
                    Name = flockData["name"].ToString()!
                });
            }

            return invitesData;
        }

        public async Task<List<string>> LoadFlockUsers(string flockId)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("users", out var usersObj) || usersObj is not List<object> usersList)
            {
                throw new Exception("Users array not found.");
            }

            List<string> userUids = new();

            foreach (var user in usersList)
            {
                if (user is DocumentReference userRef)
                {
                    userUids.Add(userRef.Id);
                }
            }

            return userUids;
        }

        public async Task AddUserToSentListAsync(string otherUid, string flockId)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("sentInvites", out var sentInvitesObj) || sentInvitesObj is not List<object> sentInvitesList)
            {
                sentInvitesList = new List<object>();
                flockData["sentInvites"] = sentInvitesList;
            }

            sentInvitesList.Add(otherUid);

            await flockRef.UpdateAsync("sentInvites", sentInvitesList);
        }

        public async Task AddFlockToReceivedList(string otherUid, string flockId)
        {
            var userInvitesRef = _db.Collection("users").Document(otherUid).Collection("invites-received");

            var flockDocRef = userInvitesRef.Document(flockId);

            await flockDocRef.SetAsync(new Dictionary<string, object>());
        }

        public async Task DeleteFlockFromReceivedInvites(string otherUid, string flockId)
        {
            var userInvitesRef = _db.Collection("users").Document(otherUid).Collection("invites-received");

            var flockDocRef = userInvitesRef.Document(flockId);

            await flockDocRef.DeleteAsync();
        }

        public async Task DeleteUserFromSentList(string otherUid, string flockId)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("sentInvites", out var sentInvitesObj) || sentInvitesObj is not List<object> sentInvitesList)
            {
                throw new Exception("sentInvites array not found.");
            }

            sentInvitesList.Remove(otherUid);

            await flockRef.UpdateAsync("sentInvites", sentInvitesList);
        }

        public async Task AddUserToFlock(string otherUid, string flockId)
        {
            var flockRef = _db.Collection("flocks").Document(flockId);

            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("users", out var usersObj) || usersObj is not List<object> usersList)
            {
                throw new Exception("Users array not found.");
            }

            var userRef = _db.Collection("users").Document(otherUid);

            snapshot = await userRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("User not found.");
            }

            usersList.Add(userRef);

            await flockRef.UpdateAsync("users", usersList);
        }

        public async Task CreateDmDocument(string user1, string user2)
        {
            var user1Ref = _userCollection.Document(user1);
            var user2Ref = _userCollection.Document(user2);

            // Check if a DM document already exists with both users
            var query = _dmsCollection.WhereArrayContains("users", user1Ref);
            var querySnapshot = await query.GetSnapshotAsync();

            var dmExists = querySnapshot.Documents
                .Any(doc =>
                {
                    var users = doc.GetValue<IList<DocumentReference>>("users");
                    return users.Contains(user2Ref); // Ensure both users exist in the array
                });

            if (dmExists)
            {
                Console.WriteLine("DM document already exists for these users.");
                return; // Abort the method
            }

            var newDmDoc = _dmsCollection.Document();

            var dmData = new Dictionary<string, object>
            {
                { "users", new List<DocumentReference> { user1Ref, user2Ref } }
            };

            await newDmDoc.SetAsync(dmData);

            var messagesCollection = newDmDoc.Collection("messages");
            await messagesCollection.Document().SetAsync(new Dictionary<string, object>
            {
                { "text", "Hey new friend!" },
                { "createdAt", DateTime.UtcNow },
                { "createdBy", user2Ref }
            });
        }

        public async Task CreateNewChannel(string flockId, string name, string userId)
        {
            var flockRef = _flocksCollection.Document(flockId);

            // Get the flock document snapshot
            var snapshot = await flockRef.GetSnapshotAsync();
            if (!snapshot.Exists)
            {
                throw new Exception("Flock document not found.");
            }

            // Get the flock data
            var flockData = snapshot.ToDictionary();
            if (!flockData.TryGetValue("channels", out var channelsObj) || channelsObj is not List<object> channelsList)
            {
                channelsList = new List<object>();
                flockData["channels"] = channelsList;
            }

            // Add the new channel name to the channels list
            channelsList.Add(name);

            // Update the flock document with the new channels list
            await flockRef.UpdateAsync("channels", channelsList);

            var channelRef = flockRef.Collection(name).Document();

            var createdBy = _userCollection.Document(userId);

            var channelData = new Dictionary<string, object>
            {
                { "text", "Hello!" },
                { "createdAt", DateTime.UtcNow },
                { "createdBy", createdBy }
            };

            await channelRef.SetAsync(channelData);
        }
    }
}