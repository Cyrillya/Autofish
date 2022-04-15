using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Autofish
{
    public class Autofish : Mod
    {
        public static ModKeybind AutocastKeybind;
        public static ModKeybind LockcastDirectionKeybind;

        public override void Load() {
            LockcastDirectionKeybind = KeybindLoader.RegisterKeybind(this, "Lock Casting Target To Cursor", "L");
        }

        public override void Unload() {
            LockcastDirectionKeybind = null;
        }
    }

    [Label("$Mods.Autofish.Config.Label")]
    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => AutofishPlayer.Configuration = this;

        [Label("$Mods.Autofish.Config.PullingDelay.Label")]
        [Tooltip("$Mods.Autofish.Config.PullingDelay.Tooltip")]
        [Range(0f, 1.5f)]
        [Increment(.1f)]
        [DefaultValue(.5f)]
        public float PullingDelay;

        [Label("$Mods.Autofish.Config.CatchCrates")]
        [DefaultValue(true)]
        public bool CatchCrates;

        [Label("$Mods.Autofish.Config.CatchAccessories")]
        [DefaultValue(true)]
        public bool CatchAccessories;

        [Label("$Mods.Autofish.Config.CatchTools")]
        [DefaultValue(true)]
        public bool CatchTools;

        [Label("$Mods.Autofish.Config.CatchQuestFishes")]
        [DefaultValue(true)]
        public bool CatchQuestFishes;

        [Label("$Mods.Autofish.Config.CatchWhiteRarityCatches.Label")]
        [Tooltip("$Mods.Autofish.Config.CatchWhiteRarityCatches.Tooltip")]
        [DefaultValue(false)]
        public bool CatchWhiteRarityCatches;

        [Label("$Mods.Autofish.Config.CatchNormalCatches.Label")]
        [Tooltip("$Mods.Autofish.Config.CatchNormalCatches.Tooltip")]
        [DefaultValue(true)]
        public bool CatchNormalCatches;

        [Label("$Mods.Autofish.Config.CatchEnemies")]
        [DefaultValue(true)]
        public bool CatchEnemies;
    }
}