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
        new Harmony(ModManifest.UniqueID).PatchAll();
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        Monitor.Log("CombatProgressionRework loaded!", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        WillyKey.KeyTexture = CreateKeyTexture(Game1.graphics.GraphicsDevice);
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
    }

    // Simple 16x16 old-fashioned key — brass ring on top, shaft with two teeth
    private static Texture2D CreateKeyTexture(GraphicsDevice device)
    {
        var pixels = new Color[16 * 16];
        var brass = new Color(210, 160, 50);
        var dark  = new Color(140, 100, 25);

        void Fill(int row, int col, Color c) => pixels[row * 16 + col] = c;

        // Ring (outline circle, 5×5, centered at col 6)
        Fill(1, 5, dark);  Fill(1, 6, dark);  Fill(1, 7, dark);
        Fill(2, 4, dark);  Fill(2, 5, brass); Fill(2, 6, brass); Fill(2, 7, brass); Fill(2, 8, dark);
        Fill(3, 4, dark);  Fill(3, 5, brass); Fill(3, 6, brass); Fill(3, 7, brass); Fill(3, 8, dark);
        Fill(4, 5, dark);  Fill(4, 6, dark);  Fill(4, 7, dark);

        // Shaft
        Fill(5, 6, brass);
        Fill(6, 6, brass);
        Fill(7, 6, brass);
        Fill(8, 6, brass);

        // Two teeth pointing right
        Fill(7, 7, brass); Fill(7, 8, brass);
        Fill(9, 7, brass);

        // Tip
        Fill(9, 6, brass);
        Fill(10, 6, dark);

        var tex = new Texture2D(device, 16, 16);
        tex.SetData(pixels);
        return tex;
    }
}
