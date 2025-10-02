using server.Interfaces;
using System.Security.Cryptography;

namespace server.Services
{
    public class TokenService : ITokenService
    {
        private IFirestoreService _firestoreService;
        public TokenService(IFirestoreService firestoreService)
        {
            _firestoreService = firestoreService;
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Convert.ToBase64String(randomBytes);
        }

        public async Task RotateRefreshToken(string userId, string oldToken, string newToken)
        {
            await _firestoreService.RotateRefreshToken(userId, oldToken, newToken);
        }

        public async Task SaveRefreshToken(string userId, string refreshToken)
        {
            await _firestoreService.SaveRefreshTokenToFirestore(userId, refreshToken);
        }

        public async Task<Dictionary<string, object?>> ValidateRefreshToken(string refreshToken)
        {
            return await _firestoreService.ValidateRefreshToken(refreshToken);
        }

        public async Task RemoveRefreshToken(string userId)
        {
            await _firestoreService.RemoveRefreshToken(userId);
        }
    }
}
