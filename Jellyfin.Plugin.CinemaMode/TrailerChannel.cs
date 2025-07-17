using MediaBrowser.Controller.Channels;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Channels;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Jellyfin.Plugin.CinemaMode
{
    public class TrailerChannel : IChannel, ISupportsLatestMedia
//, IRequiresMediaInfoCallback
, IDisableMediaSourceDisplay
    {
        private readonly IUserManager _userManager;
        private readonly ILibraryManager _libraryManager;
        private readonly ILogger<TrailerChannel> _logger;

        public TrailerChannel(
            IUserManager userManager,
            ILibraryManager libraryManager,
            ILogger<TrailerChannel> logger)
        {
            _userManager = userManager;
            _libraryManager = libraryManager;
            _logger = logger;

            _logger.LogWarning("|cinema-max| Init trailer channel");
        }

        public string Name => "Local Trailers2";

        //  public string Key => "Local Trailers2";
        public string Category => "Trailers";


        public string Description => "Channel showing local trailers from your media library.";

        public string HomePageUrl => "https://jellyfin.org";

        public ChannelParentalRating ParentalRating => ChannelParentalRating.GeneralAudience;

        public ChannelMediaContentType MediaType => ChannelMediaContentType.Trailer;

        public string DataVersion => Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

        public InternalChannelFeatures GetChannelFeatures()
        {
            _logger.LogInformation("|cinema-max| get channel features");
            return new InternalChannelFeatures
            {
                ContentTypes = new List<ChannelMediaContentType>
                {
                    ChannelMediaContentType.Trailer
                },
                MediaTypes = new List<ChannelMediaType> {
                    ChannelMediaType.Video
                },
                AutoRefreshLevels = 4
            };
        }

        public bool IsEnabledFor(string userId)
        {
            return true; // Enable for all users; add logic if needed
        }

        public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
        {
            /*
              _logger.LogInformation("|cinema-max| Searching for trailer items in the library {ID}.", query.FolderId);


              var query2 = new InternalItemsQuery();
              query2.IncludeItemTypes = [Jellyfin.Data.Enums.BaseItemKind.Trailer];
              // query2.Recursive = true;

              var allItems2 = _libraryManager.GetItemList(query2);

  _logger.LogInformation("|cinema-max| Found {Count} all items.", allItems2.Count());

               var allItems = new [] {
                  new {
                      Name = "Test1",
                      Id = 1,
                      DateCreated = DateTime.Now,
                      PrimaryImagePath = "/"
                  }, 
                  new {
                      Name = "Test " + Random.Shared.Next(1, int.MaxValue),
                      Id = Random.Shared.Next(1, int.MaxValue),
                      DateCreated = DateTime.Now,
                      PrimaryImagePath = "/"
                  } 
                 } ;
  */
            string directory = "/media/fernsehfilm/";
            var trailerFiles = Directory.EnumerateFiles(directory, "*-trailer.mp4", SearchOption.AllDirectories)
                .Where(f => Path.GetFileName(f).Contains("-trailer.") &&
                            f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase));

            var allItems = trailerFiles.Select(p => new
            {
                Name = TrimTrailerSuffix(Path.GetFileName(p), "-trailer.mp4"),
                Id = Path.GetFileName(p),
                DateCreated = File.GetCreationTime(p),
                PrimaryImagePath = "",
                Path = p
            });

            var channelItems = allItems
            .Select(item => new ChannelItemInfo
            {
                Name = item.Name,
                Id = item.Id.ToString(),
                MediaType = ChannelMediaType.Video,
                Type = ChannelItemType.Media,
                ImageUrl = item.PrimaryImagePath,
                ContentType = ChannelMediaContentType.Trailer,
                DateCreated = item.DateCreated,
                MediaSources = new List<MediaSourceInfo>
    {
        new MediaSourceInfo
        {
            Path = item.Path,
            Protocol = MediaProtocol.File,
            MediaStreams = new List<MediaStream>(), // optional: add audio/video streams
            Container = "mp4"
        }
    }
            })
            .ToList();

            _logger.LogInformation("|cinema-max| Found {Count} trailer items.", channelItems.Count());

            return await Task.FromResult(new ChannelItemResult
            {
                Items = channelItems,
                TotalRecordCount = channelItems.Count()
            });

        }

        public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
        {
            _logger.LogWarning("|cinema-max| get channel image");
            return Task.FromResult<DynamicImageResponse>(null);
        }

        public IEnumerable<ImageType> GetSupportedChannelImages()
        {
            return Array.Empty<ImageType>();
        }

        public async Task<ChannelItemResult> GetChannelItemMediaInfo(string id, CancellationToken cancellationToken)
        {
            _logger.LogInformation("|cinema-max| get channel item media info {ID}", id);
            if (!Guid.TryParse(id, out var itemId))
            {
                _logger.LogWarning("|cinema-max| Invalid GUID format for item ID: {Id}", id);
                return null;
            }

            _logger.LogInformation("|cinema-max| Fetching media info for item ID: {Id}", id);

            var item = _libraryManager.GetItemById(itemId);
            if (item == null)
            {
                _logger.LogWarning("|cinema-max| Item not found for ID: {Id}", id);
                return null;
            }

            _logger.LogInformation("|cinema-max| Found item: {Name} ({Id})", item.Name, item.Id);

            var result = new ChannelItemResult()
            {
                Items = new List<ChannelItemInfo>
                {
                    new ChannelItemInfo
                    {
                        Id = item.Id.ToString(),
                        Name = item.Name,
                        MediaType = ChannelMediaType.Video,
                        ContentType = ChannelMediaContentType.Trailer,
                        MediaSources = new List < MediaSourceInfo >
                        {
                            new MediaSourceInfo
                            {
                                Path = item.Path,
                                Protocol = MediaProtocol.File,
                                Id = item.Id.ToString()
                            }
                        }
                    }
                }
            };

            return await Task.FromResult(result);
        }

        public Task<IEnumerable<ChannelItemInfo>> GetLatestMedia(ChannelLatestMediaSearch request, CancellationToken cancellationToken)
        {
            // Implementing ISupportsLatestMedia is currently the only way to "automatically" refresh the library.

            _logger.LogInformation("|cinema-max| get latest media");
            throw new NotImplementedException();
        }

        private static string TrimTrailerSuffix(string input, string suffix)
        {
            if (input.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return input.Substring(0, input.Length - suffix.Length);
            return input;
        }
    }
}