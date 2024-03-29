using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Drawing;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Configuration;
using Casper.Plugin.Jellyscrubberr.Configuration;

namespace Casper.Plugin.Jellyscrubberr.Providers;

public class BIFMetadataProvider : ICustomMetadataProvider<Episode>,
    ICustomMetadataProvider<MusicVideo>,
    ICustomMetadataProvider<Movie>,
    ICustomMetadataProvider<Video>,
    IHasItemChangeMonitor,
    IHasOrder,
    IForcedProvider
{
    private readonly ILogger<BIFMetadataProvider> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly IMediaEncoder _mediaEncoder;
    private readonly IServerConfigurationManager _configurationManager;
    private readonly EncodingHelper _encodingHelper;

    public BIFMetadataProvider(
        ILogger<BIFMetadataProvider> logger,
        ILoggerFactory loggerFactory,
        IFileSystem fileSystem,
        IApplicationPaths appPaths,
        ILibraryMonitor libraryMonitor,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager configurationManager,
        EncodingHelper encodingHelper)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystem = fileSystem;
        _appPaths = appPaths;
        _libraryMonitor = libraryMonitor;
        _mediaEncoder = mediaEncoder;
        _configurationManager = configurationManager;
        _encodingHelper = encodingHelper;
    }

    public string Name => "Jellyscrubberr Trickplay Generator";

    public int Order => 1000;

    public bool HasChanged(BaseItem item, IDirectoryService directoryService)
    {
        if (item.IsFileProtocol)
        {
            var file = directoryService.GetFile(item.Path);
            if (file != null && item.DateModified != file.LastWriteTimeUtc)
            {
                return true;
            }
        }

        return false;
    }

    public Task<ItemUpdateType> FetchAsync(Episode item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    public Task<ItemUpdateType> FetchAsync(MusicVideo item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    public Task<ItemUpdateType> FetchAsync(Movie item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    public Task<ItemUpdateType> FetchAsync(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        return FetchInternal(item, options, cancellationToken);
    }

    private async Task<ItemUpdateType> FetchInternal(Video item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        var config = JellyscrubberrPlugin.Instance!.Configuration;

        if (config.extractionDuringLibraryScan)
        {
            var videoProcessor = new VideoProcessor(_loggerFactory, _loggerFactory.CreateLogger<VideoProcessor>(), _mediaEncoder, _configurationManager, _fileSystem, _appPaths, _libraryMonitor, _encodingHelper);

            switch (config.ScanBehavior)
            {
                case MetadataScanBehaviour.Blocking:
                    await videoProcessor.Run(item, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                case MetadataScanBehaviour.NonBlocking:
                    _ = videoProcessor.Run(item, cancellationToken).ConfigureAwait(false);
                    break;
            }
        }

        // The core doesn't need to trigger any save operations over this
        return ItemUpdateType.None;
    }
}
