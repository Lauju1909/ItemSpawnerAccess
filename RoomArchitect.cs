using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class Room
    {
        public string Name { get; set; }
        public float Cleanliness { get; set; } = 0f;
        public float Temperature { get; set; } = 20f;
        public bool IsTemperatureLocked { get; set; } = false;
        public float Beauty { get; set; } = 0f;
        public float Wealth { get; set; } = 0f;
    }

    public static class RoomArchitect
    {
        public static void PurifyAndBeautifyRoom(Room room)
        {
            if (room == null)
            {
                Tolk.Speak("Fehler: Es ist kein Raum ausgewählt.");
                return;
            }

            // Schmutz (Blut, Erbrochenes, Dreck) restlos entfernen
            room.Cleanliness = 100f; // Entspricht maximaler Sterilität

            // Wohlfühltemperatur erzwingen und festnageln
            room.Temperature = 21.0f;
            room.IsTemperatureLocked = true;

            // Schönheit und Reichtum aufs absolute Maximum setzen für extremen Mood-Boost
            room.Beauty = float.MaxValue;
            room.Wealth = float.MaxValue;

            Tolk.Speak($"Der Raum '{room.Name}' wurde restlos von allem Schmutz gereinigt. Die Temperatur ist nun dauerhaft auf angenehme 21 Grad festgenagelt und der Raum wurde in einen wahren Palast verwandelt. Wer hier eintritt, wird unglaubliche Glücksgefühle erleben.");
        }
    }
}
