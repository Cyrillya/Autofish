using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Autofish
{
    internal class AutofishGlobalBuff : GlobalBuff
    {
        public static readonly List<int> FishingPotions = new() { ItemID.FishingPotion, ItemID.SonarPotion, ItemID.CratePotion };
        public static readonly List<int> FishingBuffs = new() { BuffID.Fishing, BuffID.Sonar, BuffID.Crate };

        public override void Update(int type, Player player, ref int buffIndex) {
            if (!FishingBuffs.Contains(type) || player.buffTime[buffIndex] != 1 || player.whoAmI != Main.myPlayer
                || (type == BuffID.Fishing && !AutofishPlayer.Configuration.AutoFishing)
                || (type == BuffID.Sonar && !AutofishPlayer.Configuration.AutoSonar)
                || (type == BuffID.Crate && !AutofishPlayer.Configuration.AutoCrate))
                return;

            SoundStyle? legacySoundStyle = null;
            if (player.CountBuffs() != Player.MaxBuffs) {
                int i;
                for (i = 0; i < player.inventory.Length - 1; i++) {
                    Item item = player.inventory[i];
                    if (item.stack <= 0 || item.buffType != type || !FishingPotions.Contains(item.type) ||
                        !CombinedHooks.CanUseItem(player, item))
                        continue;

                    ItemLoader.UseItem(item, player);
                    legacySoundStyle = item.UseSound;
                    int buffTime = item.buffTime;
                    if (buffTime == 0)
                        buffTime = 3600;

                    player.AddBuff(item.buffType, buffTime);
                    if (item.consumable && ItemLoader.ConsumeItem(item, player)) {
                        item.stack--;

                        if (item.stack <= 0)
                            item.TurnToAir();
                    }
                    break;
                }
                // 没有物品
                if (i == player.inventory.Length - 1) {
                    int potionIndex = FishingBuffs.FindIndex(i => i == type);
                    if (potionIndex is -1)
                        return;
                    int potionType = FishingPotions[potionIndex];
                    Main.NewText(Language.GetTextValue("Mods.Autofish.Tips.PotionsRanOut",
                        $"{Lang.GetItemNameValue(potionType)}[i:{potionType}]"), Color.OrangeRed);
                    return;
                }
            }

            if (legacySoundStyle is not null) {
                SoundEngine.PlaySound(legacySoundStyle.Value, player.position);
                Recipe.FindRecipes();
            }
        }
    }
}
