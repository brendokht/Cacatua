namespace server.Models
{
    public class VerificationResultModel
    {
        public bool IsTokenValid { get; set; }
        public bool IsTokenExpired { get; set; }
        public UserInfo UserData { get; set; }
    }
}
