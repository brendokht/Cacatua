using System.Runtime.CompilerServices;
using FirebaseAdmin;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace server.Controllers
{
    public class CleanupTokenController : BackgroundService
    {
        private readonly ILogger<CleanupTokenController> _logger;
        private readonly FirestoreDb _db;

        public CleanupTokenController(ILogger<CleanupTokenController> logger, IConfiguration configuration, FirebaseApp app)
        {
            _logger = logger;
            var credentials = app.Options.Credential;
            var channelCreds = credentials.ToChannelCredentials();
            var builder = new FirestoreClientBuilder()
            {
                ChannelCredentials = channelCreds
            };
            var firestoreClient = builder.Build();
            _db = FirestoreDb.Create(configuration["FirebaseConfig:ProjectId"], firestoreClient);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Cleanup Service running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await CleanUpExpiredTokens();
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }

            _logger.LogInformation("Token Cleanup Service is stopping.");
        }

        private async Task CleanUpExpiredTokens()
        {
            try
            {
                CollectionReference usersCollection = _db.Collection("pre_verification");

                Query query = usersCollection.WhereLessThan("validDate", DateTime.UtcNow);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    _logger.LogInformation($"Removing expired token: {documentSnapshot.Id}");
                    await documentSnapshot.Reference.DeleteAsync();
                }

                _logger.LogInformation("Token cleanup completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during token cleanup: {ex.Message}");
            }
        }
    }
}
