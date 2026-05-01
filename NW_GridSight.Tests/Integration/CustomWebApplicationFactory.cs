using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NW_GridSight.Services;

namespace NW_GridSight.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IEiaService>? MockEiaService { get; private set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["EiaApi:BaseUrl"] = "https://api.eia.gov/v2",
                    ["EiaApi:ApiKey"] = "test-api-key"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the real EiaService
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEiaService));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Add mocked EiaService for integration tests
                MockEiaService = new Mock<IEiaService>();
                services.AddScoped(_ => MockEiaService.Object);
            });
        }
    }
}