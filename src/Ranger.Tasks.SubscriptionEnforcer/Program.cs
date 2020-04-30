using System;
using System.IO;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ranger.InternalHttpClient;
using Ranger.Logging;

namespace Ranger.Tasks.SubscriptionEnforcer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseLogging()
                .ConfigureAppConfiguration(builder =>
                {
                    var config = builder
                                    .SetBasePath(Directory.GetCurrentDirectory())
                                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: false, reloadOnChange: true)
                                    .AddEnvironmentVariables()
                                    .Build();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddPollyPolicyRegistry();
                    services.AddTenantsHttpClient("http://tenants:8082", "tenantsApi", "cKprgh9wYKWcsm");
                    services.AddHostedService<SubscriptionEnforcer>();
                    services.AddAutofac();
                });
    }
}
