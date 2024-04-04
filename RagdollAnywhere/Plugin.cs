using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Zorro.Core;
using System.Reflection;
using Zorro.Settings;
using static Item;


namespace RagdollAnywhere
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static PlayerRagdoll localPLayer;
        public static KeyCodeSetting ragdollKeySetting;
        public static MethodInfo ragdollMethod;
        public bool isRagdoll;
        public float ragdollTimer;

        private void Awake()
        {
            PluginConfig.InitConfig(Config);

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID + ".patch");
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Update()
        {
            if (ragdollMethod == null)
                return;

            if(ragdollKeySetting != null)
            {
                if (Input.GetKeyDown((KeyCode)ragdollKeySetting.Value))
                {
                    isRagdoll = true;
                    ragdollTimer = 0.75f;
                    ragdollMethod.Invoke(localPLayer, [1f]);
                }
                else if (Input.GetKeyUp((KeyCode)ragdollKeySetting.Value))
                    isRagdoll = false;
            }

            ragdollTimer -= Time.deltaTime;

            if(isRagdoll && ragdollTimer <= 0)
            {
                ragdollTimer = 0.75f;
                ragdollMethod.Invoke(localPLayer, [1f]);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    class PatchPlayerRagdoll
    {
        public static void Postfix(Player __instance)
        {
            if (__instance.IsLocal)
            {
                Plugin.localPLayer = __instance.gameObject.GetComponent<PlayerRagdoll>();
                Plugin.ragdollMethod = Plugin.localPLayer.GetType().GetMethod("CallFall", BindingFlags.NonPublic | BindingFlags.Instance);
            }
        }
    }

    [HarmonyPatch(typeof(SettingsHandler))]
    [HarmonyPatch(MethodType.Constructor)]
    class PatchSettingsHandlerCtor
    {
        public static void Postfix(ref List<Setting> ___settings, ref ISettingsSaveLoad ____settingsSaveLoad)
        {
            if (___settings == null)
                return;

            RagdollKeybindSetting ragdollKeySetting = new RagdollKeybindSetting();
            ragdollKeySetting.Load(____settingsSaveLoad);
            ragdollKeySetting.ApplyValue();

            ___settings.Add(ragdollKeySetting);

            Plugin.ragdollKeySetting = ragdollKeySetting;
        }
    }
}
