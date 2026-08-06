using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    // Erweiterung der Mock-Pawn-Klasse um Clone-Funktion
    public partial class Pawn
    {
        public IntVec3 Position { get; set; } = new IntVec3(0, 0, 0);

        public Pawn Clone()
        {
            var clone = new Pawn
            {
                Name = this.Name + " (Klon)",
                IsColonist = this.IsColonist,
                Position = new IntVec3(this.Position.x + 1, this.Position.y, this.Position.z), // Spawnt direkt daneben
                Abilities = new List<string>(this.Abilities)
            };

            // Gesundheitszustand kopieren
            clone.Health = new HealthState();
            foreach (var part in this.Health.BodyParts)
            {
                clone.Health.BodyParts.Add(new BodyPart
                {
                    Name = part.Name,
                    IsMissing = part.IsMissing,
                    HasScar = part.HasScar,
                    HasDisease = part.HasDisease,
                    InstalledImplant = part.InstalledImplant
                });
            }

            // Eigenschaften (Traits) kopieren
            clone.Story = new TraitSet();
            clone.Story.Traits = new List<string>(this.Story.Traits);

            // Psi-Fähigkeiten kopieren
            clone.Psi = new PawnPsi
            {
                PsylinkLevel = this.Psi.PsylinkLevel,
                PsychoFocus = this.Psi.PsychoFocus,
                NeuralHeat = this.Psi.NeuralHeat,
                Abilities = new List<string>(this.Psi.Abilities)
            };

            // Bedürfnisse kopieren
            clone.Needs = new PawnNeeds
            {
                Food = this.Needs.Food,
                Rest = this.Needs.Rest,
                Recreation = this.Needs.Recreation,
                Comfort = this.Needs.Comfort,
                Beauty = this.Needs.Beauty
            };

            // Alter kopieren
            clone.Age = new PawnAge
            {
                BiologicalAge = this.Age.BiologicalAge,
                ChronologicalAge = this.Age.ChronologicalAge
            };

            return clone;
        }
    }

    public static class PawnCloner
    {
        public static void CloneColonist(Map map, Pawn original)
        {
            if (original == null)
            {
                Tolk.Speak("Es ist kein Kolonist zum Klonen ausgewählt.");
                return;
            }

            Pawn doppelganger = original.Clone();
            map.AllPawns.Add(doppelganger);

            Tolk.Speak($"Der Kolonist {original.Name} wurde erfolgreich geklont. Ein exakter Doppelgänger mit identischen Fähigkeiten, Eigenschaften und Gesundheit ist direkt daneben gespawnt.");
        }
    }
}
