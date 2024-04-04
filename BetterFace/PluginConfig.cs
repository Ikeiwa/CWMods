using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace BetterFace
{
    internal static class PluginConfig
    {
        public static ConfigFile config;
        public static ConfigEntry<KeyboardShortcut> customizationMenuKey;

        public static void InitConfig(ConfigFile _config)
        {
            config = _config;

            customizationMenuKey = config.Bind("General", "CustomizationMenuKey", new KeyboardShortcut(KeyCode.F2),
                new ConfigDescription("The key to open the customization menu.", null));
        }
    }
}
