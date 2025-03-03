using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace API.Helpers
{
    public static class ConfigurationHelper
    {
        private static readonly IConfigurationRoot Configuration = null;

        static ConfigurationHelper()
        {
            var builder = new ConfigurationBuilder()
              .SetBasePath(Directory.GetCurrentDirectory())
              .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public static string Value(string key)
        {
            return Configuration[key];
        }

        public static string GetConnectionString(string key)
        {
            return Configuration.GetConnectionString(key);
        }

        public static IConfigurationSection GetSection(string key)
        {
            return Configuration.GetSection(key);
        }
    }
}
