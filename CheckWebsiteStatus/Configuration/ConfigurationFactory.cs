using System;
using CheckWebsiteStatus.SimpleLogger;
using Optional;
using Optional.Unsafe;

namespace CheckWebsiteStatus.Configuration
{
    public class ConfigurationFactory : IConfigurationFactory
    {
        private static readonly ICLogger Logger = CLogger<ConfigurationFactory>.GetLogger();
        
        public Option<string> ReadEnvironmentVariable(string value)
        {
            //Put some sugar here to tell why the container stops.
            return Environment.GetEnvironmentVariable(value).SomeNotNull().Match(
                some: Option.Some,
                none: () =>
                {
                    Logger.Log($"No entry found for environment variable {value}");
                    return Option.None<string>();
                }
            );


        }
        
    }
}