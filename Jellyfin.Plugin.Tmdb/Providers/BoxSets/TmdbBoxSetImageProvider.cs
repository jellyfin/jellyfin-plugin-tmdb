using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Tmdb.Providers.BoxSets
{
    /// <summary>
    /// Tmdb box set image provider.
    /// </summary>
    public class TmdbBoxSetImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbBoxSetImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="tmdbClientManager">Instance of <see cref="TmdbClientManager"/>.</param>
        public TmdbBoxSetImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is BoxSet;
        }

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
            yield return ImageType.Backdrop;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var tmdbId = Convert.ToInt32(item.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (tmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var collection = await _tmdbClientManager.GetCollectionAsync(tmdbId, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken).ConfigureAwait(false);

            if (collection?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var remoteImages = new List<RemoteImageInfo>();

            for (var i = 0; i < collection.Images.Posters.Count; i++)
            {
                var poster = collection.Images.Posters[i];
                remoteImages.Add(new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetPosterUrl(poster.FilePath),
                    CommunityRating = poster.VoteAverage,
                    VoteCount = poster.VoteCount,
                    Width = poster.Width,
                    Height = poster.Height,
                    Language = TmdbUtils.AdjustImageLanguage(poster.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
                    RatingType = RatingType.Score
                });
            }

            for (var i = 0; i < collection.Images.Backdrops.Count; i++)
            {
                var backdrop = collection.Images.Backdrops[i];
                remoteImages.Add(new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetBackdropUrl(backdrop.FilePath),
                    CommunityRating = backdrop.VoteAverage,
                    VoteCount = backdrop.VoteCount,
                    Width = backdrop.Width,
                    Height = backdrop.Height,
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    RatingType = RatingType.Score
                });
            }

            return remoteImages.OrderByLanguageDescending(language);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }
    }
}
