using Google.Cloud.Firestore;
using System.Text.Json.Serialization;

namespace server.Models
{
    [FirestoreData]
    public class FriendModel
    {
        [JsonPropertyName("uid")]
        [FirestoreProperty]
        public string Uid { get; set; } = string.Empty;

        [JsonPropertyName("date")]
        [FirestoreProperty]
        public Timestamp Date { get; set; } = Timestamp.FromDateTime(DateTime.UtcNow);
    }
}
