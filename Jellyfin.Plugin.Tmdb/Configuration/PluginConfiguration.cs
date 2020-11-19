using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Tmdb.Configuration
{
    /// <summary>
    /// The plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Gets or sets the api key.
        /// </summary>
        public string ApiKey { get; set; } = "4219e299c89411838049ab0dab19ebd5";
    }
}