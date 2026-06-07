using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class Equipment
    {
        public string Name { get; set; }
        public float HitPoints { get; set; }
        public float MaxHitPoints { get; set; } = 100f;
        public string Quality { get; set; } = "Normal";
    }

    public class PawnApparelAndWeapons
    {
        public List<Equipment> Items { get; set; } = new List<Equipment>();
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnApparelAndWeapons Gear { get; set; } = new PawnApparelAndWeapons();
    }

    public static class EquipmentMaster
    {
        public static void RepairAndUpgradeGear(Pawn pawn, bool upgradeToLegendary = false)
        {
            if (pawn == null)
            {
                Tolk.Speak("Fehler: Es ist kein Kolonist ausgewählt.");
                return;
            }

            if (pawn.Gear.Items.Count == 0)
            {
                Tolk.Speak($"{pawn.Name} trägt derzeit weder Waffen noch Rüstung oder Kleidung, die repariert werden könnten.");
                return;
            }

            int count = 0;
            foreach (var item in pawn.Gear.Items)
            {
                // Repariere auf 100% Haltbarkeit (HitPoints = MaxHitPoints)
                item.HitPoints = item.MaxHitPoints;

                if (upgradeToLegendary)
                {
                    item.Quality = "Legendär"; // Auf maximales Level pushen
                }
                count++;
            }

            if (upgradeToLegendary)
            {
                Tolk.Speak($"Der Schmied der Götter hat gesprochen! {count} Ausrüstungsteile und Waffen von {pawn.Name} wurden restlos repariert und sofort in der Qualität auf 'Legendär' aufgewertet. Das Problem mit zerschlissener Kleidung ist für immer Geschichte.");
            }
            else
            {
                Tolk.Speak($"Wartung abgeschlossen! Sämtliche Waffen und Kleidungsstücke von {pawn.Name} wurden vollständig auf 100 Prozent Haltbarkeit repariert.");
            }
        }
    }
}
