using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace CombatProgressionRework;

internal static class WillyKey
{
    public const int Which = 861337;
    public const string MailFlag = "willyBackRoomInvitation";

    public static IMonitor Monitor = null!;
    public static ITranslationHelper Translations = null!;
    public static Texture2D? KeyTexture;

    public static string ItemName => Translations.Get("willy-key.name");
    public static string HoldUpMessage => Translations.Get("willy-key.hold-up-message");
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.actionWhenReceived))]
internal static class WillyKeyActionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SpecialItem __instance, Farmer who)
    {
        try
        {
            if (__instance.which.Value != WillyKey.Which)
                return true;

            who.mailReceived.Add(WillyKey.MailFlag);
            return false;
        }
        catch (Exception ex)
        {
            WillyKey.Monitor.Log($"Failed in {nameof(WillyKeyActionPatch)}:\n{ex}", LogLevel.Error);
            return true;
        }
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.checkForSpecialItemHoldUpMeessage))]
internal static class WillyKeyMessagePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, ref string __result)
    {
        try
        {
            if (__instance.which.Value == WillyKey.Which)
                __result = WillyKey.HoldUpMessage;
        }
        catch (Exception ex)
        {
            WillyKey.Monitor.Log($"Failed in {nameof(WillyKeyMessagePatch)}:\n{ex}", LogLevel.Error);
        }
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.Name), MethodType.Getter)]
internal static class WillyKeyNamePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, ref string __result)
    {
        try
        {
            if (__instance.which.Value == WillyKey.Which)
                __result = WillyKey.ItemName;
        }
        catch (Exception ex)
        {
            WillyKey.Monitor.Log($"Failed in {nameof(WillyKeyNamePatch)}:\n{ex}", LogLevel.Error);
        }
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.getTemporarySpriteForHoldingUp))]
internal static class WillyKeySpritePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, Vector2 position, ref TemporaryAnimatedSprite __result)
    {
        try
        {
            if (__instance.which.Value != WillyKey.Which || WillyKey.KeyTexture == null)
                return;

            // Mirrors the vanilla hold-up sprites: 4x scale, offset to center over the
            // player, and a 2500ms single-frame animation so it lasts the whole pose
            __result = new TemporaryAnimatedSprite("LooseSprites\\Cursors",
                new Rectangle(0, 0, 16, 16), 2500f, 1, 0,
                position + new Vector2(16f, 0f), false, false, 1f, 0f,
                Color.White, 4f, 0f, 0f, 0f)
            {
                texture = WillyKey.KeyTexture
            };
        }
        catch (Exception ex)
        {
            WillyKey.Monitor.Log($"Failed in {nameof(WillyKeySpritePatch)}:\n{ex}", LogLevel.Error);
        }
    }
}
