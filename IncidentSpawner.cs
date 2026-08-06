using System;

namespace RimWorldAccess_UniversalPatcher
{
    public static class IncidentSpawner
    {
        public static void SpawnShipCrash(Map map)
        {
            // Simuliert das Spawnen eines abgestürzten Schiffs
            Tolk.Speak("Ein Raumschiffteil ist krachend auf der Karte abgestürzt. Es enthält extrem seltene Ressourcen, fortschrittliche Bauteile und Plasteel.");
        }

        public static void SpawnThrumbos(Map map)
        {
            // Simuliert das Erscheinen einer Thrumbo-Herde
            Tolk.Speak("Eine majestätische Herde seltener Thrumbos ist auf der Karte aufgetaucht. Ihr Horn und ihr Fell sind unschätzbar wertvoll.");
        }

        public static void SpawnOrbitalTrader(Map map, string traderType = "Exotischer Güterhändler")
        {
            // Simuliert einen neuen orbitalen Händler
            Tolk.Speak($"Ein orbitaler Händler vom Typ '{traderType}' hat unsere Funkfeuer erfasst und befindet sich nun im Orbit. Der Handel kann beginnen.");
        }

        public static void SpawnCaravan(Map map, string factionName = "verbündeten Fraktion")
        {
            // Simuliert das Eintreffen einer Karawane
            Tolk.Speak($"Eine Händlerkarawane der {factionName} hat unsere Basis erreicht. Sie schlagen in Kürze ihr Lager auf.");
        }

        public static void SpawnMeteorite(Map map, string material = "Gold")
        {
            // Simuliert einen gezielten Meteoriteneinschlag
            Tolk.Speak($"Ein massiver Meteorit aus reinem {material} ist mit gewaltiger Wucht vom Himmel gestürzt. Die Ressourcen können nun abgebaut werden.");
        }
    }
}
