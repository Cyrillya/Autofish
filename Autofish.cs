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

        [Label("$Mods.Autofish.Config.AutoPullBobbers.Label")]
        [Tooltip("$Mods.Autofish.Config.AutoPullBobbers.Tooltip")]
        [DefaultValue(true)]
        public bool AutoPullBobbers;

        [Label("$Mods.Autofish.Config.AutofishEnemies.Label")]
        [Tooltip("$Mods.Autofish.Config.AutofishEnemies.Tooltip")]
        [DefaultValue(false)]
        public bool AutofishEnemies;
    }
}