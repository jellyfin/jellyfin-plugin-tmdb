using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Tmdb.Providers.TV
{
    /// <summary>
    /// Tmdb series image provider.
    /// </summary>
    public class TmdbSeriesImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbSeriesImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="tmdbClientManager">Instance of the <see cref="TmdbClientManager"/>.</param>
        public TmdbSeriesImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is Series;
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
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);

            if (string.IsNullOrEmpty(tmdbId))
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var series = await _tmdbClientManager
                .GetSeriesAsync(Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture), language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken)
                .ConfigureAwait(false);

            if (series?.Images == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var posters = series.Images.Posters;
            var backdrops = series.Images.Backdrops;

            var remoteImages = new RemoteImageInfo[posters.Count + backdrops.Count];

            for (var i = 0; i < posters.Count; i++)
            {
                var poster = posters[i];
                remoteImages[i] = new RemoteImageInfo
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
                };
            }

            for (var i = 0; i < backdrops.Count; i++)
            {
                var backdrop = series.Images.Backdrops[i];
                remoteImages[posters.Count + i] = new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetBackdropUrl(backdrop.FilePath),
                    CommunityRating = backdrop.VoteAverage,
                    VoteCount = backdrop.VoteCount,
                    Width = backdrop.Width,
                    Height = backdrop.Height,
                    ProviderName = Name,
                    Type = ImageType.Backdrop,
                    RatingType = RatingType.Score
                };
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
