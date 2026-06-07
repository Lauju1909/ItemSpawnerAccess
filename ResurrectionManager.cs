using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class Corpse
    {
        public Pawn InnerPawn { get; set; }
        public bool IsDessicated { get; set; }
    }

    public static class ResurrectionManager
    {
        public static void Resurrect(Corpse corpse, Map map)
        {
            if (corpse == null || corpse.InnerPawn == null)
            {
                Tolk.Speak("Es wurde keine gültige Leiche zur Wiederbelebung ausgewählt.");
                return;
            }

            Pawn resurrectedPawn = corpse.InnerPawn;

            // Vollständige Heilung: Ein neuer HealthState-Mock simuliert 100% Gesundheit
            // ohne Narben, fehlende Körperteile, Demenz oder Auferstehungs-Psychose.
            resurrectedPawn.Health = new HealthState();

            // Bedürfnisse wieder auffüllen, da sie bei Toten leer waren
            resurrectedPawn.Needs.Food = 1.0f;
            resurrectedPawn.Needs.Rest = 1.0f;
            resurrectedPawn.Needs.Recreation = 1.0f;
            resurrectedPawn.Needs.Comfort = 1.0f;
            resurrectedPawn.Needs.Beauty = 1.0f;

            // Den Pawn wieder lebendig auf der Map registrieren, falls er abgemeldet war
            if (!map.AllPawns.Contains(resurrectedPawn))
            {
                map.AllPawns.Add(resurrectedPawn);
            }

            string typeName = resurrectedPawn.IsColonist ? "Der Kolonist" : "Das Tier";
            
            Tolk.Speak($"Wunder vollbracht! {typeName} '{resurrectedPawn.Name}' wurde makellos von den Toten zurückgeholt. Keine Hirnschäden, keine Auferstehungspsychose – die Gesundheit liegt sofort bei 100 Prozent.");
        }
    }
}
