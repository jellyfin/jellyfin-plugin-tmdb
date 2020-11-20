using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Tmdb
{
    /// <summary>
    /// Register tmdb services.
    /// </summary>
    public class TmdbPluginServiceRegistrator : IPluginServiceRegistrator
    {
        /// <inheritdoc />
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TmdbClientManager>();
        }
    }
}