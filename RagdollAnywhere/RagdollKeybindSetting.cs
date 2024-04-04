using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Zorro.Settings;

namespace RagdollAnywhere
{
    public class RagdollKeybindSetting : KeyCodeSetting, IExposedSetting
    {
        protected override KeyCode GetDefaultKey()
        {
            return KeyCode.X;
        }

        public SettingCategory GetSettingCategory()
        {
            return SettingCategory.Controls;
        }

        public string GetDisplayName()
        {
            return "Ragdoll";
        }
    }
}
