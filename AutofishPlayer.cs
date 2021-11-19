using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using Terraria;
using Terraria.GameInput;
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

        public override void ProcessTriggers(TriggersSet triggersSet)
        {
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

        public override void PreUpdate()
        {
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

        public override void OnEnterWorld(Player player)
        {
            Lockcast = false;
            CastPosition = default;
            Autocast = false;
            base.OnEnterWorld(player);
        }

        public override void Load()
        {
            IL.Terraria.Projectile.FishingCheck += Projectile_FishingCheck;
            base.Load();
        }

        public override void Unload()
        {
            IL.Terraria.Projectile.FishingCheck -= Projectile_FishingCheck;
            base.Unload();
        }

        // 用PlayerLoader的话可能会存在因Mod加载顺序不同而出现冲突的Bug
        private void Projectile_FishingCheck(ILContext il)
        {
            // 流程：定位到Roll完渔获后 -> 检查有没有成功(bool V_24 = fisher.rolledItemDrop > 0) -> 决定是否拉钩
            ILCursor iLCursor = new ILCursor(il);
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdcI4(i, 0));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchCgt(i));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 24)); // 推入V_24后
            iLCursor.Emit(OpCodes.Ldarg_0); // 推入当前Projectile实例
            iLCursor.EmitDelegate<Func<bool, Projectile, bool>>((returnValue, projectile) => {
                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (returnValue == true && CatchNonEnemies && player.PullTimer == 0) {
                    player.PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
                }
                return returnValue; // 怎么来的怎么走
            });

            // 钓出怪物的代码，原理和上方都一样，只不过是V_28
            iLCursor = new ILCursor(il);
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdcI4(i, 0));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchCgt(i));
            iLCursor.GotoNext(MoveType.After, (Instruction i) => ILPatternMatchingExt.MatchLdloc(i, 28));
            iLCursor.Emit(OpCodes.Ldarg_0);
            iLCursor.EmitDelegate<Func<bool, Projectile, bool>>((returnValue, projectile) => {
                var player = Main.player[projectile.owner].GetModPlayer<AutofishPlayer>();
                if (returnValue == true && CatchEnemies && player.PullTimer == 0) {
                    player.PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
                }
                return returnValue;
            });
        }

        private bool CatchEnemies => ModContent.GetInstance<Configuration>().AutoCatchMode == "[c/22CC22:Catch All]" || ModContent.GetInstance<Configuration>().AutoCatchMode == "[c/22CC22:Only Catch Enemies]";
        private bool CatchNonEnemies => ModContent.GetInstance<Configuration>().AutoCatchMode == "[c/22CC22:Catch All]" || ModContent.GetInstance<Configuration>().AutoCatchMode == "[c/22CC22:Only Catch Non-Enemies]";
    }
}
