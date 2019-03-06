using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace myfirstbot.unittest.Helpers
{
    public class StringLocalizerFactory
    {
        static public StringLocalizer<T> GetStringLocalizer<T>()
        {
            ResourceManagerStringLocalizerFactory factory = new ResourceManagerStringLocalizerFactory(
                Options.Create(new LocalizationOptions() { ResourcesPath = "Resources" }), NullLoggerFactory.Instance);
            var localizer = new StringLocalizer<T>(factory);
            return localizer;
        }
    }
}