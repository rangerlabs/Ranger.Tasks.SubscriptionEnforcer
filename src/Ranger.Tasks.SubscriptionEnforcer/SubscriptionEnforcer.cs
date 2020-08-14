using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ranger.Common;
using Ranger.InternalHttpClient;
using Ranger.RabbitMQ;

namespace Ranger.Tasks.SubscriptionEnforcer
{
    public class SubscriptionEnforcer : BackgroundService
    {
        private readonly ILogger<SubscriptionEnforcer> _logger;
        private readonly TaskOptions options;
        private readonly IBusPublisher _busPublisher;
        private readonly TenantsHttpClient _tenantsHttpClient;

        public SubscriptionEnforcer(IBusPublisher busPublisher, TenantsHttpClient tenantsHttpClient, ILogger<SubscriptionEnforcer> logger, TaskOptions options)
        {
            _busPublisher = busPublisher;
            _tenantsHttpClient = tenantsHttpClient;
            _logger = logger;
            this.options = options;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var firstExecution = true;
            _logger.LogInformation("Starting Subscription Enforcer");
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!firstExecution)
                {
                    _logger.LogInformation("Executing Subscription Enforcer");
                    try
                    {
                        var tenants = await _tenantsHttpClient.GetAllTenantsAsync<IEnumerable<ContextTenant>>(stoppingToken);
                        if (tenants.Result.Any())
                        {
                            _logger.LogInformation("Sending enforcment for {TenantCount} tenants", tenants.Result.Count());
                            _busPublisher.Send(new EnforceSubscriptions(tenants.Result.Select(t => t.TenantId)), CorrelationContext.FromId(Guid.NewGuid()));
                        }
                        else
                        {
                            _logger.LogWarning("No tenants were returned");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to retrieve tenants");
                    }
                    _logger.LogInformation("Executed Subscription Enforcer");
                }
                firstExecution = false;
                _logger.LogInformation("Executing Subscription Enforcer in {Interval} seconds", options.IntervalInSeconds);
                await Task.Delay(TimeSpan.FromSeconds(options.IntervalInSeconds), stoppingToken);
            }
            _logger.LogInformation("Stopping Subscription Enforcer");
        }
    }
}