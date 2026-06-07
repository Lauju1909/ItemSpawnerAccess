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

            Tolk.Speak("Starte Tests für Archotech- und Bionik-Chirurgen.");
            p.InitializeMockBody();
            BionicSurgeon.RegrowMissingParts(p);
            BionicSurgeon.HealScarsAndDiseases(p);
            BionicSurgeon.InstallImplant(p, "Rechtes Auge", "Archotech-Auge");
            BionicSurgeon.InstallImplant(p, "Linker Arm", "Bionischer Arm");

            Tolk.Speak("Starte Tests für Mechanoiden- und Roboter-Hacker.");
            
            Mechanoid enemyScyther = new Mechanoid { Name = "Scyther", IsFriendly = false, HealthPercent = 100f };
            Mechanoid alliedLancer = new Mechanoid { Name = "Lancer", IsFriendly = true, HealthPercent = 30f };
            List<Mechanoid> mapMechs = new List<Mechanoid> { enemyScyther, alliedLancer };
            
            MechHacker.HackMechanoid(enemyScyther);
            MechHacker.RepairFriendlyMechs(mapMechs);

            MechCluster cluster = new MechCluster { Name = "Alpha-Cluster" };
            cluster.Mechs.Add(new Mechanoid { Name = "Pikeman", IsFriendly = false, HealthPercent = 100f });
            cluster.Mechs.Add(new Mechanoid { Name = "Centipede", IsFriendly = false, HealthPercent = 100f });
            MechHacker.DestroyMechCluster(cluster);

            Tolk.Speak("Starte Tests für Fraktions- und Diplomatie-Manager.");
            Faction empire = new Faction { Name = "Das gefallene Imperium", Goodwill = 0 };
            Faction pirates = new Faction { Name = "Die wilden Piraten", Goodwill = -100, IsAtWar = true };

            DiplomacyManager.SetGoodwill(empire, 100);
            DiplomacyManager.SetGoodwill(pirates, -100);
            DiplomacyManager.ForcePeaceTreaty(pirates);
            DiplomacyManager.ForcePeaceTreaty(empire);

            Tolk.Speak("Starte Tests für Ereignis- und Wetter-Manipulator.");
            EventManager.StartCondition(gameMap, "Giftiger Niederschlag");
            EventManager.StartCondition(gameMap, "Sonnenfinsternis");
            EventManager.EndCondition(gameMap, "Giftiger Niederschlag");
            EventManager.ChangeWeather(gameMap, "Schnee");
            EventManager.ChangeWeather(gameMap, "Regen");

            Tolk.Speak("Starte Tests für Forschungs- und Technologie-Hacker.");
            ResearchManager researchManager = new ResearchManager();
            researchManager.InitializeMockResearch();
            
            ResearchHacker.FinishCurrentResearch(researchManager); // Soll Elektrizität abschließen
            ResearchHacker.UnlockAllTechnologies(researchManager); // Soll die restlichen 3 abschließen
            ResearchHacker.UnlockAllTechnologies(researchManager); // Soll melden, dass bereits alles frei ist

            Tolk.Speak("Starte Tests für Psycho- und Mental-Manager.");
            p.Mind.MoodPercentage = 15f;
            p.Mind.CurrentMentalState = new MentalState { Name = "Berserker", IsActive = true };

            MentalManager.EndMentalBreak(p);
            MentalManager.EndMentalBreak(p); // Test, wenn keiner aktiv ist
            MentalManager.MaximizeMood(p);

            Tolk.Speak("Starte Tests für Alters- und Jugend-Manipulator.");
            p.Age.BiologicalAge = 87.5f;
            AgeManipulator.MakeYoungAgain(p);
            AgeManipulator.MakeYoungAgain(p); // Test, ob bereits jung

            Tolk.Speak("Starte Tests für Psycast- und Psi-Meister.");
            p.Psi.PsylinkLevel = 1;
            p.Psi.PsychoFocus = 0.2f;
            p.Psi.NeuralHeat = 80f;

            PsycastMaster.MaximizePsylink(p);
            PsycastMaster.RechargePsychoFocus(p);
            PsycastMaster.ClearNeuralHeat(p);

            Tolk.Speak("Starte Tests für Bedürfnis- und Needs-Maximierer.");
            p.IsColonist = true;
            Pawn p2 = new Pawn { Name = "Gabi", IsColonist = true };
            gameMap.AllPawns.Add(p);
            gameMap.AllPawns.Add(p2);

            NeedsMaximizer.MaximizePawnNeeds(p);
            NeedsMaximizer.MaximizeAllColonistsNeeds(gameMap);

            Tolk.Speak("Starte Tests für Eigenschaften- und Trait-Manager.");
            p.Story.Traits.Add("Pyromane");

            TraitManager.AddTrait(p, "Zäh");
            TraitManager.AddTrait(p, "Zäh"); // Soll sagen: Besitzt er schon
            TraitManager.RemoveTrait(p, "Pyromane");
            TraitManager.RemoveTrait(p, "Kannibale"); // Soll sagen: Besitzt er nicht

            Tolk.Speak("Starte Tests für Beziehungs- und Familien-Manager.");
            RelationManager.AddRelation(p, p2, "Ehepartner");
            RelationManager.AddRelation(p, p2, "Ehepartner"); // Schon vorhanden
            RelationManager.AddRelation(p, p2, "Rivale");
            RelationManager.RemoveRelation(p, p2, "Rivale");
            RelationManager.RemoveRelation(p, p2, "Geschwister"); // Nicht vorhanden

            Tolk.Speak("Starte Tests für Kolonisten-Kloner.");
            PawnCloner.CloneColonist(gameMap, p);

            Tolk.Speak("Starte Tests für Gott-Modus Bauherr.");
            gameMap.Constructions.Add(new ConstructionSite { BuildingName = "Holzwand", IsBlueprint = true });
            gameMap.Constructions.Add(new ConstructionSite { BuildingName = "Holzwand", IsBlueprint = true });
            gameMap.Constructions.Add(new ConstructionSite { BuildingName = "Stahltür", IsFrame = true });
            
            GodModeBuilder.FinishAllConstructions(gameMap);
            GodModeBuilder.FinishAllConstructions(gameMap); // Test: Keine Baustellen vorhanden

            Tolk.Speak("Alle Tests abgeschlossen.");
        }
    }
}
