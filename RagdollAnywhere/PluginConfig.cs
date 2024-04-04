using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RagdollAnywhere
{
    internal static class PluginConfig
    {
        public static ConfigFile config;

        public static void InitConfig(ConfigFile _config)
        {
            config = _config;
        }
    }
}
