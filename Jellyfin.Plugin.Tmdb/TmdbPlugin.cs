using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Tmdb.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Tmdb
{
    /// <summary>
    /// Tmdb Plugin.
    /// </summary>
    public class TmdbPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public TmdbPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets current plugin instance.
        /// </summary>
        public static TmdbPlugin? Instance { get; private set; }

        /// <inheritdoc />
        public override string Name => "TheMovieDb";

        /// <inheritdoc />
        public override Guid Id => new Guid("963FD785-53E7-440C-80BD-71661BE1B7DF");

        /// <inheritdoc />
        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
            };
        }
    }
}