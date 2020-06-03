using Microsoft.Extensions.Configuration;

namespace Slice.Tests.NUnit.Infrastructure

{
    public static class TestHelper
    {
        public static IConfiguration GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        

    }
}
