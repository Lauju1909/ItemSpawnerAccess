using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class Pregnancy
    {
        public bool IsPregnant { get; set; }
        public Pawn Father { get; set; }
        public float GestationProgress { get; set; } // 0.0 to 1.0
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public bool IsFemale { get; set; }
        public Pregnancy Reproduction { get; set; } = new Pregnancy();
    }

    public static class PregnancyManager
    {
        public static void ForcePregnancy(Pawn mother, Pawn father)
        {
            if (mother == null)
            {
                Tolk.Speak("Fehler: Keine Mutter ausgewählt.");
                return;
            }

            if (!mother.IsFemale)
            {
                Tolk.Speak($"{mother.Name} ist nicht weiblich und kann daher nicht schwanger werden.");
                return;
            }

            if (mother.Reproduction.IsPregnant)
            {
                Tolk.Speak($"{mother.Name} ist bereits schwanger.");
                return;
            }

            mother.Reproduction.IsPregnant = true;
            mother.Reproduction.Father = father;
            mother.Reproduction.GestationProgress = 0f;

            string fatherName = father != null ? father.Name : "unbekanntem Spender";
            Tolk.Speak($"Wunder der Biologie! {mother.Name} wurde auf wundersame Weise künstlich befruchtet. Das Genmaterial stammt von {fatherName}.");
        }

        public static void InstantBirth(Pawn mother)
        {
            if (mother == null)
            {
                Tolk.Speak("Fehler: Keine Mutter ausgewählt.");
                return;
            }

            if (!mother.Reproduction.IsPregnant)
            {
                Tolk.Speak($"{mother.Name} ist momentan nicht schwanger. Eine Blitz-Geburt ist nicht möglich.");
                return;
            }

            // Schwangerschaft sofort auf 100% setzen und beenden
            mother.Reproduction.GestationProgress = 1f;
            mother.Reproduction.IsPregnant = false;
            
            Tolk.Speak($"Zellbeschleunigung abgeschlossen! Die Schwangerschaft von {mother.Name} wurde augenblicklich und 100 Prozent sicher beendet. Ein neues, kerngesundes Leben hat soeben das Licht der Welt erblickt!");
        }
    }
}
