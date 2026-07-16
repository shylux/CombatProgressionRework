namespace CombatProgressionRework;

public sealed class ModConfig
{
    public int BoatHardwoodCost { get; set; } = 20;
    public int BoatIridiumBarCost { get; set; } = 1;
    public int BoatBatteryPackCost { get; set; } = 2;
    public bool UnlockQiRoom { get; set; } = true;
    public bool FreeFirstMastery { get; set; } = true;
    public float XpRequirementModifier { get; set; } = 0.6f;
    public bool VillagerWeaponGifts { get; set; } = true;
    public int VillagerWeaponHearts { get; set; } = 4;
}
