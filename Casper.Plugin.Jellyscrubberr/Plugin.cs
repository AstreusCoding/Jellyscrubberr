using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Casper.Plugin.Jellyscrubberr.Configuration;

namespace Casper.Plugin.Jellyscrubberr;

/// <summary>
/// Jellyscrub plugin.
/// </summary>
public class JellyscrubberrPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <inheritdoc />
    public override string Name => "Jellyscrubberr";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("6b961c92-5678-45d1-a7ac-dd5003f03460");

    /// <inheritdoc />
    public override string Description => "Smooth mouse-over video scrubbing previews.";

    /// <summary>
    /// Initializes a new instance of the <see cref="JellyscrubberrPlugin"/> class.
    /// </summary>
    public JellyscrubberrPlugin(
        IApplicationPaths applicationPaths,
        IXmlSerializer xmlSerializer,
        ILogger<JellyscrubberrPlugin> logger,
        IServerConfigurationManager configurationManager)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }


    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static JellyscrubberrPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = "Jellyscrubberr",
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.configPage.html"
            }
        };
    }
}