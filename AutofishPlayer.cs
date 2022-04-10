using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;

namespace Autofish
{
    public class AutofishPlayer : ModPlayer
    {
        public static bool[] IsFishingCrate = ItemID.Sets.Factory.CreateBoolSet(2334, 2335, 2336, 3203, 3204, 3205, 3206, 3207, 3208);
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

        public override void CatchFish(Item fishingRod, Item bait, int power, int liquidType, int poolSize, int worldLayer, int questFish, ref int caughtType, ref bool junk) {
            if (caughtType > 0) {
                PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
            }
            if (PullTimer == 0 && caughtType > 0) {
                if ((IsFishingCrate[caughtType] && ModContent.GetInstance<Configuration>().CatchCrates)
                    || (bait.accessory && ModContent.GetInstance<Configuration>().CatchAccessories)
                    || (bait.damage > 0 && ModContent.GetInstance<Configuration>().CatchTools)
                    || (bait.questItem && ModContent.GetInstance<Configuration>().CatchQuestFishes))
                    PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);

                if (!IsFishingCrate[caughtType] && !bait.accessory && bait.damage <= 0 && !bait.questItem && ModContent.GetInstance<Configuration>().CatchNormalCatches)
                    PullTimer = (int)(ModContent.GetInstance<Configuration>().PullingDelay * 60 + 1);
            }
        }

        public override void PreUpdate() {
            if (PullTimer > 0) {
                PullTimer--;
                if (PullTimer == 0) {
                    player.controlUseItem = true;
                    player.releaseUseItem = true;
                    player.ItemCheck(player.selectedItem);
                }
            }
            if (Autocast) {
                AutocastDelay--;
                if (player.HeldItem.fishingPole == 0 || AutocastDelay > 0) {
                    return;
                }
                for (int i = 0; i < 1000; i++) {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.owner == player.whoAmI && projectile.bobber) {
                        return;
                    }
                }

                if (Lockcast) {
                    Main.mouseX = CastPosition.X - (int)Main.screenPosition.X;
                    Main.mouseY = CastPosition.Y - (int)Main.screenPosition.Y;
                }

                player.controlUseItem = true;
                player.releaseUseItem = true;
                player.ItemCheck(player.selectedItem); // 1.3进行ItemCheck即可抛竿，不需再NewProj
                AutocastDelay = 10;
            }
        }

        public override void OnEnterWorld(Player player) {
            Lockcast = false;
            CastPosition = default;
            Autocast = false;
            base.OnEnterWorld(player);
        }
    }
}
