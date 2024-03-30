using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;
using System.Text.Json;
using Casper.Plugin.Jellyscrubberr.Drawing;
using MediaBrowser.Controller.Library;

namespace Casper.Plugin.Jellyscrubberr.FileManagement;

public class ManifestManager
{
    private readonly ILogger<ManifestManager> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly PluginConfiguration _config;
    private readonly ILibraryMonitor _libraryMonitor;

    public ManifestManager(
        ILoggerFactory loggerFactory,
        ILogger<ManifestManager> logger,
        IFileSystem fileSystem,
        ILibraryMonitor libraryMonitor)
    {
        _logger = logger;
        _fileSystem = fileSystem;
        _config = JellyscrubberrPlugin.Instance!.Configuration;
        _libraryMonitor = libraryMonitor;
    }

    public async Task CreateManifest(BaseItem item)
    {
        // Create Manifest object with current configuration
        Manifest newManifest = new Manifest()
        {
            Version = JellyscrubberrPlugin.Instance!.Version.ToString(),
            imageWidthResolution = _config.imageWidthResolution,
            qScaleInput = _config.qScaleInput,
            imageInterval = _config.imageInterval
        };

        // Check if a manifest already exists for this item and if it matches the current configuration
        if (HasManifest(item))
        {
            // If manifest exists, check if it matches current configuration
            if (ManifestMatches(item))
            {
                // If manifest matches, do nothing
                return;
            }
        }

        // Get path to manifest file
        var path = GetNewManifestPath(item);

        _logger.LogInformation("Creating manifest file at {0}", path);

        _libraryMonitor.ReportFileSystemChangeBeginning(path);

        try
        {
            // Serialize and write to manifest file
            using FileStream createStream = File.Create(path);
            await JsonSerializer.SerializeAsync(createStream, newManifest);
            await createStream.DisposeAsync();
        }
        finally
        {
            _libraryMonitor.ReportFileSystemChangeComplete(path, true);
        }
    }

    public bool HasManifest(BaseItem item)
    {
        return !string.IsNullOrWhiteSpace(GetExistingManifestPath(item));
    }

    public bool ManifestMatches(BaseItem item)
    {
        var path = GetExistingManifestPath(item);
        if (path == null)
        {
            return false;
        }

        using FileStream openStream = File.OpenRead(path);

        try
        {
            Manifest? manifest = JsonSerializer.Deserialize<Manifest>(openStream);

            if (manifest == null)
            {
                return false;
            }

            if (manifest.imageWidthResolution != JellyscrubberrPlugin.Instance!.Configuration.imageWidthResolution)
            {
                return false;
            }

            if (manifest.qScaleInput != JellyscrubberrPlugin.Instance!.Configuration.qScaleInput)
            {
                return false;
            }

            if (manifest.imageInterval != JellyscrubberrPlugin.Instance!.Configuration.imageInterval)
            {
                return false;
            }

            return true;

        }
        catch (JsonException)
        {
            _logger.LogWarning("Error deserializing manifest file at {0} most likely missing arguments in manifest", path);
            return false;
        }
    }

    public bool DeleteManifest(BaseItem item)
    {
        var path = GetExistingManifestPath(item);

        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        _logger.LogInformation("Deleting manifest file at {0}", path);

        _libraryMonitor.ReportFileSystemChangeBeginning(path);

        try
        {
            _fileSystem.DeleteFile(path);
        }
        finally
        {
            _libraryMonitor.ReportFileSystemChangeComplete(path, true);
        }

        return true;
    }

    public string? GetExistingManifestPath(BaseItem item)
    {
        var path = JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalManifestPath(item) : GetInternalManifestPath(item);

        return _fileSystem.FileExists(path) ? path : null;
    }

    private string GetNewManifestPath(BaseItem item)
    {
        return JellyscrubberrPlugin.Instance!.Configuration.LocalMediaFolderSaving ? GetLocalManifestPath(item) : GetInternalManifestPath(item);
    }

    private string GetLocalManifestPath(BaseItem item)
    {
        var filename = Path.GetFileNameWithoutExtension(item.Path);
        filename += "-" + "manifest.json";

        var folderPath = item.ContainingFolderPath;

        if (JellyscrubberrPlugin.Instance!.Configuration.fileSaveLocation == FileSaveLocation.CustomFolder)
        {
            folderPath = Path.Combine(item.ContainingFolderPath, JellyscrubberrPlugin.Instance!.Configuration.customFolderName);
            if (!Directory.Exists(folderPath))
            {
                FileManager.CreateDirectory(folderPath);
            }
        }

        return Path.Combine(folderPath, filename);
    }

    private string GetInternalManifestPath(BaseItem item)
    {
        return Path.Combine(item.GetInternalMetadataPath(), "trickplay", "manifest.json");
    }
}