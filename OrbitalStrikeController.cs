using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class EnemyGroup
    {
        public string Name { get; set; }
        public int MemberCount { get; set; }
        public IntVec3 Position { get; set; }
        public bool IsDestroyed { get; set; }
    }

    public static class OrbitalStrikeController
    {
        public static void CallOrbitalBombardment(EnemyGroup target)
        {
            if (target == null || target.IsDestroyed)
            {
                Tolk.Speak("Fehler bei der Zielerfassung. Die feindliche Gruppe existiert nicht oder wurde bereits restlos vernichtet.");
                return;
            }

            target.IsDestroyed = true;
            target.MemberCount = 0;
            Tolk.Speak($"Zielkoordinaten bestätigt! Ein gewaltiges orbitales Flächenbombardement regnet gnadenlos auf '{target.Name}' herab. Die feindliche Stellung wurde in einem flammenden Inferno vollständig pulverisiert!");
        }

        public static void CallOrbitalLaser(EnemyGroup target)
        {
            if (target == null || target.IsDestroyed)
            {
                Tolk.Speak("Laser-Fehlfunktion: Das Zielgebiet ist nicht mehr verfügbar oder der Feind ist bereits Asche.");
                return;
            }

            target.IsDestroyed = true;
            target.MemberCount = 0;
            Tolk.Speak($"Uplink hergestellt! Ein gleißender, konzentrierter orbitaler Laserstrahl schneidet durch '{target.Name}'. Die Belagerer wurden sofort vaporisiert. Keine Überlebenden.");
        }
    }
}
