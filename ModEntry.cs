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
    public override void Entry(IModHelper helper)
    {
        WillyKey.Monitor = Monitor;
        WillyKey.Translations = helper.Translation;

        new Harmony(ModManifest.UniqueID).PatchAll();
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        Monitor.Log("CombatProgressionRework loaded!", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        WillyKey.KeyTexture = Helper.ModContent.Load<Texture2D>("assets/willy-key.png");
    }

    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        if (!e.IsLocalPlayer || e.NewLocation is not MineShaft mine || mine.mineLevel != MineShaft.bottomOfMineLevel)
            return;

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
}
