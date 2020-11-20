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
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace Jellyfin.Plugin.Tmdb.Providers.TV
{
    /// <summary>
    /// Tmdb episode provider.
    /// </summary>
    public class TmdbEpisodeProvider : IRemoteMetadataProvider<Episode, EpisodeInfo>
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TmdbClientManager _tmdbClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="TmdbEpisodeProvider"/> class.
        /// </summary>
        /// <param name="httpClientFactory">Instance of the <see cref="IHttpClientFactory"/> interface.</param>
        /// <param name="tmdbClientManager">Instance of the <see cref="TmdbClientManager"/>.</param>
        public TmdbEpisodeProvider(IHttpClientFactory httpClientFactory, TmdbClientManager tmdbClientManager)
        {
            _httpClientFactory = httpClientFactory;
            _tmdbClientManager = tmdbClientManager;
        }

        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public async Task<IEnumerable<RemoteSearchResult>> GetSearchResults(EpisodeInfo searchInfo, CancellationToken cancellationToken)
        {
            // The search query must either provide an episode number or date
            if (!searchInfo.IndexNumber.HasValue || !searchInfo.ParentIndexNumber.HasValue)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var metadataResult = await GetMetadata(searchInfo, cancellationToken).ConfigureAwait(false);

            if (!metadataResult.HasMetadata)
            {
                return Enumerable.Empty<RemoteSearchResult>();
            }

            var item = metadataResult.Item;

            return new[]
            {
                new RemoteSearchResult
                {
                    IndexNumber = item.IndexNumber,
                    Name = item.Name,
                    ParentIndexNumber = item.ParentIndexNumber,
                    PremiereDate = item.PremiereDate,
                    ProductionYear = item.ProductionYear,
                    ProviderIds = item.ProviderIds,
                    SearchProviderName = Name,
                    IndexNumberEnd = item.IndexNumberEnd
                }
            };
        }

        /// <inheritdoc />
        public async Task<MetadataResult<Episode>> GetMetadata(EpisodeInfo info, CancellationToken cancellationToken)
        {
            var metadataResult = new MetadataResult<Episode>();

            // Allowing this will dramatically increase scan times
            if (info.IsMissingEpisode)
            {
                return metadataResult;
            }

            info.SeriesProviderIds.TryGetValue(MetadataProvider.Tmdb.ToString(), out var tmdbId);

            var seriesTmdbId = Convert.ToInt32(tmdbId, CultureInfo.InvariantCulture);
            if (seriesTmdbId <= 0)
            {
                return metadataResult;
            }

            var seasonNumber = info.ParentIndexNumber;
            var episodeNumber = info.IndexNumber;

            if (!seasonNumber.HasValue || !episodeNumber.HasValue)
            {
                return metadataResult;
            }

            var episodeResult = await _tmdbClientManager
                .GetEpisodeAsync(seriesTmdbId, seasonNumber.Value, episodeNumber.Value, info.MetadataLanguage, TmdbUtils.GetImageLanguagesParam(info.MetadataLanguage), cancellationToken)
                .ConfigureAwait(false);

            if (episodeResult == null)
            {
                return metadataResult;
            }

            metadataResult.HasMetadata = true;
            metadataResult.QueriedById = true;

            if (!string.IsNullOrEmpty(episodeResult.Overview))
            {
                // if overview is non-empty, we can assume that localized data was returned
                metadataResult.ResultLanguage = info.MetadataLanguage;
            }

            var item = new Episode
            {
                Name = info.Name,
                IndexNumber = info.IndexNumber,
                ParentIndexNumber = info.ParentIndexNumber,
                IndexNumberEnd = info.IndexNumberEnd
            };

            if (!string.IsNullOrEmpty(episodeResult.ExternalIds?.TvdbId))
            {
                item.SetProviderId(MetadataProvider.Tvdb, episodeResult.ExternalIds.TvdbId);
            }

            item.PremiereDate = episodeResult.AirDate;
            item.ProductionYear = episodeResult.AirDate?.Year;

            item.Name = episodeResult.Name;
            item.Overview = episodeResult.Overview;

            item.CommunityRating = Convert.ToSingle(episodeResult.VoteAverage);

            if (episodeResult.Videos?.Results != null)
            {
                foreach (var video in episodeResult.Videos.Results)
                {
                    if (TmdbUtils.IsTrailerType(video))
                    {
                        item.AddTrailerUrl("https://www.youtube.com/watch?v=" + video.Key);
                    }
                }
            }

            var credits = episodeResult.Credits;

            if (credits?.Cast != null)
            {
                foreach (var actor in credits.Cast.OrderBy(a => a.Order).Take(TmdbUtils.MaxCastMembers))
                {
                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = actor.Name.Trim(),
                        Role = actor.Character,
                        Type = PersonType.Actor,
                        SortOrder = actor.Order
                    });
                }
            }

            if (credits?.GuestStars != null)
            {
                foreach (var guest in credits.GuestStars.OrderBy(a => a.Order).Take(TmdbUtils.MaxCastMembers))
                {
                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = guest.Name.Trim(),
                        Role = guest.Character,
                        Type = PersonType.GuestStar,
                        SortOrder = guest.Order
                    });
                }
            }

            // and the rest from crew
            if (credits?.Crew != null)
            {
                foreach (var person in credits.Crew)
                {
                    // Normalize this
                    var type = TmdbUtils.MapCrewToPersonType(person);

                    if (!TmdbUtils.WantedCrewTypes.Contains(type, StringComparer.OrdinalIgnoreCase)
                        && !TmdbUtils.WantedCrewTypes.Contains(person.Job ?? string.Empty, StringComparer.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    metadataResult.AddPerson(new PersonInfo
                    {
                        Name = person.Name.Trim(),
                        Role = person.Job,
                        Type = type
                    });
                }
            }

            metadataResult.Item = item;

            return metadataResult;
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetImageResponse(string url, CancellationToken cancellationToken)
        {
            return _httpClientFactory.CreateClient(NamedClient.Default).GetAsync(new Uri(url), cancellationToken);
        }
    }
}
