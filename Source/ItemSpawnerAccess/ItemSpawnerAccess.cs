using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace ItemSpawnerAccess
{
    // ---------------------------------------------------------
    //  Tolk-Wrapper – falls RimWorldAccess aktiv ist, nutzen
    //  wir dessen DLL; ansonsten fallen wir auf Messages zurück.
    // ---------------------------------------------------------
    public static class TTS
    {
        private static bool _tolkLoaded;
        private static bool _tolkTried;

        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Load();
        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void Tolk_Unload();
        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Tolk_Output([MarshalAs(UnmanagedType.LPWStr)] string str,
                                               [MarshalAs(UnmanagedType.Bool)] bool interrupt);
        [DllImport("Tolk.dll", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool Tolk_IsLoaded();

        private static void EnsureLoaded()
        {
            if (_tolkTried) return;
            _tolkTried = true;
            try
            {
                Tolk_Load();
                _tolkLoaded = Tolk_IsLoaded();
            }
            catch
            {
                _tolkLoaded = false;
            }
        }

        public static void Say(string text, bool interrupt = true)
        {
            if (string.IsNullOrEmpty(text)) return;
            EnsureLoaded();
            if (_tolkLoaded)
            {
                try { Tolk_Output(text, interrupt); return; } catch { }
            }
            // Fallback: Sprachmeldung im Spiel
            Messages.Message(text, MessageTypeDefOf.NeutralEvent, false);
        }
    }

    // ---------------------------------------------------------
    //  Barrierefreies Listenmenü (ersetzt FloatMenu komplett)
    // ---------------------------------------------------------
    public static class AccessibleWindowlessMenu
    {
        private static string _title;
        private static System.Collections.Generic.List<(string Label, System.Action Action)> _allItems;
        private static System.Collections.Generic.List<(string Label, System.Action Action)> _filteredItems;
        private static int _selectedIndex;
        private static string _searchString = "";
        
        public static bool IsActive => _allItems != null;

        public static void Open(string title, System.Collections.Generic.List<(string Label, System.Action Action)> items)
        {
            if (items == null) items = new System.Collections.Generic.List<(string, System.Action)>();
            _title = title;
            _allItems = items;
            _filteredItems = new System.Collections.Generic.List<(string, System.Action)>(_allItems);
            _selectedIndex = 0;
            _searchString = "";
            AnnounceSelected();
        }

        public static void Close()
        {
            _allItems = null;
            _filteredItems = null;
        }

        private static void UpdateFilter()
        {
            if (string.IsNullOrEmpty(_searchString))
            {
                _filteredItems = new System.Collections.Generic.List<(string, System.Action)>(_allItems);
            }
            else
            {
                _filteredItems = _allItems.FindAll(i => i.Label.ToLower().Contains(_searchString));
            }
            _selectedIndex = 0;
        }

        private static void AnnounceSelected()
        {
            if (_filteredItems == null || _filteredItems.Count == 0)
            {
                string prefix = _searchString.Length > 0 ? $"[{_searchString}] " : "";
                TTS.Say($"{prefix}Keine Ergebnisse.");
                return;
            }
            string sPrefix = _searchString.Length > 0 ? $"[{_searchString}] " : "";
            TTS.Say($"{sPrefix}{_filteredItems[_selectedIndex].Label}. {_selectedIndex + 1} {Verse.Translator.Translate("ISA_Of")} {_filteredItems.Count}");
        }

        public static void HandleInput()
        {
            if (!IsActive) return;

            UnityEngine.Event e = UnityEngine.Event.current;
            if (e.type != UnityEngine.EventType.KeyDown) return;

            UnityEngine.KeyCode key = e.keyCode;

            if (key == UnityEngine.KeyCode.Escape)
            {
                if (_searchString.Length > 0)
                {
                    _searchString = "";
                    UpdateFilter();
                    TTS.Say("Suche geleert.");
                    AnnounceSelected();
                }
                else
                {
                    TTS.Say(Verse.Translator.Translate("ISA_Closed"));
                    Close();
                }
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.Return || key == UnityEngine.KeyCode.KeypadEnter)
            {
                if (_filteredItems != null && _filteredItems.Count > 0)
                {
                    var action = _filteredItems[_selectedIndex].Action;
                    Close();
                    action?.Invoke();
                }
                else
                {
                    TTS.Say("Keine Aktion verfügbar.");
                }
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.UpArrow)
            {
                if (_filteredItems == null || _filteredItems.Count == 0) return;
                if (_selectedIndex > 0)
                {
                    _selectedIndex--;
                    AnnounceSelected();
                }
                else
                {
                    Verse.Sound.SoundStarter.PlayOneShotOnCamera(RimWorld.SoundDefOf.ClickReject, null);
                    TTS.Say(Verse.Translator.Translate("ISA_BoundaryTop"));
                }
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.DownArrow)
            {
                if (_filteredItems == null || _filteredItems.Count == 0) return;
                if (_selectedIndex < _filteredItems.Count - 1)
                {
                    _selectedIndex++;
                    AnnounceSelected();
                }
                else
                {
                    Verse.Sound.SoundStarter.PlayOneShotOnCamera(RimWorld.SoundDefOf.ClickReject, null);
                    TTS.Say(Verse.Translator.Translate("ISA_BoundaryBottom"));
                }
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.Home)
            {
                if (_filteredItems == null || _filteredItems.Count == 0) return;
                _selectedIndex = 0;
                AnnounceSelected();
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.End)
            {
                if (_filteredItems == null || _filteredItems.Count == 0) return;
                _selectedIndex = _filteredItems.Count - 1;
                AnnounceSelected();
                e.Use();
                return;
            }

            if (key == UnityEngine.KeyCode.Backspace)
            {
                if (_searchString.Length > 0)
                {
                    _searchString = _searchString.Substring(0, _searchString.Length - 1);
                    UpdateFilter();
                    if (_searchString.Length == 0)
                        TTS.Say("Suche geleert.");
                    else
                        TTS.Say("Suche: " + _searchString);
                    AnnounceSelected();
                }
                e.Use();
                return;
            }

            // Filtering
            char c = e.character;
            if (c != 0 && !char.IsControl(c))
            {
                _searchString += c.ToString().ToLower();
                UpdateFilter();
                TTS.Say("Suche: " + _searchString);
                AnnounceSelected();
                e.Use();
            }
        }
    }

            

    // ---------------------------------------------------------
    //  Hilfsmethode: einfaches Menü öffnen
    // ---------------------------------------------------------
    public static class MenuHelper
    {
        public static void Open(string title, List<(string, Action)> items)
        {
            if (items == null || items.Count == 0)
            {
                TTS.Say("ISA_NoEntries".Translate());
                Messages.Message("ISA_NoEntries".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            AccessibleWindowlessMenu.Open(title, items);
        }

        public static void SelectTargetCell(Verse.Map map, Action<Verse.IntVec3> onCellSelected)
        {
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }

            var items = new List<(string, Action)>
            {
                ("In der Nähe eines bestimmten Kolonisten", () => 
                {
                    var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.NameShortColored.Resolve()).ToList();
                    if (colonists.Count == 0) { TTS.Say("Keine Kolonisten verfügbar."); return; }
                    var colItems = colonists.Select(p => 
                    {
                        string label = p.LabelShort;
                        Action act = () => onCellSelected(p.Position);
                        return (label, act);
                    }).ToList();
                    Open("Kolonisten auswählen", colItems);
                }),
                ("In einer bestimmten Zone", () => 
                {
                    var zones = map.zoneManager.AllZones.OrderBy(z => z.label).ToList();
                    if (zones.Count == 0) { TTS.Say("Keine Zonen verfügbar."); return; }
                    var zItems = zones.Select(z => 
                    {
                        string label = z.label;
                        Action act = () => 
                        {
                            var cell = System.Linq.Enumerable.FirstOrDefault(z.Cells);
                            if (cell.IsValid) onCellSelected(cell);
                            else TTS.Say("Zone ist leer.");
                        };
                        return (label, act);
                    }).ToList();
                    Open("Zone auswählen", zItems);
                }),
                ("In der Kartenmitte", () => onCellSelected(map.Center))
            };
            Open("Wo soll gespawnt werden?", items);
        }
    }

    // ---------------------------------------------------------
    //  Initialisierung & Tastatur-Listener
    // ---------------------------------------------------------
    [StaticConstructorOnStartup]
    public static class ItemSpawnerAccessInit
    {
        static ItemSpawnerAccessInit()
        {
            var go = new GameObject("ItemSpawnerAccessListener");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<ItemSpawnerAccessListener>();
        }
    }

    public class ItemSpawnerAccessListener : MonoBehaviour
    {
        public void OnGUI()
        {
            if (AccessibleWindowlessMenu.IsActive)
            {
                AccessibleWindowlessMenu.HandleInput();
            }
        }

        public void Update()
        {
            // F11 ohne Modifier
            if (Input.GetKeyDown(KeyCode.F11)
                && !Input.GetKey(KeyCode.LeftShift)  && !Input.GetKey(KeyCode.RightShift)
                && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)
                && !Input.GetKey(KeyCode.LeftAlt)     && !Input.GetKey(KeyCode.RightAlt)
                && (int)Current.ProgramState == 2)
            {
                OpenMasterMenu();
            }
        }

        // ---------------------------------------------------
        //  MASTER-MENÜ (17 Einträge)
        // ---------------------------------------------------
        private void OpenMasterMenu()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_Master_ItemSpawner".Translate(),    OpenItemSpawner),
                ("ISA_Master_PawnEditor".Translate(),     OpenPawnEditor),
                ("Health & Bionics Editor",               OpenHealthEditor),
                ("Relationship Manager",                  OpenRelationshipManager),
                ("ISA_WeatherConditionsEvents".Translate(), OpenWeatherConditionsEventsMenu),
                ("Research & Tech Editor",                OpenResearchAndTechEditor),
                ("ISA_Master_Storyteller".Translate(),    OpenStoryteller),
                ("ISA_Master_BaseMapTools".Translate(),   OpenBaseMapTools),
                ("ISA_Master_NeedsMood".Translate(),      OpenNeedsMood),
                ("ISA_Master_ColonyEnemy".Translate(),    OpenColonyEnemy),
                ("ISA_Master_CaravanWorld".Translate(),   OpenCaravanWorld),
                ("ISA_Master_ArchotechMech".Translate(),  OpenArchotechMech),
                ("ISA_Master_SkillMaster".Translate(),    OpenSkillMaster),
                ("ISA_Master_BaseMaintenance".Translate(),OpenBaseMaintenance),
                ("ISA_Master_NatureControl".Translate(),  OpenNatureControl),
                ("ISA_Master_RoyaltyPsycast".Translate(), OpenRoyaltyPsycast),
                ("ISA_Master_BiotechGenetics".Translate(),OpenBiotechGenetics),
                ("ISA_Master_IdeologyBelief".Translate(), OpenIdeologyBelief),
                ("ISA_Master_AnimalTaming".Translate(),   OpenAnimalTaming),
                ("ISA_Master_FactionManager".Translate(), OpenFactionManager),
                ("ISA_Master_ColonyManager".Translate(),  OpenColonyManager),
                ("ISA_Master_QuestTrade".Translate(),     OpenQuestTrade),
                ("Entwickler- & Debug-Manager",               OpenDebugManager),
                ("Kamera- & Sichtfeld-Controller",            OpenCameraController),
                ("Zonen- & Raum-Analysator",                  OpenRoomAnalyzer),
            };
            TTS.Say("ISA_MasterMenuOpened".Translate());
            AccessibleWindowlessMenu.Open("ISA_MasterMenuTitle".Translate(), items);
        }

        // ---------------------------------------------------
        //  1) ITEM SPAWNER
        // ---------------------------------------------------
        
        
        
        
        
        private void OpenColonyManager()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>();
            items.Add(("ISA_HealAllColonists".Translate().ToString(), HealAllColonists));
            items.Add(("ISA_FeedAllColonists".Translate().ToString(), FeedAllColonists));
            items.Add(("ISA_CM_RecruitAllPrisoners".Translate().ToString(), CM_RecruitAllPrisoners2));

            TTS.Say("Kolonie-Manager-Menü");
            AccessibleWindowlessMenu.Open("Kolonie-Manager", items);
        }

        private void OpenQuestTrade()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("ISA_GenerateQuest".Translate(), () => GenerateRandomQuest()),
                ("ISA_SpawnTradeCaravan".Translate(), () => SpawnTradeCaravan()),
                ("ISA_CallOrbitalTrader".Translate(), () => CallOrbitalTrader())
            };

            TTS.Say("Quest- und Handels-Manager Menü");
            AccessibleWindowlessMenu.Open("Quest- und Handels-Manager", items);
        }

        private void GenerateRandomQuest()
        {
            RimWorld.QuestScriptDef questScriptDef = Verse.DefDatabase<RimWorld.QuestScriptDef>.GetRandom();
            if (questScriptDef != null)
            {
                float points = RimWorld.StorytellerUtility.DefaultThreatPointsNow(Verse.Find.CurrentMap);
                RimWorld.Quest quest = RimWorld.QuestUtility.GenerateQuestAndMakeAvailable(questScriptDef, points);
                TTS.Say("ISA_QuestGenerated".Translate());
            }
            else
            {
                TTS.Say("Fehler beim Generieren der Quest.");
            }
        }

        private void SpawnTradeCaravan()
        {
            var map = Verse.Find.CurrentMap;
            if (map != null)
            {
                RimWorld.IncidentParms parms = RimWorld.StorytellerUtility.DefaultParmsNow(RimWorld.IncidentDefOf.TraderCaravanArrival.category, map);
                parms.target = map;
                parms.faction = Verse.Find.FactionManager.RandomNonHostileFaction(false, false, false, RimWorld.TechLevel.Undefined);
                if (parms.faction != null && RimWorld.IncidentDefOf.TraderCaravanArrival.Worker.TryExecute(parms))
                {
                    TTS.Say("ISA_TradeCaravanSpawned".Translate());
                    return;
                }
            }
            TTS.Say("Fehler beim Spawnen der Handelskarawane.");
        }

        private void CallOrbitalTrader()
        {
            var map = Verse.Find.CurrentMap;
            if (map != null)
            {
                RimWorld.IncidentParms parms = RimWorld.StorytellerUtility.DefaultParmsNow(RimWorld.IncidentDefOf.OrbitalTraderArrival.category, map);
                if (RimWorld.IncidentDefOf.OrbitalTraderArrival.Worker.TryExecute(parms))
                {
                    TTS.Say("ISA_OrbitalTraderCalled".Translate());
                    return;
                }
            }
            TTS.Say("Fehler beim Rufen des orbitalen Händlers.");
        }


        private void HealAllColonists()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                Verse.HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
            }
            TTS.Say("Alle Kolonisten geheilt.");
        }

        private void FeedAllColonists()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                if (pawn.needs?.food != null)
                {
                    pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
                }
            }
            TTS.Say("Alle Kolonisten gefüttert.");
        }

        

        private void OpenWeatherController()
        {
            var weatherDefs = Verse.DefDatabase<Verse.WeatherDef>.AllDefsListForReading;
            var items = new System.Collections.Generic.List<(string, System.Action)>();
            
            foreach(var weather in weatherDefs)
            {
                string label = Verse.GenText.CapitalizeFirst(weather.label ?? weather.defName);
                Verse.WeatherDef capturedDef = weather;
                items.Add((label, () => ChangeWeather(capturedDef)));
            }

            TTS.Say("Wetter-Kontroll-Menü");
            AccessibleWindowlessMenu.Open("Wetter-Kontrolle", items);
        }

        private void ChangeWeather(Verse.WeatherDef weatherDef)
        {
            var map = Verse.Find.CurrentMap;
            if (map != null)
            {
                map.weatherManager.TransitionTo(weatherDef);
                string msg = "Wetter geändert zu " + (weatherDef.label ?? weatherDef.defName);
                Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.TaskCompletion, false);
                TTS.Say(msg);
            }
            else
            {
                TTS.Say(Verse.Translator.Translate("ISA_NoValidTarget"));
            }
        }

        private void OpenRaidTrigger()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("ISA_TriggerRandomRaid".Translate(), () => TriggerRandomRaid()),
            };
            TTS.Say("Raid-Auslöser-Menü");
            AccessibleWindowlessMenu.Open("Raid auslösen", items);
        }

        private void TriggerRandomRaid()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say(Verse.Translator.Translate("ISA_NoValidTarget")); return; }
            RimWorld.IncidentParms parms = RimWorld.StorytellerUtility.DefaultParmsNow(RimWorld.IncidentCategoryDefOf.ThreatBig, map);
            parms.forced = true;
            if (RimWorld.IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                TTS.Say("Feindlichen Raid ausgelöst.");
            }
            else
            {
                TTS.Say("Fehler beim Auslösen des Raids.");
            }
        }

        private void OpenFactionManager()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MakePeaceWithAll".Translate(), () => MakePeaceWithAll()),
                ("ISA_MaxReputationWithAll".Translate(), () => MaxReputationWithAll()),
                ("ISA_ManageSpecificFaction".Translate(), () => OpenFactionList())
            };
            MenuHelper.Open("ISA_FactionManager".Translate(), items);
        }

        private void MakePeaceWithAll()
        {
            int count = 0;
            foreach (var faction in Verse.Find.FactionManager.AllFactionsVisible)
            {
                if (faction != RimWorld.Faction.OfPlayer && faction.HostileTo(RimWorld.Faction.OfPlayer))
                {
                    faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Neutral, false);
                    count++;
                }
            }
            string msg = $"Frieden mit {count} Fraktionen geschlossen.";
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MaxReputationWithAll()
        {
            int count = 0;
            foreach (var faction in Verse.Find.FactionManager.AllFactionsVisible)
            {
                if (faction != RimWorld.Faction.OfPlayer && !faction.IsPlayer)
                {
                    faction.TryAffectGoodwillWith(RimWorld.Faction.OfPlayer, 100, true, true, null, null);
                    count++;
                }
            }
            string msg = $"Ruf bei {count} Fraktionen maximiert.";
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenFactionList()
        {
            var factions = Verse.Find.FactionManager.AllFactionsVisible
                .Where(f => !f.IsPlayer && !f.Hidden)
                .OrderBy(f => f.Name)
                .ToList();

            var items = factions.Select(f => 
            {
                string label = $"{f.Name} (Ruf: {f.GoodwillWith(RimWorld.Faction.OfPlayer)})";
                Action act = () => OpenFactionActionMenu(f);
                return (label, act);
            }).ToList();

            MenuHelper.Open("ISA_SelectFaction".Translate(), items);
        }

        private void OpenFactionActionMenu(RimWorld.Faction faction)
        {
            var items = new List<(string, Action)>
            {
                ("ISA_GoodwillPlus10".Translate(), () => { faction.TryAffectGoodwillWith(RimWorld.Faction.OfPlayer, 10, true, true, null, null); TTS.Say("ISA_GoodwillNow".Translate() + " " + faction.GoodwillWith(RimWorld.Faction.OfPlayer)); }),
                ("ISA_GoodwillMinus10".Translate(), () => { faction.TryAffectGoodwillWith(RimWorld.Faction.OfPlayer, -10, true, true, null, null); TTS.Say("ISA_GoodwillNow".Translate() + " " + faction.GoodwillWith(RimWorld.Faction.OfPlayer)); }),
                ("ISA_MakeAllied".Translate(), () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Ally, false); TTS.Say(faction.Name + " " + "ISA_IsNowAllied".Translate()); }),
                ("ISA_MakeNeutral".Translate(), () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Neutral, false); TTS.Say(faction.Name + " " + "ISA_IsNowNeutral".Translate()); }),
                ("ISA_MakeHostile".Translate(), () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Hostile, false); TTS.Say(faction.Name + " " + "ISA_IsNowHostile".Translate()); }),
                ("ISA_ManageLeader".Translate(), () => OpenFactionLeaderMenu(faction))
            };
            MenuHelper.Open("ISA_Manage".Translate() + " " + faction.Name, items);
        }

        private void OpenFactionLeaderMenu(RimWorld.Faction faction)
        {
            var items = new List<(string, Action)>();
            if (faction.leader != null)
            {
                items.Add(("ISA_KillLeader".Translate() + " (" + faction.leader.LabelShort + ")", () => 
                {
                    faction.leader.Kill(null, null);
                    TTS.Say("ISA_LeaderKilled".Translate());
                }));
                items.Add(("ISA_ReplaceLeader".Translate(), () => 
                {
                    faction.TryGenerateNewLeader();
                    TTS.Say("ISA_LeaderReplaced".Translate() + " " + (faction.leader?.LabelShort ?? "None"));
                }));
            }
            else
            {
                items.Add(("ISA_GenerateLeader".Translate(), () => 
                {
                    faction.TryGenerateNewLeader();
                    TTS.Say("ISA_LeaderGenerated".Translate() + " " + (faction.leader?.LabelShort ?? "None"));
                }));
            }
            MenuHelper.Open("ISA_ManageLeader".Translate() + ": " + faction.Name, items);
        }

        private void OpenAnimalTaming()
        {
            var sel = Verse.Find.Selector.SingleSelectedThing as Verse.Pawn;
            bool hasSelectedAnimal = sel != null && sel.RaceProps.Animal;

            var items = new System.Collections.Generic.List<(string, System.Action)>();

            if (hasSelectedAnimal)
            {
                items.Add(("Ausgewähltes Tier mutieren (Menschenjäger)", (System.Action)(() => MutateSingleAnimal(sel))));
            }

            items.Add(("Alle wilden Tiere auf der Karte zähmen", (System.Action)(() => TameAllAnimals())));
            items.Add(("Alle wilden Tiere mutieren (Menschenjäger-Rudel!)", (System.Action)(() => MutateAllWildAnimals())));
            items.Add(("Alle eigenen Tiere vollständig heilen", (System.Action)(() => HealAllColonyAnimals())));
            
            MenuHelper.Open("Haustier- & Tier-Mutator", items);
        }

        private void MutateSingleAnimal(Verse.Pawn animal)
        {
            if (animal.mindState != null && animal.mindState.mentalStateHandler != null)
            {
                animal.mindState.mentalStateHandler.TryStartMentalState(RimWorld.MentalStateDefOf.Manhunter);
                TTS.Say($"{animal.LabelShort} ist mutiert und jetzt ein Menschenjäger!");
            }
            else
            {
                TTS.Say("Mutation fehlgeschlagen.");
            }
        }

        private void MutateAllWildAnimals()
        {
            if (Verse.Find.CurrentMap == null) { TTS.Say("Keine Karte gefunden."); return; }
            int count = 0;
            foreach (var pawn in Verse.Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Animal && pawn.Faction != RimWorld.Faction.OfPlayer)
                {
                    if (pawn.mindState != null && pawn.mindState.mentalStateHandler != null)
                    {
                        pawn.mindState.mentalStateHandler.TryStartMentalState(RimWorld.MentalStateDefOf.Manhunter);
                        count++;
                    }
                }
            }
            TTS.Say($"Warnung: {count} wilde Tiere sind mutiert und jagen jetzt Menschen!");
            
        }

        private void HealAllColonyAnimals()
        {
            if (Verse.Find.CurrentMap == null) { TTS.Say("Keine Karte gefunden."); return; }
            int count = 0;
            foreach (var pawn in Verse.Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Animal && pawn.Faction == RimWorld.Faction.OfPlayer)
                {
                    Verse.HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
                    count++;
                }
            }
            TTS.Say($"{count} eigene Tiere wurden vollständig geheilt.");
        }

        private void TameAllAnimals()
        {
            if (Verse.Find.CurrentMap == null) { TTS.Say("Keine Karte gefunden."); return; }
            int count = 0;
            foreach (var pawn in Verse.Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Animal && pawn.Faction != RimWorld.Faction.OfPlayer)
                {
                    pawn.SetFaction(RimWorld.Faction.OfPlayer);
                    count++;
                }
            }
            TTS.Say($"{count} wilde Tiere wurden gezähmt.");
        }

        private void OpenItemSpawner()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_Menu_Items".Translate(),    OpenItemCategories),
                ("ISA_Menu_Buildings".Translate(), OpenBuildings),
                ("ISA_Menu_Pawns".Translate(),    OpenPawnKinds),
            };
            MenuHelper.Open("ISA_Master_ItemSpawner".Translate(), items);
        }

        private void OpenItemCategories()
        {
            var roots = DefDatabase<ThingCategoryDef>.AllDefs
                .Where(c => c.parent == null || c == ThingCategoryDefOf.Root)
                .ToList();
            OpenCategoryLevel(roots);
        }

        private void OpenCategoryLevel(List<ThingCategoryDef> cats)
        {
            var items = cats
                .OrderBy(c => c.label ?? c.defName)
                .Select(cat =>
                {
                    string label = GenText.CapitalizeFirst(cat.label ?? cat.defName);
                    Action act = () =>
                    {
                        var sub = new List<(string, Action)>();
                        if (cat.childCategories != null && cat.childCategories.Count > 0)
                            sub.Add(("ISA_SubCategories".Translate(), () => OpenCategoryLevel(cat.childCategories)));
                        if (cat.childThingDefs != null && cat.childThingDefs.Count > 0)
                            sub.Add(("ISA_ItemsInCategory".Translate(), () => OpenThingList(cat.childThingDefs.ToList(), cat.label ?? cat.defName)));
                        if (sub.Count == 0)
                            Messages.Message("ISA_Empty".Translate(), MessageTypeDefOf.RejectInput, false);
                        else if (sub.Count == 1)
                            sub[0].Item2();
                        else
                            MenuHelper.Open(label, sub);
                    };
                    return (label, act);
                }).ToList();

            MenuHelper.Open("ISA_Menu_Items".Translate(), items);
        }

        private void OpenThingList(List<ThingDef> things, string title)
        {
            var items = things
                .OrderBy(d => d.label ?? d.defName)
                .Select(def =>
                {
                    string label = GenText.CapitalizeFirst(def.label ?? def.defName);
                    Action act = () =>
                    {
                        if (def.MadeFromStuff) OpenStuffMenu(def);
                        else OpenQuantityMenu(def, null, null);
                    };
                    return (label, act);
                }).ToList();

            MenuHelper.Open(title, items);
        }

        private void OpenStuffMenu(ThingDef itemDef)
        {
            var stuffList = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsStuff && d.stuffProps != null && d.stuffProps.CanMake(itemDef))
                .OrderBy(d => d.label ?? d.defName)
                .ToList();

            if (stuffList.Count == 0)
            {
                OpenQuantityMenu(itemDef, null, null);
                return;
            }

            var items = stuffList.Select(stuff =>
            {
                string label = GenText.CapitalizeFirst(stuff.label ?? stuff.defName);
                Action act = () => OpenQuantityMenu(itemDef, stuff, null);
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_SelectMaterial".Translate(), items);
        }

        // ---------------------------------------------------------
        //  Menübasierte Mengenauswahl (ersetzt Dialog_SpawnQuantity)
        // ---------------------------------------------------------
        private void OpenQuantityMenu(ThingDef itemDef, ThingDef stuffDef, PawnKindDef pawnKind)
        {
            string name = itemDef != null
                ? Verse.GenText.CapitalizeFirst(itemDef.label ?? itemDef.defName)
                : Verse.GenText.CapitalizeFirst(pawnKind?.label ?? pawnKind?.defName ?? "?");

            var quantities = new System.Collections.Generic.List<int> { 1, 5, 10, 50, 100, 200, 300, 400, 500, 600, 700, 800, 900, 1000, 1100, 1200, 2000, 3000, 4000, 5000, 10000 };
            var items = quantities.Select(qty =>
            {
                string label = qty + "x";
                Action act = () => OpenSpawnLocationMenu(itemDef, stuffDef, pawnKind, qty);
                return (label, act);
            }).ToList();

            TTS.Say("ISA_QuantityFor".Translate() + " " + name);
            MenuHelper.Open("ISA_QuantityFor".Translate() + " " + name, items);
        }

        // ---------------------------------------------------------
        //  Menübasierte Standortauswahl ? dann spawnen
        // ---------------------------------------------------------
        private void OpenSpawnLocationMenu(ThingDef itemDef, ThingDef stuffDef, PawnKindDef pawnKind, int qty)
        {
            var map = Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }

            MenuHelper.SelectTargetCell(map, (IntVec3 targetCell) =>
            {
                if (!targetCell.IsValid || !targetCell.InBounds(map)) return;
                DoSpawnItems(itemDef, stuffDef, pawnKind, qty, targetCell, map);
            });
        }

        private void DoSpawnItems(ThingDef itemDef, ThingDef stuffDef, PawnKindDef pawnKind, int qty, IntVec3 cell, Map map)
        {
            try
            {
                if (pawnKind != null)
                {
                    for (int i = 0; i < qty; i++)
                    {
                        var req = new PawnGenerationRequest(pawnKind, Faction.OfPlayer);
                        Pawn pawn = PawnGenerator.GeneratePawn(req);
                        IntVec3 spawnCell = CellFinder.StandableCellNear(cell, map, 5f);
                        if (!spawnCell.IsValid || !spawnCell.InBounds(map)) spawnCell = cell;
                        GenSpawn.Spawn(pawn, spawnCell, map);
                    }
                }
                else if (itemDef != null)
                {
                    int stack = itemDef.stackLimit > 0 ? itemDef.stackLimit : 1;
                    int rem   = qty;
                    while (rem > 0)
                    {
                        int batchCount = Mathf.Min(rem, stack);
                        var t = ThingMaker.MakeThing(itemDef, stuffDef);
                        t.stackCount = batchCount;

                        if (itemDef.Minifiable)
                            t = t.MakeMinified();

                        if (!GenPlace.TryPlaceThing(t, cell, map, ThingPlaceMode.Near, out _))
                            GenSpawn.Spawn(t, cell, map, WipeMode.Vanish);

                        rem -= batchCount;
                    }
                }

                string name = itemDef != null
                    ? GenText.CapitalizeFirst(itemDef.label ?? itemDef.defName)
                    : GenText.CapitalizeFirst(pawnKind?.label ?? pawnKind?.defName ?? "?");
                string msg = qty + "x " + name + " " + "ISA_SpawnedSuffix".Translate();
                Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                TTS.Say(msg);
            }
            catch (System.Exception ex)
            {
                Log.Error("ItemSpawnerAccess Spawn Error: " + ex);
                TTS.Say("Fehler beim Spawnen");
            }
        }

        private void OpenBuildings()
        {
            var list = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.category == ThingCategory.Building && d.BuildableByPlayer)
                .ToList();
            OpenThingList(list, "ISA_Menu_Buildings".Translate());
        }

        private void OpenPawnKinds()
        {
            var items = DefDatabase<PawnKindDef>.AllDefs
                .OrderBy(d => d.label ?? d.defName)
                .Select(pk =>
                {
                    string label = GenText.CapitalizeFirst(pk.label ?? pk.defName);
                    Action act = () => OpenQuantityMenu(null, null, pk);
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_Menu_Pawns".Translate(), items);
        }

        // ---------------------------------------------------
        //  2) EVENT SPAWNER
        // ---------------------------------------------------
        private void OpenEventSpawner()
        {
            var items = new List<(string, Action)>
            {
                ("Vorfall auslösen (Standardpunkte)", () => OpenIncidentCategories(-1f)),
                ("Vorfall auslösen (Eigene Punkte)", OpenIncidentPointsMenu),
                ("Quest spawnen", OpenQuestSpawner)
            };
            MenuHelper.Open("ISA_Master_EventSpawner".Translate(), items);
        }

        private void OpenIncidentPointsMenu()
        {
            var pointsList = new List<float> { 100f, 500f, 1000f, 3000f, 5000f, 10000f };
            var items = pointsList.Select(pts => 
            {
                string label = $"{pts} Punkte";
                Action act = () => OpenIncidentCategories(pts);
                return (label, act);
            }).ToList();
            MenuHelper.Open("Bedrohungspunkte auswählen", items);
        }

        private void OpenQuestSpawner()
        {
            var quests = Verse.DefDatabase<RimWorld.QuestScriptDef>.AllDefs
                .OrderBy(q => q.label ?? q.defName)
                .ToList();
            
            var items = quests.Select(q => 
            {
                string label = GenText.CapitalizeFirst(q.label ?? q.defName);
                Action act = () => 
                {
                    var slate = new RimWorld.QuestGen.Slate();
                    var quest = RimWorld.QuestUtility.GenerateQuestAndMakeAvailable(q, slate);
                    if (quest != null)
                    {
                        RimWorld.QuestUtility.SendLetterQuestAvailable(quest);
                        TTS.Say($"Quest gespawnt: {label}");
                    }
                    else
                    {
                        TTS.Say($"Fehler beim Spawnen der Quest: {label}");
                    }
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("Quest spawnen", items);
        }

        private void OpenIncidentCategories(float customPoints = -1f)
        {
            var allIncidents = DefDatabase<IncidentDef>.AllDefs.ToList();
            var categories = allIncidents
                .Select(i => i.category)
                .Distinct()
                .OrderBy(c => c.label ?? c.defName)
                .ToList();

            var items = categories.Select(cat =>
            {
                string label = GenText.CapitalizeFirst(cat.label ?? cat.defName);
                Action act = () =>
                {
                    var subItems = allIncidents
                        .Where(i => i.category == cat)
                        .OrderBy(i => i.label ?? i.defName)
                        .Select(inc =>
                        {
                            string incLabel = GenText.CapitalizeFirst(inc.label ?? inc.defName);
                            Action incAct = () => TriggerIncident(inc, customPoints);
                            return (incLabel, incAct);
                        }).ToList();
                    MenuHelper.Open(label, subItems);
                };
                return (label, act);
            }).ToList();

            MenuHelper.Open(customPoints > 0 ? $"Incidents ({customPoints} pts)" : "ISA_Master_EventSpawner".Translate(), items);
        }

        private void TriggerIncident(IncidentDef def, float customPoints = -1f)
        {
            IIncidentTarget target = null;
            if (Find.CurrentMap != null && def.TargetAllowed(Find.CurrentMap))
                target = Find.CurrentMap;
            else if (def.TargetAllowed(Find.World))
                target = Find.World;

            if (target == null)
            {
                Messages.Message("ISA_NoValidTarget".Translate(), MessageTypeDefOf.RejectInput, false);
                TTS.Say("ISA_NoValidTarget".Translate());
                return;
            }

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, target);
            if (def.pointsScaleable)
            {
                parms.points = customPoints > 0 ? customPoints : StorytellerUtility.DefaultThreatPointsNow(target);
            }

            if (def.Worker.TryExecute(parms))
            {
                string msg = "ISA_EventTriggered".Translate() + " " + (def.label ?? def.defName);
                Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                TTS.Say(msg);
            }
            else
            {
                Messages.Message("ISA_EventFailed".Translate(), MessageTypeDefOf.RejectInput, false);
                TTS.Say("ISA_EventFailed".Translate());
            }
        }

        // ---------------------------------------------------
        //  3) PAWN EDITOR
        // ---------------------------------------------------
        private void OpenHealthEditor()
        {
            var sel = Verse.Find.Selector.SingleSelectedThing as Verse.Pawn;
            if (sel != null)
            {
                OpenHealthEditorForPawn(sel);
            }
            else
            {
                var map = Verse.Find.CurrentMap;
                if (map == null) { TTS.Say("Kein gültiges Ziel."); return; }
                var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.NameShortColored.Resolve()).ToList();
                if (colonists.Count == 0) { TTS.Say("Keine Kolonisten verfügbar."); return; }
                
                var items = colonists.Select(p => 
                {
                    string label = p.LabelShort;
                    System.Action act = () => OpenHealthEditorForPawn(p);
                    return (label, act);
                }).ToList();
                MenuHelper.Open("Kolonist für Gesundheits-Editor auswählen", items);
            }
        }

        private void OpenHealthEditorForPawn(Verse.Pawn pawn)
        {
            var items = new List<(string, System.Action)>
            {
                ("Vollständig heilen", () => { Verse.HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn); TTS.Say($"{pawn.LabelShort} wurde geheilt."); }),
                ("Fehlende Körperteile wiederherstellen", () => RestoreMissingBodyParts(pawn)),
                ("Bionik / Implantat hinzufügen", () => OpenAddBionicMenu(pawn)),
                ("Krankheit / Verletzung hinzufügen", () => OpenAddHediff(pawn)),
                ("Krankheit / Verletzung entfernen", () => OpenRemoveHediff(pawn))
            };
            MenuHelper.Open($"Gesundheit: {pawn.LabelShort}", items);
        }

        private void RestoreMissingBodyParts(Verse.Pawn pawn)
        {
            bool restored = false;
            foreach (var hediff in pawn.health.hediffSet.GetMissingPartsCommonAncestors().ToList())
            {
                pawn.health.RestorePart(hediff.Part, null, true);
                restored = true;
            }
            if (restored)
                TTS.Say($"Fehlende Körperteile bei {pawn.LabelShort} wiederhergestellt.");
            else
                TTS.Say($"{pawn.LabelShort} hat keine fehlenden Körperteile.");
        }

        private void OpenAddBionicMenu(Verse.Pawn pawn)
        {
            var recipes = Verse.DefDatabase<Verse.RecipeDef>.AllDefs
                .Where(r => r.isViolation == false && r.targetsBodyPart && r.Worker is RimWorld.Recipe_InstallArtificialBodyPart)
                .OrderBy(r => r.label)
                .ToList();

            var items = recipes.Select(r => 
            {
                string label = Verse.GenText.CapitalizeFirst(r.label ?? r.defName);
                System.Action act = () => ApplyBionicRecipe(pawn, r);
                return (label, act);
            }).ToList();

            if (items.Count == 0) { TTS.Say("Keine Bionik-Rezepte gefunden."); return; }
            MenuHelper.Open("Bionik auswählen", items);
        }

        private void ApplyBionicRecipe(Verse.Pawn pawn, Verse.RecipeDef recipe)
        {
            var parts = recipe.Worker.GetPartsToApplyOn(pawn, recipe).ToList();
            if (parts.Count == 0)
            {
                TTS.Say("Kein passendes Körperteil für dieses Implantat gefunden.");
                return;
            }
            
            if (parts.Count == 1)
            {
                recipe.Worker.ApplyOnPawn(pawn, parts[0], null, null, null);
                TTS.Say($"{recipe.label} erfolgreich installiert.");
            }
            else
            {
                var items = parts.Select(p => 
                {
                    string label = p.Label;
                    System.Action act = () => {
                        recipe.Worker.ApplyOnPawn(pawn, p, null, null, null);
                        TTS.Say($"{recipe.label} an {p.Label} installiert.");
                    };
                    return (label, act);
                }).ToList();
                MenuHelper.Open("Körperteil auswählen", items);
            }
        }

        private void OpenRelationshipManager()
        {
            var sel = Verse.Find.Selector.SingleSelectedThing as Verse.Pawn;
            if (sel != null)
            {
                OpenRelationshipManagerForPawn(sel);
            }
            else
            {
                var map = Verse.Find.CurrentMap;
                if (map == null) { TTS.Say("Kein gültiges Ziel."); return; }
                var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.NameShortColored.Resolve()).ToList();
                if (colonists.Count == 0) { TTS.Say("Keine Kolonisten verfügbar."); return; }
                
                var items = colonists.Select(p => 
                {
                    string label = p.LabelShort;
                    System.Action act = () => OpenRelationshipManagerForPawn(p);
                    return (label, act);
                }).ToList();
                MenuHelper.Open("Kolonist für Beziehungs-Manager auswählen", items);
            }
        }

        private void OpenRelationshipManagerForPawn(Verse.Pawn pawn)
        {
            var items = new List<(string, System.Action)>
            {
                ("Beziehung hinzufügen", () => OpenAddRelationshipMenu(pawn)),
                ("Beziehung entfernen", () => OpenRemoveRelationshipMenu(pawn)),
                ("Alle Beziehungen löschen", () => { pawn.relations.ClearAllRelations(); TTS.Say("Alle Beziehungen gelöscht."); })
            };
            MenuHelper.Open($"Beziehungen: {pawn.LabelShort}", items);
        }

        private void OpenAddRelationshipMenu(Verse.Pawn pawn1)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            var others = map.mapPawns.FreeColonists.Where(p => p != pawn1).OrderBy(p => p.NameShortColored.Resolve()).ToList();
            if (others.Count == 0) { TTS.Say("Keine anderen Kolonisten für eine Beziehung verfügbar."); return; }

            var items = others.Select(p2 => 
            {
                string label = p2.LabelShort;
                System.Action act = () => OpenSelectRelationDefMenu(pawn1, p2);
                return (label, act);
            }).ToList();
            MenuHelper.Open("Zielperson auswählen", items);
        }

        private void OpenSelectRelationDefMenu(Verse.Pawn pawn1, Verse.Pawn pawn2)
        {
            var relationDefs = Verse.DefDatabase<RimWorld.PawnRelationDef>.AllDefs.OrderBy(r => r.label).ToList();
            var items = relationDefs.Select(r => 
            {
                string label = Verse.GenText.CapitalizeFirst(r.label ?? r.defName);
                System.Action act = () => 
                {
                    pawn1.relations.AddDirectRelation(r, pawn2);
                    TTS.Say($"Beziehung {label} zwischen {pawn1.LabelShort} und {pawn2.LabelShort} hinzugefügt.");
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("Beziehungsart auswählen", items);
        }

        private void OpenRemoveRelationshipMenu(Verse.Pawn pawn)
        {
            var relations = pawn.relations.DirectRelations.ToList();
            if (relations.Count == 0)
            {
                TTS.Say("Keine aktiven Beziehungen vorhanden.");
                return;
            }
            
            var items = relations.Select(rel => 
            {
                string label = $"{rel.def.label} - {rel.otherPawn.LabelShort}";
                System.Action act = () => 
                {
                    pawn.relations.RemoveDirectRelation(rel.def, rel.otherPawn);
                    TTS.Say($"Beziehung entfernt: {label}");
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("Beziehung zum Entfernen auswählen", items);
        }

        private void OpenPawnEditor()
        {
            var sel = Verse.Find.Selector.SingleSelectedThing as Verse.Pawn;
            if (sel != null)
            {
                OpenPawnEditorForPawn(sel);
            }
            else
            {
                var map = Verse.Find.CurrentMap;
                if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
                var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.NameShortColored.Resolve()).ToList();
                if (colonists.Count == 0) { TTS.Say("Keine Kolonisten verfügbar."); return; }
                
                var items = colonists.Select(p => 
                {
                    string label = p.LabelShort;
                    Action act = () => OpenPawnEditorForPawn(p);
                    return (label, act);
                }).ToList();
                MenuHelper.Open("ISA_SelectColonist".Translate(), items);
            }
        }

        private void OpenPawnEditorForPawn(Verse.Pawn sel)
        {
            var items = new List<(string, Action)>
            {
                ((string)"ISA_HealPawn".Translate(), () => HealPawn(sel)),
            };

            if (sel.skills != null)
            {
                items.Add(((string)"ISA_MaxSkills".Translate(), () => MaxSkills(sel)));
                items.Add(((string)"ISA_EditSkillsAdd".Translate(), () => OpenIndividualSkills(sel, 1)));
                items.Add(((string)"ISA_EditSkillsSub".Translate(), () => OpenIndividualSkills(sel, -1)));
            }

            if (sel.story?.traits != null)
            {
                items.Add(((string)"ISA_AddTrait".Translate(), () => OpenAddTrait(sel)));
                items.Add(((string)"ISA_RemoveTrait".Translate(), () => OpenRemoveTrait(sel)));
            }

            items.Add(((string)"ISA_AddHediff".Translate(), () => OpenAddHediff(sel)));
            items.Add(((string)"ISA_RemoveHediff".Translate(), () => OpenRemoveHediff(sel)));

            if (sel.needs != null && sel.needs.AllNeeds != null && sel.needs.AllNeeds.Count > 0)
            {
                items.Add(((string)"ISA_NeedsAndThoughts".Translate(), () => OpenNeedsAndThoughtsMenu(sel)));
            }

            items.Add(((string)"ISA_SetAge".Translate(), () => OpenSetAgeMenu(sel)));
            items.Add(((string)"ISA_GiveWeapon".Translate(), () => GiveBestWeapon(sel)));

            MenuHelper.Open("ISA_Master_PawnEditor".Translate() + ": " + sel.LabelShort, items);
        }

        private void OpenNeedsAndThoughtsMenu(Verse.Pawn pawn)
        {
            var items = new List<(string, Action)>();
            
            if (pawn.needs != null && pawn.needs.AllNeeds != null && pawn.needs.AllNeeds.Count > 0)
            {
                items.Add(((string)"ISA_ManageNeeds".Translate(), () => OpenNeedsMenu(pawn)));
            }

            if (pawn.needs != null && pawn.needs.mood != null && pawn.needs.mood.thoughts != null)
            {
                items.Add(((string)"ISA_ManageThoughts".Translate(), () => OpenThoughtsMenu(pawn)));
            }

            MenuHelper.Open("ISA_NeedsAndThoughts".Translate() + ": " + pawn.LabelShort, items);
        }

        private void OpenNeedsMenu(Verse.Pawn pawn)
        {
            var items = new List<(string, Action)>();
            
            foreach (var need in pawn.needs.AllNeeds)
            {
                string needLabel = Verse.GenText.CapitalizeFirst(need.def.label ?? need.def.defName);
                string currentLvl = (need.CurLevelPercentage * 100).ToString("F0") + "%";
                string itemLabel = $"{needLabel} ({currentLvl})";
                var capturedNeed = need;

                Action act = () =>
                {
                    var subItems = new List<(string, Action)>
                    {
                        (((string)"ISA_FillTo100".Translate() != "ISA_FillTo100" ? (string)"ISA_FillTo100".Translate() : "Fill to 100%"), () => { capturedNeed.CurLevelPercentage = 1f; TTS.Say(needLabel + " " + "ISA_Filled".Translate()); }),
                        (((string)"ISA_EmptyTo0".Translate() != "ISA_EmptyTo0" ? (string)"ISA_EmptyTo0".Translate() : "Empty to 0%"), () => { capturedNeed.CurLevelPercentage = 0f; TTS.Say(needLabel + " " + "ISA_Emptied".Translate()); })
                    };
                    MenuHelper.Open(needLabel, subItems);
                };
                items.Add((itemLabel, act));
            }

            MenuHelper.Open("ISA_ManageNeeds".Translate() + ": " + pawn.LabelShort, items);
        }

        private void OpenThoughtsMenu(Verse.Pawn pawn)
        {
            var items = new List<(string, Action)>();

            var catharsisDef = Verse.DefDatabase<RimWorld.ThoughtDef>.GetNamed("Catharsis", false);
            if (catharsisDef != null)
            {
                items.Add((((string)"ISA_AddThought".Translate() != "ISA_AddThought" ? (string)"ISA_AddThought".Translate() : "Add Thought") + ": Catharsis", () =>
                {
                    pawn.needs.mood.thoughts.memories.TryGainMemory(catharsisDef);
                    TTS.Say("ISA_ThoughtAdded".Translate() + ": Catharsis");
                }));
            }

            var memories = pawn.needs.mood.thoughts.memories.Memories.ToList();
            foreach (var memory in memories)
            {
                string label = $"{Verse.GenText.CapitalizeFirst(memory.def.label ?? memory.def.defName)} ({memory.MoodOffset()})";
                var capturedMemory = memory;
                
                Action act = () =>
                {
                    var subItems = new List<(string, Action)>();
                    subItems.Add((((string)"ISA_RemoveThought".Translate() != "ISA_RemoveThought" ? (string)"ISA_RemoveThought".Translate() : "Remove Thought"), () => 
                    {
                        pawn.needs.mood.thoughts.memories.RemoveMemory(capturedMemory);
                        TTS.Say("ISA_ThoughtRemoved".Translate() + ": " + (capturedMemory.def.label ?? capturedMemory.def.defName));
                    }));
                    MenuHelper.Open(label, subItems);
                };

                items.Add((label, act));
            }

            MenuHelper.Open("ISA_ManageThoughts".Translate() + ": " + pawn.LabelShort, items);
        }

        private void OpenIndividualSkills(Verse.Pawn pawn, int modifier)
        {
            if (pawn.skills == null) return;
            var items = pawn.skills.skills.OrderBy(sk => sk.def.label).Select(sk =>
            {
                string label = $"{sk.def.LabelCap} (Level {sk.Level})";
                Action act = () =>
                {
                    sk.Level += modifier;
                    if (sk.Level > 20) sk.Level = 20;
                    if (sk.Level < 0) sk.Level = 0;
                    
                    if (sk.Level >= 15) sk.passion = RimWorld.Passion.Major;
                    else if (sk.Level >= 8) sk.passion = RimWorld.Passion.Minor;
                    else sk.passion = RimWorld.Passion.None;
                    
                    TTS.Say($"{sk.def.LabelCap} ist jetzt Stufe {sk.Level}");
                    OpenIndividualSkills(pawn, modifier); // Refresh menu
                };
                return (label, act);
            }).ToList();
            
            string title = modifier > 0 ? "ISA_IncreaseSkills".Translate() : "ISA_DecreaseSkills".Translate();
            MenuHelper.Open(title, items);
        }

        private void OpenSetAgeMenu(Verse.Pawn pawn)
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>();
            int[] ages = new int[] { 18, 25, 30, 40, 50, 60, 70, 80 };
            foreach (int age in ages)
            {
                int a = age;
                items.Add(($"{a} {"Years".Translate()}", () =>
                {
                    if (pawn.ageTracker != null)
                    {
                        long ticksPerYear = 3600000;
                        pawn.ageTracker.AgeBiologicalTicks = (long)a * ticksPerYear;
                        pawn.ageTracker.AgeChronologicalTicks = (long)a * ticksPerYear;
                        TTS.Say("ISA_SetAge".Translate() + " " + a);
                    }
                }));
            }
            MenuHelper.Open("ISA_SetAge".Translate(), items);
        }

        private void HealPawn(Pawn pawn)
        {
            foreach (var h in pawn.health.hediffSet.hediffs.ToList())
            {
                if (h.def.isBad || h is Hediff_MissingPart)
                    pawn.health.RemoveHediff(h);
            }
            string msg = "ISA_Healed".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MaxSkills(Pawn pawn)
        {
            if (pawn.skills == null) return;
            foreach (var sk in pawn.skills.skills)
            {
                sk.Level = 20;
                sk.passion = Passion.Major;
            }
            string msg = "ISA_SkillsMaxed".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenAddTrait(Pawn pawn)
        {
            var items = DefDatabase<TraitDef>.AllDefs
                .OrderBy(t => t.defName)
                .Select(td =>
                {
                    string label = td.defName;
                    Action act = () =>
                    {
                        if (td.degreeDatas != null && td.degreeDatas.Count > 1)
                        {
                            var degItems = td.degreeDatas.Select(dd =>
                            {
                                string dl = dd.label ?? (td.defName + " (" + dd.degree + ")");
                                Action da = () => AddTrait(pawn, td, dd.degree);
                                return (dl, da);
                            }).ToList();
                            MenuHelper.Open(label, degItems);
                        }
                        else
                        {
                            int deg = td.degreeDatas != null && td.degreeDatas.Count > 0 ? td.degreeDatas[0].degree : 0;
                            AddTrait(pawn, td, deg);
                        }
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_AddTrait".Translate(), items);
        }

        private void AddTrait(Pawn pawn, TraitDef def, int degree)
        {
            if (pawn.story?.traits == null)
            {
                TTS.Say("Pawn hat keine Merkmale-Eigenschaft.");
                return;
            }
            if (pawn.story.traits.HasTrait(def))
            {
                Messages.Message("ISA_HasTraitAlready".Translate(), MessageTypeDefOf.RejectInput, false);
                TTS.Say("ISA_HasTraitAlready".Translate());
                return;
            }
            pawn.story.traits.GainTrait(new Trait(def, degree, false), false);
            pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();
            string msg = "ISA_TraitAdded".Translate() + " " + def.defName;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenRemoveTrait(Pawn pawn)
        {
            if (pawn.story?.traits == null || pawn.story.traits.allTraits.Count == 0)
            {
                Messages.Message("ISA_NoTraits".Translate(), MessageTypeDefOf.RejectInput, false);
                TTS.Say("ISA_NoTraits".Translate());
                return;
            }
            var items = pawn.story.traits.allTraits.Select(t =>
            {
                string label = t.LabelCap;
                Action act = () =>
                {
                    pawn.story.traits.allTraits.Remove(t);
                    pawn.workSettings?.EnableAndInitializeIfNotAlreadyInitialized();
                    string msg = "ISA_TraitRemoved".Translate() + " " + t.LabelCap;
                    Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                    TTS.Say(msg);
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_RemoveTrait".Translate(), items);
        }

        private void GiveBestWeapon(Pawn pawn)
        {
            if (pawn.equipment == null)
            {
                TTS.Say("ISA_NoEquipment".Translate());
                return;
            }
            var weaponDef = DefDatabase<ThingDef>.AllDefs
                .Where(d => d.IsWeapon && !d.MadeFromStuff)
                .OrderByDescending(d => d.GetStatValueAbstract(StatDefOf.MeleeWeapon_AverageDPS))
                .FirstOrDefault();
            if (weaponDef == null)
            {
                TTS.Say("ISA_NoWeaponFound".Translate());
                return;
            }
            var weapon = ThingMaker.MakeThing(weaponDef);
            pawn.equipment.AddEquipment((ThingWithComps)weapon);
            string msg = "ISA_WeaponGiven".Translate() + " " + (weaponDef.label ?? weaponDef.defName);
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenAddHediff(Pawn pawn)
        {
            var items = DefDatabase<HediffDef>.AllDefs
                .OrderBy(h => h.label ?? h.defName)
                .Select(h =>
                {
                    string label = GenText.CapitalizeFirst(h.label ?? h.defName);
                    return (label, (Action)(() =>
                    {
                        OpenBodyPartSelectionForHediff(pawn, h);
                    }));
                }).ToList();
            MenuHelper.Open("ISA_AddHediff".Translate(), items);
        }

        private void OpenBodyPartSelectionForHediff(Pawn pawn, HediffDef hediffDef)
        {
            var items = new List<(string, Action)>();
            items.Add(((string)"ISA_WholeBody".Translate(), () => 
            {
                var hediff = HediffMaker.MakeHediff(hediffDef, pawn);
                pawn.health.AddHediff(hediff);
                TTS.Say("ISA_HediffAdded".Translate() + " " + (hediffDef.label ?? hediffDef.defName));
            }));

            if (pawn.RaceProps.body != null)
            {
                foreach (var part in pawn.RaceProps.body.AllParts)
                {
                    string partLabel = part.LabelCap;
                    var p = part;
                    items.Add((partLabel, () => 
                    {
                        var hediff = HediffMaker.MakeHediff(hediffDef, pawn, p);
                        pawn.health.AddHediff(hediff, p, null, null);
                        TTS.Say("ISA_HediffAdded".Translate() + " " + (hediffDef.label ?? hediffDef.defName) + " on " + partLabel);
                    }));
                }
            }

            MenuHelper.Open("ISA_SelectBodyPart".Translate(), items);
        }

        private void OpenRemoveHediff(Pawn pawn)
        {
            if (pawn.health?.hediffSet?.hediffs == null || pawn.health.hediffSet.hediffs.Count == 0)
            {
                TTS.Say("ISA_NoHediffs".Translate());
                return;
            }
            var items = pawn.health.hediffSet.hediffs
                .Select(h =>
                {
                    string label = h.LabelBase ?? h.def.defName;
                    return (label, (Action)(() =>
                    {
                        pawn.health.RemoveHediff(h);
                        TTS.Say("ISA_HediffRemoved".Translate() + " " + label);
                    }));
                }).ToList();
            MenuHelper.Open("ISA_RemoveHediff".Translate(), items);
        }

        // ---------------------------------------------------
        //  4) WETTER & ZEIT
        // ---------------------------------------------------
        private void OpenWeatherConditionsEventsMenu()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ChangeWeather".Translate(), OpenWeatherList),
                ("ISA_ManageGameConditions".Translate(), OpenGameConditionsMenu),
                ("ISA_Master_EventSpawner".Translate(), OpenEventSpawner),
                ("ISA_TriggerRandomRaid".Translate(), TriggerRandomRaid),
                ("ISA_ChangeTime".Translate(),   OpenTimeMenu),
                ("ISA_SkipDay".Translate(),       SkipDay),
                ("ISA_SkipSeason".Translate(),    SkipSeason),
            };
            MenuHelper.Open("ISA_WeatherConditionsEvents".Translate(), items);
        }

        private void EndAllGameConditions()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var conditions = map.gameConditionManager.ActiveConditions.ToList();
            int count = 0;
            foreach (var cond in conditions)
            {
                cond.End();
                count++;
            }
            string msg = $"Beendete {count} aktive Spielbedingungen.";
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenGameConditionsMenu()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null)
            {
                TTS.Say("ISA_NoValidTarget".Translate());
                return;
            }
            var items = new List<(string, Action)>();
            items.Add(("ISA_EndAllGameConditions".Translate(), EndAllGameConditions));

            var allDefs = Verse.DefDatabase<Verse.GameConditionDef>.AllDefs.OrderBy(d => d.label ?? d.defName).ToList();
            foreach (var conditionDef in allDefs)
            {
                bool isActive = map.gameConditionManager.ConditionIsActive(conditionDef);
                string status = isActive ? "ISA_Active".Translate() : "ISA_Inactive".Translate();
                string label = $"{Verse.GenText.CapitalizeFirst(conditionDef.label ?? conditionDef.defName)} ({status})";
                Verse.GameConditionDef capturedDef = conditionDef;
                
                Action act = () =>
                {
                    if (isActive)
                    {
                        var activeCond = map.gameConditionManager.GetActiveCondition(capturedDef);
                        if (activeCond != null)
                        {
                            activeCond.End();
                            TTS.Say("ISA_ConditionEnded".Translate() + " " + (capturedDef.label ?? capturedDef.defName));
                        }
                    }
                    else
                    {
                        TriggerGameCondition(capturedDef);
                    }
                };
                items.Add((label, act));
            }
            MenuHelper.Open("ISA_ManageGameConditions".Translate(), items);
        }

        private void TriggerGameCondition(Verse.GameConditionDef def)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int duration = 180000; // fallback to 3 days (60000 * 3)
            var cond = RimWorld.GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(cond);
            string msg = "ISA_ConditionStarted".Translate() + " " + (def.label ?? def.defName);
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenWeatherList()
        {
            var items = DefDatabase<WeatherDef>.AllDefs
                .OrderBy(w => w.label ?? w.defName)
                .Select(w =>
                {
                    string label = GenText.CapitalizeFirst(w.label ?? w.defName);
                    Action act = () =>
                    {
                        if (Find.CurrentMap == null)
                        {
                            TTS.Say("ISA_NoValidTarget".Translate());
                            return;
                        }
                        Find.CurrentMap.weatherManager.TransitionTo(w);
                        string msg = "ISA_WeatherChanged".Translate() + " " + w.label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_ChangeWeather".Translate(), items);
        }

        private void OpenTimeMenu()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_TimeMorning".Translate(), () => SetHour(6)),
                ("ISA_TimeNoon".Translate(),    () => SetHour(12)),
                ("ISA_TimeEvening".Translate(), () => SetHour(18)),
                ("ISA_TimeNight".Translate(),   () => SetHour(22)),
            };
            MenuHelper.Open("ISA_ChangeTime".Translate(), items);
        }

        private void SetHour(int h)
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int dayTick = GenLocalDate.DayTick(Find.CurrentMap);
            int delta   = h * 2500 - dayTick;
            if (delta <= 0) delta += 60000;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta);
            string msg = "ISA_TimeChanged".Translate() + " " + h + ":00";
            Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SkipDay()
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 60000);
            string msg = "ISA_DaySkipped".Translate();
            Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SkipSeason()
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 900000);
            string msg = "ISA_SeasonSkipped".Translate();
        }

        // ---------------------------------------------------
        //  5) FORSCHUNG & FRAKTIONEN
        // ---------------------------------------------------
        private void OpenResearchAndTechEditor()
        {
            var items = new List<(string, Action)>
            {
                (((string)"ISA_FinishAllResearch".Translate() != "ISA_FinishAllResearch" ? (string)"ISA_FinishAllResearch".Translate() : "Finish All Research"), FinishAllResearch)
            };

            var techLevels = Enum.GetValues(typeof(RimWorld.TechLevel)).Cast<RimWorld.TechLevel>().ToList();
            var allProjects = Verse.DefDatabase<Verse.ResearchProjectDef>.AllDefs.ToList();

            foreach (var tl in techLevels)
            {
                var projects = allProjects.Where(p => p.techLevel == tl).OrderBy(p => p.label ?? p.defName).ToList();
                if (projects.Count > 0)
                {
                    items.Add(($"Tech Level: {tl.ToString()}", () => OpenResearchCategory(tl, projects)));
                }
            }

            MenuHelper.Open("Research & Tech Editor", items);
        }

        private void OpenResearchCategory(RimWorld.TechLevel tl, List<Verse.ResearchProjectDef> projects)
        {
            var items = new List<(string, Action)>();
            
            foreach(var proj in projects)
            {
                string status = proj.IsFinished ? "Abgeschlossen" : "Gesperrt";
                string label = $"{Verse.GenText.CapitalizeFirst(proj.label ?? proj.defName)} ({status})";
                var capturedProj = proj;

                items.Add((label, () => OpenResearchProjectOptions(capturedProj)));
            }

            MenuHelper.Open($"Tech Level: {tl.ToString()}", items);
        }

        private void OpenResearchProjectOptions(Verse.ResearchProjectDef proj)
        {
            var items = new List<(string, Action)>();
            
            string label = Verse.GenText.CapitalizeFirst(proj.label ?? proj.defName);

            items.Add(("Forschung abschließen", () => 
            {
                Verse.Find.ResearchManager.FinishProject(proj, false, null, true);
                TTS.Say($"{label} abgeschlossen.");
            }));

            items.Add(("Forschung zurücksetzen / sperren", () => 
            {
                var rm = Verse.Find.ResearchManager;
                var dictField = typeof(RimWorld.ResearchManager).GetField("progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (dictField != null)
                {
                    var dict = dictField.GetValue(rm) as System.Collections.Generic.Dictionary<Verse.ResearchProjectDef, float>;
                    if (dict != null && dict.ContainsKey(proj))
                    {
                        dict.Remove(proj);
                    }
                }
                
                var currentProjField = typeof(RimWorld.ResearchManager).GetField("currentProj", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (currentProjField != null)
                {
                    var current = currentProjField.GetValue(rm) as Verse.ResearchProjectDef;
                    if (current == proj)
                    {
                        currentProjField.SetValue(rm, null);
                    }
                }

                typeof(RimWorld.ResearchManager).GetMethod("ReapplyAllMods", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(rm, null);
                TTS.Say($"{label} gesperrt.");
            }));

            items.Add(("50% Fortschritt hinzufügen", () => 
            {
                if (!proj.IsFinished)
                {
                    float amount = proj.baseCost * 0.5f;
                    var rm = Verse.Find.ResearchManager;
                    
                    var dictField = typeof(RimWorld.ResearchManager).GetField("progress", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (dictField != null)
                    {
                        var dict = dictField.GetValue(rm) as System.Collections.Generic.Dictionary<Verse.ResearchProjectDef, float>;
                        if (dict != null)
                        {
                            if (!dict.ContainsKey(proj)) dict[proj] = 0f;
                            dict[proj] += amount;
                            
                            if (dict[proj] >= proj.baseCost)
                            {
                                rm.FinishProject(proj, false, null, true);
                                TTS.Say($"{label} durch Fortschritt abgeschlossen.");
                            }
                            else
                            {
                                TTS.Say($"{label} Fortschritt hinzugefügt. Jetzt bei {(dict[proj] / proj.baseCost * 100f):F0}%.");
                            }
                        }
                    }
                }
                else
                {
                    TTS.Say($"{label} ist bereits abgeschlossen.");
                }
            }));

            string status = proj.IsFinished ? "Abgeschlossen" : "Gesperrt";
            TTS.Say($"{label} - {status}");
            MenuHelper.Open(label, items);
        }

        private void FinishAllResearch()
        {
            foreach (var rp in Verse.DefDatabase<Verse.ResearchProjectDef>.AllDefs)
                if (!rp.IsFinished)
                    Verse.Find.ResearchManager.FinishProject(rp, false, null, true);
            string msg = ((string)"ISA_ResearchFinished".Translate() != "ISA_ResearchFinished" ? (string)"ISA_ResearchFinished".Translate() : "All research finished.");
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void PeaceWithAll()
        {
            foreach (var f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f == Faction.OfPlayer || f.def.permanentEnemy || f.Hidden) continue;
                int delta = 50 - f.GoodwillWith(Faction.OfPlayer);
                if (delta > 0)
                    f.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }
            string msg = "ISA_PeaceMade".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MaxRelations()
        {
            foreach (var f in Find.FactionManager.AllFactionsListForReading)
            {
                if (f == Faction.OfPlayer || f.def.permanentEnemy || f.Hidden) continue;
                int delta = 100 - f.GoodwillWith(Faction.OfPlayer);
                if (delta > 0)
                    f.TryAffectGoodwillWith(Faction.OfPlayer, delta, false, false, null, null);
            }
            string msg = "ISA_RelationsMaxed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void AddResource(ThingDef def, int count)
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var thing = ThingMaker.MakeThing(def);
            thing.stackCount = count;
            GenSpawn.Spawn(thing, DropCellFinder.TradeDropSpot(Find.CurrentMap), Find.CurrentMap);
            string msg = count + "x " + (def.label ?? def.defName) + " " + "ISA_SpawnedSuffix".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  6) STORYTELLER
        // ---------------------------------------------------
        private void OpenStoryteller()
        {
            var items = new List<(string, Action)>
            {
                (((string)"ISA_ChangeStoryteller".Translate()), OpenStorytellerList),
                (((string)"ISA_ChangeDifficulty".Translate()),  OpenDifficultyList),
                (((string)"ISA_AdjustThreatScale".Translate()), OpenThreatScaleMenu),
                (((string)"ISA_ToggleMajorThreats".Translate()), ToggleMajorThreats),
                (((string)"ISA_AdjustCropYield".Translate()),   OpenCropYieldMenu),
            };
            MenuHelper.Open("ISA_Master_Storyteller".Translate(), items);
        }

        private void OpenThreatScaleMenu()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            var items = new List<(string, Action)>
            {
                ("Aktuell: " + (diff.threatScale * 100f).ToString("F0") + "%", () => {}),
                ("ISA_Increase10".Translate(), () => { diff.threatScale += 0.1f; TTS.Say("ISA_ThreatScaleNow".Translate() + " " + (diff.threatScale * 100f).ToString("F0") + "%"); OpenThreatScaleMenu(); }),
                ("ISA_Decrease10".Translate(), () => { diff.threatScale = UnityEngine.Mathf.Max(0f, diff.threatScale - 0.1f); TTS.Say("ISA_ThreatScaleNow".Translate() + " " + (diff.threatScale * 100f).ToString("F0") + "%"); OpenThreatScaleMenu(); }),
                ("ISA_Increase50".Translate(), () => { diff.threatScale += 0.5f; TTS.Say("ISA_ThreatScaleNow".Translate() + " " + (diff.threatScale * 100f).ToString("F0") + "%"); OpenThreatScaleMenu(); }),
                ("ISA_Decrease50".Translate(), () => { diff.threatScale = UnityEngine.Mathf.Max(0f, diff.threatScale - 0.5f); TTS.Say("ISA_ThreatScaleNow".Translate() + " " + (diff.threatScale * 100f).ToString("F0") + "%"); OpenThreatScaleMenu(); }),
            };
            MenuHelper.Open("ISA_AdjustThreatScale".Translate(), items);
        }

        private void ToggleMajorThreats()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            diff.allowBigThreats = !diff.allowBigThreats;
            string state = diff.allowBigThreats ? "Enabled".Translate() : "Disabled".Translate();
            TTS.Say("ISA_MajorThreats".Translate() + " " + state);
        }

        private void OpenCropYieldMenu()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            var items = new List<(string, Action)>
            {
                ("Aktuell: " + (diff.cropYieldFactor * 100f).ToString("F0") + "%", () => {}),
                ("ISA_Increase10".Translate(), () => { diff.cropYieldFactor += 0.1f; TTS.Say("ISA_CropYieldNow".Translate() + " " + (diff.cropYieldFactor * 100f).ToString("F0") + "%"); OpenCropYieldMenu(); }),
                ("ISA_Decrease10".Translate(), () => { diff.cropYieldFactor = UnityEngine.Mathf.Max(0.1f, diff.cropYieldFactor - 0.1f); TTS.Say("ISA_CropYieldNow".Translate() + " " + (diff.cropYieldFactor * 100f).ToString("F0") + "%"); OpenCropYieldMenu(); }),
                ("ISA_Increase50".Translate(), () => { diff.cropYieldFactor += 0.5f; TTS.Say("ISA_CropYieldNow".Translate() + " " + (diff.cropYieldFactor * 100f).ToString("F0") + "%"); OpenCropYieldMenu(); }),
                ("ISA_Decrease50".Translate(), () => { diff.cropYieldFactor = UnityEngine.Mathf.Max(0.1f, diff.cropYieldFactor - 0.5f); TTS.Say("ISA_CropYieldNow".Translate() + " " + (diff.cropYieldFactor * 100f).ToString("F0") + "%"); OpenCropYieldMenu(); }),
            };
            MenuHelper.Open("ISA_AdjustCropYield".Translate(), items);
        }

        private void OpenStorytellerList()
        {
            var items = DefDatabase<StorytellerDef>.AllDefs
                .OrderBy(s => s.label ?? s.defName)
                .Select(s =>
                {
                    string label = GenText.CapitalizeFirst(s.label ?? s.defName);
                    Action act = () =>
                    {
                        Find.Storyteller.def = s;
                        Find.Storyteller.Notify_DefChanged();
                        string msg = "ISA_StorytellerChanged".Translate() + " " + s.label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_ChangeStoryteller".Translate(), items);
        }

        private void OpenDifficultyList()
        {
            var items = DefDatabase<DifficultyDef>.AllDefs
                .OrderBy(d => d.defName)
                .Select(d =>
                {
                    string label = GenText.CapitalizeFirst(d.label ?? d.defName);
                    Action act = () =>
                    {
                        Find.Storyteller.difficulty = new Difficulty(d);
                        string msg = "ISA_DifficultyChanged".Translate() + " " + d.label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_ChangeDifficulty".Translate(), items);
        }

        // ---------------------------------------------------
        //  7) BASIS & KARTEN-WERKZEUGE
        // ---------------------------------------------------
        private void OpenBaseMapTools()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ToggleInstantBuild".Translate(), ToggleGodMode),
                ("ISA_ClearFog".Translate(),           ClearFog),
                ("ISA_MaxPlantGrowth".Translate(),     MaxPlantGrowth),
                ("ISA_RemoveAllRoofs".Translate(),     RemoveAllRoofs),
                ("ISA_DestroyAllBlueprints".Translate(),DestroyBlueprints),
                ("Terrain-Werkzeuge...", OpenTerrainManager),
            };
            MenuHelper.Open("ISA_Master_BaseMapTools".Translate(), items);
        }

        private void ToggleGodMode()
        {
            DebugSettings.godMode = !DebugSettings.godMode;
            string msg = "ISA_InstantBuildToggled".Translate() + " " + (DebugSettings.godMode ? "ON" : "OFF");
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void ClearFog()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            Find.CurrentMap.fogGrid.ClearAllFog();
            string msg = "ISA_FogCleared".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MaxPlantGrowth()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Plant).ToList())
            {
                if (t is Plant p && p.def.plant != null)
                    p.Growth = 1f;
            }
            string msg = "ISA_PlantsMaximized".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void RemoveAllRoofs()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var area = Find.CurrentMap.cellIndices.NumGridCells;
            for (int i = 0; i < area; i++)
                Find.CurrentMap.roofGrid.SetRoof(Find.CurrentMap.cellIndices.IndexToCell(i), null);
            string msg = "ISA_RoofsRemoved".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void DestroyBlueprints()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var bp in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).ToList())
                bp.Destroy();
            string msg = "ISA_BlueprintsDestroyed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  8) STIMMUNGS-MANAGER
        // ---------------------------------------------------
        private void OpenNeedsMood()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MaxAllNeeds".Translate(),     MaxAllNeeds),
                ("ISA_StopMentalBreaks".Translate(),StopMentalBreaks),
                ("Inspiration auslösen...",          OpenInspirationMenu),
                ("Katharsis geben (Stimmungs-Buff)",      GiveCatharsis),
                ("ISA_MassTame".Translate(),        MassTame),
                ("ISA_FeedAllAnimals".Translate(),  FeedAllAnimals),
            };
            MenuHelper.Open("ISA_Master_NeedsMood".Translate(), items);
        }

        private void GiveCatharsis()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            var catharsis = Verse.DefDatabase<RimWorld.ThoughtDef>.GetNamed("Catharsis", false);
            if (catharsis != null)
            {
                foreach (var p in map.mapPawns.FreeColonists)
                {
                    p.needs?.mood?.thoughts?.memories?.TryGainMemory(catharsis);
                }
                TTS.Say("Allen Kolonisten Katharsis gegeben.");
            }
        }

        private void OpenInspirationMenu()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("Kein gültiges Ziel."); return; }
            var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.LabelShort).ToList();
            if (colonists.Count == 0) { TTS.Say("Keine Kolonisten verfügbar."); return; }

            var items = colonists.Select(p => 
            {
                string label = p.LabelShort;
                Action act = () => OpenInspirationForPawn(p);
                return (label, act);
            }).ToList();
            MenuHelper.Open("Select Colonist for Inspiration", items);
        }

        private void OpenInspirationForPawn(Verse.Pawn pawn)
        {
            if (pawn.mindState?.inspirationHandler == null) return;
            var items = Verse.DefDatabase<RimWorld.InspirationDef>.AllDefs.OrderBy(i => i.label).Select(iDef =>
            {
                string label = iDef.label ?? iDef.defName;
                Action act = () => 
                {
                    pawn.mindState.inspirationHandler.TryStartInspiration(iDef, "ItemSpawnerAccess");
                    TTS.Say($"Inspiration {label} für {pawn.LabelShort} gestartet");
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open($"Inspiration für {pawn.LabelShort}", items);
        }

        private void MaxAllNeeds()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if ((p.IsColonist || p.IsPrisonerOfColony) && p.needs != null)
                    foreach (var n in p.needs.AllNeeds)
                        n.CurLevelPercentage = 1f;
            }
            string msg = "ISA_NeedsMaximized".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void StopMentalBreaks()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (!p.IsColonist && !p.IsPrisonerOfColony) continue;
                if (p.InMentalState) p.MentalState?.RecoverFromState();
                if (p.needs?.mood != null)
                {
                    p.needs.mood.thoughts.memories.Memories.RemoveAll(m => m.MoodOffset() < 0f);
                    p.needs.mood.CurLevelPercentage = 1f;
                }
            }
            string msg = "ISA_MentalBreaksStopped".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MassTame()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int count = 0;
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p.RaceProps.Animal && p.Faction == null)
                {
                    p.SetFaction(Faction.OfPlayer);
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_AnimalsTamed", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void FeedAllAnimals()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (p.RaceProps.Animal && p.needs?.food != null)
                    p.needs.food.CurLevelPercentage = 1f;
            }
            string msg = "ISA_AnimalsFed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  9) KOLONIE-MANAGER
        // ---------------------------------------------------
        private void OpenColonyEnemy()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_CM_RecruitAllPrisoners".Translate(), CM_RecruitAllPrisoners2),
                ("ISA_KillAllEnemies".Translate(),      KillAllEnemies),
                ("ISA_CleanMap".Translate(),            CleanMap),
                ("ISA_AddColonist".Translate(),         AddColonist),
            };
            MenuHelper.Open("ISA_Master_ColonyEnemy".Translate(), items);
        }

        private void CM_RecruitAllPrisoners2()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (p.IsPrisonerOfColony)
                {
                    p.guest?.SetGuestStatus(null, GuestStatus.Guest);
                    p.SetFaction(Faction.OfPlayer);
                }
            }
            string msg = "ISA_PrisonersRecruited".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void KillAllEnemies()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList())
            {
                if (GenHostility.HostileTo(p, Faction.OfPlayer) && !p.Dead)
                    p.Kill(null, null);
            }
            string msg = "ISA_EnemiesKilled".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void CleanMap()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse).ToList())
                t.Destroy();
            string msg = "ISA_MapCleaned".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void AddColonist()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var pk = PawnKindDefOf.Colonist;
            var req = new PawnGenerationRequest(pk, Faction.OfPlayer);
            var pawn = PawnGenerator.GeneratePawn(req);
            var cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            GenSpawn.Spawn(pawn, cell, Find.CurrentMap);
            string msg = "ISA_ColonistAdded".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  10) KARAWANEN-MANAGER
        // ---------------------------------------------------
        private void OpenCaravanWorld()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_TeleportCaravans".Translate(), TeleportCaravans),
                ("ISA_FillCaravanFood".Translate(),  FillCaravanFood),
                ("ISA_RevealWorld".Translate(),      RevealWorld),
                ("ISA_HealCaravans".Translate(),     HealCaravans),
            };
            MenuHelper.Open("ISA_Master_CaravanWorld".Translate(), items);
        }

        private void TeleportCaravans()
        {
            int count = 0;
            foreach (var c in Find.WorldObjects.Caravans.ToList())
            {
                if (c.Faction == Faction.OfPlayer && c.pather != null && c.pather.Moving)
                {
                    c.Tile = c.pather.Destination;
                    c.pather.StopDead();
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_CaravansTeleported", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void FillCaravanFood()
        {
            int count = 0;
            foreach (var c in Find.WorldObjects.Caravans.ToList())
            {
                if (c.Faction == Faction.OfPlayer)
                {
                    var food = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
                    food.stackCount = 100;
                    CaravanInventoryUtility.GiveThing(c, food);
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_CaravanFoodFilled", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void RevealWorld()
        {
            foreach (var f in Find.FactionManager.AllFactionsListForReading)
                if (f.def != null) f.def.hidden = false;
            string msg = "ISA_WorldRevealed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void HealCaravans()
        {
            int healed = 0;
            foreach (var c in Find.WorldObjects.Caravans)
            {
                if (c.Faction != Faction.OfPlayer) continue;
                foreach (var p in c.pawns)
                {
                    foreach (var h in p.health.hediffSet.hediffs.ToList())
                        if (h.def.isBad || h is Hediff_MissingPart)
                            p.health.RemoveHediff(h);
                    healed++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_CaravansHealed", healed.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  11) ARCHOTECH-MANAGER
        // ---------------------------------------------------
        private void OpenArchotechMech()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_HackMechs".Translate(),       HackMechs),
                ("ISA_ArtifactDelivery".Translate(), DeliverArtifacts),
                ("ISA_DroneSwarm".Translate(),       SpawnDroneSwarm),
                ("ISA_SpawnMechHerd".Translate(),    SpawnMechHerd),
            };
            MenuHelper.Open("ISA_Master_ArchotechMech".Translate(), items);
        }

        private void HackMechs()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int count = 0;
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (p.RaceProps?.IsMechanoid == true && GenHostility.HostileTo(p, Faction.OfPlayer))
                {
                    p.stances?.stunner?.StunFor(5000, null, true, true);
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_MechsHacked", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void DeliverArtifacts()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            foreach (var defName in new[] { "MechSerumHealer", "MechSerumResurrector", "OrbitalBombardmentTargeter" })
            {
                var def = DefDatabase<ThingDef>.GetNamed(defName, false);
                if (def != null) GenSpawn.Spawn(ThingMaker.MakeThing(def), cell, Find.CurrentMap);
            }
            string msg = "ISA_ArtifactsDelivered".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SpawnDroneSwarm()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            var pk = DefDatabase<PawnKindDef>.GetNamed("Mech_Scyther", false);
            if (pk == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            for (int i = 0; i < 3; i++)
            {
                var req = new PawnGenerationRequest(pk, Faction.OfPlayer);
                GenSpawn.Spawn(PawnGenerator.GeneratePawn(req), cell, Find.CurrentMap);
            }
            string msg = "ISA_SwarmCalled".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SpawnMechHerd()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            var pk = DefDatabase<PawnKindDef>.GetNamed("Mech_Centipede", false);
            if (pk == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            for (int i = 0; i < 3; i++)
            {
                var req = new PawnGenerationRequest(pk, Faction.OfPlayer);
                GenSpawn.Spawn(PawnGenerator.GeneratePawn(req), cell, Find.CurrentMap);
            }
            string msg = "ISA_MechHerdSpawned".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  12) SKILL-MEISTER
        // ---------------------------------------------------
        private void OpenSkillMaster()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MaxAllColonistSkills".Translate(),    MaxAllColonistSkills),
                ("ISA_MaxSelectedPawnSkills".Translate(),   MaxSelectedPawnSkills),
                ("ISA_SetPassion".Translate(),              OpenPassionMenu),
                ("ISA_GrantInspiration".Translate(),        GrantInspiration),
            };
            MenuHelper.Open("ISA_Master_SkillMaster".Translate(), items);
        }

        private void MaxAllColonistSkills()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.FreeColonists)
            {
                if (p.skills == null) continue;
                foreach (var sk in p.skills.skills)
                {
                    sk.Level = 20;
                    sk.passion = Passion.Major;
                }
            }
            string msg = "ISA_AllSkillsMaxed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void MaxSelectedPawnSkills()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn?.skills == null)
            {
                TTS.Say("ISA_NoPawnSelected".Translate());
                return;
            }
            MaxSkills(pawn);
        }

        private void OpenPassionMenu()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn?.skills == null)
            {
                TTS.Say("ISA_NoPawnSelected".Translate());
                return;
            }
            var passionItems = new List<(string, Action)>
            {
                ("ISA_PassionMinor".Translate(), () => SetPassion(pawn, Passion.Minor)),
                ("ISA_PassionMajor".Translate(), () => SetPassion(pawn, Passion.Major)),
                ("ISA_PassionNone".Translate(),  () => SetPassion(pawn, Passion.None)),
            };
            MenuHelper.Open("ISA_SetPassion".Translate(), passionItems);
        }

        private void SetPassion(Pawn pawn, Passion passion)
        {
            if (pawn.skills == null) return;
            foreach (var sk in pawn.skills.skills)
                sk.passion = passion;
            string msg = "ISA_PassionSet".Translate() + " " + passion.ToString();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void GrantInspiration()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var inspirationDef = DefDatabase<InspirationDef>.AllDefs.FirstOrDefault();
            if (inspirationDef == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.FreeColonists)
                p.mindState?.inspirationHandler?.TryStartInspiration(inspirationDef, null);
            string msg = "ISA_InspirationGranted".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  13) BASIS-INSTANDHALTUNG
        // ---------------------------------------------------
        private void OpenBaseMaintenance()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_RepairAllBuildings".Translate(),    RepairAllBuildings),
                ("ISA_FinishAllConstruction".Translate(), FinishAllConstruction),
                ("ISA_RefuelAll".Translate(),             RefuelAll),
                ("ISA_RechargeAll".Translate(),           RechargeAll),
            };
            MenuHelper.Open("ISA_Master_BaseMaintenance".Translate(), items);
        }

        private void RepairAllBuildings()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int count = 0;
            foreach (var t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).ToList())
            {
                if (t is Building b && b.HitPoints < b.MaxHitPoints)
                {
                    b.HitPoints = b.MaxHitPoints;
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_BuildingsRepaired", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void FinishAllConstruction()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var f in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Blueprint).ToList())
            {
                if (f is Blueprint bp)
                    bp.TryReplaceWithSolidThing(null, out _, out _);
            }
            string msg = "ISA_ConstructionFinished".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void RefuelAll()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).ToList())
            {
                var comp = t.TryGetComp<CompRefuelable>();
                comp?.Refuel(comp.Props.fuelCapacity - comp.Fuel);
            }
            string msg = "ISA_AllRefueled".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void RechargeAll()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            foreach (var t in Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.BuildingArtificial).ToList())
            {
                var comp = t.TryGetComp<CompPowerBattery>();
                if (comp != null) comp.AddEnergy(comp.Props.storedEnergyMax - comp.StoredEnergy);
            }
            string msg = "ISA_AllRecharged".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  TERRAIN & KARTEN-EDITOR
        // ---------------------------------------------------
        private void OpenTerrainManager()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ChangeTerrain".Translate(), OpenTerrainDefList),
                ("Spawn Steam Geyser", SpawnGeyser),
                ("Spawn Meteorite", OpenMeteoriteMenu)
            };
            MenuHelper.Open("ISA_TerrainAndMapEditor".Translate(), items);
        }

        private void OpenTerrainDefList()
        {
            var terrains = DefDatabase<TerrainDef>.AllDefs
                .OrderBy(t => t.label ?? t.defName)
                .Select(tDef => 
                {
                    string label = GenText.CapitalizeFirst(tDef.label ?? tDef.defName);
                    Action act = () => OpenTerrainTargetMenu(tDef);
                    return (label, act);
                }).ToList();
                
            MenuHelper.Open("ISA_SelectTerrain".Translate(), terrains);
        }

        private void OpenTerrainTargetMenu(TerrainDef tDef)
        {
            var items = new List<(string, Action)>
            {
                ("ISA_TargetZone".Translate(), () => OpenTerrainZoneMenu(tDef)),
                ("ISA_TargetColonistRadius".Translate(), () => OpenTerrainColonistMenu(tDef))
            };
            MenuHelper.Open("ISA_SelectTargetArea".Translate() + " (" + (tDef.label ?? tDef.defName) + ")", items);
        }

        private void OpenTerrainZoneMenu(TerrainDef tDef)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var zones = map.zoneManager.AllZones.OrderBy(z => z.label).ToList();
            if (zones.Count == 0) { TTS.Say("ISA_NoZones".Translate()); return; }
            var items = zones.Select(z => 
            {
                string label = z.label;
                Action act = () => 
                {
                    int changed = 0;
                    foreach (var cell in z.Cells)
                    {
                        map.terrainGrid.SetTerrain(cell, tDef);
                        changed++;
                    }
                    TTS.Say("ISA_TerrainChangedZone".Translate() + $" ({changed} cells in {z.label})");
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_SelectZone".Translate(), items);
        }

        private void OpenTerrainColonistMenu(TerrainDef tDef)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.LabelShort).ToList();
            if (colonists.Count == 0) { TTS.Say("ISA_NoColonists".Translate()); return; }

            var items = colonists.Select(p => 
            {
                string label = p.LabelShort;
                Action act = () => OpenTerrainRadiusMenu(tDef, p);
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_SelectColonist".Translate(), items);
        }

        private void OpenTerrainRadiusMenu(TerrainDef tDef, Pawn pawn)
        {
            var map = pawn.Map;
            var items = new List<int> { 3, 5, 10 }.Select(r => 
            {
                string label = "ISA_Radius".Translate() + " " + r;
                Action act = () => 
                {
                    int changed = 0;
                    foreach (var cell in GenRadial.RadialCellsAround(pawn.Position, r, true))
                    {
                        if (cell.InBounds(map))
                        {
                            map.terrainGrid.SetTerrain(cell, tDef);
                            changed++;
                        }
                    }
                    TTS.Say("ISA_TerrainChangedRadius".Translate() + $" ({changed} cells)");
                };
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_SelectRadius".Translate(), items);
        }

        private void SpawnGeyser()
        {
            var map = Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            
            MenuHelper.SelectTargetCell(map, (cell) =>
            {
                if (!cell.InBounds(map)) return;
                var def = DefDatabase<ThingDef>.GetNamed("SteamGeyser", false);
                if (def != null)
                {
                    GenSpawn.Spawn(def, cell, map);
                    TTS.Say("Steam Geyser spawned.");
                }
            });
        }

        private void OpenMeteoriteMenu()
        {
            var map = Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }

            var materials = new List<string> { "Steel", "Plasteel", "Gold", "Silver", "Uranium", "Jade" };
            var items = materials.Select(m => 
            {
                Action act = () =>
                {
                    MenuHelper.SelectTargetCell(map, (cell) =>
                    {
                        if (!cell.InBounds(map)) return;
                        var thingDef = DefDatabase<ThingDef>.GetNamed(m, false);
                        if (thingDef != null)
                        {
                            SkyfallerMaker.SpawnSkyfaller(ThingDefOf.MeteoriteIncoming, thingDef, cell, map);
                            TTS.Say("Meteorite incoming: " + m);
                        }
                    });
                };
                return (m, act);
            }).ToList();
            MenuHelper.Open("Spawn Meteorite", items);
        }

        // ---------------------------------------------------
        //  14) NATUR-KONTROLLE
        // ---------------------------------------------------
        private void OpenNatureControl()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MaxAllPlants".Translate(),    MaxAllPlants),
                ("ISA_RemoveAllWildAnimals".Translate(), RemoveWildAnimals),
                ("ISA_SpawnAnimalHerd".Translate(), OpenSpawnAnimalMenu),
                ("ISA_ChangeSeasonTick".Translate(), OpenSeasonMenu),
            };
            MenuHelper.Open("ISA_Master_NatureControl".Translate(), items);
        }

        private void MaxAllPlants()
        {
            MaxPlantGrowth();
        }

        private void RemoveWildAnimals()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int count = 0;
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned.ToList())
            {
                if (p.RaceProps.Animal && p.Faction == null && !p.Dead)
                {
                    p.Destroy();
                    count++;
                }
            }
            string msg = TranslatorFormattedStringExtensions.Translate("ISA_WildAnimalsRemoved", count.ToString());
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenSpawnAnimalMenu()
        {
            var animalKinds = DefDatabase<PawnKindDef>.AllDefs
                .Where(pk => pk.RaceProps?.Animal == true)
                .OrderBy(pk => pk.label ?? pk.defName)
                .Select(pk =>
                {
                    string label = GenText.CapitalizeFirst(pk.label ?? pk.defName);
                    Action act = () => SpawnAnimalHerd(pk, 5);
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_SpawnAnimalHerd".Translate(), animalKinds);
        }

        private void SpawnAnimalHerd(PawnKindDef pk, int count)
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var cell = DropCellFinder.TradeDropSpot(Find.CurrentMap);
            for (int i = 0; i < count; i++)
            {
                var req = new PawnGenerationRequest(pk, null);
                GenSpawn.Spawn(PawnGenerator.GeneratePawn(req), cell, Find.CurrentMap);
            }
            string msg = count + "x " + (pk.label ?? pk.defName) + " " + "ISA_SpawnedSuffix".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenSeasonMenu()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_SeasonSpring".Translate(), () => SkipToSeason(0)),
                ("ISA_SeasonSummer".Translate(), () => SkipToSeason(15000)),
                ("ISA_SeasonFall".Translate(),   () => SkipToSeason(30000)),
                ("ISA_SeasonWinter".Translate(), () => SkipToSeason(45000)),
            };
            MenuHelper.Open("ISA_ChangeSeasonTick".Translate(), items);
        }

        private void SkipToSeason(int targetTickInYear)
        {
            int yearLen = 3600000;
            int curTick = Find.TickManager.TicksGame % yearLen;
            int delta = targetTickInYear - curTick;
            if (delta < 0) delta += yearLen;
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + delta);
            TTS.Say("ISA_SeasonSkipped".Translate());
        }

        // ---------------------------------------------------
        //  15) ROYALTY-PSYCAST (optional, falls DLC vorhanden)
        // ---------------------------------------------------
        private void OpenRoyaltyPsycast()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                TTS.Say("ISA_NoPawnSelected".Translate());
                return;
            }

            var items = new List<(string, Action)>
            {
                ("ISA_MaxPsyfocus".Translate(),   () => MaxPsyfocus(pawn)),
                ("ISA_GrantPsylink".Translate(),  () => GrantPsylink(pawn)),
                ("ISA_GrantRoyalTitle".Translate(),() => OpenGrantRoyalTitle(pawn)),
                ("ISA_UnlockAllPsycasts".Translate(), () => UnlockAllPsycasts(pawn)),
            };
            MenuHelper.Open("ISA_Master_RoyaltyPsycast".Translate() + ": " + pawn.LabelShort, items);
        }

        private void MaxPsyfocus(Pawn pawn)
        {
            if (pawn.psychicEntropy == null) { TTS.Say("ISA_NoPsychic".Translate()); return; }
            pawn.psychicEntropy.SetInitialPsyfocusLevel();
            pawn.psychicEntropy.TryAddEntropy(-pawn.psychicEntropy.EntropyValue, null, false, false);
            string msg = "ISA_PsyfocusMaxed".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void GrantPsylink(Pawn pawn)
        {
            var psylinkDef = DefDatabase<HediffDef>.GetNamed("PsychicAmplifier", false);
            if (psylinkDef == null) { TTS.Say("ISA_RoyaltyNotInstalled".Translate()); return; }
            if (pawn.health.hediffSet.HasHediff(psylinkDef))
            {
                // Increase level
                var h = pawn.health.hediffSet.GetFirstHediffOfDef(psylinkDef);
                (h as Hediff_Psylink)?.ChangeLevel(1, true);
            }
            else
            {
                pawn.health.AddHediff(psylinkDef);
            }
            string msg = "ISA_PsylinkGranted".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenGrantRoyalTitle(Pawn pawn)
        {
            var empire = Find.FactionManager.AllFactionsListForReading
                .FirstOrDefault(f => f.def?.royalTitleTags?.Contains("Empire") == true);
            if (empire == null) { TTS.Say("ISA_RoyaltyNotInstalled".Translate()); return; }

            var titles = DefDatabase<RoyalTitleDef>.AllDefs
                .OrderBy(t => t.seniority)
                .Select(t =>
                {
                    string label = GenText.CapitalizeFirst(t.label ?? t.defName);
                    Action act = () =>
                    {
                        pawn.royalty?.SetTitle(empire, t, true, true, false);
                        string msg = "ISA_TitleGranted".Translate() + " " + label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_GrantRoyalTitle".Translate(), titles);
        }

        private void UnlockAllPsycasts(Pawn pawn)
        {
            if (pawn.abilities == null) { TTS.Say("ISA_NoPsychic".Translate()); return; }
            foreach (var ab in DefDatabase<AbilityDef>.AllDefs.Where(a => a.IsPsycast))
            {
                if (pawn.abilities.GetAbility(ab) == null)
                    pawn.abilities.GainAbility(ab);
            }
            string msg = "ISA_PsycastsUnlocked".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  16) BIOTECH-GENETIK
        // ---------------------------------------------------
        private void OpenBiotechGenetics()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                TTS.Say("ISA_NoPawnSelected".Translate());
                return;
            }

            var items = new List<(string, Action)>
            {
                ("ISA_AddGene".Translate(),       () => OpenAddGeneMenu(pawn)),
                ("ISA_RemoveGene".Translate(),    () => OpenRemoveGeneMenu(pawn)),
                ("ISA_ApplyXenotype".Translate(), () => OpenApplyXenotypeMenu(pawn)),
                ("ISA_MaxHemogen".Translate(),    () => MaxHemogen(pawn)),
            };
            MenuHelper.Open("ISA_Master_BiotechGenetics".Translate() + ": " + pawn.LabelShort, items);
        }

        private void OpenAddGeneMenu(Pawn pawn)
        {
            if (pawn.genes == null) { TTS.Say("ISA_BiotechNotInstalled".Translate()); return; }
            var genes = DefDatabase<GeneDef>.AllDefs
                .OrderBy(g => g.label ?? g.defName)
                .Select(g =>
                {
                    string label = GenText.CapitalizeFirst(g.label ?? g.defName);
                    Action act = () =>
                    {
                        pawn.genes.AddGene(g, true);
                        string msg = "ISA_GeneAdded".Translate() + " " + label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_AddGene".Translate(), genes);
        }

        private void OpenRemoveGeneMenu(Pawn pawn)
        {
            if (pawn.genes == null) { TTS.Say("ISA_BiotechNotInstalled".Translate()); return; }
            var genes = pawn.genes.GenesListForReading
                .Select(g =>
                {
                    string label = GenText.CapitalizeFirst(g.def.label ?? g.def.defName);
                    Action act = () =>
                    {
                        pawn.genes.RemoveGene(g);
                        string msg = "ISA_GeneRemoved".Translate() + " " + label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_RemoveGene".Translate(), genes);
        }

        private void OpenApplyXenotypeMenu(Pawn pawn)
        {
            if (pawn.genes == null) { TTS.Say("ISA_BiotechNotInstalled".Translate()); return; }
            var types = DefDatabase<XenotypeDef>.AllDefs
                .OrderBy(x => x.label ?? x.defName)
                .Select(x =>
                {
                    string label = GenText.CapitalizeFirst(x.label ?? x.defName);
                    Action act = () =>
                    {
                        pawn.genes.SetXenotype(x);
                        string msg = "ISA_XenotypeApplied".Translate() + " " + label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_ApplyXenotype".Translate(), types);
        }

        private void MaxHemogen(Pawn pawn)
        {
            var need = pawn.genes?.GetFirstGeneOfType<Gene_Hemogen>();
            if (need == null) { TTS.Say("ISA_NoPawnSelected".Translate()); return; }
            if (need != null) need.Value = need.Max;
            string msg = "ISA_HemogenMaxed".Translate() + " " + pawn.LabelShort;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ---------------------------------------------------
        //  17) IDEOLOGY-GLAUBEN
        // ---------------------------------------------------
        private void OpenIdeologyBelief()
        {
            var pawn = Find.Selector.SingleSelectedThing as Pawn;

            var items = new List<(string, Action)>
            {
                ("ISA_MaxCertainty".Translate(),      () => MaxCertainty(pawn)),
                ("ISA_ConvertAllColonists".Translate(), () => ConvertAll()),
                ("ISA_ApplyRitualRole".Translate(),   () => TTS.Say("ISA_NotImplemented".Translate())),
            };
            MenuHelper.Open("ISA_Master_IdeologyBelief".Translate(), items);
        }

        private void MaxCertainty(Pawn pawn)
        {
            if (pawn?.ideo == null)
            {
                // Alle Kolonisten
                if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
                foreach (var p in Find.CurrentMap.mapPawns.FreeColonists)
                    if (p.ideo != null) p.ideo.OffsetCertainty(1f);
            }
            else
            {
                pawn.ideo.OffsetCertainty(1f);
            }
            string msg = "ISA_CertaintyMaxed".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void ConvertAll()
        {
            if (Find.CurrentMap == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var playerIdeo = Faction.OfPlayer.ideos?.PrimaryIdeo;
            if (playerIdeo == null) { TTS.Say("ISA_IdeologyNotInstalled".Translate()); return; }
            foreach (var p in Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (p.IsColonist && p.ideo != null)
                {
                    p.ideo.SetIdeo(playerIdeo);
                    p.ideo.OffsetCertainty(1f);
                }
            }
            string msg = "ISA_AllConverted".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }



        // ---------------------------------------------------
        //  23) ENTWICKLER- & DEBUG-MANAGER
        // ---------------------------------------------------
        private void OpenDebugManager()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("Gott-Modus umschalten", (System.Action)(() => {
                    Verse.DebugSettings.godMode = !Verse.DebugSettings.godMode;
                    TTS.Say(Verse.DebugSettings.godMode ? "Gott-Modus aktiviert." : "Gott-Modus deaktiviert.");
                })),
                ("Forschungs-Debug umschalten", (System.Action)(() => {
                    Verse.Prefs.DevMode = !Verse.Prefs.DevMode;
                    TTS.Say(Verse.Prefs.DevMode ? "Entwicklermodus aktiviert." : "Entwicklermodus deaktiviert.");
                })),
            };
            MenuHelper.Open("Entwickler- & Debug-Manager", items);
        }

        // ---------------------------------------------------
        //  24) KAMERA- & SICHTFELD-CONTROLLER
        // ---------------------------------------------------
        private void OpenCameraController()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("Auf ausgewähltes Objekt zentrieren", (System.Action)(() => {
                    var sel = Verse.Find.Selector.SingleSelectedThing;
                    if (sel != null) {
                        Verse.CameraJumper.TryJump(sel);
                        TTS.Say($"Kamera zentriert auf: {sel.LabelShort}.");
                    } else {
                        TTS.Say("Kein Objekt ausgewählt.");
                    }
                })),
                ("Auf einen zufälligen Kolonisten springen", (System.Action)(() => {
                    var map = Verse.Find.CurrentMap;
                    if (map != null && map.mapPawns.FreeColonistsCount > 0) {
                        var pawn = map.mapPawns.FreeColonists.RandomElement();
                        Verse.CameraJumper.TryJump(pawn);
                        TTS.Say($"Kamera gesprungen zu: {pawn.LabelShort}.");
                    } else {
                        TTS.Say("Keine Kolonisten auf dieser Karte.");
                    }
                })),
                ("Auf das Zentrum der Karte zentrieren", (System.Action)(() => {
                    var map = Verse.Find.CurrentMap;
                    if (map != null) {
                        Verse.CameraJumper.TryJump(map.Center, map);
                        TTS.Say("Kamera auf das Zentrum der Karte ausgerichtet.");
                    } else {
                        TTS.Say("Keine Karte gefunden.");
                    }
                })),
            };
            MenuHelper.Open("Kamera- & Sichtfeld-Controller", items);
        }

        // ---------------------------------------------------
        //  25) ZONEN- & RAUM-ANALYSATOR
        // ---------------------------------------------------
        private void OpenRoomAnalyzer()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("Raum des ausgewählten Kolonisten analysieren", (System.Action)(() => AnalyzeSelectedPawnRoom())),
            };
            MenuHelper.Open("Zonen- & Raum-Analysator", items);
        }

        private void AnalyzeSelectedPawnRoom()
        {
            var pawn = Verse.Find.Selector.SingleSelectedThing as Verse.Pawn;
            if (pawn == null)
            {
                TTS.Say("Bitte wähle zuerst einen Kolonisten oder ein Lebewesen aus.");
                return;
            }

            var room = pawn.GetRoom(Verse.RegionType.Set_All);
            if (room == null || room.PsychologicallyOutdoors)
            {
                TTS.Say($"{pawn.LabelShort} befindet sich draußen oder in keinem geschlossenen Raum.");
                return;
            }

            float temp = room.Temperature;
            float beauty = room.GetStat(RimWorld.RoomStatDefOf.Beauty);
            float clean = room.GetStat(RimWorld.RoomStatDefOf.Cleanliness);
            float wealth = room.GetStat(RimWorld.RoomStatDefOf.Wealth);

            string report = $"Raum von {pawn.LabelShort}: Temperatur {temp.ToString("F1")} Grad. ";
            report += $"Schönheit: {beauty.ToString("F1")}. ";
            report += $"Sauberkeit: {clean.ToString("F1")}. ";
            report += $"Reichtum: {wealth.ToString("F0")}.";

            TTS.Say(report);
        }
    }

    // ---------------------------------------------------------
    //  Hinweis: Dialog_SpawnQuantity wurde entfernt.
    //  Der Spawn-Flow läuft jetzt vollständig über
    //  AccessibleWindowlessMenu (100% blind-zugänglich).
    // ---------------------------------------------------------

    // ---------------------------------------------------------
    //  Hinweis: Dialog_SetAge wurde entfernt.
    //  OpenSetAgeMenu() ist jetzt in ItemSpawnerAccessListener.
    // ---------------------------------------------------------
}
