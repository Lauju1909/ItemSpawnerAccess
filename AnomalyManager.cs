using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess_UniversalPatcher
{
    public class DarkEntity
    {
        public string Name { get; set; }
        public bool IsDestroyed { get; set; } = false;
        public bool IsCaptured { get; set; } = false;
        public bool IsSubjugated { get; set; } = false;
    }

    public class OccultRitual
    {
        public string Name { get; set; }
        public bool IsInProgress { get; set; }
        public bool IsCompleted { get; set; }
        public float Quality { get; set; }
    }

    // Erweiterung der Mock-Map-Klasse für Anomaly-Inhalte
    public partial class Map
    {
        public List<DarkEntity> Entities { get; set; } = new List<DarkEntity>();
        public List<OccultRitual> Rituals { get; set; } = new List<OccultRitual>();
    }

    public static class AnomalyManager
    {
        public static void ForceCompleteRitual(Map map, string ritualName)
        {
            var ritual = map.Rituals.FirstOrDefault(r => r.Name.Equals(ritualName, StringComparison.OrdinalIgnoreCase) && !r.IsCompleted);
            
            if (ritual == null)
            {
                // Ritual erschaffen und sofort erzwingen, falls nicht mal gestartet
                ritual = new OccultRitual { Name = ritualName, IsInProgress = true };
                map.Rituals.Add(ritual);
            }

            ritual.IsCompleted = true;
            ritual.IsInProgress = false;
            ritual.Quality = 1.0f; // 100% Perfektion

            Tolk.Speak($"Die Dunkelheit weicht deinem Willen. Das okkulte Ritual '{ritualName}' wurde sofort und bedingungslos mit 100 Prozent Qualität erzwungen.");
        }

        public static void PurgeAllEntities(Map map)
        {
            int count = 0;
            foreach (var entity in map.Entities.Where(e => !e.IsDestroyed))
            {
                entity.IsDestroyed = true;
                count++;
            }

            if (count == 0)
            {
                Tolk.Speak("Es gibt derzeit keine aktiven Entitäten auf der Karte, die vernichtet werden könnten.");
                return;
            }

            Tolk.Speak($"Das Licht der Erlösung. {count} dunkle Entitäten wurden auf der gesamten Karte augenblicklich zu Asche verbrannt.");
        }

        public static void CaptureAllEntities(Map map)
        {
            int count = 0;
            foreach (var entity in map.Entities.Where(e => !e.IsDestroyed && !e.IsCaptured))
            {
                entity.IsCaptured = true;
                count++;
            }

            if (count == 0)
            {
                Tolk.Speak("Es gibt keine freien Entitäten mehr, die gefesselt werden könnten.");
                return;
            }

            Tolk.Speak($"Eindämmungsprotokoll aktiviert! {count} dunkle Entitäten wurden sofort in Ketten gelegt und sicher in die Haltezellen teleportiert.");
        }

        public static void SubjugateAllEntities(Map map)
        {
            int count = 0;
            foreach (var entity in map.Entities.Where(e => !e.IsDestroyed && !e.IsSubjugated))
            {
                entity.IsSubjugated = true;
                count++;
            }

            if (count == 0)
            {
                Tolk.Speak("Es gibt keine Entitäten, die noch unterworfen werden müssen.");
                return;
            }

            Tolk.Speak($"Der Wille der Leere ist gebrochen. {count} Schrecken der Finsternis wurden dauerhaft gefügig gemacht und gehorchen ab sofort ausschließlich deinen Befehlen.");
        }
    }
}
