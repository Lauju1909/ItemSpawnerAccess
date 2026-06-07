using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class Caravan
    {
        public string Name { get; set; }
        public IntVec3 Position { get; set; }
        public float MoveSpeedMultiplier { get; set; } = 1.0f;
        public bool HasInfiniteCapacity { get; set; } = false;
        public float CarriedWeight { get; set; } = 0f;
        public float MaxCapacity { get; set; } = 100f;
    }

    public static class CaravanManipulator
    {
        public static void TeleportCaravan(Caravan caravan, IntVec3 destination, string locationName)
        {
            if (caravan == null)
            {
                Tolk.Speak("Es ist keine Karawane ausgewählt.");
                return;
            }

            caravan.Position = destination;
            Tolk.Speak($"Transwarp-Sprung initiiert! Die Karawane '{caravan.Name}' wurde sofort erfolgreich nach '{locationName}' teleportiert.");
        }

        public static void BoostSpeed(Caravan caravan)
        {
            if (caravan == null)
            {
                Tolk.Speak("Es ist keine Karawane ausgewählt.");
                return;
            }

            caravan.MoveSpeedMultiplier = 500f; // 500-fache Geschwindigkeit
            Tolk.Speak($"Die Basis-Reisegeschwindigkeit der Karawane '{caravan.Name}' wurde dramatisch erhöht. Sie bewegen sich nun mit rasender Geschwindigkeit über die Weltkarte.");
        }

        public static void SetInfiniteCapacity(Caravan caravan)
        {
            if (caravan == null)
            {
                Tolk.Speak("Es ist keine Karawane ausgewählt.");
                return;
            }

            caravan.HasInfiniteCapacity = true;
            caravan.MaxCapacity = float.MaxValue; // Quasi unendlich
            Tolk.Speak($"Gravitationspuffer aktiviert! Das Tragegewicht der Karawane '{caravan.Name}' wurde auf Unendlich gesetzt. Sie kann ab sofort unbegrenzt Gegenstände tragen und niemals überladen sein.");
        }
    }
}
