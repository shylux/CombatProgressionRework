using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace CombatProgressionRework;

internal static class XpRequirement
{
    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;
}

/**
 * Reduces the XP required beyond level 5 for skill levels 6-10 by the
 * configured modifier, e.g. with 0.6 level 10 needs 9860 XP instead of 15000.
 */
[HarmonyPatch(typeof(Farmer), nameof(Farmer.getBaseExperienceForLevel))]
internal static class XpRequirementPatch
{
    private const int Level5Xp = 2150;

    [HarmonyPostfix]
    private static void Postfix(int level, ref int __result)
    {
        try
        {
            // At modifier 1 leave the result untouched to avoid conflicts
            // with other mods patching this method
            var modifier = XpRequirement.Config.XpRequirementModifier;
            if (modifier == 1f)
                return;

            if (level >= 6 && level <= 10 && __result > 0)
                __result = Level5Xp + (int)Math.Round((__result - Level5Xp) * modifier);
        }
        catch (Exception ex)
        {
            XpRequirement.Monitor.Log($"Failed in {nameof(XpRequirementPatch)}:\n{ex}", LogLevel.Error);
        }
    }
}
