using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Tmdb.Providers.TV
{
    /// <summary>
    /// Tmdb episode image provider.
    /// </summary>
    public class TmdbEpisodeImageProvider : IRemoteImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbEpisodeImageProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="tmdbClientManager">Instance of the <see cref="TmdbClientManager"/>.</param>
        public TmdbEpisodeImageProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public IEnumerable<ImageType> GetSupportedImages(BaseItem item)
        {
            yield return ImageType.Primary;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteImageInfo>> GetImages(BaseItem item, CancellationToken cancellationToken)
        {
            var episode = (MediaBrowser.Controller.Entities.TV.Episode)item;
            var series = episode.Series;

            var seriesTmdbId = Convert.ToInt32(series?.GetProviderId(MetadataProvider.Tmdb), CultureInfo.InvariantCulture);

            if (seriesTmdbId <= 0)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var seasonNumber = episode.ParentIndexNumber;
            var episodeNumber = episode.IndexNumber;

            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var language = item.GetPreferredMetadataLanguage();

            var episodeResult = await _tmdbClientManager
                .GetEpisodeAsync(seriesTmdbId, seasonNumber.Value, episodeNumber.Value, language, TmdbUtils.GetImageLanguagesParam(language), cancellationToken)
                .ConfigureAwait(false);

            var stills = episodeResult?.Images?.Stills;
            if (stills == null)
            {
                return Enumerable.Empty<RemoteImageInfo>();
            }

            var remoteImages = new RemoteImageInfo[stills.Count];
            for (var i = 0; i < stills.Count; i++)
            {
                var image = stills[i];
                remoteImages[i] = new RemoteImageInfo
                {
                    Url = _tmdbClientManager.GetStillUrl(image.FilePath),
                    CommunityRating = image.VoteAverage,
                    VoteCount = image.VoteCount,
                    Width = image.Width,
                    Height = image.Height,
                    Language = TmdbUtils.AdjustImageLanguage(image.Iso_639_1, language),
                    ProviderName = Name,
                    Type = ImageType.Primary,
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

        /// <inheritdoc />
        public bool Supports(BaseItem item)
        {
            return item is MediaBrowser.Controller.Entities.TV.Episode;
        }
    }
}
