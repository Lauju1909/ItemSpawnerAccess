using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class TraitSet
    {
        public List<string> Traits { get; set; } = new List<string>();
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public TraitSet Story { get; set; } = new TraitSet();
    }

    public static class TraitManager
    {
        public static void AddTrait(Pawn pawn, string traitName)
        {
            if (pawn.Story.Traits.Contains(traitName))
            {
                Tolk.Speak($"{pawn.Name} besitzt die Eigenschaft '{traitName}' bereits.");
                return;
            }

            pawn.Story.Traits.Add(traitName);
            Tolk.Speak($"Die Eigenschaft '{traitName}' wurde sofort erfolgreich zu {pawn.Name} hinzugefügt.");
        }

        public static void RemoveTrait(Pawn pawn, string traitName)
        {
            if (!pawn.Story.Traits.Contains(traitName))
            {
                Tolk.Speak($"{pawn.Name} besitzt die Eigenschaft '{traitName}' nicht und kann daher nicht entfernt werden.");
                return;
            }

            pawn.Story.Traits.Remove(traitName);
            Tolk.Speak($"Die Eigenschaft '{traitName}' wurde restlos von {pawn.Name} entfernt.");
        }
    }
}
