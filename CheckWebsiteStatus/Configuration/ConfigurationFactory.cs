using System;
using Optional;

namespace CheckWebsiteStatus.Configuration
{
    public class ConfigurationFactory : IConfigurationFactory
    {
        
        public Option<string> ReadEnvironmentVariable(string value)
        {
            return Environment.GetEnvironmentVariable(value).SomeNotNull();
        }
        
    }
}