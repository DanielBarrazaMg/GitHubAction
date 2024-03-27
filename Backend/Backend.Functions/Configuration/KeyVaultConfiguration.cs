using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Backend.Functions.Configuration
{
    public static class KeyVaultConfig
    {
        /// <summary>
        /// Configura la conexión con Azure Key Vault. Lanza una excepción si en el archivo de configuración no existe la sección "KeyVault"
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static IServiceCollection AddKeyVault(this IServiceCollection services, ConfigurationManager configuration)
        {
            var url = configuration.GetSection("keyVault:URL").Value;
            var clientId = configuration.GetSection("keyVault:ClientId").Value;
            var clientSecret = configuration.GetSection("keyVault:ClientSecret").Value;
            var directoryId = configuration.GetSection("keyVault:DirectoryId").Value;

            if (url == null || clientId == null || clientSecret == null || directoryId == null)
                throw new ArgumentException("No KeyVault Configured in settings");

            configuration.AddAzureKeyVault(url, clientId, clientSecret, new DefaultKeyVaultSecretManager());

            return services;
        }
    }
}
