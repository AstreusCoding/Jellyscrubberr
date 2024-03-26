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
/// Jellyscrubberr plugin.
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
        if (!string.IsNullOrWhiteSpace(applicationPaths.WebPath))
        {
            var indexFile = Path.Combine(applicationPaths.WebPath, "index.html");
            if (File.Exists(indexFile))
            {
                string indexContents = File.ReadAllText(indexFile);
                string basePath = "";

                // Get base path from network config
                try
                {
                    var networkConfig = configurationManager.GetConfiguration("network");
                    var configType = networkConfig.GetType();
                    var basePathField = configType.GetProperty("BaseUrl");
                    var confBasePath = basePathField?.GetValue(networkConfig)?.ToString()?.Trim('/');

                    if (!string.IsNullOrEmpty(confBasePath)) basePath = "/" + confBasePath.ToString();
                }
                catch (Exception e)
                {
                    logger.LogError("Unable to get base path from config, using '/': {0}", e);
                }

                // Don't run if script already exists
                string scriptReplace = "<script plugin=\"Jellyscrubberr\".*?></script>";
                string scriptElement = string.Format("<script plugin=\"Jellyscrubberr\" version=\"1.0.0.0\" src=\"{0}/Trickplay/ClientScript\"></script>", basePath);

                if (!indexContents.Contains(scriptElement))
                {
                    logger.LogInformation("Attempting to inject trickplay script code in {0}", indexFile);

                    // Replace old Jellyscrubberr scrips
                    indexContents = Regex.Replace(indexContents, scriptReplace, "");

                    // Insert script last in body
                    int bodyClosing = indexContents.LastIndexOf("</body>");
                    if (bodyClosing != -1)
                    {
                        indexContents = indexContents.Insert(bodyClosing, scriptElement);

                        try
                        {
                            File.WriteAllText(indexFile, indexContents);
                            logger.LogInformation("Finished injecting trickplay script code in {0}", indexFile);
                        }
                        catch (Exception e)
                        {
                            logger.LogError("Encountered exception while writing to {0}: {1}", indexFile, e);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Could not find closing body tag in {0}", indexFile);
                    }
                }
                else
                {
                    logger.LogInformation("Found client script injected in {0}", indexFile);
                }
            }
        }
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