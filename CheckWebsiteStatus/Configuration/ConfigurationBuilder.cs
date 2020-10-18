using CheckWebsiteStatus.SimpleLogger;
using Optional;

namespace CheckWebsiteStatus.Configuration
{
    public class ConfigurationItems
    {
        public string SitemapUrl;
    }

    public enum EnvEntries
    {
        SitemapUrl
    }


    public class ConfigurationBuilder
    {
        private static readonly ICLogger Logger = CLogger<ConfigurationBuilder>.GetLogger();
        private readonly IConfigurationFactory _configurationFactory;

        public ConfigurationBuilder(IConfigurationFactory configurationFactory)
        {
            _configurationFactory = configurationFactory;
        }

        public Option<ConfigurationItems> GetConfiguration()
        {
            var returnItem = new ConfigurationItems();

            return _configurationFactory.ReadEnvironmentVariable(EnvEntries.SitemapUrl.ToString()).Map(sitemapUrl =>
            {
                Logger.Log($"Successfully reading EnvironmentVariable {EnvEntries.SitemapUrl}");
                returnItem.SitemapUrl = sitemapUrl;
                return returnItem;
            }).Else(() =>
            {
                Logger.Log($"Could not read EnvironmentVariable {EnvEntries.SitemapUrl}");
                return Option.None<ConfigurationItems>();
            });
        }
    }
}