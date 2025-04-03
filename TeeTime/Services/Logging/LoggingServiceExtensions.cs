using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TeeTime.Services.Logging
{
    public static class LoggingServiceExtensions
    {
        public static IServiceCollection AddLoggingService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<ILoggerService>(provider => 
                LoggerFactory.CreateConfiguredLogger(configuration, "Application"));
            
            return services;
        }
        
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }
    }
} 