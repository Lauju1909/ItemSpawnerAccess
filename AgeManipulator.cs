using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnAge
    {
        public float BiologicalAge { get; set; }
        public float ChronologicalAge { get; set; }
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnAge Age { get; set; } = new PawnAge();
    }

    public static class AgeManipulator
    {
        public static void MakeYoungAgain(Pawn pawn, float targetAge = 21f)
        {
            if (pawn.Age.BiologicalAge <= targetAge)
            {
                Tolk.Speak($"{pawn.Name} ist biologisch bereits jünger als oder exakt {targetAge} Jahre alt.");
                return;
            }

            float oldAge = pawn.Age.BiologicalAge;
            pawn.Age.BiologicalAge = targetAge;

            Tolk.Speak($"Verjüngung erfolgreich. Das biologische Alter von {pawn.Name} wurde von {Math.Round(oldAge, 1)} auf ideale {targetAge} Jahre zurückgesetzt. Altersschwäche und Gebrechlichkeit wurden gestoppt.");
        }
    }
}
