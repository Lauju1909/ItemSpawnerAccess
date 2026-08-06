using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class MentalState
    {
        public bool IsActive { get; set; }
        public string Name { get; set; }
    }

    public class PawnMind
    {
        public MentalState CurrentMentalState { get; set; }
        public float MoodPercentage { get; set; } // 0 bis 100
        public System.Collections.Generic.List<Thought> Memories { get; set; } = new System.Collections.Generic.List<Thought>();
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnMind Mind { get; set; } = new PawnMind();
    }

    public static class MentalManager
    {
        public static void EndMentalBreak(Pawn pawn)
        {
            if (pawn.Mind.CurrentMentalState == null || !pawn.Mind.CurrentMentalState.IsActive)
            {
                Tolk.Speak($"{pawn.Name} hat derzeit keinen Nervenzusammenbruch.");
                return;
            }

            string breakName = pawn.Mind.CurrentMentalState.Name;
            pawn.Mind.CurrentMentalState.IsActive = false;
            pawn.Mind.CurrentMentalState = null;

            Tolk.Speak($"Der Nervenzusammenbruch '{breakName}' von {pawn.Name} wurde sofort beendet. Der Kolonist ist wieder ansprechbar.");
        }

        public static void MaximizeMood(Pawn pawn)
        {
            if (pawn.Mind.MoodPercentage >= 100f)
            {
                Tolk.Speak($"Die Stimmung von {pawn.Name} ist bereits auf dem Maximum.");
                return;
            }

            pawn.Mind.MoodPercentage = 100f;
            Tolk.Speak($"Die Stimmung von {pawn.Name} wurde sofort auf 100 Prozent maximiert. Künftige Nervenzusammenbrüche sind vorerst abgewendet.");
        }
    }
}
