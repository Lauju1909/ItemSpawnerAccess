using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnNeeds
    {
        public float Food { get; set; }
        public float Rest { get; set; }
        public float Recreation { get; set; }
        public float Comfort { get; set; }
        public float Beauty { get; set; }

        public void MaximizeAll()
        {
            Food = 1.0f;
            Rest = 1.0f;
            Recreation = 1.0f;
            Comfort = 1.0f;
            Beauty = 1.0f;
        }
    }

    public partial class Pawn
    {
        public PawnNeeds Needs { get; set; } = new PawnNeeds();
        public bool IsColonist { get; set; }
    }

    public partial class Map
    {
        public List<Pawn> AllPawns { get; set; } = new List<Pawn>();
    }

    public static class NeedsMaximizer
    {
        public static void MaximizePawnNeeds(Pawn pawn)
        {
            if (pawn.Needs == null)
            {
                Tolk.Speak($"{pawn.Name} hat keine Bedürfnisse, die maximiert werden können.");
                return;
            }

            pawn.Needs.MaximizeAll();
            Tolk.Speak($"Alle primären Bedürfnisse von {pawn.Name} – wie Nahrung, Schlaf, Erholung, Komfort und Schönheit – wurden sofort auf 100 Prozent aufgefüllt.");
        }

        public static void MaximizeAllColonistsNeeds(Map map)
        {
            int count = 0;
            foreach (var p in map.AllPawns)
            {
                if (p.IsColonist && p.Needs != null)
                {
                    p.Needs.MaximizeAll();
                    count++;
                }
            }

            if (count == 0)
            {
                Tolk.Speak("Es wurden keine eigenen Kolonisten auf der Karte gefunden.");
            }
            else
            {
                Tolk.Speak($"Die Bedürfnisse von {count} Kolonisten wurden gleichzeitig auf 100 Prozent maximiert. Die gesamte Kolonie ist nun vollkommen wunschlos glücklich.");
            }
        }
    }
}
