using Backend.Common.Middleware;
using Backend.Functions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker.Extensions.OpenApi.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Backend.Functions
{
    /// <summary>
    /// Azure Function entry point
    /// </summary>
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
              .ConfigureFunctionsWebApplication(builder =>
              {
                  builder.UseMiddleware<SessionMiddleware>();
                  builder.UseNewtonsoftJson();
              })
              .ConfigureOpenApi()
              .AddAppConfiguration()
              .ConfigureServices(services =>
              {
                  services.Configure<KestrelServerOptions>(options =>
                  {
                      options.AllowSynchronousIO = true;
                  });
                  services.AddOpenAPIConfigure();
                  services.AddServices();
              })
              .Build();

            host.Run();
        }
    }
}