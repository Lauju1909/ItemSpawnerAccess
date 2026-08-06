using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnRoyalty
    {
        public string Title { get; set; } = "Bürgerlicher";
        public int Honor { get; set; } = 0;
        public bool IgnoresRoomRequirements { get; set; } = false;
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnRoyalty Royalty { get; set; } = new PawnRoyalty();
    }

    public static class RoyaltyManager
    {
        public static void GrantTitle(Pawn pawn, string titleName)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            pawn.Royalty.Title = titleName;
            Tolk.Speak($"Erhebt euch! Dem Kolonisten {pawn.Name} wurde auf direkten königlichen Erlass der majestätische Titel '{titleName}' verliehen. Lang lebe der Adel!");
        }

        public static void GrantInfiniteHonor(Pawn pawn)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            pawn.Royalty.Honor = 9999999; // Quasi unendliche Ehre
            Tolk.Speak($"Das Imperium beugt sich! {pawn.Name} hat nun unbegrenzte imperiale Ehre erhalten und kann jeden erdenklichen Gefallen vom Herrscher einfordern.");
        }

        public static void DeactivateRoomRequirements(Pawn pawn)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            pawn.Royalty.IgnoresRoomRequirements = true;
            Tolk.Speak($"Bürgerliches Privileg gewährt! Der Adlige {pawn.Name} wird sich ab sofort niemals wieder über einen fehlenden Thronsaal oder ein unwürdiges Schlafzimmer beschweren. Die Raum-Anforderungen sind komplett deaktiviert.");
        }
    }
}
