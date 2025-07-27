using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Autofish;

public class ClickFishingItem : GlobalItem
{
    public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
        if (item.fishingPole > 0) {
            var configuration = AutofishPlayer.Configuration;
            var autoState = Language.GetTextValue("Mods.Autofish.Tips.AutoFishingState");
            var lockState = Language.GetTextValue("Mods.Autofish.Tips.TargetLockState");
            autoState += GetSwitchState(configuration.AutoCatch);
            lockState += GetSwitchState(configuration.AutoLockCast);
            tooltips.Add(new TooltipLine(Mod, "State", autoState) { OverrideColor = Color.Pink });
            tooltips.Add(new TooltipLine(Mod, "LockState", lockState) { OverrideColor = Color.Pink });
        }
    }

    public string GetSwitchState(bool state) {
        return Language.GetTextValue(state
            ? "Mods.Autofish.Tips.Enable"
            : "Mods.Autofish.Tips.Disable");
    }

    public void ClickItem() {
        var configuration = AutofishPlayer.Configuration;
        if (Autofish.SwitchAutoFishingState.JustPressed) configuration.AutoCatch = !configuration.AutoCatch;
        if (Autofish.LockcastDirectionKeybind.JustPressed) configuration.AutoLockCast = !configuration.AutoLockCast;
    }
}