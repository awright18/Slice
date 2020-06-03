using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Slice.Tests.Infrastructure
{
    public static class TestHelper
    {
        public static IConfiguration GetConfiguration(string outputPath = "")
        {
            return new ConfigurationBuilder()
                //.SetBasePath(outputPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

    }
}
