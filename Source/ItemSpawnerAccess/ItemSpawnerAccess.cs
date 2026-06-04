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
    // ─────────────────────────────────────────────────────────
    //  Tolk-Wrapper – falls RimWorldAccess aktiv ist, nutzen
    //  wir dessen DLL; ansonsten fallen wir auf Messages zurück.
    // ─────────────────────────────────────────────────────────
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

    // ─────────────────────────────────────────────────────────
    //  Barrierefreies Listenmenü (ersetzt FloatMenu komplett)
    // ─────────────────────────────────────────────────────────
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
                TTS.Say($"{prefix}No results.");
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
                    TTS.Say("Search cleared.");
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
                    TTS.Say("No action available");
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
                        TTS.Say("Search cleared.");
                    else
                        TTS.Say("Search: " + _searchString);
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
                TTS.Say("Search: " + _searchString);
                AnnounceSelected();
                e.Use();
            }
        }
    }

            

    // ─────────────────────────────────────────────────────────
    //  Hilfsmethode: einfaches Menü öffnen
    // ─────────────────────────────────────────────────────────
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
                ("Near a specific Colonist", () => 
                {
                    var colonists = map.mapPawns.FreeColonists.OrderBy(p => p.NameShortColored.Resolve()).ToList();
                    if (colonists.Count == 0) { TTS.Say("No colonists available."); return; }
                    var colItems = colonists.Select(p => 
                    {
                        string label = p.LabelShort;
                        Action act = () => onCellSelected(p.Position);
                        return (label, act);
                    }).ToList();
                    Open("Select Colonist", colItems);
                }),
                ("In a specific Zone", () => 
                {
                    var zones = map.zoneManager.AllZones.OrderBy(z => z.label).ToList();
                    if (zones.Count == 0) { TTS.Say("No zones available."); return; }
                    var zItems = zones.Select(z => 
                    {
                        string label = z.label;
                        Action act = () => 
                        {
                            var cell = System.Linq.Enumerable.FirstOrDefault(z.Cells);
                            if (cell.IsValid) onCellSelected(cell);
                            else TTS.Say("Zone is empty.");
                        };
                        return (label, act);
                    }).ToList();
                    Open("Select Zone", zItems);
                }),
                ("At Map Center", () => onCellSelected(map.Center))
            };
            Open("Where to spawn? / Wo soll es gespawnt werden?", items);
        }
    }

    // ─────────────────────────────────────────────────────────
    //  Initialisierung & Tastatur-Listener
    // ─────────────────────────────────────────────────────────
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

        // ═══════════════════════════════════════════════════
        //  MASTER-MENÜ (17 Einträge)
        // ═══════════════════════════════════════════════════
        private void OpenMasterMenu()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_Master_ItemSpawner".Translate(),    OpenItemSpawner),
                ("ISA_Master_EventSpawner".Translate(),   OpenEventSpawner),
                ("ISA_Master_PawnEditor".Translate(),     OpenPawnEditor),
                ("ISA_Master_WeatherTime".Translate(),    OpenWeatherTime),
                ("ISA_Master_ResearchFaction".Translate(),OpenResearchFaction),
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
                  ("ISA_Master_AnimalTaming".Translate(), OpenAnimalTaming),
                  ("ISA_Master_FactionManager".Translate(), OpenFactionManager),
                  ("ISA_Master_RaidTrigger".Translate(), OpenRaidTrigger),
                  ("ISA_Master_WeatherController".Translate(), OpenWeatherController),
                  ("ISA_Master_ColonyManager".Translate(), OpenColonyManager),
                  ("ISA_Master_QuestTrade".Translate(), OpenQuestTrade),
              };
            TTS.Say("ISA_MasterMenuOpened".Translate());
            AccessibleWindowlessMenu.Open("ISA_MasterMenuTitle".Translate(), items);
        }

        // ═══════════════════════════════════════════════════
        //  1) ITEM SPAWNER
        // ═══════════════════════════════════════════════════
        
        
        
        
        
        private void OpenColonyManager()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("ISA_HealAllColonists".Translate(), () => HealAllColonists()),
                ("ISA_FeedAllColonists".Translate(), () => FeedAllColonists()),
                ("ISA_CM_RecruitAllPrisoners".Translate(), () => CM_RecruitAllPrisoners())
            };

            TTS.Say("Colony Manager Menu");
            AccessibleWindowlessMenu.Open("Colony Manager", items);
        }

        private void OpenQuestTrade()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("ISA_GenerateQuest".Translate(), () => GenerateRandomQuest()),
                ("ISA_SpawnTradeCaravan".Translate(), () => SpawnTradeCaravan()),
                ("ISA_CallOrbitalTrader".Translate(), () => CallOrbitalTrader())
            };

            TTS.Say("Quest and Trade Manager Menu");
            AccessibleWindowlessMenu.Open("Quest and Trade Manager", items);
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
                TTS.Say("Failed to generate quest");
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
            TTS.Say("Failed to spawn trade caravan");
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
            TTS.Say("Failed to call orbital trader");
        }

        private void CM_RecruitAllPrisoners()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            foreach (var pawn in map.mapPawns.PrisonersOfColony)
            {
                if (pawn.guest != null)
                {
                    pawn.guest.SetGuestStatus(null, RimWorld.GuestStatus.Guest);
                    pawn.SetFaction(RimWorld.Faction.OfPlayer);
                }
            }
            TTS.Say("Recruited all prisoners.");
        }

        private void HealAllColonists()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) return;
            foreach (var pawn in map.mapPawns.FreeColonists)
            {
                Verse.HealthUtility.HealNonPermanentInjuriesAndRestoreLegs(pawn);
            }
            TTS.Say("Healed all colonists.");
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
            TTS.Say("Fed all colonists.");
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

            TTS.Say("Weather Controller Menu");
            AccessibleWindowlessMenu.Open("Weather Controller", items);
        }

        private void ChangeWeather(Verse.WeatherDef weatherDef)
        {
            var map = Verse.Find.CurrentMap;
            if (map != null)
            {
                map.weatherManager.TransitionTo(weatherDef);
                string msg = "Changed weather to " + (weatherDef.label ?? weatherDef.defName);
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
            TTS.Say("Raid Trigger Menu");
            AccessibleWindowlessMenu.Open("Raid Trigger", items);
        }

        private void TriggerRandomRaid()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say(Verse.Translator.Translate("ISA_NoValidTarget")); return; }
            RimWorld.IncidentParms parms = RimWorld.StorytellerUtility.DefaultParmsNow(RimWorld.IncidentCategoryDefOf.ThreatBig, map);
            parms.forced = true;
            if (RimWorld.IncidentDefOf.RaidEnemy.Worker.TryExecute(parms))
            {
                TTS.Say("Triggered Enemy Raid.");
            }
            else
            {
                TTS.Say("Failed to trigger raid.");
            }
        }

        private void OpenFactionManager()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MakePeaceWithAll".Translate(), () => MakePeaceWithAll()),
                ("ISA_MaxReputationWithAll".Translate(), () => MaxReputationWithAll()),
                ("Manage Specific Faction...", () => OpenFactionList())
            };
            MenuHelper.Open("Faction Manager", items);
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
            string msg = $"Made peace with {count} factions.";
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
            string msg = $"Maximized reputation with {count} factions.";
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
                string label = $"{f.Name} (Goodwill: {f.GoodwillWith(RimWorld.Faction.OfPlayer)})";
                Action act = () => OpenFactionActionMenu(f);
                return (label, act);
            }).ToList();

            MenuHelper.Open("Select Faction", items);
        }

        private void OpenFactionActionMenu(RimWorld.Faction faction)
        {
            var items = new List<(string, Action)>
            {
                ("+10 Goodwill", () => { faction.TryAffectGoodwillWith(RimWorld.Faction.OfPlayer, 10, true, true, null, null); TTS.Say($"Goodwill is now {faction.GoodwillWith(RimWorld.Faction.OfPlayer)}"); }),
                ("-10 Goodwill", () => { faction.TryAffectGoodwillWith(RimWorld.Faction.OfPlayer, -10, true, true, null, null); TTS.Say($"Goodwill is now {faction.GoodwillWith(RimWorld.Faction.OfPlayer)}"); }),
                ("Make Allied", () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Ally, false); TTS.Say($"{faction.Name} is now Allied"); }),
                ("Make Neutral", () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Neutral, false); TTS.Say($"{faction.Name} is now Neutral"); }),
                ("Make Hostile", () => { faction.SetRelationDirect(RimWorld.Faction.OfPlayer, RimWorld.FactionRelationKind.Hostile, false); TTS.Say($"{faction.Name} is now Hostile"); })
            };
            MenuHelper.Open($"Manage {faction.Name}", items);
        }

        private void OpenAnimalTaming()
        {
            var items = new System.Collections.Generic.List<(string, System.Action)>
            {
                ("ISA_TameAllAnimals".Translate(), () => TameAllAnimals()),
            };
            TTS.Say("Animal Taming Menu");
            AccessibleWindowlessMenu.Open("Animal Taming", items);
        }

        private void TameAllAnimals()
        {
            if (Verse.Find.CurrentMap == null) return;
            int count = 0;
            foreach (var pawn in Verse.Find.CurrentMap.mapPawns.AllPawnsSpawned)
            {
                if (pawn.RaceProps.Animal && pawn.Faction != RimWorld.Faction.OfPlayer)
                {
                    pawn.SetFaction(RimWorld.Faction.OfPlayer);
                    count++;
                }
            }
            TTS.Say($"{count} animals tamed.");
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
                        else Find.WindowStack.Add(new Dialog_SpawnQuantity(def, null, null));
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
                Find.WindowStack.Add(new Dialog_SpawnQuantity(itemDef, null, null));
                return;
            }

            var items = stuffList.Select(stuff =>
            {
                string label = GenText.CapitalizeFirst(stuff.label ?? stuff.defName);
                Action act = () => Find.WindowStack.Add(new Dialog_SpawnQuantity(itemDef, stuff, null));
                return (label, act);
            }).ToList();
            MenuHelper.Open("ISA_SelectMaterial".Translate(), items);
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
                    Action act = () => Find.WindowStack.Add(new Dialog_SpawnQuantity(null, null, pk));
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_Menu_Pawns".Translate(), items);
        }

        // ═══════════════════════════════════════════════════
        //  2) EVENT SPAWNER
        // ═══════════════════════════════════════════════════
        private void OpenEventSpawner()
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
                            Action incAct = () => TriggerIncident(inc);
                            return (incLabel, incAct);
                        }).ToList();
                    MenuHelper.Open(label, subItems);
                };
                return (label, act);
            }).ToList();

            MenuHelper.Open("ISA_Master_EventSpawner".Translate(), items);
        }

        private void TriggerIncident(IncidentDef def)
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
                parms.points = StorytellerUtility.DefaultThreatPointsNow(target);

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

        // ═══════════════════════════════════════════════════
        //  3) PAWN EDITOR
        // ═══════════════════════════════════════════════════
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
                if (colonists.Count == 0) { TTS.Say("No colonists available."); return; }
                
                var items = colonists.Select(p => 
                {
                    string label = p.LabelShort;
                    Action act = () => OpenPawnEditorForPawn(p);
                    return (label, act);
                }).ToList();
                MenuHelper.Open("Select Colonist to Edit", items);
            }
        }

        private void OpenPawnEditorForPawn(Verse.Pawn sel)
        {
            var items = new List<(string, Action)>
            {
                ("ISA_HealPawn".Translate(), () => HealPawn(sel)),
            };

            if (sel.skills != null)
            {
                items.Add(("ISA_MaxSkills".Translate(), () => MaxSkills(sel)));
                items.Add(("Edit Individual Skills (Add +1)...", () => OpenIndividualSkills(sel, 1)));
                items.Add(("Edit Individual Skills (Subtract -1)...", () => OpenIndividualSkills(sel, -1)));
            }

            if (sel.story?.traits != null)
            {
                items.Add(("ISA_AddTrait".Translate(), () => OpenAddTrait(sel)));
                items.Add(("ISA_RemoveTrait".Translate(), () => OpenRemoveTrait(sel)));
            }

            items.Add(("ISA_AddHediff".Translate(), () => OpenAddHediff(sel)));
            items.Add(("ISA_RemoveHediff".Translate(), () => OpenRemoveHediff(sel)));

            items.Add(("ISA_SetAge".Translate(), () => Find.WindowStack.Add(new Dialog_SetAge(sel))));
            items.Add(("ISA_GiveWeapon".Translate(), () => GiveBestWeapon(sel)));

            MenuHelper.Open("ISA_Master_PawnEditor".Translate() + ": " + sel.LabelShort, items);
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
                    
                    TTS.Say($"{sk.def.LabelCap} is now level {sk.Level}");
                    OpenIndividualSkills(pawn, modifier); // Refresh menu
                };
                return (label, act);
            }).ToList();
            
            string title = modifier > 0 ? "Increase Skills (+1)" : "Decrease Skills (-1)";
            MenuHelper.Open(title, items);
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
                TTS.Say("Pawn has no traits capability.");
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
                        var hediff = HediffMaker.MakeHediff(h, pawn);
                        pawn.health.AddHediff(hediff);
                        TTS.Say("ISA_HediffAdded".Translate() + " " + label);
                    }));
                }).ToList();
            MenuHelper.Open("ISA_AddHediff".Translate(), items);
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

        // ═══════════════════════════════════════════════════
        //  4) WETTER & ZEIT
        // ═══════════════════════════════════════════════════
        private void OpenWeatherTime()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ChangeWeather".Translate(), OpenWeatherList),
                ("End All Game Conditions (e.g. Toxic Fallout)", EndAllGameConditions),
                ("Change Temperature...",         OpenTemperatureMenu),
                ("ISA_ChangeTime".Translate(),   OpenTimeMenu),
                ("ISA_SkipDay".Translate(),       SkipDay),
                ("ISA_SkipSeason".Translate(),    SkipSeason),
            };
            MenuHelper.Open("ISA_Master_WeatherTime".Translate(), items);
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
            string msg = $"Ended {count} active game conditions.";
            Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenTemperatureMenu()
        {
            var items = new List<(string, Action)>
            {
                ("Start Heat Wave", () => TriggerGameCondition(RimWorld.GameConditionDefOf.HeatWave)),
                ("Start Cold Snap", () => TriggerGameCondition(RimWorld.GameConditionDefOf.ColdSnap)),
                ("Start Volcanic Winter", () => TriggerGameCondition(RimWorld.GameConditionDefOf.VolcanicWinter)),
            };
            MenuHelper.Open("Change Temperature", items);
        }

        private void TriggerGameCondition(Verse.GameConditionDef def)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            int duration = 300000; // 5 days
            var cond = RimWorld.GameConditionMaker.MakeCondition(def, duration);
            map.gameConditionManager.RegisterCondition(cond);
            string msg = $"Triggered {def.label} for 5 days.";
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
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SkipDay()
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 60000);
            string msg = "ISA_DaySkipped".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void SkipSeason()
        {
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 900000);
            string msg = "ISA_SeasonSkipped".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        // ═══════════════════════════════════════════════════
        //  5) FORSCHUNG & FRAKTIONEN
        // ═══════════════════════════════════════════════════
        private void OpenResearchFaction()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_FinishAllResearch".Translate(),  FinishAllResearch),
                ("ISA_FinishOneResearch".Translate(),  OpenFinishOneResearch),
                ("ISA_PeaceWithAll".Translate(),       PeaceWithAll),
                ("ISA_MaxRelations".Translate(),       MaxRelations),
                ("ISA_AddSilver".Translate(),          () => AddResource(ThingDefOf.Silver, 1000)),
            };
            MenuHelper.Open("ISA_Master_ResearchFaction".Translate(), items);
        }

        private void FinishAllResearch()
        {
            foreach (var rp in DefDatabase<ResearchProjectDef>.AllDefs)
                if (!rp.IsFinished)
                    Find.ResearchManager.FinishProject(rp, false, null, true);
            string msg = "ISA_ResearchFinished".Translate();
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
        }

        private void OpenFinishOneResearch()
        {
            var items = DefDatabase<ResearchProjectDef>.AllDefs
                .Where(r => !r.IsFinished)
                .OrderBy(r => r.label ?? r.defName)
                .Select(r =>
                {
                    string label = GenText.CapitalizeFirst(r.label ?? r.defName);
                    Action act = () =>
                    {
                        Find.ResearchManager.FinishProject(r, false, null, true);
                        string msg = "ISA_ResearchFinishedOne".Translate() + " " + label;
                        Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
                        TTS.Say(msg);
                    };
                    return (label, act);
                }).ToList();
            MenuHelper.Open("ISA_FinishOneResearch".Translate(), items);
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

        // ═══════════════════════════════════════════════════
        //  6) STORYTELLER
        // ═══════════════════════════════════════════════════
        private void OpenStoryteller()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ChangeStoryteller".Translate(), OpenStorytellerList),
                ("ISA_ChangeDifficulty".Translate(),  OpenDifficultyList),
                ("Adjust Threat Scale...",            OpenThreatScaleMenu),
                ("Toggle Major Threats",              ToggleMajorThreats),
                ("Adjust Crop Yield...",              OpenCropYieldMenu),
            };
            MenuHelper.Open("ISA_Master_Storyteller".Translate(), items);
        }

        private void OpenThreatScaleMenu()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            var items = new List<(string, Action)>
            {
                ("Current: " + (diff.threatScale * 100f).ToString("F0") + "%", () => {}),
                ("Increase by 10%", () => { diff.threatScale += 0.1f; TTS.Say($"Threat scale is now {diff.threatScale * 100f:F0}%"); OpenThreatScaleMenu(); }),
                ("Decrease by 10%", () => { diff.threatScale = UnityEngine.Mathf.Max(0f, diff.threatScale - 0.1f); TTS.Say($"Threat scale is now {diff.threatScale * 100f:F0}%"); OpenThreatScaleMenu(); }),
                ("Increase by 50%", () => { diff.threatScale += 0.5f; TTS.Say($"Threat scale is now {diff.threatScale * 100f:F0}%"); OpenThreatScaleMenu(); }),
                ("Decrease by 50%", () => { diff.threatScale = UnityEngine.Mathf.Max(0f, diff.threatScale - 0.5f); TTS.Say($"Threat scale is now {diff.threatScale * 100f:F0}%"); OpenThreatScaleMenu(); }),
            };
            MenuHelper.Open("Adjust Threat Scale", items);
        }

        private void ToggleMajorThreats()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            diff.allowBigThreats = !diff.allowBigThreats;
            string state = diff.allowBigThreats ? "Enabled" : "Disabled";
            TTS.Say($"Major threats {state}");
        }

        private void OpenCropYieldMenu()
        {
            var diff = Verse.Find.Storyteller.difficulty;
            var items = new List<(string, Action)>
            {
                ("Current: " + (diff.cropYieldFactor * 100f).ToString("F0") + "%", () => {}),
                ("Increase by 10%", () => { diff.cropYieldFactor += 0.1f; TTS.Say($"Crop yield is now {diff.cropYieldFactor * 100f:F0}%"); OpenCropYieldMenu(); }),
                ("Decrease by 10%", () => { diff.cropYieldFactor = UnityEngine.Mathf.Max(0.1f, diff.cropYieldFactor - 0.1f); TTS.Say($"Crop yield is now {diff.cropYieldFactor * 100f:F0}%"); OpenCropYieldMenu(); }),
                ("Increase by 50%", () => { diff.cropYieldFactor += 0.5f; TTS.Say($"Crop yield is now {diff.cropYieldFactor * 100f:F0}%"); OpenCropYieldMenu(); }),
                ("Decrease by 50%", () => { diff.cropYieldFactor = UnityEngine.Mathf.Max(0.1f, diff.cropYieldFactor - 0.5f); TTS.Say($"Crop yield is now {diff.cropYieldFactor * 100f:F0}%"); OpenCropYieldMenu(); }),
            };
            MenuHelper.Open("Adjust Crop Yield", items);
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
                .OrderBy(d => d.difficulty)
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

        // ═══════════════════════════════════════════════════
        //  7) BASIS & KARTEN-WERKZEUGE
        // ═══════════════════════════════════════════════════
        private void OpenBaseMapTools()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_ToggleInstantBuild".Translate(), ToggleGodMode),
                ("ISA_ClearFog".Translate(),           ClearFog),
                ("ISA_MaxPlantGrowth".Translate(),     MaxPlantGrowth),
                ("ISA_RemoveAllRoofs".Translate(),     RemoveAllRoofs),
                ("ISA_DestroyAllBlueprints".Translate(),DestroyBlueprints),
                ("Terrain Tools...", OpenTerrainManager),
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

        // ═══════════════════════════════════════════════════
        //  8) STIMMUNGS-MANAGER
        // ═══════════════════════════════════════════════════
        private void OpenNeedsMood()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_MaxAllNeeds".Translate(),     MaxAllNeeds),
                ("ISA_StopMentalBreaks".Translate(),StopMentalBreaks),
                ("ISA_MassTame".Translate(),        MassTame),
                ("ISA_FeedAllAnimals".Translate(),  FeedAllAnimals),
            };
            MenuHelper.Open("ISA_Master_NeedsMood".Translate(), items);
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

        // ═══════════════════════════════════════════════════
        //  9) KOLONIE-MANAGER
        // ═══════════════════════════════════════════════════
        private void OpenColonyEnemy()
        {
            var items = new List<(string, Action)>
            {
                ("ISA_CM_RecruitAllPrisoners".Translate(), CM_RecruitAllPrisoners),
                ("ISA_KillAllEnemies".Translate(),      KillAllEnemies),
                ("ISA_CleanMap".Translate(),            CleanMap),
                ("ISA_AddColonist".Translate(),         AddColonist),
            };
            MenuHelper.Open("ISA_Master_ColonyEnemy".Translate(), items);
        }

        private void CM_CM_RecruitAllPrisoners()
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

        // ═══════════════════════════════════════════════════
        //  10) KARAWANEN-MANAGER
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  11) ARCHOTECH-MANAGER
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  12) SKILL-MEISTER
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  13) BASIS-INSTANDHALTUNG
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  TERRAIN & KARTEN-EDITOR
        // ═══════════════════════════════════════════════════
        private void OpenTerrainManager()
        {
            var items = new List<(string, Action)>
            {
                ("Spawn Steam Geyser", SpawnGeyser),
                ("Change Terrain (Single Target)", OpenChangeTerrainMenu),
                ("Change Terrain (Entire Zone)", OpenChangeTerrainZoneMenu),
                ("Spawn Meteorite", OpenMeteoriteMenu)
            };
            MenuHelper.Open("Terrain & Map Editor", items);
        }

        private void OpenChangeTerrainZoneMenu()
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            var zones = map.zoneManager.AllZones.OrderBy(z => z.label).ToList();
            if (zones.Count == 0) { TTS.Say("No zones available."); return; }
            var items = zones.Select(z => 
            {
                string label = z.label;
                Action act = () => OpenTerrainSelectionForZone(z);
                return (label, act);
            }).ToList();
            MenuHelper.Open("Select Zone to Change Terrain", items);
        }

        private void OpenTerrainSelectionForZone(Verse.Zone zone)
        {
            var map = zone.Map;
            var terrains = new List<string> { "Soil", "Sand", "WaterShallow", "WaterDeep", "Gravel", "Concrete" };
            var items = terrains.Select(t =>
            {
                Action act = () => 
                {
                    var tDef = DefDatabase<TerrainDef>.GetNamed(t, false);
                    if (tDef != null)
                    {
                        foreach (var cell in zone.Cells)
                        {
                            map.terrainGrid.SetTerrain(cell, tDef);
                        }
                        TTS.Say($"Changed {zone.Cells.Count} cells in {zone.label} to {t}");
                    }
                };
                return (t, act);
            }).ToList();
            MenuHelper.Open($"Set Terrain for {zone.label}", items);
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

        private void OpenChangeTerrainMenu()
        {
            var map = Find.CurrentMap;
            if (map == null) { TTS.Say("ISA_NoValidTarget".Translate()); return; }
            
            var terrains = new List<string> { "Soil", "Sand", "WaterShallow", "WaterDeep", "Gravel", "Concrete" };
            var items = terrains.Select(t =>
            {
                Action act = () => 
                {
                    MenuHelper.SelectTargetCell(map, (cell) =>
                    {
                        if (!cell.InBounds(map)) return;
                        var tDef = DefDatabase<TerrainDef>.GetNamed(t, false);
                        if (tDef != null)
                        {
                            map.terrainGrid.SetTerrain(cell, tDef);
                            TTS.Say("Terrain changed to " + t);
                        }
                    });
                };
                return (t, act);
            }).ToList();
            
            MenuHelper.Open("Change Terrain", items);
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

        // ═══════════════════════════════════════════════════
        //  14) NATUR-KONTROLLE
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  15) ROYALTY-PSYCAST (optional, falls DLC vorhanden)
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  16) BIOTECH-GENETIK
        // ═══════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════
        //  17) IDEOLOGY-GLAUBEN
        // ═══════════════════════════════════════════════════
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
    }

    // ─────────────────────────────────────────────────────────
    //  Dialog: Menge eingeben und Item/Pawn spawnen
    // ─────────────────────────────────────────────────────────
    public class Dialog_SpawnQuantity : Window
    {
        private readonly ThingDef   _itemDef;
        private readonly ThingDef   _stuffDef;
        private readonly PawnKindDef _pawnKind;
        private string _buffer = "1";

        public override Vector2 InitialSize => new Vector2(360f, 180f);

        public Dialog_SpawnQuantity(ThingDef itemDef, ThingDef stuffDef, PawnKindDef pawnKind)
            : base(null)
        {
            _itemDef  = itemDef;
            _stuffDef = stuffDef;
            _pawnKind = pawnKind;
            doCloseX  = true;
            forcePause = true;
            closeOnClickedOutside = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            string name = _itemDef != null
                ? (_itemDef.label ?? _itemDef.defName)
                : (_pawnKind?.label ?? _pawnKind?.defName ?? "?");
            TTS.Say("ISA_QuantityFor".Translate() + " " + name + ". " + "ISA_EnterQuantity".Translate());
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            string name = _itemDef != null
                ? (_itemDef.label ?? _itemDef.defName)
                : (_pawnKind?.label ?? _pawnKind?.defName ?? "?");

            Widgets.Label(new Rect(0, 0, inRect.width, 28f), "ISA_QuantityFor".Translate() + " " + name + ":");
            _buffer = Widgets.TextField(new Rect(0, 34f, inRect.width, 30f), _buffer);

            if (Widgets.ButtonText(new Rect(0, 74f, inRect.width, 34f), "ISA_SpawnBtn".Translate()))
                TrySpawn();

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                TrySpawn();
                Event.current.Use();
            }
        }

        private void TrySpawn()
        {
            if (!int.TryParse(_buffer, out int qty) || qty <= 0)
            {
                Messages.Message("ISA_InvalidQuantity".Translate(), MessageTypeDefOf.RejectInput, false);
                TTS.Say("ISA_InvalidQuantity".Translate());
                return;
            }
            Close();
            DoSpawn(qty);
        }

        private void DoSpawn(int qty)
        {
            var map = Verse.Find.CurrentMap;
            if (map == null) { TTS.Say(Verse.Translator.Translate("ISA_NoValidTarget")); return; }
            
            ItemSpawnerAccess.MenuHelper.SelectTargetCell(map, (Verse.IntVec3 targetCell) =>
            {
                Verse.IntVec3 cell = targetCell;
                if (!cell.IsValid || !cell.InBounds(map)) return;

                try
                {
                    if (_pawnKind != null)
                    {
                        for (int i = 0; i < qty; i++)
                        {
                            var req = new Verse.PawnGenerationRequest(_pawnKind, RimWorld.Faction.OfPlayer);
                            Verse.Pawn pawn = Verse.PawnGenerator.GeneratePawn(req);
                            Verse.IntVec3 spawnCell = Verse.CellFinder.StandableCellNear(cell, map, 5f);
                            if (!spawnCell.IsValid || !spawnCell.InBounds(map)) spawnCell = cell;
                            Verse.GenSpawn.Spawn(pawn, spawnCell, map);
                        }
                    }
                    else if (_itemDef != null)
                    {
                        int stack = _itemDef.stackLimit > 0 ? _itemDef.stackLimit : 1;
                        int rem   = qty;
                        while (rem > 0)
                        {
                            var t = Verse.ThingMaker.MakeThing(_itemDef, _stuffDef);
                            t.stackCount = UnityEngine.Mathf.Min(rem, stack);
                            
                            if (_itemDef.Minifiable)
                            {
                                t = t.MakeMinified();
                            }

                            if (!Verse.GenPlace.TryPlaceThing(t, cell, map, Verse.ThingPlaceMode.Near, out _))
                            {
                                Verse.GenSpawn.Spawn(t, cell, map);
                            }
                            rem -= t.stackCount;
                        }
                    }

                    string name = _itemDef != null
                        ? (_itemDef.label ?? _itemDef.defName)
                        : (_pawnKind?.label ?? _pawnKind?.defName ?? "?");
                    string msg = qty + "x " + name + " " + Verse.Translator.Translate("ISA_SpawnedSuffix");
                    Verse.Messages.Message(msg, RimWorld.MessageTypeDefOf.PositiveEvent, false);
                    TTS.Say(msg);
                }
                catch (System.Exception ex)
                {
                    Verse.Log.Error("ItemSpawnerAccess Spawn Error: " + ex);
                    TTS.Say("Spawn Error");
                }
            });
        }

    }

    // ─────────────────────────────────────────────────────────
    //  Dialog: Pawn-Alter setzen
    // ─────────────────────────────────────────────────────────
    public class Dialog_SetAge : Window
    {
        private readonly Pawn _pawn;
        private string _buffer = "25";

        public override Vector2 InitialSize => new Vector2(320f, 150f);

        public Dialog_SetAge(Pawn pawn) : base(null)
        {
            _pawn = pawn;
            doCloseX  = true;
            forcePause = true;
            closeOnClickedOutside = true;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            TTS.Say("ISA_SetAge".Translate() + ": " + _pawn.LabelShort);
        }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 28f), "ISA_SetAge".Translate() + " " + _pawn.LabelShort + ":");
            _buffer = Widgets.TextField(new Rect(0, 34f, inRect.width, 30f), _buffer);

            if (Widgets.ButtonText(new Rect(0, 74f, inRect.width, 34f), "ISA_ConfirmAge".Translate()))
                TrySetAge();

            if (Event.current.type == EventType.KeyDown &&
                (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
            {
                TrySetAge();
                Event.current.Use();
            }
        }

        private void TrySetAge()
        {
            if (!int.TryParse(_buffer, out int age) || age < 0)
            {
                TTS.Say("ISA_InvalidQuantity".Translate());
                return;
            }
            long ticks = (long)age * 3600000L;
            _pawn.ageTracker.AgeBiologicalTicks = ticks;
            _pawn.ageTracker.AgeChronologicalTicks = ticks;
            string msg = "ISA_AgeSet".Translate() + " " + _pawn.LabelShort + " " + age;
            Messages.Message(msg, MessageTypeDefOf.PositiveEvent, false);
            TTS.Say(msg);
            Close();
        }
    }
}
