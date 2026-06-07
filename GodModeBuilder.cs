using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class ConstructionSite
    {
        public bool IsBlueprint { get; set; }
        public bool IsFrame { get; set; }
        public bool IsCompleted { get; set; }
        public string BuildingName { get; set; }
    }

    // Erweiterung der Mock-Map-Klasse
    public partial class Map
    {
        public List<ConstructionSite> Constructions { get; set; } = new List<ConstructionSite>();
    }

    public static class GodModeBuilder
    {
        public static void FinishAllConstructions(Map map)
        {
            int completedCount = 0;

            foreach (var site in map.Constructions)
            {
                if ((site.IsBlueprint || site.IsFrame) && !site.IsCompleted)
                {
                    site.IsCompleted = true;
                    site.IsBlueprint = false;
                    site.IsFrame = false;
                    completedCount++;
                }
            }

            if (completedCount == 0)
            {
                Tolk.Speak("Es gibt derzeit keine aktiven Baupläne oder Gerüste auf der Karte.");
            }
            else
            {
                Tolk.Speak($"{completedCount} Baupläne und Gerüste wurden erfolgreich im Gott-Modus fertiggestellt. Ressourcen oder Arbeit waren nicht erforderlich.");
            }
        }
    }
}
