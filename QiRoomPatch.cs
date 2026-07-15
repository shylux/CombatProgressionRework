using HarmonyLib;
using StardewModdingAPI;
using StardewValley.Locations;

namespace CombatProgressionRework;

internal static class QiRoom
{
    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;
}

/**
 * Opens Mr. Qi's walnut room on Ginger Island immediately instead of
 * requiring 100 golden walnuts.
 */
[HarmonyPatch(typeof(IslandWest), nameof(IslandWest.IsQiWalnutRoomDoorUnlocked))]
internal static class QiRoomDoorPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        try
        {
            if (QiRoom.Config.UnlockQiRoom)
                __result = true;
        }
        catch (Exception ex)
        {
            QiRoom.Monitor.Log($"Failed in {nameof(QiRoomDoorPatch)}:\n{ex}", LogLevel.Error);
        }
    }
}
