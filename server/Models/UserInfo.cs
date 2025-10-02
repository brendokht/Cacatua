using Google.Cloud.Firestore;
using System.Text.Json.Serialization;

namespace server.Models
{
    public class UserInfo
    {
        [JsonPropertyName("uid")]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("displayName")]
        public string DisplayName { get; set; } = string.Empty;

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("phoneNumber")]
        public string PhoneNumber { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("photoUrl")]
        public string PhotoUrl { get; set; } = string.Empty;

        [JsonPropertyName("dateRegistered")]
        public DateTime DateRegistered { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        /*     REVISE TO COLLECTION     */
        //[JsonPropertyName("friendList")]
        //public List<FriendModel> FriendList { get; set; } = new();
        //[JsonPropertyName("friendRequestList")]
        //public List<FriendModel> FriendRequestList { get; set; } = new();
        //[JsonPropertyName("friendRecievedList")]
        //public List<FriendModel> FriendRecievedList { get; set; } = new();

        public static implicit operator UserInfo(RegisterRequestModel registerRequest)
        {
            if (registerRequest == null)
            {
                throw new ArgumentNullException(nameof(registerRequest));
            }

            return new UserInfo
            {
                Email = registerRequest.Email,
                FirstName = registerRequest.FirstName,
                LastName = registerRequest.LastName,
                DisplayName = registerRequest.DisplayName,
                Password = registerRequest.Password,
                // Setting default values for fields not available in RegisterRequestModel
                PhoneNumber = string.Empty,
                PhotoUrl = string.Empty,
                //FriendList = [],
                //FriendRequestList = [],
                //FriendRecievedList = [],
                DateRegistered = DateTime.UtcNow,
            };
        }

    }
}
