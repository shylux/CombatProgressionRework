using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Menus;

namespace CombatProgressionRework;

internal static class Mastery
{
    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;
}

/**
 * Makes the first mastery free: as soon as the mastery cave is unlocked the
 * player can claim one mastery immediately. Each further mastery costs what
 * the previous one costs in vanilla (2nd: 10,000 XP ... 5th: 70,000 XP).
 *
 * Implemented by shifting the requested level down by one so the vanilla
 * XP table returns the previous rank's cost. All mastery logic (claim
 * buttons, progress bar, level-up toast) goes through this method.
 */
[HarmonyPatch(typeof(MasteryTrackerMenu), nameof(MasteryTrackerMenu.getMasteryExpNeededForLevel))]
internal static class MasteryExpPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref int level)
    {
        try
        {
            if (Mastery.Config.FreeFirstMastery && level >= 1)
                level--;
        }
        catch (Exception ex)
        {
            Mastery.Monitor.Log($"Failed in {nameof(MasteryExpPatch)}:\n{ex}", LogLevel.Error);
        }
    }
}
