using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace CombatProgressionRework;

public class ModEntry : Mod
{
    public override void Entry(IModHelper helper)
    {
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Monitor.Log("CombatProgressionRework loaded!", LogLevel.Info);
    }
}
