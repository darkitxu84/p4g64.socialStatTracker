using p4g64.socialStatTracker.Template.Configuration;
using Reloaded.Mod.Interfaces.Structs;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace p4g64.socialStatTracker.Configuration
{
    public class Config : Configurable<Config>
    {
        /*
            User Properties:
                - Please put all of your configurable properties here.
    
            By default, configuration saves as "Config.json" in mod user config folder.    
            Need more config files/classes? See Configuration.cs
    
            Available Attributes:
            - Category
            - DisplayName
            - Description
            - DefaultValue

            // Technically Supported but not Useful
            - Browsable
            - Localizable

            The `DefaultValue` attribute is used as part of the `Reset` button in Reloaded-Launcher.
        */

        // CHANGE ME my english is bad
        [DisplayName("Show Above Max")]
        [Description("Displays points after maxing a social stat.")]
        [DefaultValue(true)]
        public bool ShowAboveMax { get; set; } = true;

        [DisplayName("Debug Mode")]
        [Description("Logs additional information to the console.")]
        [DefaultValue(false)]
        public bool DebugEnabled { get; set; } = false;

        // CHANGE ME my english is bad
        [DisplayName("Display Top")]
        [Description("Renders the text above the social stat instead of under it.")]
        [DefaultValue(false)]
        public bool DisplayTop { get; set; } = true;

    }

    /// <summary>
    /// Allows you to override certain aspects of the configuration creation process (e.g. create multiple configurations).
    /// Override elements in <see cref="ConfiguratorMixinBase"/> for finer control.
    /// </summary>
    public class ConfiguratorMixin : ConfiguratorMixinBase
    {
        // 
    }
}
