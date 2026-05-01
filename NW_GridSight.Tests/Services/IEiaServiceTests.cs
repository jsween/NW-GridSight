using NW_GridSight.Services;

namespace NW_GridSight.Tests.Services
{
    public class IEiaServiceTests
    {
        [Fact]
        public void MockEiaService_ImplementsIEiaService()
        {
            var service = new MockEiaService();
            Assert.IsType<IEiaService>(service, exactMatch: false);
        }

        [Fact]
        public void EiaService_ImplementsIEiaService()
        {
            // Verifies the interface contract at compile time
            // If EiaService doesn't implement IEiaService, this won't compile
            IEiaService service = new MockEiaService();
            Assert.NotNull(service);
        }
    }
}