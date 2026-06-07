using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess_UniversalPatcher
{
    public class Mechanoid
    {
        public string Name { get; set; }
        public bool IsFriendly { get; set; }
        public float HealthPercent { get; set; }
        public bool IsDestroyed { get; set; }
    }

    public class MechCluster
    {
        public string Name { get; set; }
        public List<Mechanoid> Mechs { get; set; } = new List<Mechanoid>();
        public bool IsDestroyed { get; set; }
    }

    public static class MechHacker
    {
        public static void HackMechanoid(Mechanoid mech)
        {
            if (mech.IsDestroyed)
            {
                Tolk.Speak($"Der Mechanoid {mech.Name} ist bereits zerstört und kann nicht gehackt werden.");
                return;
            }

            if (mech.IsFriendly)
            {
                Tolk.Speak($"Der Mechanoid {mech.Name} gehört bereits zu unserer Fraktion.");
                return;
            }

            mech.IsFriendly = true;
            Tolk.Speak($"Feindlicher Mechanoid {mech.Name} wurde erfolgreich gehackt und kämpft nun für uns.");
        }

        public static void RepairFriendlyMechs(List<Mechanoid> mechs)
        {
            var friendlyMechs = mechs.Where(m => m.IsFriendly && !m.IsDestroyed && m.HealthPercent < 100f).ToList();

            if (friendlyMechs.Count == 0)
            {
                Tolk.Speak("Es gibt keine verbündeten Mechanoiden, die repariert werden müssen.");
                return;
            }

            foreach (var mech in friendlyMechs)
            {
                mech.HealthPercent = 100f;
            }

            Tolk.Speak($"Reparatur abgeschlossen. {friendlyMechs.Count} verbündete Mechanoiden sind wieder auf hundert Prozent Gesundheit.");
        }

        public static void DestroyMechCluster(MechCluster cluster)
        {
            if (cluster.IsDestroyed)
            {
                Tolk.Speak($"Das Mechanoiden-Cluster {cluster.Name} ist bereits zerstört.");
                return;
            }

            cluster.IsDestroyed = true;
            int destroyedMechs = 0;

            foreach (var mech in cluster.Mechs)
            {
                if (!mech.IsDestroyed)
                {
                    mech.IsDestroyed = true;
                    mech.HealthPercent = 0f;
                    destroyedMechs++;
                }
            }

            Tolk.Speak($"Mechanoiden-Cluster {cluster.Name} wurde sofort vernichtet. {destroyedMechs} feindliche Einheiten wurden ausgeschaltet.");
        }
    }
}
