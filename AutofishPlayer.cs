using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace Autofish
{
    public class AutofishPlayer : ModPlayer
    {
        internal bool Lockcast = false;
        internal Point CastPosition;
        internal int PullTimer = 0;
        internal bool Autocast = false;
        internal int AutocastDelay = 0;

        public override void ProcessTriggers(TriggersSet triggersSet) {
            if (Autofish.AutocastKeybind.JustPressed) {
                Autocast = !Autocast;
                if (Autocast) {
                    Main.NewText("Auto cast bobbers are now [c/22CC22:activated].");
                    return;
                }
                Main.NewText("Auto cast bobbers are now [c/BB2222:deactivated].");
            }
            if (Autofish.LockcastDirectionKeybind.JustPressed) {
                Lockcast = !Lockcast;
                if (Lockcast) {
                    CastPosition = Main.MouseWorld.ToPoint();
                    Main.NewText("Casting target are now [c/22CC22:locked] to current cursor position.");
                    return;
                }
                Main.NewText("Casting target are now [c/BB2222:unlocked].");
            }
        }

        public override void PreUpdate() {
            if (PullTimer > 0) {
                PullTimer--;
                if (PullTimer == 0) {
                    Player.controlUseItem = true;
                    Player.releaseUseItem = true;
                    Player.ItemCheck(Player.selectedItem);
                }
            }
            if (Autocast) {
                AutocastDelay--;
                if (Player.HeldItem.fishingPole == 0 || AutocastDelay > 0) {
                    return;
                }
                for (int i = 0; i < 1000; i++) {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.owner == Player.whoAmI && projectile.bobber) {
                        return;
                    }
                }

                var mouseX = Main.mouseX; var mouseY = Main.mouseY;
                if (Lockcast) {
                    Main.mouseX = CastPosition.X - (int)Main.screenPosition.X;
                    Main.mouseY = CastPosition.Y - (int)Main.screenPosition.Y;
                }

                var item = Player.inventory[Player.selectedItem];
                Player.controlUseItem = true;
                Player.releaseUseItem = true;
                Player.ItemCheck(Player.selectedItem); // casting animation
                if (CombinedHooks.CanShoot(Player, item)) {
                    Projectile.NewProjectile(Player.GetProjectileSource_Item(Player.HeldItem), Player.Center, Vector2.Normalize(Main.MouseWorld - Player.Center) * Player.HeldItem.shootSpeed, Player.HeldItem.shoot, 0, 0f, Player.whoAmI);
                }
                AutocastDelay = 10;

                if (Lockcast) { Main.mouseX = mouseX; Main.mouseY = mouseY; }
            }
        }

        public override void OnEnterWorld(Player player) {
            Lockcast = false;
            CastPosition = default;
            Autocast = false;
            base.OnEnterWorld(player);
        }

        public override void Load() {
            IL.Terraria.Projectile.FishingCheck += Projectile_FishingCheck;
            base.Load();
        }

        // 用PlayerLoader的话可能会存在因Mod加载顺序不同而出现冲突的Bug
        private void Projectile_FishingCheck(ILContext il) {
            // 流程：检查有没有成功(fisher.rolledItemDrop > 0) -> 决定是否拉钩
            ILCursor c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(typeof(FishingAttempt), nameof(FishingAttempt.rolledItemDrop))))
                throw new Exception("Hook location not found, if (fisher.rolledItemDrop > 0)");
            c.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            c.EmitDelegate<Func<int, Projectile, int>>((returnValue, projectile) => {
                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (player.PullTimer == 0 && returnValue > 0) {
                    var item = new Item();
                    item.SetDefaults(returnValue);
                    Main.NewText($"[i:{returnValue}]");

                    if ((ItemID.Sets.IsFishingCrate[returnValue] && ModContent.GetInstance<Configuration>().CatchCrates)
                        || (item.accessory && ModContent.GetInstance<Configuration>().CatchAccessories)
                        || (item.damage > 0 && ModContent.GetInstance<Configuration>().CatchTools)
                        || (item.questItem && ModContent.GetInstance<Configuration>().CatchQuestFishes))
                        player.PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);

                    if (!ItemID.Sets.IsFishingCrate[returnValue] && !item.accessory && item.damage <= 0 && !item.questItem && ModContent.GetInstance<Configuration>().CatchNormalCatches)
                        player.PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
                }
                return returnValue; // 怎么来的怎么走
            });

            // 钓出怪物的代码，原理和上方都一样
            c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchLdfld(typeof(FishingAttempt), nameof(FishingAttempt.rolledEnemySpawn))))
                throw new Exception("Hook location not found, if (fisher.rolledEnemySpawn > 0)");
            c.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            c.EmitDelegate<Func<int, Projectile, int>>((returnValue, projectile) => {
                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (returnValue > 0 && ModContent.GetInstance<Configuration>().CatchEnemies && player.PullTimer == 0) {
                    player.PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
                }
                return returnValue; // 怎么来的怎么走
            });
        }
    }
}
