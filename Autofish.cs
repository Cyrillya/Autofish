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
        internal static Configuration Configuration;
        public static bool[] IsFishingCrate = ItemID.Sets.Factory.CreateBoolSet(2334, 2335, 2336, 3203, 3204, 3205, 3206, 3207, 3208);
        public static ModHotKey AutocastKeybind;
        public static ModHotKey LockcastDirectionKeybind;

        public override void Load() {
            IL.Terraria.Projectile.FishingCheck += Projectile_FishingCheck;
            AutocastKeybind = RegisterHotKey("Toggle Auto Cast Fishing Pole", "Y");
            LockcastDirectionKeybind = RegisterHotKey("Lock Casting Target To Cursor", "L");
        }

        public override void Unload() {
            AutocastKeybind = null;
            LockcastDirectionKeybind = null;
            IL.Terraria.Projectile.FishingCheck -= Projectile_FishingCheck;
        }

        private void Projectile_FishingCheck(ILContext il) {
            /* 这里1.3和1.4的IL码是不一样的
             * IL_0FEE: ldloc.s V_7
             * IL_0FF0: ldloc.s V_14
             * IL_0FF2: ldloc.s V_26
             * IL_0FF4: ldloca.s V_13
             * IL_0FF6: ldloca.s V_27
             * IL_0FF8: call      void Terraria.ModLoader.PlayerHooks::CatchFish(class Terraria.Player, class Terraria.Item, int32, int32, int32, int32, int32, int32&, bool&)
             * IL_0FFD: ldloc.s V_13
             * 检测是否大于0，然后改PullTimer
             */

            ILCursor iLCursor = new ILCursor(il);
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 7));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 14));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 26));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloca(i, 13));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloca(i, 27));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchCall(i, typeof(PlayerHooks), "CatchFish"));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 13));
            iLCursor.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            iLCursor.EmitDelegate<Func<int, Projectile, int>>((caughtType, projectile) => {
                if (!Main.player.IndexInRange(projectile.owner))
                    return caughtType;

                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (player.PullTimer == 0 && caughtType > 0) {
                    var item = new Item();
                    item.SetDefaults(caughtType);

                    int fishType = 0; // 0 for normal
                    if (IsFishingCrate.IndexInRange(caughtType) && IsFishingCrate[caughtType]) fishType = 1; // 1 for vanilla crates
                    if (item.accessory) fishType = 2; // 2 for accessories
                    if (item.damage > 0) fishType = 3; // 3 for weapons and tools
                    if (item.questItem) fishType = 4; // 4 for quests
                    if (fishType == 0 && item.rare <= ItemRarityID.White) fishType = 5; // 5 for wastes

                    switch (fishType) {
                        case 1:
                            if (Configuration.CatchCrates) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                        case 2:
                            if (Configuration.CatchAccessories) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                        case 3:
                            if (Configuration.CatchTools) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                        case 4:
                            if (Configuration.CatchQuestFishes) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                        case 5:
                            if (Configuration.CatchWhiteRarityCatches) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                        default:
                            if (Configuration.CatchNormalCatches) player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                            break;
                    }
                }
                return caughtType; // 怎么来的怎么走
            });
        }
    }

    [Label("$Mods.Autofish.Configs.Title")]
    public class Configuration : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        public override void OnLoaded() => Autofish.Configuration = this;

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

        [Label("$Mods.Autofish.Configs.CatchWhiteRarityCatchesLabel")]
        [Tooltip("$Mods.Autofish.Configs.CatchWhiteRarityCatchesTooltip")]
        [DefaultValue(false)]
        public bool CatchWhiteRarityCatches;

        [Label("$Mods.Autofish.Configs.CatchNormalCatchesLabel")]
        [Tooltip("$Mods.Autofish.Configs.CatchNormalCatchesTooltip")]
        [DefaultValue(true)]
        public bool CatchNormalCatches;
    }
}