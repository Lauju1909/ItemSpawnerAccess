using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnRelation
    {
        public string Type { get; set; }
        public Pawn OtherPawn { get; set; }
    }

    public class PawnRelationsTracker
    {
        public List<PawnRelation> DirectRelations { get; set; } = new List<PawnRelation>();
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnRelationsTracker Relations { get; set; } = new PawnRelationsTracker();
    }

    public static class RelationManager
    {
        public static void AddRelation(Pawn pawn1, Pawn pawn2, string relationType)
        {
            if (pawn1 == pawn2)
            {
                Tolk.Speak("Ein Kolonist kann keine Beziehung zu sich selbst haben.");
                return;
            }

            var existingRelation = pawn1.Relations.DirectRelations.FirstOrDefault(r => r.OtherPawn == pawn2 && r.Type.Equals(relationType, StringComparison.OrdinalIgnoreCase));
            if (existingRelation != null)
            {
                Tolk.Speak($"{pawn1.Name} und {pawn2.Name} haben die Beziehung '{relationType}' bereits.");
                return;
            }

            pawn1.Relations.DirectRelations.Add(new PawnRelation { Type = relationType, OtherPawn = pawn2 });
            
            // Einfache symmetrische Verknüpfung im Mock
            pawn2.Relations.DirectRelations.Add(new PawnRelation { Type = relationType, OtherPawn = pawn1 }); 

            string feedback = "";
            if (relationType.Equals("Ehepartner", StringComparison.OrdinalIgnoreCase))
                feedback = $"{pawn1.Name} und {pawn2.Name} sind nun miteinander verheiratet.";
            else if (relationType.Equals("Rivale", StringComparison.OrdinalIgnoreCase))
                feedback = $"{pawn1.Name} und {pawn2.Name} sind nun tief verfeindet.";
            else
                feedback = $"Die Beziehung '{relationType}' zwischen {pawn1.Name} und {pawn2.Name} wurde sofort erfolgreich hergestellt.";

            Tolk.Speak(feedback);
        }

        public static void RemoveRelation(Pawn pawn1, Pawn pawn2, string relationType)
        {
            int removed1 = pawn1.Relations.DirectRelations.RemoveAll(r => r.OtherPawn == pawn2 && r.Type.Equals(relationType, StringComparison.OrdinalIgnoreCase));
            int removed2 = pawn2.Relations.DirectRelations.RemoveAll(r => r.OtherPawn == pawn1 && r.Type.Equals(relationType, StringComparison.OrdinalIgnoreCase));

            if (removed1 == 0 && removed2 == 0)
            {
                Tolk.Speak($"{pawn1.Name} und {pawn2.Name} haben die Beziehung '{relationType}' nicht.");
                return;
            }

            Tolk.Speak($"Die Beziehung '{relationType}' zwischen {pawn1.Name} und {pawn2.Name} wurde restlos gelöscht.");
        }
    }
}
