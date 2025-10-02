namespace server.Interfaces
{
    public interface ITokenService
    {
        public string GenerateRefreshToken();
        public Task SaveRefreshToken(string userId, string refreshToken);
        public Task RotateRefreshToken(string userId, string oldToken, string newToken);
        public Task<Dictionary<string, object?>> ValidateRefreshToken(string refreshToken);
        public Task RemoveRefreshToken(string userId);
    }
}
