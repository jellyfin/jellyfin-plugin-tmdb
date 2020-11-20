using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Tmdb.Providers.BoxSets
{
    /// <summary>
    /// Tmdb box set provider.
    /// </summary>
    public class TmdbBoxSetProvider : IRemoteMetadataProvider<BoxSet, BoxSetInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbBoxSetProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="tmdbClientManager">Instance of <see cref="TmdbClientManager"/>.</param>
        public TmdbBoxSetProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(BoxSetInfo searchInfo, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(searchInfo.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = searchInfo.MetadataLanguage;

            if (tmdbId > 0)
            {
                var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken).ConfigureAwait(false);

                if (collection == null)
                {
                    return Enumerable.Empty<RemoteSearchResult>();
                }

                var result = new RemoteSearchResult
                {
                    Name = collection.Name,
                    SearchProviderName = Name
                };

                if (collection.Images != null)
                {
                    result.ImageUrl = _tmdbClientManager.GetPosterUrl(collection.PosterPath);
                }

                result.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                return new[] { result };
            }

            var collectionSearchResults = await _tmdbClientManager.SearchCollectionAsync(searchInfo.Name, language, cancellationToken).ConfigureAwait(false);

            var collections = new List<RemoteSearchResult>();
            for (var i = 0; i < collectionSearchResults.Count; i++)
            {
                var collection = new RemoteSearchResult
                {
                    Name = collectionSearchResults[i].Name,
                    SearchProviderName = Name
                };
                collection.SetProviderId(MetadataProvider.Tmdb, collectionSearchResults[i].Id.ToString(CultureInfo.InvariantCulture));

                collections.Add(collection);
            }

            return collections;
        }

        /// <inheritdoc />
        public async Task<MetadataResult<BoxSet>> GetMetadata(BoxSetInfo id, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(id.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);
            var language = id.MetadataLanguage;
            // We don't already have an Id, need to fetch it
            if (tmdbId <= 0)
            {
                var searchResults = await _tmdbClientManager.SearchCollectionAsync(id.Name, language, cancellationToken).ConfigureAwait(false);

                if (searchResults != null && searchResults.Count > 0)
                {
                    tmdbId = searchResults[0].Id;
                }
            }

            var result = new MetadataResult<BoxSet>();

            if (tmdbId > 0)
            {
                var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken).ConfigureAwait(false);

                if (collection != null)
                {
                    var item = new BoxSet
                    {
                        Name = collection.Name,
                        Overview = collection.Overview
                    };

                    item.SetProviderId(MetadataProvider.Tmdb, collection.Id.ToString(CultureInfo.InvariantCulture));

                    result.HasMetadata = true;
                    result.Item = item;
                }
            }

            return result;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }
    }
}
