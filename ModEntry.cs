using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Objects;

namespace CombatProgressionRework;

public class ModEntry : Mod
{
    private ModConfig Config = null!;

    public override void Entry(IModHelper helper)
    {
        Config = helper.ReadConfig<ModConfig>();

        WillyKey.Monitor = Monitor;
        WillyKey.Translations = helper.Translation;
        GalaxySword.Monitor = Monitor;
        GalaxySword.Translations = helper.Translation;
        BoatCost.Monitor = Monitor;
        BoatCost.Config = Config;
        QiRoom.Monitor = Monitor;
        QiRoom.Config = Config;
        XpRequirement.Monitor = Monitor;
        XpRequirement.Config = Config;
        Mastery.Monitor = Monitor;
        Mastery.Config = Config;
        VillagerWeaponGift.Monitor = Monitor;
        VillagerWeaponGift.Config = Config;
        VillagerWeaponGift.Translations = helper.Translation;
        RailroadRock.Monitor = Monitor;
        RailroadRock.Config = Config;

        new Harmony(ModManifest.UniqueID).PatchAll();
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.DayStarted += RailroadRock.OnDayStarted;
        helper.Events.GameLoop.DayEnding += VillagerWeaponGift.OnDayEnding;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        Monitor.Log("CombatProgressionRework loaded!", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        WillyKey.KeyTexture = Helper.ModContent.Load<Texture2D>("assets/willy-key.png");
        RegisterConfigMenu();
    }

    /**
     * Updates the boat repair dialogue to show the configured material costs
     * instead of the vanilla ones (works for all languages since the numbers
     * are written as digits in every translation).
     */
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.NameWithoutLocale.IsEquivalentTo("Strings/Locations"))
        {
            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, string>().Data;
                ReplaceCost(data, "BoatTunnel_DonateHardwood", 200, Config.BoatHardwoodCost);
                ReplaceCost(data, "BoatTunnel_DonateHardwoodHint", 200, Config.BoatHardwoodCost);
                ReplaceCost(data, "BoatTunnel_DonateIridium", 5, Config.BoatIridiumBarCost);
                ReplaceCost(data, "BoatTunnel_DonateIridiumHint", 5, Config.BoatIridiumBarCost);
                ReplaceCost(data, "BoatTunnel_DonateBatteries", 5, Config.BoatBatteryPackCost);
                ReplaceCost(data, "BoatTunnel_DonateBatteriesHint", 5, Config.BoatBatteryPackCost);
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Quests"))
        {
            e.Edit(asset =>
            {
                // Basic/<title>/<description>/<objective>/<conditions>/<next quests>/<money>/<reward desc>/<cancellable>
                var data = asset.AsDictionary<string, string>().Data;
                data[GalaxySword.QuestId] = string.Join("/",
                    "Basic",
                    Helper.Translation.Get("galaxy.quest.title"),
                    Helper.Translation.Get("galaxy.quest.description"),
                    Helper.Translation.Get("galaxy.quest.objective"),
                    "", "-1", "0", "-1", "false");
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/mail"))
        {
            e.Edit(asset => VillagerWeaponGift.AddMailEntries(asset.AsDictionary<string, string>().Data));
        }
        else if (Config.MonsterMuskUnlock && e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
        {
            e.Edit(asset =>
            {
                // <ingredients>/<unused>/<yield>/<big craftable>/<unlock conditions>[/<display name>]
                var data = asset.AsDictionary<string, string>().Data;
                if (!data.TryGetValue("Monster Musk", out var recipe))
                    return;
                var fields = recipe.Split('/');
                if (fields.Length <= 4)
                {
                    Monitor.Log($"Unexpected Monster Musk recipe format: {recipe}", LogLevel.Warn);
                    return;
                }
                // Learned on the combat level 7 level-up screen, in addition
                // to the vanilla unlock (Wizard's Prismatic Jelly special order)
                fields[4] = "Combat 7";
                data["Monster Musk"] = string.Join("/", fields);
            });
        }
        else if (Config.MonsterEradicationModifier != 1f
            && e.NameWithoutLocale.IsEquivalentTo("Data/MonsterSlayerQuests"))
        {
            e.Edit(asset =>
            {
                // Only the big goals and Pepper Rex are modified
                foreach (var goal in asset.AsDictionary<string, StardewValley.GameData.MonsterSlayerQuestData>().Data.Values)
                    if (goal.Count >= 100 || goal.Targets?.Contains("Pepper Rex") == true)
                        goal.Count = Math.Max(1, (int)Math.Round(goal.Count * Config.MonsterEradicationModifier));
            });
        }
        else if (e.NameWithoutLocale.IsEquivalentTo("Data/Events/BoatTunnel"))
        {
            e.Edit(asset =>
            {
                // Willy's boat intro event where he says he needs 200 hardwood to patch the hull
                var data = asset.AsDictionary<string, string>().Data;
                foreach (var key in data.Keys.Where(k => k.Split('/')[0] == "9348571").ToArray())
                    ReplaceCost(data, key, 200, Config.BoatHardwoodCost);
            });
        }
    }

    private static void ReplaceCost(IDictionary<string, string> data, string key, int vanillaCost, int newCost)
    {
        if (newCost == vanillaCost || !data.TryGetValue(key, out var text))
            return;
        // (?<!\d)...(?!\d) instead of \b so the match also works in languages
        // where digits are directly followed by word characters (e.g. Chinese)
        data[key] = System.Text.RegularExpressions.Regex.Replace(
            text, $@"(?<!\d){vanillaCost}(?!\d)", newCost.ToString());
    }

    private void RegisterConfigMenu()
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm == null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: () => RailroadRock.Config = VillagerWeaponGift.Config = Mastery.Config = XpRequirement.Config = QiRoom.Config = BoatCost.Config = Config = new ModConfig(),
            save: () =>
            {
                Helper.WriteConfig(Config);
                Helper.GameContent.InvalidateCache(asset =>
                    asset.NameWithoutLocale.IsEquivalentTo("Strings/Locations")
                    || asset.NameWithoutLocale.IsEquivalentTo("Data/Events/BoatTunnel")
                    || asset.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes")
                    || asset.NameWithoutLocale.IsEquivalentTo("Data/MonsterSlayerQuests"));
            });

        gmcm.AddSectionTitle(ModManifest,
            text: () => Helper.Translation.Get("config.section.boat.name"),
            tooltip: () => Helper.Translation.Get("config.section.boat.tooltip"));
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.BoatHardwoodCost,
            setValue: value => Config.BoatHardwoodCost = value,
            name: () => Helper.Translation.Get("config.boat-hardwood.name"),
            tooltip: () => Helper.Translation.Get("config.boat-hardwood.tooltip"),
            min: 0, max: 200);
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.BoatIridiumBarCost,
            setValue: value => Config.BoatIridiumBarCost = value,
            name: () => Helper.Translation.Get("config.boat-iridium.name"),
            tooltip: () => Helper.Translation.Get("config.boat-iridium.tooltip"),
            min: 0, max: 5);
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.BoatBatteryPackCost,
            setValue: value => Config.BoatBatteryPackCost = value,
            name: () => Helper.Translation.Get("config.boat-batteries.name"),
            tooltip: () => Helper.Translation.Get("config.boat-batteries.tooltip"),
            min: 0, max: 5);

        gmcm.AddSectionTitle(ModManifest,
            text: () => Helper.Translation.Get("config.section.other.name"));
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.XpRequirementModifier,
            setValue: value => Config.XpRequirementModifier = value,
            name: () => Helper.Translation.Get("config.xp-modifier.name"),
            tooltip: () => Helper.Translation.Get("config.xp-modifier.tooltip"),
            min: 0.1f, max: 2f, interval: 0.05f,
            formatValue: value => $"{value:0.00}x");
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.FreeFirstMastery,
            setValue: value => Config.FreeFirstMastery = value,
            name: () => Helper.Translation.Get("config.free-mastery.name"),
            tooltip: () => Helper.Translation.Get("config.free-mastery.tooltip"));
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.UnlockQiRoom,
            setValue: value => Config.UnlockQiRoom = value,
            name: () => Helper.Translation.Get("config.qi-room.name"),
            tooltip: () => Helper.Translation.Get("config.qi-room.tooltip"));
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.MonsterMuskUnlock,
            setValue: value => Config.MonsterMuskUnlock = value,
            name: () => Helper.Translation.Get("config.monster-musk.name"),
            tooltip: () => Helper.Translation.Get("config.monster-musk.tooltip"));
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.EarlyRailroadRock,
            setValue: value => Config.EarlyRailroadRock = value,
            name: () => Helper.Translation.Get("config.railroad-rock.name"),
            tooltip: () => Helper.Translation.Get("config.railroad-rock.tooltip"));
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.MonsterEradicationModifier,
            setValue: value => Config.MonsterEradicationModifier = value,
            name: () => Helper.Translation.Get("config.eradication-modifier.name"),
            tooltip: () => Helper.Translation.Get("config.eradication-modifier.tooltip"),
            min: 0.1f, max: 4f, interval: 0.05f,
            formatValue: value => $"{value:0.00}x");
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.VillagerWeaponGifts,
            setValue: value => Config.VillagerWeaponGifts = value,
            name: () => Helper.Translation.Get("config.weapon-gifts.name"),
            tooltip: () => Helper.Translation.Get("config.weapon-gifts.tooltip"));
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.VillagerWeaponHearts,
            setValue: value => Config.VillagerWeaponHearts = value,
            name: () => Helper.Translation.Get("config.weapon-gifts-hearts.name"),
            tooltip: () => Helper.Translation.Get("config.weapon-gifts-hearts.tooltip"),
            min: 1, max: 10);
        gmcm.AddBoolOption(ModManifest,
            getValue: () => Config.HarveyRingGift,
            setValue: value => Config.HarveyRingGift = value,
            name: () => Helper.Translation.Get("config.ring-gift.name"),
            tooltip: () => Helper.Translation.Get("config.ring-gift.tooltip"));
        gmcm.AddNumberOption(ModManifest,
            getValue: () => Config.HarveyRingHearts,
            setValue: value => Config.HarveyRingHearts = value,
            name: () => Helper.Translation.Get("config.ring-gift-hearts.name"),
            tooltip: () => Helper.Translation.Get("config.ring-gift-hearts.tooltip"),
            min: 1, max: 10);
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer)
            return;

        if (e.NewLocation is MineShaft mine)
        {
            if (mine.mineLevel == MineShaft.bottomOfMineLevel)
                HandleMineBottom(mine);
            else if (mine.mineLevel != MineShaft.quarryMineShaft
                && mine.mineLevel >= MineShaft.bottomOfMineLevel + GalaxySword.RequiredSkullCavernLevel)
                HandleSkullCavernDepth();
        }
        else if (e.NewLocation is Caldera caldera)
            HandleCaldera(caldera);
    }

    /**
     * Replaces the skull key at the bottom of the mines with willy's backroom key.
     */
    private static void HandleMineBottom(MineShaft mine)
    {
        var pos = new Vector2(9f, 9f);
        if (!mine.overlayObjects.TryGetValue(pos, out var obj) || obj is not Chest chest)
            return;

        var skullKey = chest.Items.FirstOrDefault(i => i is SpecialItem si && si.which.Value == SpecialItem.skullKey);
        if (skullKey == null)
            return;

        chest.Items.Remove(skullKey);

        if (!Game1.player.mailReceived.Contains(WillyKey.MailFlag))
            chest.Items.Add(new SpecialItem(WillyKey.Which));
        else if (chest.Items.Count == 0)
            mine.overlayObjects.Remove(pos);
    }

    /**
     * Unlocks the galaxy sword when the player reaches skull cavern level 50
     * without having placed any staircases this run (same counter the
     * Mr. Qi level 100 cutscene uses).
     */
    private void HandleSkullCavernDepth()
    {
        if (MineShaft.numberOfCraftedStairsUsedThisRun > 0
            || Game1.player.mailReceived.Contains(GalaxySword.MailFlag))
            return;

        Game1.player.mailReceived.Add(GalaxySword.MailFlag);

        // Complete the quest even if the player never picked it up at the pillars
        if (!Game1.player.hasQuest(GalaxySword.QuestId))
            Game1.player.addQuest(GalaxySword.QuestId);
        Game1.player.completeQuest(GalaxySword.QuestId);

        Game1.drawObjectDialogue(Helper.Translation.Get("galaxy.worthy"));
    }

    /**
     * Replaces prismatic shard in chest on top of the volcano with the skull key.
     */
    private static void HandleCaldera(Caldera caldera)
    {
        // Is the chest present?
        var pos = new Vector2(25f, 28f);
        if (!caldera.overlayObjects.TryGetValue(pos, out var obj) || obj is not Chest chest)
            return;

        var shard = chest.Items.FirstOrDefault(i => i?.QualifiedItemId == "(O)74");
        // Is the chest looted?
        if (shard == null)
            return;

        chest.Items.Remove(shard);
        chest.Items.Add(new SpecialItem(SpecialItem.skullKey));
    }
}
