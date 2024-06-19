#if NET6_0_OR_GREATER
using Microsoft.Extensions.Configuration;
using System.Linq;
#else
using System.Configuration;
#endif
using System.Collections.Generic;

namespace DotCMISUnitTest
{
    public static class AppSettingsHelper
    {
#if NET6_0_OR_GREATER
        private static IConfiguration _configuration;

        private static IConfiguration Configuration => _configuration ?? (_configuration = (new ConfigurationBuilder()
            .SetBasePath(System.IO.Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()).Build());
#endif
        public static string GetAppSetting(string settingKey)
        {
#if NETFRAMEWORK
            return System.Configuration.ConfigurationManager.AppSettings[settingKey];
#else
            return Configuration.GetSection($"Settings:{settingKey}").Value;
#endif
        }

        public static Dictionary<string, string> GetDictionaryAppSettings()
        {
#if NETFRAMEWORK
            Dictionary<string, string> parameters = new Dictionary<string, string>();

            foreach (string key in ConfigurationManager.AppSettings.AllKeys)
            {
                parameters[key] = ConfigurationManager.AppSettings.Get(key);
            }
            return parameters;
#else
            return Configuration.GetSection("Settings").GetChildren()
                  .ToDictionary(x => x.Key, x => x.Value);
#endif
        }
    }
}
