using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Extensions;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Tools;

namespace CombatProgressionRework;

/**
 * Lets the player destroy the rock blocking the railroad path on the mountain
 * before the Summer 3 earthquake, using a bomb or a copper (or better)
 * pickaxe. Behaves like the boulder blocking the dwarf in the mines.
 *
 * The rock is not an object or map tile: Mountain draws it manually and
 * blocks its rectangle in isCollidingPosition based on a private NetBool,
 * so the patches flip that NetBool directly. The NetBool is re-initialized
 * from DaysPlayed on every save load, so a mail flag records the early
 * destruction and OnDayStarted clears the rock again.
 */
internal static class RailroadRock
{
    public const string MailFlag = "CPR_RailroadRockDestroyed";
    private const int PickaxeHitsRequired = 4;

    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;

    private static readonly AccessTools.FieldRef<Mountain, NetBool> BlockedRef =
        AccessTools.FieldRefAccess<Mountain, NetBool>("railroadAreaBlocked");
    private static readonly AccessTools.FieldRef<Mountain, Rectangle> RockRectRef =
        AccessTools.FieldRefAccess<Mountain, Rectangle>("railroadBlockRect");

    private static int pickaxeHits;

    public static bool IsBlocking(Mountain mountain) => BlockedRef(mountain).Value;

    public static Rectangle GetRockPixelRect(Mountain mountain) => RockRectRef(mountain);

    public static bool HandlePickaxeHit(Mountain mountain, Pickaxe pickaxe, int tileX, int tileY)
    {
        mountain.playSound("hammer", new Vector2(tileX, tileY));
        if (pickaxe.UpgradeLevel < 1)
        {
            Game1.drawObjectDialogue(Game1.parseText(
                Game1.content.LoadString("Strings\\StringsFromCSFiles:Pickaxe.cs.14194")));
            return false;
        }

        Game1.createRadialDebris(mountain, 14, tileX, tileY, Game1.random.Next(2, 5), resource: false);
        return ++pickaxeHits >= PickaxeHitsRequired;
    }

    public static void Destroy(Mountain mountain)
    {
        BlockedRef(mountain).Value = false;
        pickaxeHits = 0;
        Game1.addMail(MailFlag, noLetter: true, sendToEveryone: true);

        // Smoke puffs across the rock's area, like the dwarf boulder breaking
        mountain.playSound("boulderBreak");
        var rect = GetRockPixelRect(mountain);
        var sprites = new TemporaryAnimatedSpriteList();
        for (int x = rect.Left; x < rect.Right; x += 64)
            for (int y = rect.Top; y < rect.Bottom; y += 64)
                sprites.Add(new TemporaryAnimatedSprite(5, new Vector2(x, y), Color.Gray, 8, Game1.random.NextBool(), 50f)
                {
                    delayBeforeAnimationStart = Game1.random.Next(600)
                });
        Game1.Multiplayer.broadcastSprites(mountain, sprites);

        Monitor.Log("Railroad rock destroyed early.", LogLevel.Debug);
    }

    /**
     * Re-applies an early destruction after loading a save, since the
     * NetBool is re-initialized from DaysPlayed in Mountain's constructor.
     */
    public static void OnDayStarted(object? sender, StardewModdingAPI.Events.DayStartedEventArgs e)
    {
        // Party-wide mail can be recorded with a %&NL&% suffix (see the
        // leoMoved checks in Mountain.ApplyTreehouseIfNecessary)
        if (!Context.IsMainPlayer
            || (!Game1.MasterPlayer.hasOrWillReceiveMail(MailFlag)
                && !Game1.MasterPlayer.hasOrWillReceiveMail(MailFlag + "%&NL&%")))
            return;
        if (Game1.getLocationFromName("Mountain") is Mountain mountain && IsBlocking(mountain))
            BlockedRef(mountain).Value = false;
    }
}

/**
 * Pickaxe swings at the rock: the pickaxe calls performToolAction on the
 * location before checking objects or terrain, which Mountain doesn't
 * override, so this handles hits inside the rock's rectangle.
 */
[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performToolAction))]
internal static class RailroadRockToolPatch
{
    [HarmonyPrefix]
    private static bool Prefix(GameLocation __instance, Tool t, int tileX, int tileY, ref bool __result)
    {
        try
        {
            if (!RailroadRock.Config.EarlyRailroadRock
                || __instance is not Mountain mountain
                || t is not Pickaxe pickaxe
                || !RailroadRock.IsBlocking(mountain)
                || !RailroadRock.GetRockPixelRect(mountain).Contains(tileX * 64 + 32, tileY * 64 + 32))
                return true;

            if (RailroadRock.HandlePickaxeHit(mountain, pickaxe, tileX, tileY))
                RailroadRock.Destroy(mountain);
            __result = true;
            return false;
        }
        catch (Exception ex)
        {
            RailroadRock.Monitor.Log($"Failed in {nameof(RailroadRockToolPatch)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }
}

/**
 * Bombs: destroy the rock when the blast circle overlaps its rectangle.
 */
[HarmonyPatch(typeof(GameLocation), nameof(GameLocation.explode))]
internal static class RailroadRockBombPatch
{
    [HarmonyPostfix]
    private static void Postfix(GameLocation __instance, Vector2 tileLocation, int radius, bool destroyObjects)
    {
        try
        {
            if (!RailroadRock.Config.EarlyRailroadRock
                || !destroyObjects
                || __instance is not Mountain mountain
                || !RailroadRock.IsBlocking(mountain))
                return;

            var rect = RailroadRock.GetRockPixelRect(mountain);
            var center = (tileLocation + new Vector2(0.5f, 0.5f)) * 64f;
            float nearestX = Math.Clamp(center.X, rect.Left, rect.Right);
            float nearestY = Math.Clamp(center.Y, rect.Top, rect.Bottom);
            float blastRadius = radius * 64f;
            if ((center.X - nearestX) * (center.X - nearestX) + (center.Y - nearestY) * (center.Y - nearestY)
                <= blastRadius * blastRadius)
                RailroadRock.Destroy(mountain);
        }
        catch (Exception ex)
        {
            RailroadRock.Monitor.Log($"Failed in {nameof(RailroadRockBombPatch)}:\n{ex}", LogLevel.Error);
        }
    }
}
