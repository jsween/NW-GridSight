using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NW_GridSight.Services;

namespace NW_GridSight.Tests.Integration
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        public Mock<IEiaService>? MockEiaService { get; private set; }
        public Mock<IMemoryCache>? MockCache { get; private set; }

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
                var eiaDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IEiaService));
                if (eiaDescriptor != null)
                {
                    services.Remove(eiaDescriptor);
                }

                // Remove the real IMemoryCache
                var cacheDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IMemoryCache));
                if (cacheDescriptor != null)
                {
                    services.Remove(cacheDescriptor);
                }

                // Add mocked EiaService for integration tests
                MockEiaService = new Mock<IEiaService>();
                services.AddScoped(_ => MockEiaService.Object);

                // Add mocked IMemoryCache for integration tests
                MockCache = new Mock<IMemoryCache>();

                // Setup TryGetValue to return false (no cached value by default)
                object? nullValue = null;
                MockCache.Setup(c => c.TryGetValue(It.IsAny<object>(), out nullValue))
                    .Returns(false);

                // Setup CreateEntry for cache Set operations
                MockCache.Setup(c => c.CreateEntry(It.IsAny<object>()))
                    .Returns((object key) =>
                    {
                        var mockEntry = new Mock<ICacheEntry>();
                        mockEntry.SetupProperty(e => e.Value);
                        mockEntry.SetupProperty(e => e.AbsoluteExpirationRelativeToNow);
                        // Don't setup Key property - it's read-only
                        mockEntry.SetupGet(e => e.Key).Returns(key);
                        return mockEntry.Object;
                    });

                services.AddSingleton(_ => MockCache.Object);
            });
        }
    }
}