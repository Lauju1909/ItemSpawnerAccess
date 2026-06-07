using System;
using System.Collections.Generic;

namespace RimWorldAccess_UniversalPatcher
{
    // Mocks
    public class Pawn
    {
        public string Name { get; set; }
        public List<string> Abilities { get; set; } = new List<string>();
    }

    public static class PawnAbilityMutator
    {
        public static void AddAbility(Pawn pawn, string abilityName)
        {
            if (!pawn.Abilities.Contains(abilityName))
            {
                pawn.Abilities.Add(abilityName);
                Tolk.Speak($"Fähigkeit {abilityName} zu {pawn.Name} hinzugefügt.");
            }
            else
            {
                Tolk.Speak($"{pawn.Name} hat diese Fähigkeit bereits.");
            }
        }

        public static void RemoveAbility(Pawn pawn, string abilityName)
        {
            if (pawn.Abilities.Contains(abilityName))
            {
                pawn.Abilities.Remove(abilityName);
                Tolk.Speak($"Fähigkeit {abilityName} von {pawn.Name} entfernt.");
            }
            else
            {
                Tolk.Speak($"{pawn.Name} besitzt diese Fähigkeit nicht.");
            }
        }

        public static void ListAbilities(Pawn pawn)
        {
            if (pawn.Abilities.Count == 0)
            {
                Tolk.Speak($"{pawn.Name} hat keine Fähigkeiten.");
            }
            else
            {
                string abilitiesList = string.Join(", ", pawn.Abilities);
                Tolk.Speak($"{pawn.Name} hat folgende Fähigkeiten: {abilitiesList}.");
            }
        }
    }
}
