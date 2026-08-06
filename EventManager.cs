using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class GameCondition
    {
        public string Name { get; set; }
        public bool IsActive { get; set; }
    }

    public class WeatherManager
    {
        public string CurrentWeather { get; set; } = "Klar";
    }

    // Extended Map mock
    public partial class Map
    {
        public WeatherManager Weather { get; set; } = new WeatherManager();
        public List<GameCondition> Conditions { get; set; } = new List<GameCondition>();
    }

    public static class EventManager
    {
        public static void StartCondition(Map map, string conditionName)
        {
            var condition = map.Conditions.Find(c => c.Name.Equals(conditionName, StringComparison.OrdinalIgnoreCase));
            if (condition == null)
            {
                condition = new GameCondition { Name = conditionName };
                map.Conditions.Add(condition);
            }

            if (condition.IsActive)
            {
                Tolk.Speak($"Das Ereignis {conditionName} ist bereits aktiv.");
                return;
            }

            condition.IsActive = true;
            Tolk.Speak($"Das Ereignis {conditionName} wurde erfolgreich gestartet.");
        }

        public static void EndCondition(Map map, string conditionName)
        {
            var condition = map.Conditions.Find(c => c.Name.Equals(conditionName, StringComparison.OrdinalIgnoreCase));
            if (condition == null || !condition.IsActive)
            {
                Tolk.Speak($"Das Ereignis {conditionName} ist derzeit nicht aktiv und kann nicht beendet werden.");
                return;
            }

            condition.IsActive = false;
            Tolk.Speak($"Das Ereignis {conditionName} wurde sofort beendet. Normaler Zustand wiederhergestellt.");
        }

        public static void ChangeWeather(Map map, string weatherName)
        {
            if (map.Weather.CurrentWeather.Equals(weatherName, StringComparison.OrdinalIgnoreCase))
            {
                Tolk.Speak($"Das Wetter ist bereits {weatherName}.");
                return;
            }

            string oldWeather = map.Weather.CurrentWeather;
            map.Weather.CurrentWeather = weatherName;
            Tolk.Speak($"Das globale Wetter wurde von {oldWeather} auf {weatherName} geändert.");
        }
    }
}
