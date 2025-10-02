using Newtonsoft.Json;

namespace server.Models
{
    public class FirebaseLoginResponseModel
    {
        [JsonProperty("idToken")]
        public string IdToken { get; set; } = string.Empty;

        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [JsonProperty("expiresIn")]
        public string ExpiresIn { get; set; } = string.Empty;

        [JsonProperty("localId")]
        public string LocalId { get; set; } = string.Empty;
    }
}
