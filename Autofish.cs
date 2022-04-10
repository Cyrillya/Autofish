using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.ComponentModel;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Autofish
{
    public class Autofish : Mod
    {
        public static ModHotKey AutocastKeybind;
        public static ModHotKey LockcastDirectionKeybind;

        public override void Load() {
            AutocastKeybind = RegisterHotKey("Toggle Auto Cast Fishing Pole", "Y");
            LockcastDirectionKeybind = RegisterHotKey("Lock Casting Target To Cursor", "L");
        }

        public override void Unload() {
            AutocastKeybind = null;
            LockcastDirectionKeybind = null;
        }
    }

    [Label("$Mods.Autofish.Configs.Title")]
    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => AutofishPlayer.Configuration = this;

        [Label("$Mods.Autofish.Configs.PullingDelayLabel")]
        [Tooltip("$Mods.Autofish.Configs.PullingDelayTooltip")]
        [Range(0f, 1.5f)]
        [Increment(.1f)]
        [DefaultValue(.5f)]
        public float PullingDelay;

        [Label("$Mods.Autofish.Configs.CatchCrates")]
        [DefaultValue(true)]
        public bool CatchCrates;

        [Label("$Mods.Autofish.Configs.CatchAccessories")]
        [DefaultValue(true)]
        public bool CatchAccessories;

        [Label("$Mods.Autofish.Configs.CatchTools")]
        [DefaultValue(true)]
        public bool CatchTools;

        [Label("$Mods.Autofish.Configs.CatchQuestFishes")]
        [DefaultValue(true)]
        public bool CatchQuestFishes;

        [Label("$Mods.Autofish.Configs.CatchNormalCatches")]
        [DefaultValue(true)]
        public bool CatchNormalCatches;

        [Label("$Mods.Autofish.Configs.CatchJunks")]
        [DefaultValue(false)]
        public bool CatchJunks;
    }
}