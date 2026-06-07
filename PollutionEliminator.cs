using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class MapPollution
    {
        public float OverallPollutionLevel { get; set; } = 0f;
        public int ToxicWastepacksCount { get; set; } = 0;
    }

    // Erweiterung der Mock-Map-Klasse
    public partial class Map
    {
        public MapPollution Pollution { get; set; } = new MapPollution();
    }

    public static class PollutionEliminator
    {
        public static void PurifyMap(Map map)
        {
            if (map == null)
            {
                Tolk.Speak("Keine Karte geladen.");
                return;
            }

            map.Pollution.OverallPollutionLevel = 0f;
            int vaporizedCount = map.Pollution.ToxicWastepacksCount;
            map.Pollution.ToxicWastepacksCount = 0;

            Tolk.Speak($"Die Luft ist rein! Sämtliche toxische Verschmutzung wurde auf einen Schlag von der Karte getilgt. Zudem wurden {vaporizedCount} Giftmüll-Pakete restlos vaporisiert. Die Natur kann aufatmen.");
        }

        public static void PolluteMap(Map map)
        {
            if (map == null)
            {
                Tolk.Speak("Keine Karte geladen.");
                return;
            }

            map.Pollution.OverallPollutionLevel = 100f; // Maximale Verseuchung
            Tolk.Speak("Toxischer Fallout eingeleitet! Die gesamte Karte ist nun extrem verschmutzt und ein tödliches Ödland. Feinde und schwache Tiere werden dem Gift rasch erliegen.");
        }
    }
}
