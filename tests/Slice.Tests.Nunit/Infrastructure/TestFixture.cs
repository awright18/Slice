using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Respawn;

namespace Slice.Tests.NUnit.Infrastructure
{
    public class TestFixture : WebApplicationFactory<Startup>
    {
        public IConfiguration Configuration { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            //builder.UseEnvironment("Testing");
        }

        public TestFixture()
        {
            Configuration = TestHelper.GetConfiguration();
        }

        public async Task ResetDatabase()
        {
            var checkPoint = new Checkpoint()
            {
                SchemasToInclude = new[] { "dbo" }
            };

            await checkPoint.Reset(Configuration.GetConnectionString("DefaultConnection"));
        }
    }
}
