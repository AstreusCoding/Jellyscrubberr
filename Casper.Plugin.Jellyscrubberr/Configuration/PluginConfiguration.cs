using System.Diagnostics;
using MediaBrowser.Model.Plugins;

namespace Casper.Plugin.Jellyscrubberr.Configuration;


/// <summary>
/// Class PluginConfiguration
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Whether to save BIFs in the same media folder as their corresponding video.
    /// default = false
    /// </summary>
    public string bifSaveLocation { get; set; } = "0";

    public bool LocalMediaFolderSaving { get; set; } = true;

    public HwAccelerationOptions hardwareAcceleration { get; set; } = HwAccelerationOptions.None;

    public ProcessPriorityClass ProcessPriority { get; set; } = ProcessPriorityClass.High;
    public int processThreads { get; set; } = 1;

    public int imageInterval { get; set; } = 10000;

    public int[] imageWidthResolution { get; set; } = new[] { 320 };
}