using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess_UniversalPatcher
{
    public class BodyPart
    {
        public string Name { get; set; }
        public bool IsMissing { get; set; }
        public bool HasScar { get; set; }
        public bool HasDisease { get; set; }
        public string InstalledImplant { get; set; }
    }

    public class HealthState
    {
        public List<BodyPart> BodyParts { get; set; } = new List<BodyPart>();
    }

    public partial class Pawn
    {
        public HealthState Health { get; set; } = new HealthState();

        public void InitializeMockBody()
        {
            Health.BodyParts.Add(new BodyPart { Name = "Linker Arm", IsMissing = true });
            Health.BodyParts.Add(new BodyPart { Name = "Rechtes Auge", HasScar = true });
            Health.BodyParts.Add(new BodyPart { Name = "Herz", HasDisease = true });
            Health.BodyParts.Add(new BodyPart { Name = "Rechtes Bein" });
        }
    }

    public static class BionicSurgeon
    {
        public static void RegrowMissingParts(Pawn pawn)
        {
            var missingParts = pawn.Health.BodyParts.Where(p => p.IsMissing).ToList();
            if (missingParts.Count == 0)
            {
                Tolk.Speak($"{pawn.Name} hat keine fehlenden Körperteile.");
                return;
            }

            foreach (var part in missingParts)
            {
                part.IsMissing = false;
            }

            Tolk.Speak($"{missingParts.Count} fehlende Körperteile bei {pawn.Name} sind erfolgreich nachgewachsen.");
        }

        public static void HealScarsAndDiseases(Pawn pawn)
        {
            int healedCount = 0;
            foreach (var part in pawn.Health.BodyParts)
            {
                if (part.HasScar || part.HasDisease)
                {
                    part.HasScar = false;
                    part.HasDisease = false;
                    healedCount++;
                }
            }

            if (healedCount == 0)
            {
                Tolk.Speak($"{pawn.Name} ist vollkommen gesund und hat keine Narben oder Krankheiten.");
            }
            else
            {
                Tolk.Speak($"Alle Krankheiten und Narben bei {pawn.Name} wurden geheilt. {healedCount} betroffene Stellen wurden behandelt.");
            }
        }

        public static void InstallImplant(Pawn pawn, string targetPartName, string implantName)
        {
            var part = pawn.Health.BodyParts.FirstOrDefault(p => p.Name.Equals(targetPartName, StringComparison.OrdinalIgnoreCase));
            
            if (part == null)
            {
                Tolk.Speak($"Das Körperteil {targetPartName} wurde bei {pawn.Name} nicht gefunden.");
                return;
            }

            if (part.IsMissing)
            {
                part.IsMissing = false;
            }

            part.InstalledImplant = implantName;
            Tolk.Speak($"Das Implantat {implantName} wurde erfolgreich und ohne Operationsrisiko bei {pawn.Name} am {targetPartName} installiert.");
        }
    }
}
