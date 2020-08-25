using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace RadioPlayerApp
{
    static class AppConfig
    {
        static IConfigurationRoot configurationRoot = new ConfigurationBuilder()
         .SetBasePath(System.IO.Directory.GetCurrentDirectory())
         .AddJsonFile("appsettings.json")
         .Build();
        
        public static List<Radio> radiosConfig = configurationRoot.GetSection("radios").Get<List<Radio>>();

    }
}
