using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace CombatProgressionRework;

internal static class GalaxySword
{
    public const string MailFlag = "CPR_GalaxyWorthy";
    public const string QuestId = "CPR_GalaxyQuest";
    public const int RequiredSkullCavernLevel = 50;

    public static IMonitor Monitor = null!;
    public static ITranslationHelper Translations = null!;
}

/**
 * Blocks the galaxy sword at the desert pillars until the player has proven
 * themselves by reaching skull cavern level 50 without using staircases.
 */
[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performTouchAction), typeof(string[]), typeof(Vector2))]
internal static class GalaxySwordTouchActionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(string[] action)
    {
        try
        {
            if (action.Length == 0 || action[0] != "legendarySword"
                || Game1.player.ActiveObject?.QualifiedItemId != "(O)74"
                || Game1.player.mailReceived.Contains("galaxySword")
                || Game1.player.mailReceived.Contains(GalaxySword.MailFlag))
                return true;

            Game1.drawObjectDialogue(GalaxySword.Translations.Get("galaxy.not-worthy"));
            if (!Game1.player.hasQuest(GalaxySword.QuestId))
                Game1.player.addQuest(GalaxySword.QuestId);
            return false;
        }
        catch (Exception ex)
        {
            GalaxySword.Monitor.Log($"Failed in {nameof(GalaxySwordTouchActionPatch)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }
}
