using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Abstractions;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Configurations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Backend.Functions.Configuration
{
    /// <summary>
    /// Static class providing extension methods for configuring OpenAPI documentation settings.
    /// Allows for adding OpenAPI configuration options to the service collection.
    /// </summary>
    public static class OpenAPIConfiguration
    {
        /// <summary>
        /// Extension method for IServiceCollection that adds OpenAPI configuration options.
        /// </summary>
        /// <param name="services">The collection of services to which OpenAPI configuration options will be added.</param>
        /// <returns>The collection of services with the added OpenAPI configuration options.</returns>
        public static IServiceCollection AddOpenAPIConfigure(this IServiceCollection services)
        {
            services.AddSingleton<IOpenApiConfigurationOptions>(_ =>
            {
                var options = new OpenApiConfigurationOptions()
                {
                    Info = new OpenApiInfo()
                    {
                        Version = "1.0.0",
                        Title = "API",
                        Description = "API",
                        Contact = new OpenApiContact()
                        {
                            Name = "Support",
                            Email = "support@example.com",
                        },
                    },
                    ForceHttps = false,
                    ForceHttp = false,
                };

                return options;
            });

            return services;
        }
    }

}
