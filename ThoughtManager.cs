using System;
using System.Collections.Generic;
using System.Linq;

namespace RimWorldAccess_UniversalPatcher
{
    public class Thought
    {
        public string Name { get; set; }
        public float MoodOffset { get; set; }
        public bool IsNegative => MoodOffset < 0;
    }



    public static class ThoughtManager
    {
        public static void EraseNegativeThoughts(Pawn pawn)
        {
            if (pawn == null)
            {
                Tolk.Speak("Fehler: Kein Kolonist ausgewählt.");
                return;
            }

            int removedCount = pawn.Mind.Memories.RemoveAll(t => t.IsNegative);

            if (removedCount == 0)
            {
                Tolk.Speak($"Der Verstand von {pawn.Name} ist bereits frei von negativen Gedanken.");
            }
            else
            {
                Tolk.Speak($"Neuro-Wäsche abgeschlossen! {removedCount} negative Gedanken und quälende Erinnerungen (wie 'Ohne Tisch gegessen' oder 'Verwandter gestorben') wurden restlos aus dem Verstand von {pawn.Name} gelöscht.");
            }
        }

        public static void ImplantGodlikeEcstasy(Pawn pawn)
        {
            if (pawn == null)
            {
                Tolk.Speak("Fehler: Kein Kolonist ausgewählt.");
                return;
            }

            // Prüfen, ob der Gedanke nicht schon existiert, um Spam zu vermeiden
            if (!pawn.Mind.Memories.Any(t => t.Name == "Gottgleiche Ekstase"))
            {
                pawn.Mind.Memories.Add(new Thought { Name = "Gottgleiche Ekstase", MoodOffset = 100f });
            }

            Tolk.Speak($"Künstliche Endorphinausschüttung maximiert! Ein permanenter Zustand der 'Gottgleichen Ekstase' (+100 Laune) wurde in das Gehirn von {pawn.Name} eingepflanzt. Niemand wird jemals wieder einen mentalen Zusammenbruch erleiden.");
        }
    }
}
