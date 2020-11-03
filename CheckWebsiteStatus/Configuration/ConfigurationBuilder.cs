using CheckWebsiteStatus.SimpleLogger;
using Optional;
using Optional.Linq;

namespace CheckWebsiteStatus.Configuration
{
    public class ConfigurationItems
    {
        public readonly string SitemapUrl;
        public readonly int RunsEvery;

        public ConfigurationItems(string sitemapUrl, int runsEvery)
        {
            SitemapUrl = sitemapUrl;
            RunsEvery = runsEvery;
        }
        
    }

    public enum EnvEntries
    {
        SitemapUrl,
        RunsEvery
    }


    public class ConfigurationBuilder
    {
        private static readonly ICLogger Logger = CLogger<ConfigurationBuilder>.GetLogger();
        private readonly IConfigurationFactory _configurationFactory;

        public ConfigurationBuilder(IConfigurationFactory configurationFactory)
        {
            _configurationFactory = configurationFactory;
        }

        private Option<int> CheckEnvironmentOfInt(EnvEntries entryKey)
        {
            return _configurationFactory
                .ReadEnvironmentVariable(entryKey.ToString())
                .FlatMap(valueString => int.TryParse(valueString, out var valueInt) ? Option.Some(valueInt) : Option.None<int>());
        }

        private Option<string> CheckEnvironmentOfString(EnvEntries entryKey)
        {
            return _configurationFactory
                .ReadEnvironmentVariable(entryKey.ToString());
        }
        

        public Option<ConfigurationItems> GetConfiguration()
        {
            return (
                from sitemapUrl in CheckEnvironmentOfString(EnvEntries.SitemapUrl)
                from runsEvery in CheckEnvironmentOfInt(EnvEntries.RunsEvery)
                select new ConfigurationItems(sitemapUrl, runsEvery)
            );
        }
    }
}