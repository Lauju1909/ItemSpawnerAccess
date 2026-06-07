using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class Ideology
    {
        public string Name { get; set; }
    }

    public class PawnBeliefs
    {
        public Ideology CurrentIdeology { get; set; }
        public float Certainty { get; set; } // Glaubenspunkte (0 bis 100)
    }

    // Extended Pawn mock
    public partial class Pawn
    {
        public PawnBeliefs Beliefs { get; set; } = new PawnBeliefs();
    }

    public static class IdeologyManager
    {
        public static void ConvertPawn(Pawn pawn, Ideology newIdeology)
        {
            if (pawn.Beliefs.CurrentIdeology?.Name == newIdeology.Name)
            {
                Tolk.Speak($"{pawn.Name} glaubt bereits an die Ideologie {newIdeology.Name}.");
                return;
            }

            pawn.Beliefs.CurrentIdeology = newIdeology;
            pawn.Beliefs.Certainty = 50f; // Standard nach Konvertierung
            Tolk.Speak($"{pawn.Name} wurde sofort zur Ideologie {newIdeology.Name} konvertiert. Aktuelle Überzeugung: 50 Prozent.");
        }

        public static void ChangeCertainty(Pawn pawn, float amount)
        {
            if (pawn.Beliefs.CurrentIdeology == null)
            {
                Tolk.Speak($"{pawn.Name} hat derzeit keine Ideologie. Glaubenspunkte können nicht geändert werden.");
                return;
            }

            pawn.Beliefs.Certainty += amount;
            pawn.Beliefs.Certainty = Math.Clamp(pawn.Beliefs.Certainty, 0f, 100f);

            string action = amount >= 0 ? "erhöht" : "verringert";
            Tolk.Speak($"Die Glaubenspunkte von {pawn.Name} wurden um {Math.Abs(amount)} {action}. Der aktuelle Wert ist {pawn.Beliefs.Certainty} Prozent.");
        }

        public static void CheckIdeologyStatus(Pawn pawn)
        {
            if (pawn.Beliefs.CurrentIdeology == null)
            {
                Tolk.Speak($"{pawn.Name} folgt keiner Ideologie.");
            }
            else
            {
                Tolk.Speak($"{pawn.Name} folgt der Ideologie {pawn.Beliefs.CurrentIdeology.Name} mit einer Überzeugung von {pawn.Beliefs.Certainty} Prozent.");
            }
        }
    }
}
