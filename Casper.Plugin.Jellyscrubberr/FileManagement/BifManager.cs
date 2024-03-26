using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using System.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Configuration;
using Casper.Plugin.Jellyscrubberr.Drawing;

namespace Casper.Plugin.Jellyscrubberr.FileManagement;
public class BifManager
{
    private readonly ILogger<VideoProcessor> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly OldMediaEncoder _oldEncoder;
    private readonly FileManager _fileManager;
    public BifManager(
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
        _appPaths = appPaths;
        _libraryMonitor = libraryMonitor;
        _oldEncoder = new OldMediaEncoder(loggerFactory.CreateLogger<OldMediaEncoder>(), mediaEncoder, configurationManager, fileSystem, encodingHelper);
        _fileManager = new FileManager(loggerFactory, logger, mediaEncoder, configurationManager, fileSystem, appPaths, libraryMonitor, encodingHelper);
    }
    public readonly SemaphoreSlim BifWriterSemaphore = new SemaphoreSlim(1, 1);

    public async Task CreateBif(Stream stream, List<FileSystemMetadata> images, int interval)
    {
        var magicNumber = new byte[] { 0x89, 0x42, 0x49, 0x46, 0x0d, 0x0a, 0x1a, 0x0a };
        await stream.WriteAsync(magicNumber, 0, magicNumber.Length);

        // Version
        var bytes = _fileManager.GetBytes(0);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        // Image count
        bytes = _fileManager.GetBytes(images.Count);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        // Interval in ms
        bytes = _fileManager.GetBytes(interval);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        // Reserved
        for (var i = 20; i <= 63; i++)
        {
            bytes = new byte[] { 0x00 };
            await stream.WriteAsync(bytes, 0, bytes.Length);
        }

        // Write the bif index
        var index = 0;
        long imageOffset = 64 + (8 * images.Count) + 8;

        foreach (var img in images)
        {
            bytes = _fileManager.GetBytes(index);
            await stream.WriteAsync(bytes, 0, bytes.Length);

            bytes = _fileManager.GetBytes(imageOffset);
            await stream.WriteAsync(bytes, 0, bytes.Length);

            imageOffset += img.Length;

            index++;
        }

        bytes = new byte[] { 0xff, 0xff, 0xff, 0xff };
        await stream.WriteAsync(bytes, 0, bytes.Length);

        bytes = _fileManager.GetBytes(imageOffset);
        await stream.WriteAsync(bytes, 0, bytes.Length);

        // Write the images
        foreach (var img in images)
        {
            using (var imgStream = new FileStream(img.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                await imgStream.CopyToAsync(stream).ConfigureAwait(false);
            }
        }
    }

    public Task CreateBif(BaseItem item, int width, int interval, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
    {
        var path = GetNewBifPath(item, width);

        return CreateBif(path, width, interval, item, mediaSource, cancellationToken);
    }

    private async Task CreateBif(string path, int width, int interval, BaseItem item, MediaSourceInfo mediaSource, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating trickplay files at {0} width, for {1} [ID: {2}]", width, mediaSource.Path, item.Id);

        var protocol = mediaSource.Protocol;

        var tempDirectory = Path.Combine(_appPaths.TempDirectory, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            var videoStream = mediaSource.VideoStream;

            var inputPath = mediaSource.Path;

            await _oldEncoder.ExtractVideoImagesOnInterval(inputPath, mediaSource.Container, videoStream, mediaSource, mediaSource.Video3DFormat,
                    TimeSpan.FromMilliseconds(interval), tempDirectory, "img_", width, cancellationToken)
                    .ConfigureAwait(false);

            var images = _fileSystem.GetFiles(tempDirectory, new string[] { ".jpg" }, false, false)
                .Where(img => string.Equals(img.Extension, ".jpg", StringComparison.Ordinal))
                .OrderBy(i => i.FullName)
                .ToList();

            if (images.Count == 0) throw new InvalidOperationException("Cannot make BIF file from 0 images.");

            var bifTempPath = Path.Combine(tempDirectory, Guid.NewGuid().ToString("N"));

            using (var fs = new FileStream(bifTempPath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                await CreateBif(fs, images, interval).ConfigureAwait(false);
            }

            _libraryMonitor.ReportFileSystemChangeBeginning(path);

            try
            {
                Directory.CreateDirectory(Directory.GetParent(path)!.FullName);
                File.Copy(bifTempPath, path, true);

                // Create .ignore file so trickplay folder is not picked up as a season when TV folder structure is improper.
                var ignorePath = Path.Combine(Directory.GetParent(path)!.FullName, ".ignore");
                if (!File.Exists(ignorePath)) await File.Create(ignorePath).DisposeAsync();

                _logger.LogInformation("Finished creation of trickplay file {0}", path);
            }
            finally
            {
                _libraryMonitor.ReportFileSystemChangeComplete(path, false);
            }
        }
        finally
        {
            _fileManager.DeleteDirectory(tempDirectory);
        }
    }

    public bool HasBif(BaseItem item, IFileSystem fileSystem, int width)
    {
        return !string.IsNullOrWhiteSpace(GetExistingBifPath(item, fileSystem, width));
    }

    public static string? GetExistingBifPath(BaseItem item, IFileSystem fileSystem, int width)
    {
        var path = JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalBifPath(item, width) : GetInternalBifPath(item, width);

        return fileSystem.FileExists(path) ? path : null;
    }

    private static string GetNewBifPath(BaseItem item, int width)
    {
        return JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalBifPath(item, width) : GetInternalBifPath(item, width);
    }

    private static string GetLocalBifPath(BaseItem item, int width)
    {
        var filename = Path.GetFileNameWithoutExtension(item.Path);
        filename += "-" + width.ToString(CultureInfo.InvariantCulture) + ".bif";

        var folder = item.ContainingFolderPath;

        if (JellyscrubberrPlugin.Instance!.Configuration.fileSaveLocation == FileSaveLocation.CustomFolder)
        {
            folder = Path.Combine(item.ContainingFolderPath, JellyscrubberrPlugin.Instance!.Configuration.customFolderName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, filename);
    }

    private static string GetInternalBifPath(BaseItem item, int width)
    {
        return Path.Combine(item.GetInternalMetadataPath(), "trickplay", width.ToString(CultureInfo.InvariantCulture) + ".bif");
    }
}