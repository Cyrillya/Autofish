﻿using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;

namespace Autofish
{
    public class AutofishPlayer : ModPlayer
    {
        internal static Configuration Configuration;
        internal bool Lockcast;
        internal Point CastPosition;
        internal int PullTimer;
        internal bool ActivatedByMod; // check if this item use is activated by Mod
        internal bool Autocast;
        internal int AutocastDelay;

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (Autofish.LockcastDirectionKeybind.JustPressed) {
                Lockcast = !Lockcast;
                if (Lockcast) {
                    CastPosition = Main.MouseWorld.ToPoint();
                    Main.NewText(Language.GetTextValue("Mods.Autofish.Tips.TargetLock"));
                    return;
                }
                Main.NewText(Language.GetTextValue("Mods.Autofish.Tips.TargetUnlock"));
            }
        }

        public override void PreUpdate() {
            if (Player.whoAmI != Main.myPlayer)
                return;

            ActivatedByMod = false;
            if (PullTimer > 0) {
                PullTimer--;
                if (PullTimer == 0) {
                    Player.controlUseItem = true;
                    Player.releaseUseItem = true;
                    ActivatedByMod = true;
                    Player.ItemCheck();
                }
            }

            if (Autocast) {
                AutocastDelay--;
                if (Player.HeldItem.fishingPole == 0) {
                    Autocast = false; // 当前物品不是鱼竿，关闭自动抛竿
                    return;
                }
                if (AutocastDelay > 0 || CheckBobbersActive(Player.whoAmI)) {
                    return;
                }

                var mouseX = Main.mouseX; var mouseY = Main.mouseY;
                if (Lockcast) {
                    Main.mouseX = CastPosition.X - (int)Main.screenPosition.X;
                    Main.mouseY = CastPosition.Y - (int)Main.screenPosition.Y;
                }

                Player.controlUseItem = true;
                Player.releaseUseItem = true;
                ActivatedByMod = true;
                Player.ItemCheck();
                AutocastDelay = 10;

                if (Lockcast) { Main.mouseX = mouseX; Main.mouseY = mouseY; }
            }
        }

        public static bool CheckBobbersActive(int whoAmI) {
            foreach (var _ in from p in Main.projectile where p.active && p.owner == whoAmI && p.bobber select p) {
                return true;
            }
            return false;
        }

        public override void OnEnterWorld() {
            Lockcast = false;
            CastPosition = default;
            Autocast = false;
        }

        public override void Load() {
            On_Player.ItemCheck_CheckFishingBobbers += Player_ItemCheck_CheckFishingBobbers;
            On_Player.ItemCheck_Shoot += Player_ItemCheck_Shoot;
            IL_Projectile.FishingCheck += Projectile_FishingCheck;
        }

        public override void Unload() {
            Configuration = null;
        }

        private bool Player_ItemCheck_CheckFishingBobbers(On_Player.orig_ItemCheck_CheckFishingBobbers orig, Player player, bool canUse) {
            // 只有当执行收杆动作，且是玩家执行的时，才会关闭效果
            // 只有whoAmI=myPlayer才会执行这里，所以不需要判断
            bool flag = orig.Invoke(player, canUse); // 返回值若为false，则是拉杆
            if (!flag && player.whoAmI == Main.myPlayer && player.TryGetModPlayer(out AutofishPlayer modPlayer) && !modPlayer.ActivatedByMod) {
                modPlayer.Autocast = false;
            }
            return flag;
        }

        // 注意：收杆根本不会执行这个方法
        private void Player_ItemCheck_Shoot(On_Player.orig_ItemCheck_Shoot orig, Player player, int i, Item sItem, int weaponDamage) {
            // 只有当执行抛竿动作，且是玩家执行的时，才会开启效果
            if (player.whoAmI == Main.myPlayer && player.TryGetModPlayer(out AutofishPlayer modPlayer) && !modPlayer.ActivatedByMod && sItem.fishingPole > 0) {
                modPlayer.Autocast = true;
            }
            orig.Invoke(player, i, sItem, weaponDamage);
        }

        // 用PlayerLoader的话可能会存在因Mod加载顺序不同而出现冲突的Bug
        private void Projectile_FishingCheck(ILContext il) {
            // 流程：检查有没有成功(fisher.rolledItemDrop > 0) -> 决定是否拉钩
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(typeof(FishingAttempt), nameof(FishingAttempt.rolledItemDrop))))
                throw new Exception("Hook location not found, if (fisher.rolledItemDrop > 0)");
            c.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            c.EmitDelegate<Func<int, Projectile, int>>((caughtType, projectile) => {
                if (projectile.owner != Main.myPlayer || !Main.player[projectile.owner].active || Main.player[projectile.owner].dead || !Configuration.AutoCatch)
                    return caughtType;

                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (player.PullTimer == 0 && caughtType > 0) {
                    if (!Main.player[projectile.owner].sonarPotion) {
                        player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                        return caughtType; // 没有声纳药水都是无脑拉
                    }

                    ItemDefinition itemDefinition = new(caughtType);

                    if (Configuration.Blacklist is not null && Configuration.Blacklist.Contains(itemDefinition)) {
                        return caughtType; // 黑名单里面的不拉
                    }

                    if (Configuration.OtherCatches is not null && Configuration.OtherCatches.Contains(itemDefinition)) {
                        player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                        return caughtType; // 额外清单里包含直接拉
                    }

                    var item = new Item();
                    item.SetDefaults(caughtType);

                    int fishType = 0; // 0 for normal
                    if (ItemID.Sets.IsFishingCrate[caughtType]) fishType = 1; // 1 for vanilla crates
                    if (item.accessory) fishType = 2; // 2 for accessories
                    if (item.damage > 0) fishType = 3; // 3 for weapons and tools
                    if (item.questItem) fishType = 4; // 4 for quests
                    if (fishType == 0 && item.OriginalRarity <= ItemRarityID.White) fishType = 5; // 5 for wastes

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

            // 钓出怪物的代码，原理和上方都一样
            c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(typeof(FishingAttempt), nameof(FishingAttempt.rolledEnemySpawn))))
                throw new Exception("Hook location not found, if (fisher.rolledEnemySpawn > 0)");
            c.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            c.EmitDelegate<Func<int, Projectile, int>>((caughtType, projectile) => {
                if (projectile.owner != Main.myPlayer || !Main.player[projectile.owner].active || Main.player[projectile.owner].dead)
                    return caughtType;

                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (caughtType > 0 && Configuration.CatchEnemies && player.PullTimer == 0) {
                    player.PullTimer = (int)(Configuration.PullingDelay * 60 + 1);
                }
                return caughtType; // 怎么来的怎么走
            });
        }
    }
}
