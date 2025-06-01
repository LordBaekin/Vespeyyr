using System.Collections.Generic;
using UnityEngine;
using DevionGames.CharacterSystem;
using Vespeyr.Network;
using Assets.Scripts.Data;
using TagDebugSystem;

public class CharacterWindowBridge : MonoBehaviour
{
    [Header("References")]
    public CharacterWindow characterWindow;

    [Tooltip("Optional: Prefab mapping by class/job name if needed.")]
    public List<CharacterPrefabMapping> prefabMappings;

    /// <summary>
    /// Fetch characters from API and populate Devion Games UI.
    /// </summary>
    public async void PopulateFromServer(string worldKey)
    {
        TD.Info(Tags.CharacterSystem, $"[PopulateFromServer] worldKey={worldKey}");

        try
        {
            List<CharacterDTO> apiCharacters = await DVGApiBridge.GetCharacters(worldKey);
            TD.Info(Tags.CharacterSystem, $"[PopulateFromServer] Received {apiCharacters?.Count ?? 0} characters from API.");
            PopulateCharacterList(apiCharacters);
        }
        catch (System.Exception ex)
        {
            TD.Error(Tags.CharacterSystem, $"[PopulateFromServer] Exception: {ex.Message}\n{ex.StackTrace}");
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
            TD.Error(Tags.CharacterSystem, "[PopulateCharacterList] CharacterWindow reference not set!");
            return;
        }

        characterWindow.Clear();
        if (apiCharacters == null)
        {
            TD.Warning(Tags.CharacterSystem, "[PopulateCharacterList] apiCharacters was null.");
            return;
        }

        List<Character> devionCharacters = new List<Character>();

        foreach (var dto in apiCharacters)
        {
            Character newCharacter = ScriptableObject.CreateInstance<Character>();
            newCharacter.CharacterName = dto.CharacterName;
            newCharacter.Name = !string.IsNullOrEmpty(dto.Name) ? dto.Name : dto.CharacterName;

            if (!string.IsNullOrEmpty(dto.CharacterId))
            {
                newCharacter.SetProperty("CharacterId", dto.CharacterId);
            }

            try
            {
                if (!string.IsNullOrEmpty(dto.Gender) && System.Enum.TryParse(dto.Gender, true, out Gender genderValue))
                {
                    newCharacter.Gender = genderValue;
                }
                else
                {
                    newCharacter.Gender = Gender.Male;
                }
            }
            catch
            {
                newCharacter.Gender = Gender.Male;
            }

            if (!string.IsNullOrEmpty(dto.Description))
            {
                newCharacter.Description = dto.Description;
            }

            GameObject prefab = null;
            if (prefabMappings != null && prefabMappings.Count > 0 && !string.IsNullOrEmpty(dto.Class))
            {
                var match = prefabMappings.Find(p => p.className == dto.Class);
                if (match != null)
                    prefab = match.prefab;
            }
            newCharacter.Prefab = prefab;

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

            TD.Info(Tags.CharacterSystem, $"Added character: {newCharacter.CharacterName} (Class={dto.Class}, Level={dto.Level})");
            devionCharacters.Add(newCharacter);
        }

        characterWindow.Add(devionCharacters.ToArray());
        TD.Info(Tags.CharacterSystem, $"[PopulateCharacterList] {devionCharacters.Count} characters populated into CharacterWindow.");
    }
}

[System.Serializable]
public class CharacterPrefabMapping
{
    public string className;
    public GameObject prefab;
}
