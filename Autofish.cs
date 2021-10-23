using System.ComponentModel;
using Terraria;
using Terraria.GameInput;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Autofish
{
	public class Autofish : Mod
    {
        public static ModKeybind AutocastKeybind;
        public static ModKeybind LockcastDirectionKeybind;

        public override void Load()
        {
            AutocastKeybind = KeybindLoader.RegisterKeybind(this, "Toggle Auto Cast Fishing Pole", "Y");
            LockcastDirectionKeybind = KeybindLoader.RegisterKeybind(this, "Lock Casting Target To Cursor", "L");
        }

        public override void Unload()
        {
            AutocastKeybind = null;
            LockcastDirectionKeybind = null;
        }
    }

    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;
        [Header("$Mods.Autofish.Config.Header")]

        [DrawTicks]
        [Label("$Mods.Autofish.Config.AutoCatchMode.Label")]
        [Tooltip("$Mods.Autofish.Config.AutoCatchMode.Tooltip")]
        [OptionStrings(new string[] { "[c/DD2222:Disabled]", "[c/22CC22:Only Catch Non-Enemies]", "[c/22CC22:Only Catch Enemies]", "[c/22CC22:Catch All]" })]
        [DefaultValue("[c/22CC22:Only Catch Non-Enemies]")]
        public string AutoCatchMode;

        [Label("$Mods.Autofish.Config.PullingDelay.Label")]
        [Tooltip("$Mods.Autofish.Config.PullingDelay.Tooltip")]
        [Range(0f, 1.5f)]
        [Increment(.1f)]
        [DefaultValue(.1f)]
        public float PullingDelay;
    }
}