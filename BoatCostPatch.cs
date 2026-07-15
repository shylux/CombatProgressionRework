using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;

namespace CombatProgressionRework;

internal static class BoatCost
{
    public const string HardwoodId = "(O)709";
    public const string IridiumBarId = "(O)337";
    public const string BatteryPackId = "(O)787";

    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;
}

/**
 * Replaces the vanilla boat repair interactions (200 hardwood, 5 iridium bars,
 * 5 battery packs) with the configured amounts.
 */
[HarmonyPatch(typeof(BoatTunnel), nameof(BoatTunnel.checkAction))]
internal static class BoatCostCheckActionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(BoatTunnel __instance, Location tileLocation, Farmer who, ref bool __result)
    {
        try
        {
            if (__instance.doesTileHaveProperty(tileLocation.X, tileLocation.Y, "Action", "Buildings") == "BoatTicket")
            {
                if (Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatTicketMachine"))
                    return true; // machine is fixed, let vanilla handle the ticket purchase

                if (who.Items.ContainsId(BoatCost.BatteryPackId, BoatCost.Config.BoatBatteryPackCost))
                    __instance.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateBatteries"), __instance.createYesNoResponses(), "WillyBoatDonateBatteries");
                else
                    Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateBatteriesHint"));
                __result = true;
                return false;
            }

            if (!Game1.MasterPlayer.mailReceived.Contains("willyBoatFixed"))
            {
                if (tileLocation.X == 6 && tileLocation.Y == 8 && !Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatHull"))
                {
                    if (who.Items.ContainsId(BoatCost.HardwoodId, BoatCost.Config.BoatHardwoodCost))
                        __instance.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateHardwood"), __instance.createYesNoResponses(), "WillyBoatDonateHardwood");
                    else
                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateHardwoodHint"));
                    __result = true;
                    return false;
                }
                if (tileLocation.X == 8 && tileLocation.Y == 10 && !Game1.MasterPlayer.hasOrWillReceiveMail("willyBoatAnchor"))
                {
                    if (who.Items.ContainsId(BoatCost.IridiumBarId, BoatCost.Config.BoatIridiumBarCost))
                        __instance.createQuestionDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateIridium"), __instance.createYesNoResponses(), "WillyBoatDonateIridium");
                    else
                        Game1.drawObjectDialogue(Game1.content.LoadString("Strings\\Locations:BoatTunnel_DonateIridiumHint"));
                    __result = true;
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            BoatCost.Monitor.Log($"Failed in {nameof(BoatCostCheckActionPatch)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }
}

[HarmonyPatch(typeof(BoatTunnel), nameof(BoatTunnel.answerDialogue))]
internal static class BoatCostAnswerDialoguePatch
{
    [HarmonyPrefix]
    private static bool Prefix(BoatTunnel __instance, Response answer, ref bool __result)
    {
        try
        {
            if (__instance.lastQuestionKey == null || __instance.afterQuestion != null)
                return true;

            switch (ArgUtility.SplitBySpaceAndGet(__instance.lastQuestionKey, 0) + "_" + answer.responseKey)
            {
                case "WillyBoatDonateBatteries_Yes":
                    Game1.Multiplayer.globalChatInfoMessage("RepairBoatMachine", Game1.player.Name);
                    Game1.player.Items.ReduceId(BoatCost.BatteryPackId, BoatCost.Config.BoatBatteryPackCost);
                    DelayedAction.playSoundAfterDelay("openBox", 600);
                    Game1.addMailForTomorrow("willyBoatTicketMachine", noLetter: true, sendToEveryone: true);
                    CheckForBoatComplete(__instance);
                    __result = true;
                    return false;
                case "WillyBoatDonateHardwood_Yes":
                    Game1.Multiplayer.globalChatInfoMessage("RepairBoatHull", Game1.player.Name);
                    Game1.player.Items.ReduceId(BoatCost.HardwoodId, BoatCost.Config.BoatHardwoodCost);
                    DelayedAction.playSoundAfterDelay("Ship", 600);
                    Game1.addMailForTomorrow("willyBoatHull", noLetter: true, sendToEveryone: true);
                    CheckForBoatComplete(__instance);
                    __result = true;
                    return false;
                case "WillyBoatDonateIridium_Yes":
                    Game1.Multiplayer.globalChatInfoMessage("RepairBoatAnchor", Game1.player.Name);
                    Game1.player.Items.ReduceId(BoatCost.IridiumBarId, BoatCost.Config.BoatIridiumBarCost);
                    DelayedAction.playSoundAfterDelay("clank", 600);
                    DelayedAction.playSoundAfterDelay("clank", 1200);
                    DelayedAction.playSoundAfterDelay("clank", 1800);
                    Game1.addMailForTomorrow("willyBoatAnchor", noLetter: true, sendToEveryone: true);
                    CheckForBoatComplete(__instance);
                    __result = true;
                    return false;
                default:
                    return true;
            }
        }
        catch (Exception ex)
        {
            BoatCost.Monitor.Log($"Failed in {nameof(BoatCostAnswerDialoguePatch)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }

    private static void CheckForBoatComplete(BoatTunnel tunnel)
    {
        AccessTools.Method(typeof(BoatTunnel), "checkForBoatComplete").Invoke(tunnel, null);
    }
}
