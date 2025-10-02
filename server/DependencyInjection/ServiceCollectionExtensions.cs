using server.Interfaces;
using server.Services;

namespace server.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddServices(this IServiceCollection services)
        {
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IFirestoreService, FirestoreService>();
            services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
            services.AddScoped<ITokenService, TokenService>();
            return services;
        }
    }
}
