using System;

namespace RimWorldAccess_UniversalPatcher
{
    public class PowerGrid
    {
        public float TotalPowerProduction { get; set; }
        public float TotalPowerConsumption { get; set; }
        public float StoredEnergy { get; set; }
        public float BatteryCapacity { get; set; }

        public float NetPower => TotalPowerProduction - TotalPowerConsumption;
    }

    public static class PowerGridManager
    {
        public static void CheckGridStatus(PowerGrid grid)
        {
            float net = grid.NetPower;
            string status = net >= 0 ? "Überschuss" : "Defizit";
            Tolk.Speak($"Stromnetz-Status: Produktion {grid.TotalPowerProduction} Watt, Verbrauch {grid.TotalPowerConsumption} Watt. Netto: {Math.Abs(net)} Watt {status}.");
            
            if (grid.BatteryCapacity > 0)
            {
                float percentage = (grid.StoredEnergy / grid.BatteryCapacity) * 100f;
                Tolk.Speak($"Batteriespeicher bei {percentage:F1} Prozent.");
            }
        }
    }
}
