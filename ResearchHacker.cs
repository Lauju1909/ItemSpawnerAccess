using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    public class ResearchProjectDef
    {
        public string Name { get; set; }
        public bool IsFinished { get; set; }
    }

    public class ResearchManager
    {
        public ResearchProjectDef CurrentProject { get; set; }
        public List<ResearchProjectDef> AllProjects { get; set; } = new List<ResearchProjectDef>();

        public void InitializeMockResearch()
        {
            AllProjects.Add(new ResearchProjectDef { Name = "Elektrizität" });
            AllProjects.Add(new ResearchProjectDef { Name = "Mikroelektronik" });
            AllProjects.Add(new ResearchProjectDef { Name = "Bionik" });
            AllProjects.Add(new ResearchProjectDef { Name = "Raumschiffbau" });
            
            CurrentProject = AllProjects[0]; // Start with Elektrizität
        }
    }

    public static class ResearchHacker
    {
        public static void FinishCurrentResearch(ResearchManager manager)
        {
            if (manager.CurrentProject == null)
            {
                Tolk.Speak("Es ist derzeit kein Forschungsprojekt ausgewählt.");
                return;
            }

            if (manager.CurrentProject.IsFinished)
            {
                Tolk.Speak($"Das Projekt {manager.CurrentProject.Name} ist bereits abgeschlossen.");
                return;
            }

            manager.CurrentProject.IsFinished = true;
            string projectName = manager.CurrentProject.Name;
            manager.CurrentProject = null; // Auswahl nach Abschluss aufheben

            Tolk.Speak($"Das Forschungsprojekt {projectName} wurde sofort erfolgreich abgeschlossen!");
        }

        public static void UnlockAllTechnologies(ResearchManager manager)
        {
            int unlockedCount = 0;
            foreach (var project in manager.AllProjects)
            {
                if (!project.IsFinished)
                {
                    project.IsFinished = true;
                    unlockedCount++;
                }
            }

            manager.CurrentProject = null;

            if (unlockedCount == 0)
            {
                Tolk.Speak("Gott-Modus: Alle Technologien im Spiel sind bereits freigeschaltet.");
            }
            else
            {
                Tolk.Speak($"Gott-Modus aktiviert: {unlockedCount} verbleibende Technologien wurden auf einen Schlag freigeschaltet. Alle Forschungen im gesamten Spiel sind nun abgeschlossen!");
            }
        }
    }
}
