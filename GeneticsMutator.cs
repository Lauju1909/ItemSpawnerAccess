using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class PawnGenes
    {
        public List<string> Endogenes { get; set; } = new List<string>();
        public List<string> Xenogenes { get; set; } = new List<string>();
        public string XenotypeName { get; set; } = "Baseliners";
    }

    // Erweiterung der Mock-Pawn-Klasse
    public partial class Pawn
    {
        public PawnGenes Genes { get; set; } = new PawnGenes();
    }

    public static class GeneticsMutator
    {
        public static void AddGene(Pawn pawn, string geneName, bool isXenogene = true)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            var targetList = isXenogene ? pawn.Genes.Xenogenes : pawn.Genes.Endogenes;
            string type = isXenogene ? "Xenogen" : "Endogen";

            if (targetList.Contains(geneName))
            {
                Tolk.Speak($"DNA-Fehler: {pawn.Name} besitzt das {type} '{geneName}' bereits.");
                return;
            }

            targetList.Add(geneName);
            Tolk.Speak($"Genetische Mutation erfolgreich! Das {type} '{geneName}' wurde sofort ohne Labor in die DNA von {pawn.Name} implantiert.");
        }

        public static void RemoveGene(Pawn pawn, string geneName, bool isXenogene = true)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            var targetList = isXenogene ? pawn.Genes.Xenogenes : pawn.Genes.Endogenes;
            string type = isXenogene ? "Xenogen" : "Endogen";

            if (!targetList.Contains(geneName))
            {
                Tolk.Speak($"DNA-Fehler: {pawn.Name} besitzt das {type} '{geneName}' nicht. Eine Extraktion ist unmöglich.");
                return;
            }

            targetList.Remove(geneName);
            Tolk.Speak($"Genetische Extraktion erfolgreich! Das {type} '{geneName}' wurde restlos aus der DNA von {pawn.Name} gespült.");
        }

        public static void SetXenotype(Pawn pawn, string xenotypeName)
        {
            if (pawn == null)
            {
                Tolk.Speak("Es ist kein Kolonist ausgewählt.");
                return;
            }

            pawn.Genes.XenotypeName = xenotypeName;
            Tolk.Speak($"Vollständige Biotransformation abgeschlossen! Die DNA von {pawn.Name} wurde komplett umgeschrieben. Der neue Xenotyp lautet ab sofort '{xenotypeName}'.");
        }
    }
}
