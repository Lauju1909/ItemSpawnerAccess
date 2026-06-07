using System;

namespace RimWorldAccess_UniversalPatcher
{
    public static class TestProgram
    {
        public static void Main()
        {
            Tolk.Speak("Starte Tests für Pawn-Fähigkeiten-Mutator.");
            Pawn p = new Pawn { Name = "Hans" };
            PawnAbilityMutator.AddAbility(p, "Heilung");
            PawnAbilityMutator.AddAbility(p, "Heilung");
            PawnAbilityMutator.ListAbilities(p);
            PawnAbilityMutator.RemoveAbility(p, "Heilung");
            PawnAbilityMutator.ListAbilities(p);

            Tolk.Speak("Starte Tests für Energie- und Stromnetz-Manager.");
            PowerGrid grid = new PowerGrid { TotalPowerProduction = 1500, TotalPowerConsumption = 1200, StoredEnergy = 500, BatteryCapacity = 1000 };
            PowerGridManager.CheckGridStatus(grid);
            
            grid.TotalPowerConsumption = 2000;
            PowerGridManager.CheckGridStatus(grid);

            Tolk.Speak("Starte Tests für Ideologie- und Gesinnungs-Manager.");
            Ideology technokratie = new Ideology { Name = "Technokratie" };
            IdeologyManager.CheckIdeologyStatus(p);
            IdeologyManager.ConvertPawn(p, technokratie);
            IdeologyManager.CheckIdeologyStatus(p);
            IdeologyManager.ChangeCertainty(p, 25.5f);
            IdeologyManager.ChangeCertainty(p, -10f);

            Tolk.Speak("Starte Tests für Karten- und Terrain-Manipulator.");
            Map gameMap = new Map();
            TerrainManipulator.RemoveFogOfWar(gameMap);
            TerrainManipulator.RemoveFogOfWar(gameMap); // Testen, ob es bereits deaktiviert ist
            TerrainManipulator.SmoothBaseTerrain(gameMap);
            TerrainManipulator.ReclaimLand(gameMap);

            Tolk.Speak("Alle Tests abgeschlossen.");
        }
    }
}
