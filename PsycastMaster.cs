using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnPsi
    {
        public int PsylinkLevel { get; set; }
        public float PsychoFocus { get; set; } // 0.0 bis 1.0 (0 bis 100%)
        public float NeuralHeat { get; set; }
        public List<string> Abilities { get; set; } = new List<string>();
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnPsi Psi { get; set; } = new PawnPsi();
    }

    public static class PsycastMaster
    {
        private static readonly List<string> AllPsycasts = new List<string> 
        { 
            "Betäuben", "Schmerzblocker", "Blendimpuls", "Wort der Freude", 
            "Wort der Liebe", "Unsichtbarkeit", "Massenchaos" 
        };

        public static void MaximizePsylink(Pawn pawn)
        {
            if (pawn.Psi.PsylinkLevel == 6 && pawn.Psi.Abilities.Count >= AllPsycasts.Count)
            {
                Tolk.Speak($"{pawn.Name} ist bereits ein Psi-Meister auf Level 6 und kennt alle Fähigkeiten.");
                return;
            }

            pawn.Psi.PsylinkLevel = 6;
            foreach (var cast in AllPsycasts)
            {
                if (!pawn.Psi.Abilities.Contains(cast))
                {
                    pawn.Psi.Abilities.Add(cast);
                }
            }

            Tolk.Speak($"Psylink-Level von {pawn.Name} auf 6 erhöht und alle Psi-Fähigkeiten freigeschaltet.");
        }

        public static void RechargePsychoFocus(Pawn pawn)
        {
            if (pawn.Psi.PsychoFocus >= 1.0f)
            {
                Tolk.Speak($"Der Psycho-Fokus von {pawn.Name} ist bereits bei 100 Prozent.");
                return;
            }

            pawn.Psi.PsychoFocus = 1.0f;
            Tolk.Speak($"Psycho-Fokus von {pawn.Name} sofort auf 100 Prozent geladen.");
        }

        public static void ClearNeuralHeat(Pawn pawn)
        {
            if (pawn.Psi.NeuralHeat <= 0f)
            {
                Tolk.Speak($"Die neurale Hitze von {pawn.Name} ist bereits bei null.");
                return;
            }

            pawn.Psi.NeuralHeat = 0f;
            Tolk.Speak($"Neurale Hitze von {pawn.Name} komplett auf null abgebaut.");
        }
    }
}
