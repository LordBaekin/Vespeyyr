// Assets/Scripts/Bridges/CharacterWindowBridge.cs

using System.Collections.Generic;
using UnityEngine;
using DevionGames.CharacterSystem;
using Vespeyr.Network; // Or your correct DTO/bridge namespace
using Assets.Scripts.Data;

public class CharacterWindowBridge : MonoBehaviour
{
    [Header("References")]
    public CharacterWindow characterWindow; // Assign in inspector

    [Tooltip("Optional: Prefab mapping by class/job name if needed.")]
    public List<CharacterPrefabMapping> prefabMappings; // See struct below

    /// <summary>
    /// Fetch characters from API and populate Devion Games UI.
    /// </summary>
    public async void PopulateFromServer(string worldKey)
    {
        try
        {
            List<CharacterDTO> apiCharacters = await DVGApiBridge.GetCharacters(worldKey);
            PopulateCharacterList(apiCharacters);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterWindowBridge] Failed to fetch or populate characters: {ex}");
            characterWindow?.Clear();
        }
    }

    /// <summary>
    /// Converts API DTOs into Devion Games Character objects and populates the CharacterWindow.
    /// </summary>
    public void PopulateCharacterList(List<CharacterDTO> apiCharacters)
    {
        if (characterWindow == null)
        {
            Debug.LogError("[CharacterWindowBridge] CharacterWindow reference not set!");
            return;
        }

        characterWindow.Clear();
        if (apiCharacters == null)
            return;

        List<Character> devionCharacters = new List<Character>();

        foreach (var dto in apiCharacters)
        {
            Character newCharacter = ScriptableObject.CreateInstance<Character>();

            // Set base fields
            newCharacter.CharacterName = dto.CharacterName;
            newCharacter.Name = !string.IsNullOrEmpty(dto.Name) ? dto.Name : dto.CharacterName;

            // CRITICAL: Store the server's CharacterId
            if (!string.IsNullOrEmpty(dto.CharacterId))
            {
                newCharacter.SetProperty("CharacterId", dto.CharacterId); // <- ADD THIS
            }

            // Gender handling
            try
            {
                if (!string.IsNullOrEmpty(dto.Gender))
                {
                    if (System.Enum.TryParse(dto.Gender, true, out Gender genderValue))
                        newCharacter.Gender = genderValue;
                    else
                        newCharacter.Gender = Gender.Male;
                }
            }
            catch { newCharacter.Gender = Gender.Male; }

            // Description
            if (!string.IsNullOrEmpty(dto.Description))
                newCharacter.Description = dto.Description;

            // Prefab assignment - your existing logic is correct
            GameObject prefab = null;
            if (prefabMappings != null && prefabMappings.Count > 0 && !string.IsNullOrEmpty(dto.Class))
            {
                var match = prefabMappings.Find(p => p.className == dto.Class);
                if (match != null)
                    prefab = match.prefab;
            }
            newCharacter.Prefab = prefab;

            // Set custom properties - your existing logic is correct
            if (!string.IsNullOrEmpty(dto.Class))
                newCharacter.SetProperty("Class", dto.Class);
            if (dto.Level > 0)
                newCharacter.SetProperty("Level", dto.Level);
            if (dto.Experience > 0)
                newCharacter.SetProperty("Experience", dto.Experience);
            if (!string.IsNullOrEmpty(dto.Faction))
                newCharacter.SetProperty("Faction", dto.Faction);

            if (dto.Attributes != null)
                newCharacter.SetProperty("Attributes", dto.Attributes);

            devionCharacters.Add(newCharacter);
        }

        characterWindow.Add(devionCharacters.ToArray());
    }
}

/// <summary>
/// Maps a class/job name to a prefab.
/// </summary>
[System.Serializable]
public class CharacterPrefabMapping
{
    public string className;
    public GameObject prefab;
}
