using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;

namespace CombatProgressionRework;

/**
 * Marriage candidates who own a themed weapon (everyone except Emily and
 * Shane) send it to the player by mail the morning after reaching the
 * configured friendship level. In vanilla these weapons are only sold at
 * the Desert Festival for 70 calico eggs. The gifted weapon comes with a
 * random innate enchantment.
 */
[HarmonyPatch(typeof(LetterViewerMenu), MethodType.Constructor, typeof(string), typeof(string), typeof(bool))]
internal static class VillagerWeaponGift
{
    public static IMonitor Monitor = null!;
    public static ModConfig Config = null!;
    public static ITranslationHelper Translations = null!;

    /** Weapon IDs from Data/Weapons, keyed by the owning villager. */
    private static readonly Dictionary<string, string> Weapons = new()
    {
        ["Alex"] = "(W)25",      // Alex's Bat
        ["Sam"] = "(W)30",       // Sam's Old Guitar
        ["Elliott"] = "(W)35",   // Elliott's Pencil
        ["Maru"] = "(W)36",      // Maru's Wrench
        ["Harvey"] = "(W)37",    // Harvey's Mallet
        ["Penny"] = "(W)38",     // Penny's Fryer
        ["Leah"] = "(W)39",      // Leah's Whittler
        ["Abigail"] = "(W)40",   // Abby's Planchette
        ["Sebastian"] = "(W)41", // Seb's Lost Mace
        ["Haley"] = "(W)42",     // Haley's Iron
    };

    private const string MailIdPrefix = "shylux.CombatProgressionRework_WeaponGift_";

    private static string MailId(string npcName) => MailIdPrefix + npcName;

    private static bool IsGiftMailId(string? mailId) => mailId?.StartsWith(MailIdPrefix) == true;

    public static void AddMailEntries(IDictionary<string, string> data)
    {
        foreach (var (npcName, weaponId) in Weapons)
            data[MailId(npcName)] = Translations.Get($"weapon-mail.{npcName}")
                + $" %item id {weaponId} %%"
                + $"[#]{Translations.Get("weapon-mail.title", new { name = npcName })}";
    }

    public static void OnDayEnding(object? sender, DayEndingEventArgs e)
    {
        try
        {
            if (!Config.VillagerWeaponGifts)
                return;

            foreach (var npcName in Weapons.Keys)
            {
                var mailId = MailId(npcName);
                if (!Game1.player.hasOrWillReceiveMail(mailId)
                    && Game1.player.getFriendshipHeartLevelForNPC(npcName) >= Config.VillagerWeaponHearts)
                {
                    Game1.addMailForTomorrow(mailId);
                    Monitor.Log($"Queued weapon gift mail from {npcName}.");
                }
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(VillagerWeaponGift)}:\n{ex}", LogLevel.Error);
        }
    }

    /**
     * Gives the weapon attached to a gift mail a random innate enchantment,
     * like the Volcano Forge weapons have. The mail's %item command creates
     * a plain weapon, so the enchantment is rolled right after the letter
     * menu builds its attached items.
     */
    [HarmonyPostfix]
    private static void EnchantGiftWeapon(LetterViewerMenu __instance, string mailTitle)
    {
        try
        {
            if (!IsGiftMailId(mailTitle))
                return;

            foreach (var component in __instance.itemsToGrab)
            {
                if (component.item is MeleeWeapon weapon)
                    MeleeWeapon.attemptAddRandomInnateEnchantment(weapon, Game1.random, force: true);
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed in {nameof(EnchantGiftWeapon)}:\n{ex}", LogLevel.Error);
        }
    }
}
