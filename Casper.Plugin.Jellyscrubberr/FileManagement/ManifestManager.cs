using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;
using System.Text.Json;
using Casper.Plugin.Jellyscrubberr.Drawing;

namespace Casper.Plugin.Jellyscrubberr.FileManagement;

public class ManifestManager
{
    private readonly ILogger<VideoProcessor> _logger;
    private readonly IFileSystem _fileSystem;

    public ManifestManager(
        ILoggerFactory loggerFactory,
        ILogger<VideoProcessor> logger,
        IFileSystem fileSystem)
    {
        _logger = logger;
        _fileSystem = fileSystem;
    }

    public async Task CreateManifest(BaseItem item, int width)
    {
        // Create Manifest object with new resolution
        Manifest newManifest = new Manifest()
        {
            Version = JellyscrubberrPlugin.Instance!.Version.ToString(),
            imageWidthResolution = width
        };

        // If a Manifest object already exists, combine resolutions
        var path = GetNewManifestPath(item);
        if (HasManifest(item, _fileSystem))
        {
            using FileStream openStream = File.OpenRead(path);
            Manifest? oldManifest = await JsonSerializer.DeserializeAsync<Manifest>(openStream);

            if (oldManifest != null && oldManifest.imageWidthResolution != null)
            {
                //newManifest.imageWidthResolution = _
            }
        }

        // Serialize and write to manifest file
        using FileStream createStream = File.Create(path);
        await JsonSerializer.SerializeAsync(createStream, newManifest);
        await createStream.DisposeAsync();
    }

    public bool HasManifest(BaseItem item, IFileSystem fileSystem)
    {
        return !string.IsNullOrWhiteSpace(GetExistingManifestPath(item, fileSystem));
    }

    public static string? GetExistingManifestPath(BaseItem item, IFileSystem fileSystem)
    {
        var path = JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalManifestPath(item) : GetInternalManifestPath(item);

        return fileSystem.FileExists(path) ? path : null;
    }

    private static string GetNewManifestPath(BaseItem item)
    {
        return JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalManifestPath(item) : GetInternalManifestPath(item);
    }

    private static string GetLocalManifestPath(BaseItem item)
    {
        var filename = Path.GetFileNameWithoutExtension(item.Path);
        filename += "-" + "manifest.json";

        var folder = item.ContainingFolderPath;

        if (JellyscrubberrPlugin.Instance!.Configuration.fileSaveLocation == FileSaveLocation.CustomFolder)
        {
            folder = Path.Combine(item.ContainingFolderPath, JellyscrubberrPlugin.Instance!.Configuration.customFolderName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
        }

        return Path.Combine(folder, filename);
    }

    private static string GetInternalManifestPath(BaseItem item)
    {
        return Path.Combine(item.GetInternalMetadataPath(), "trickplay", "manifest.json");
    }
}