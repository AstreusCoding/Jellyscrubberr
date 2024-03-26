using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Dto;
using System.Globalization;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;
using System.Text.Json;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Configuration;
using Casper.Plugin.Jellyscrubberr.Drawing;
namespace Casper.Plugin.Jellyscrubberr.FileManagement;

public class FileManager
{
    private readonly ILogger<VideoProcessor> _logger;
    private readonly IFileSystem _fileSystem;
    private readonly IApplicationPaths _appPaths;
    private readonly ILibraryMonitor _libraryMonitor;
    private readonly PluginConfiguration _config;
    private readonly OldMediaEncoder _oldEncoder;
    public FileManager(
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
        _config = JellyscrubberrPlugin.Instance!.Configuration;
        _oldEncoder = new OldMediaEncoder(loggerFactory.CreateLogger<OldMediaEncoder>(), mediaEncoder, configurationManager, fileSystem, encodingHelper);
    }

    public bool DeleteDirectory(string directoryPath)
    {
        try
        {
            Directory.Delete(directoryPath, true);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("Error deleting directory with path {0} due to unauthorized access: {1}", directoryPath, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("Error deleting directory with path {0} due to invalid argument: {1}", directoryPath, ex);
        }
        catch (PathTooLongException ex)
        {
            _logger.LogError("Error deleting directory with path {0} due to path too long: {1}", directoryPath, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError("Error deleting directory with path {0} due to directory not found: {1}", directoryPath, ex);
        }

        return !Directory.Exists(directoryPath);
    }

    public bool CreateDirectory(string directoryPath)
    {
        try
        {
            Directory.CreateDirectory(directoryPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError("Error creating directory with path {0} due to unauthorized access: {1}", directoryPath, ex);
        }
        catch (ArgumentException ex)
        {
            _logger.LogError("Error creating directory with path {0} due to invalid argument: {1}", directoryPath, ex);
        }
        catch (PathTooLongException ex)
        {
            _logger.LogError("Error creating directory with path {0} due to path too long: {1}", directoryPath, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            _logger.LogError("Error creating directory with path {0} due to parent directory not found: {1}", directoryPath, ex);
        }
        catch (NotSupportedException ex)
        {
            _logger.LogError("Error creating directory with path {0} due to invalid path: {1}", directoryPath, ex);
        }

        return Directory.Exists(directoryPath);
    }

    public byte[] GetBytes(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    public byte[] GetBytes(long value)
    {
        var intVal = Convert.ToInt32(value);
        return GetBytes(intVal);
    }
}