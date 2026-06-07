using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class Faction
    {
        public string Name { get; set; }
        public int Goodwill { get; set; } // -100 bis 100
        public bool IsEnemy => Goodwill < -50;
        public bool IsAlly => Goodwill >= 75;
        
        // Simuliert einen aktiven Kriegszustand
        public bool IsAtWar { get; set; }
    }

    public static class DiplomacyManager
    {
        public static void SetGoodwill(Faction faction, int targetGoodwill)
        {
            int oldGoodwill = faction.Goodwill;
            faction.Goodwill = Math.Clamp(targetGoodwill, -100, 100);
            
            string status;
            if (faction.Goodwill == 100) status = "Verbündet";
            else if (faction.Goodwill == -100) status = "Todfeind";
            else if (faction.IsAlly) status = "Alliiert";
            else if (faction.IsEnemy) status = "Feindlich";
            else status = "Neutral";
            
            Tolk.Speak($"Der Ruf bei der Fraktion {faction.Name} wurde von {oldGoodwill} auf {faction.Goodwill} gesetzt. Der neue Status ist: {status}.");
        }

        public static void ForcePeaceTreaty(Faction faction)
        {
            if (!faction.IsAtWar && !faction.IsEnemy)
            {
                Tolk.Speak($"Mit der Fraktion {faction.Name} herrscht bereits Frieden.");
                return;
            }

            faction.IsAtWar = false;
            if (faction.Goodwill < 0)
            {
                faction.Goodwill = 0; // Ruf auf neutral setzen, um den Frieden zu wahren
            }

            Tolk.Speak($"Ein Friedensvertrag mit der Fraktion {faction.Name} wurde erfolgreich erzwungen. Alle Feindseligkeiten wurden sofort eingestellt.");
        }
    }
}
