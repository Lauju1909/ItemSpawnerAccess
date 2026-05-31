using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    // Mocks for Harmony and RimWorld
    public class HarmonyPatch : Attribute { public HarmonyPatch(Type t, string s) { } }
    public class Prefix : Attribute { }
    
    public static class Tolk
    {
        public static void Speak(string text) { Console.WriteLine("Tolk: " + text); }
    }

    public class IntVec3
    {
        public int x, y, z;
        public IntVec3(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
    }

    public class TargetingParameters { }

    public class Targeter
    {
        public bool IsTargeting { get; set; }
        public Action<IntVec3> action;
        public void BeginTargeting(TargetingParameters targetParams, Action<IntVec3> action) 
        {
            this.IsTargeting = true;
            this.action = action;
        }
    }

    public static class Find
    {
        public static Targeter Targeter = new Targeter();
    }

    public static class MapGrid
    {
        public static string GetCellContent(IntVec3 pos)
        {
            return "Gras und Muffalo"; // Mock implementation
        }
    }

    [HarmonyPatch(typeof(Targeter), "BeginTargeting")]
    public static class MapTargetingPatcher
    {
        public static bool IsAccessibleTargeting = false;
        public static IntVec3 CurrentVirtualCursor = new IntVec3(10, 0, 10);
        public static Action<IntVec3> CurrentAction;

        [Prefix]
        public static bool Prefix(TargetingParameters targetParams, Action<IntVec3> action)
        {
            IsAccessibleTargeting = true;
            CurrentAction = action;
            Tolk.Speak("Map-Targeting aktiviert. Benutze die Pfeiltasten, um den Cursor zu bewegen, und Enter, um das Ziel auszuwählen.");
            AnnounceCurrentCell();
            return false; // Skip original method
        }

        public static void HandleInput(ConsoleKey key)
        {
            if (!IsAccessibleTargeting) return;

            if (key == ConsoleKey.UpArrow) { CurrentVirtualCursor.z++; AnnounceCurrentCell(); }
            else if (key == ConsoleKey.DownArrow) { CurrentVirtualCursor.z--; AnnounceCurrentCell(); }
            else if (key == ConsoleKey.LeftArrow) { CurrentVirtualCursor.x--; AnnounceCurrentCell(); }
            else if (key == ConsoleKey.RightArrow) { CurrentVirtualCursor.x++; AnnounceCurrentCell(); }
            else if (key == ConsoleKey.Enter)
            {
                Tolk.Speak("Ziel ausgewählt. Aktion wird ausgeführt.");
                IsAccessibleTargeting = false;
                CurrentAction?.Invoke(CurrentVirtualCursor);
            }
            else if (key == ConsoleKey.Escape)
            {
                Tolk.Speak("Targeting abgebrochen.");
                IsAccessibleTargeting = false;
            }
        }

        private static void AnnounceCurrentCell()
        {
            string content = MapGrid.GetCellContent(CurrentVirtualCursor);
            Tolk.Speak($"Kachel {CurrentVirtualCursor.x}, {CurrentVirtualCursor.z}. Inhalt: {content}");
        }
    }
}
