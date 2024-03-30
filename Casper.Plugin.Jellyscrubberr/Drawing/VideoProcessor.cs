using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Configuration;
using Casper.Plugin.Jellyscrubberr.FileManagement;
using System.Text.Json;

namespace Casper.Plugin.Jellyscrubberr.Drawing;

public class VideoProcessor
{
    private readonly ILogger<VideoProcessor> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly PluginConfiguration _config;
    private readonly BifManager _bifManager;
    private readonly ManifestManager _manifestManager;

    public VideoProcessor(
        ILoggerFactory loggerFactory,
        ILogger<VideoProcessor> logger,
        IMediaEncoder mediaEncoder,
        IServerConfigurationManager configurationManager,
        IFileSystem fileSystem,
        IApplicationPaths appPaths,
        ILibraryMonitor libraryMonitor,
        EncodingHelper encodingHelper)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _config = JellyscrubberrPlugin.Instance!.Configuration;
        _bifManager = new BifManager(loggerFactory, logger, mediaEncoder, configurationManager, fileSystem, appPaths, libraryMonitor, encodingHelper);
        _manifestManager = new ManifestManager(loggerFactory, logger, fileSystem);
    }

    /*
     * Entry point to tell VideoProcessor to generate BIF from item
     */
    public async Task Run(BaseItem item, CancellationToken cancellationToken)
    {
        if (!EnableForItem(item, _fileSystem, _config.imageInterval)) return;

        var mediaSources = ((IHasMediaSources)item).GetMediaSources(false)
            .ToList();

        foreach (var mediaSource in mediaSources)
        {
            /*
                * It seems that in Jellyfin multiple files in the same folder exist both as separate items
                * and as sub-media sources under a single head item. Because of this, it is worth a simple check
                * to make sure we are not writing a "sub-items" trickplay data to the metadata folder of the "main" item.
                */
            if (!item.Id.Equals(Guid.Parse(mediaSource.Id))) continue;

            // check if item has a previous Manifest file.
            Manifest? itemManifest = await GetItemManifest(item, _fileSystem);
            if (itemManifest != null)
            {
                if (itemManifest.imageWidthResolution == _config.imageWidthResolution)
                {
                    _logger.LogInformation("Skipping file, existing manifest resolution matches configuration resolution");
                    continue;
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
            await Run(item, mediaSource, _config.imageWidthResolution, _config.imageInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task Run(BaseItem item, MediaSourceInfo mediaSource, int width, int interval, CancellationToken cancellationToken)
    {
        if (!_bifManager.HasBif(item, _fileSystem, width))
        {
            await _bifManager.BifWriterSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (!_bifManager.HasBif(item, _fileSystem, width))
                {
                    await _bifManager.CreateBif(item, width, interval, mediaSource, cancellationToken).ConfigureAwait(false);
                    await _manifestManager.CreateManifest(item, width).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while creating BIF file");
            }
            finally
            {
                _bifManager.BifWriterSemaphore.Release();
            }
        }
    }

    public static async Task<Manifest?> GetItemManifest(BaseItem item, IFileSystem fileSystem)
    {
        var path = ManifestManager.GetExistingManifestPath(item, fileSystem);
        if (path == null) return null;

        using FileStream openStream = File.OpenRead(path);
        Manifest? newManifest = await JsonSerializer.DeserializeAsync<Manifest>(openStream);

        return newManifest;
    }

    public async Task<bool> DoesItemHaveManifest(BaseItem item, IFileSystem fileSystem)
    {
        Manifest? itemManifest = await GetItemManifest(item, fileSystem);
        if (itemManifest == null) return false;

        return itemManifest.imageWidthResolution == _config.imageWidthResolution;
    }
    public static bool EnableForItem(BaseItem item, IFileSystem fileSystem, int interval)
    {
        if (item is not Video) return false;

        var video = (Video)item;
        var videoType = video.VideoType;

        if (videoType == VideoType.Iso || videoType == VideoType.BluRay || videoType == VideoType.Dvd)
        {
            return false;
        }

        if (video.IsShortcut)
        {
            return false;
        }

        if (!video.IsCompleteMedia)
        {
            return false;
        }

        if (!video.RunTimeTicks.HasValue || video.RunTimeTicks.Value < TimeSpan.FromMilliseconds(interval).Ticks)
        {
            return false;
        }

        if (video.IsFileProtocol)
        {
            if (!fileSystem.FileExists(item.Path))
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }
}
