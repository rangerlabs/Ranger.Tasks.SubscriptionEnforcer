using System;
using System.IO;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ranger.Common;
using Ranger.InternalHttpClient;
using Ranger.Monitoring.Logging;
using Ranger.RabbitMQ;

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
                                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: false, reloadOnChange: true)
                                    .AddEnvironmentVariables()
                                    .Build();
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .ConfigureContainer<ContainerBuilder>(builder =>
                {
                    builder.AddRabbitMq<Program>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var taskOptions = hostContext.Configuration.GetOptions<TaskOptions>("task");
                    var identityAuthority = hostContext.Configuration["httpClient:identityAuthority"];
                    services.Add(new ServiceDescriptor(typeof(TaskOptions), taskOptions));
                    services.AddPollyPolicyRegistry();
                    services.AddTenantsHttpClient("http://tenants:8082", identityAuthority, "tenantsApi", "cKprgh9wYKWcsm");
                    services.AddHostedService<SubscriptionEnforcer>();
                });
    }
}
