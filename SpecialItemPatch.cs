using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;

namespace CombatProgressionRework;

internal static class WillyKey
{
    public const int Which = 0;
    public const string ItemName = "Willy's Back Room Key";
    public const string HoldUpMessage = "You found the key to Willy's back room!";
    public const string MailFlag = "willyBackRoomInvitation";

    public static Texture2D? KeyTexture;
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.actionWhenReceived))]
internal static class WillyKeyActionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(SpecialItem __instance, Farmer who)
    {
        if (__instance.which.Value != WillyKey.Which)
            return true;

        who.mailReceived.Add(WillyKey.MailFlag);
        return false;
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.checkForSpecialItemHoldUpMeessage))]
internal static class WillyKeyMessagePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, ref string __result)
    {
        if (__instance.which.Value == WillyKey.Which)
            __result = WillyKey.HoldUpMessage;
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.Name), MethodType.Getter)]
internal static class WillyKeyNamePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, ref string __result)
    {
        if (__instance.which.Value == WillyKey.Which)
            __result = WillyKey.ItemName;
    }
}

[HarmonyPatch(typeof(SpecialItem), nameof(SpecialItem.getTemporarySpriteForHoldingUp))]
internal static class WillyKeySpritePatch
{
    [HarmonyPostfix]
    private static void Postfix(SpecialItem __instance, Vector2 position, ref TemporaryAnimatedSprite __result)
    {
        if (__instance.which.Value != WillyKey.Which || WillyKey.KeyTexture == null)
            return;

        __result = new TemporaryAnimatedSprite("LooseSprites\\Cursors",
            new Rectangle(0, 0, 16, 16), position, false, 0f, Color.White)
        {
            layerDepth = 1f
        };
        __result.texture = WillyKey.KeyTexture;
        __result.sourceRect = new Rectangle(0, 0, 16, 16);
    }
}
