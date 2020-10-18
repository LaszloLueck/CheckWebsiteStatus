using Optional;

namespace CheckWebsiteStatus.Configuration
{
    public interface IConfigurationFactory
    {
        Option<string> ReadEnvironmentVariable(string value);
    }
}