using System.Collections.Generic;
using System.ComponentModel;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Autofish
{
    public class Autofish : Mod
    {
        public static ModKeybind LockcastDirectionKeybind;

        public override void Load() {
            LockcastDirectionKeybind = KeybindLoader.RegisterKeybind(this, "LockcastDirection", "L");
        }

        public override void Unload() {
            LockcastDirectionKeybind = null;
        }
    }

    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => AutofishPlayer.Configuration = this;

        [DefaultValue(true)]
        public bool AutoCatch;

        [Range(0f, 1.5f)]
        [Increment(.1f)]
        [DefaultValue(.5f)]
        public float PullingDelay;

        [Header("Filters")]

        [DefaultValue(true)]
        public bool CatchCrates;

        [DefaultValue(true)]
        public bool CatchAccessories;

        [DefaultValue(true)]
        public bool CatchTools;

        [DefaultValue(true)]
        public bool CatchQuestFishes;

        [DefaultValue(false)]
        public bool CatchWhiteRarityCatches;

        [DefaultValue(true)]
        public bool CatchNormalCatches;

        [DefaultValue(true)]
        public bool CatchEnemies;

        public List<ItemDefinition> OtherCatches;

        public List<ItemDefinition> Blacklist;

        [Header("Utilities")]

        [DefaultValue(false)]
        public bool AutoSonar;

        [DefaultValue(false)]
        public bool AutoFishing;

        [DefaultValue(false)]
        public bool AutoCrate;
    }
}