using BepInEx;
using HarmonyLib;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Zorro.Core;
using TMPro;
using System.Reflection;


namespace BetterFace
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private bool showCustomizationMenu;
        private Rect CustomizationWindowRect;
        public const int CustomizationWindowId = -48;
        private Vector2 CustomizationWindowScrollPos;

        private PropertyInfo _curLockState;
        private PropertyInfo _curVisible;
        private int _previousCursorLockState;
        private bool _previousCursorVisible; 
        private bool _obsoleteCursor;

        private AssetBundle customFontsBundle;
        public static TMP_FontAsset fallbackFont;
        public static TMP_SpriteAsset emojiSprites;
        public static TMP_FontAsset originalFont;

        private PlayerVisor localPlayer;

        private float currentRotation = 0;
        private float currentSize = 0;
        private Optionable<float> currentColor;
        private string currentText = string.Empty;

        private void Awake()
        {
            PluginConfig.InitConfig(Config);

            customFontsBundle = AssetBundle.LoadFromMemory(Properties.Resources.betterface);
            if (customFontsBundle)
            {
                fallbackFont = customFontsBundle.LoadAsset<TMP_FontAsset>("NotoSans-Regular SDF");
                emojiSprites = customFontsBundle.LoadAsset<TMP_SpriteAsset>("EmojiSprites");
            }
            else
            {
                Logger.LogError("Error loading assetbundle");
                return;
            }

            var harmony = new Harmony(PluginInfo.PLUGIN_GUID + ".patch");
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
        }

        private void Start()
        {
            var tCursor = typeof(Cursor);
            _curLockState = tCursor.GetProperty("lockState", BindingFlags.Static | BindingFlags.Public);
            _curVisible = tCursor.GetProperty("visible", BindingFlags.Static | BindingFlags.Public);

            if (_curLockState == null && _curVisible == null)
            {
                _obsoleteCursor = true;

                _curLockState = typeof(Screen).GetProperty("lockCursor", BindingFlags.Static | BindingFlags.Public);
                _curVisible = typeof(Screen).GetProperty("showCursor", BindingFlags.Static | BindingFlags.Public);
            }
        }

        private void Update()
        {
            if (PluginConfig.customizationMenuKey.Value.IsDown())
            {
                showCustomizationMenu = !showCustomizationMenu;
                if (showCustomizationMenu)
                {
                    localPlayer = null;
                    var players = FindObjectsOfType<Player>();
                    foreach(var player in players)
                    {
                        if(player != null && player.IsLocal) 
                        { 
                            localPlayer = player.gameObject.GetComponent<PlayerVisor>();

                            if(localPlayer != null)
                            {
                                currentRotation = localPlayer.FaceRotation;
                                currentSize = localPlayer.FaceSize;
                                currentColor = localPlayer.hue;
                                currentText = localPlayer.visorFaceText.text;
                            }

                            break;
                        }
                    }

                    if (_curLockState != null)
                    {
                        _previousCursorLockState = _obsoleteCursor ? Convert.ToInt32((bool)_curLockState.GetValue(null, null)) : (int)_curLockState.GetValue(null, null);
                        _previousCursorVisible = (bool)_curVisible.GetValue(null, null);
                    }

                    GUI.FocusWindow(CustomizationWindowId);
                }
                else ResetCursor();
            }
        }

        private void LateUpdate()
        {
            if (showCustomizationMenu) SetUnlockCursor(0, true);
        }

        #region GUI

        private void SetUnlockCursor(int lockState, bool cursorVisible)
        {
            if (_curLockState != null)
            {
                // Do through reflection for unity 4 compat
                //Cursor.lockState = CursorLockMode.None;
                //Cursor.visible = true;
                if (_obsoleteCursor)
                    _curLockState.SetValue(null, Convert.ToBoolean(lockState), null);
                else
                    _curLockState.SetValue(null, lockState, null);

                _curVisible.SetValue(null, cursorVisible, null);
            }
        }

        public void ResetCursor()
        {
            if (!_previousCursorVisible || _previousCursorLockState != 0) // 0 = CursorLockMode.None
                SetUnlockCursor(_previousCursorLockState, _previousCursorVisible);
        }

        private void OnGUI()
        {
            if (showCustomizationMenu)
            {
                SetUnlockCursor(0, true);

                CustomizationWindowRect = new Rect(20, 100, 250, 400);

                if (GUI.Button(new Rect(0, 0, Screen.width, Screen.height), string.Empty, GUIStyle.none) &&
                    !CustomizationWindowRect.Contains(Input.mousePosition) || Input.GetKeyDown(KeyCode.Escape))
                {
                    showCustomizationMenu = false;
                    ResetCursor();
                }

                GUI.Box(CustomizationWindowRect, GUIContent.none);
                GUILayout.Window(CustomizationWindowId, CustomizationWindowRect, CustomizationMenuWindow, "Customization");

                Input.ResetInputAxes();
            }
        }

        private void CustomizationMenuWindow(int id)
        {
            CustomizationWindowScrollPos = GUILayout.BeginScrollView(CustomizationWindowScrollPos, false, true);

            GUILayout.BeginVertical();

            GUI.enabled = localPlayer != null;


            GUILayout.Label("Rotation");
            currentRotation = GUILayout.HorizontalSlider(currentRotation, 0,360);

            GUILayout.Label("Size");
            currentSize = GUILayout.HorizontalSlider(currentSize, 0.01f, 0.25f);

            GUILayout.Label("Color");
            currentColor = Optionable<float>.Some(GUILayout.HorizontalSlider(currentColor.Value, 0f, 1));

            GUILayout.Label("Text");
            currentText = GUILayout.TextField(currentText, 3);

            if (GUILayout.Button("Apply"))
            {
                localPlayer.SetAllFaceSettings(currentColor.Value, 0, currentText, currentRotation, currentSize);
                localPlayer.SaveFaceToPlayerPrefs();
            }


            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        #endregion
    }

    [HarmonyPatch(typeof(PlayerVisor), "Start")]
    class PatchPlayerVisorStart
    {
        public static void Postfix(PlayerVisor __instance)
        {
            if (!Plugin.originalFont)
            {
                Plugin.originalFont = __instance.visorFaceText.font;
                Plugin.originalFont.fallbackFontAssetTable.Add(Plugin.fallbackFont);
            }

            __instance.visorFaceText.font = Plugin.originalFont;
            __instance.visorFaceText.UpdateFontAsset();
            __instance.visorFaceText.spriteAsset = Plugin.emojiSprites;
        }
    }
}
