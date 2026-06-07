using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class Map
    {
        public bool FogOfWarEnabled { get; set; } = true;
        public List<TerrainCell> Cells { get; set; } = new List<TerrainCell>();

        public Map()
        {
            // Mock map initialization
            for (int i = 0; i < 100; i++)
            {
                Cells.Add(new TerrainCell { Type = i % 10 == 0 ? "Schlamm" : (i % 5 == 0 ? "Wasser" : "Stein") });
            }
        }
    }

    public class TerrainCell
    {
        public string Type { get; set; }
        public bool IsSmoothed { get; set; }
    }

    public static class TerrainManipulator
    {
        public static void RemoveFogOfWar(Map map)
        {
            if (!map.FogOfWarEnabled)
            {
                Tolk.Speak("Der Nebel des Krieges ist bereits deaktiviert. Die Karte ist vollständig sichtbar.");
                return;
            }

            map.FogOfWarEnabled = false;
            Tolk.Speak("Nebel des Krieges wurde entfernt. Alle Bereiche der Karte sind nun aufgedeckt und sichtbar.");
        }

        public static void SmoothBaseTerrain(Map map)
        {
            int count = 0;
            foreach (var cell in map.Cells)
            {
                if (cell.Type == "Stein" && !cell.IsSmoothed)
                {
                    cell.IsSmoothed = true;
                    count++;
                }
            }

            Tolk.Speak($"Böden wurden geglättet. Insgesamt {count} Kacheln wurden erfolgreich bearbeitet.");
        }

        public static void ReclaimLand(Map map)
        {
            int count = 0;
            foreach (var cell in map.Cells)
            {
                if (cell.Type == "Schlamm" || cell.Type == "Wasser")
                {
                    cell.Type = "Erde";
                    count++;
                }
            }

            Tolk.Speak($"Terrain wurde trockengelegt. {count} Kacheln aus Schlamm oder Wasser sind nun fruchtbare Erde und bebaubar.");
        }
    }
}
