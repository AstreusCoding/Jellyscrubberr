using System.Diagnostics;
using MediaBrowser.Model.Plugins;

namespace Casper.Plugin.Jellyscrubberr.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    public FileSaveLocation fileSaveLocation { get; set; } = FileSaveLocation.CustomFolder;
    public string customFolderName { get; set; } = "trickplay";

    public HwAccelerationOptions hardwareAcceleration { get; set; } = HwAccelerationOptions.None;

    public ProcessPriorityClass processPriority { get; set; } = ProcessPriorityClass.Normal;

    public bool OnDemandGeneration { get; set; } = true;
    public bool extractionDuringLibraryScan { get; set; } = false;
    public bool LocalMediaFolderSaving { get; set; } = false;
    public bool shouldRegenerateIfOldManifest { get; set; } = true;

    public int qScaleInput { get; set; } = 4;
    public int processThreads { get; set; } = 1;
    public int imageInterval { get; set; } = 10000;
    public int imageWidthResolution { get; set; } = 320;

    public MetadataScanBehaviour ScanBehavior { get; set; } = MetadataScanBehaviour.NonBlocking;
}