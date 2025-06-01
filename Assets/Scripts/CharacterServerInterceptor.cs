using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DevionGames.CharacterSystem;
using DevionGames;
using Assets.Scripts.Data;
using TagDebugSystem;

/// <summary>
/// Intercepts character operations and handles server persistence
/// Place this component in your scene to enable server character operations
/// This file goes in your main Scripts folder (Assembly-CSharp)
/// </summary>
public class CharacterServerInterceptor : MonoBehaviour
{
    [Header("Server Integration")]
    [Tooltip("Enable to intercept character operations for server persistence")]
    public bool enableServerIntercept = true;

    [Header("Debug")]
    [Tooltip("Enable detailed logging for character conversion issues")]
    public bool debugCharacterConversion = true;

    private bool isLoadingFromServer = false;
    private const string TAG = "CharacterServer";

    private void Awake()
    {
        if (enableServerIntercept)
        {
            EventHandler.Register("OnCharacterManagerLoadCharacters", InterceptCharacterLoad);
            EventHandler.Register<Character>("OnCharacterManagerCreateCharacter", InterceptCharacterCreate);
            EventHandler.Register<Character>("OnCharacterManagerDeleteCharacter", InterceptCharacterDelete);

            TD.Info(TAG, "Registered for character events", this);
        }
    }

    private void OnDestroy()
    {
        if (enableServerIntercept)
        {
            EventHandler.Unregister("OnCharacterManagerLoadCharacters", InterceptCharacterLoad);
            EventHandler.Unregister<Character>("OnCharacterManagerCreateCharacter", InterceptCharacterCreate);
            EventHandler.Unregister<Character>("OnCharacterManagerDeleteCharacter", InterceptCharacterDelete);
        }
    }

    private async void InterceptCharacterLoad()
    {
        if (isLoadingFromServer) return;

        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            TD.Info(TAG, "Server-only mode: loading from server", this);

            isLoadingFromServer = true;
            try
            {
                await LoadCharactersFromServer();
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Server load failed: {ex.Message}", this);
            }
            finally
            {
                isLoadingFromServer = false;
            }
        }
        else if (provider == SaveProviderSelectorSO.SaveProvider.Both)
        {
            TD.Info(TAG, "Both mode: trying server first", this);

            isLoadingFromServer = true;
            try
            {
                bool serverSuccess = await LoadCharactersFromServer();

                if (!serverSuccess)
                {
                    TD.Warning(TAG, "Server load failed, allowing normal local load to proceed", this);
                    CallNormalLoadCharacters();
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Server load failed: {ex.Message}", this);
                CallNormalLoadCharacters();
            }
            finally
            {
                isLoadingFromServer = false;
            }
        }
        else
        {
            TD.Info(TAG, "PlayerPrefs-only mode: delegating to normal load", this);
            CallNormalLoadCharacters();
        }
    }

    private void CallNormalLoadCharacters()
    {
        DevionGamesAdapter.LoadCharacterData((data) => {
            if (string.IsNullOrEmpty(data))
            {
                TD.Info(TAG, "No local character data found", this);
                return;
            }
            EventHandler.Execute("OnCharacterManagerProcessData", data);
        });
    }

    private async void InterceptCharacterCreate(Character character)
    {
        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            TD.Info(TAG, $"Saving created character to server: {character.CharacterName}", this);

            try
            {
                string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
                if (!string.IsNullOrEmpty(worldKey))
                {
                    var dto = ConvertToDTO(character);
                    await DVGApiBridge.SaveCharacter(worldKey, dto);
                    TD.Info(TAG, $"Character {character.CharacterName} saved to server successfully", this);
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Failed to save character to server: {ex.Message}", this);
            }
        }
    }

    private async void InterceptCharacterDelete(Character character)
    {
        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            TD.Info(TAG, $"Deleting character from server: {character.CharacterName}", this);

            try
            {
                string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
                string characterId = character.FindProperty("CharacterId")?.stringValue ?? character.CharacterName;

                if (!string.IsNullOrEmpty(worldKey) && !string.IsNullOrEmpty(characterId))
                {
                    bool success = await DVGApiBridge.DeleteCharacter(worldKey, characterId);
                    if (success)
                    {
                        TD.Info(TAG, $"Character {characterId} deleted from server successfully", this);
                    }
                    else
                    {
                        TD.Warning(TAG, $"Failed to delete character {characterId} from server", this);
                    }
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"Failed to delete character from server: {ex.Message}", this);
            }
        }
    }

    private async System.Threading.Tasks.Task<bool> LoadCharactersFromServer()
    {
        string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
        if (string.IsNullOrEmpty(worldKey))
        {
            TD.Warning(TAG, "No world key available for server load", this);
            return false;
        }

        try
        {
            var serverCharacters = await DVGApiBridge.GetCharacters(worldKey);

            if (serverCharacters != null && serverCharacters.Count > 0)
            {
                TD.Info(TAG, $"Loaded {serverCharacters.Count} characters from server", this);

                foreach (var dto in serverCharacters)
                {
                    var character = ConvertFromDTO(dto);

                    if (character != null)
                    {
                        EventHandler.Execute("OnCharacterLoaded", character);
                    }
                    else
                    {
                        TD.Error(TAG, $"Failed to convert character: {dto.CharacterName}", this);
                    }
                }
                return true;
            }
            else
            {
                TD.Info(TAG, "No characters found on server", this);
                return false;
            }
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Server load exception: {ex.Message}", this);
            return false;
        }
    }

    private Character ConvertFromDTO(CharacterDTO dto)
    {
        if (debugCharacterConversion)
        {
            TD.Info(TAG, $"Converting character: {dto.CharacterName} (ClassKey: {dto.Name ?? dto.Class ?? dto.PrefabName}, Gender: {dto.Gender})", this);
        }

        string classKey =
            !string.IsNullOrEmpty(dto.Name) ? dto.Name :
            !string.IsNullOrEmpty(dto.Class) ? dto.Class :
            !string.IsNullOrEmpty(dto.PrefabName) ? dto.PrefabName :
            "Unknown";

        DevionGames.CharacterSystem.Gender genderValue = DevionGames.CharacterSystem.Gender.Male;
        if (!string.IsNullOrEmpty(dto.Gender) && !System.Enum.TryParse(dto.Gender, true, out genderValue))
        {
            TD.Warning(TAG, $"Invalid gender '{dto.Gender}' for {dto.CharacterName}, defaulting to Male", this);
            genderValue = DevionGames.CharacterSystem.Gender.Male;
        }

        Character template = FindBestMatchingTemplate(classKey, genderValue);
        Character character;

        if (template != null)
        {
            character = ScriptableObject.Instantiate(template);
            if (debugCharacterConversion)
            {
                TD.Info(TAG, $"Instantiated template for {classKey}/{genderValue}: {template.name}", this);
            }
        }
        else
        {
            TD.Warning(TAG, $"No template for {classKey}/{genderValue}. Trying gender fallback.", this);

            var fallback = CharacterManager.Database.items.FirstOrDefault(x => x.Gender == genderValue);
            if (fallback != null)
            {
                character = ScriptableObject.Instantiate(fallback);
                TD.Info(TAG, $"Using fallback template from {fallback.name} for {dto.CharacterName}", this);
            }
            else
            {
                character = ScriptableObject.CreateInstance<Character>();
                TD.Error(TAG, $"No fallback template found for gender {genderValue}. Created blank Character SO.", this);
            }
        }

        character.CharacterName = dto.CharacterName;
        character.SetProperty("CharacterId", dto.CharacterId);
        if (dto.Level != character.FindProperty("Level")?.floatValue)
            character.SetProperty("Level", dto.Level);
        character.SetProperty("Experience", dto.Experience);
        character.Gender = genderValue;

        if (character.Prefab == null)
        {
            TD.Error(TAG, $"Character {dto.CharacterName} has no Prefab! UI instantiation will fail.", this);
        }

        if (debugCharacterConversion)
        {
            TD.Info(TAG, $"Conversion complete: {character.CharacterName} | Prefab: {(character.Prefab ? character.Prefab.name : "NULL")} | Image: {(character.CreateCharacterImage ? "Present" : "NULL")}", this);
        }

        return character;
    }

    private Character FindBestMatchingTemplate(string characterClass, DevionGames.CharacterSystem.Gender gender)
    {
        if (CharacterManager.Database == null || CharacterManager.Database.items == null)
        {
            TD.Error(TAG, "CharacterManager.Database is null!", this);
            return null;
        }

        var exactMatch = CharacterManager.Database.items.FirstOrDefault(x => x.Name == characterClass && x.Gender == gender);
        if (exactMatch != null) return exactMatch;

        var ciMatch = CharacterManager.Database.items.FirstOrDefault(x => string.Equals(x.Name, characterClass, System.StringComparison.OrdinalIgnoreCase) && x.Gender == gender);
        if (ciMatch != null) return ciMatch;

        var partialMatch = CharacterManager.Database.items.FirstOrDefault(x => x.Name.Contains(characterClass) && x.Gender == gender);
        if (partialMatch != null) return partialMatch;

        var reversePartial = CharacterManager.Database.items.FirstOrDefault(x => characterClass.Contains(x.Name) && x.Gender == gender);
        if (reversePartial != null) return reversePartial;

        if (debugCharacterConversion)
        {
            TD.Info(TAG, "Available templates for debugging:", this);
            foreach (var item in CharacterManager.Database.items)
            {
                TD.Info(TAG, $"  - {item.Name} ({item.Gender})", this);
            }
        }

        return null;
    }

    private CharacterDTO ConvertToDTO(Character character)
    {
        return new CharacterDTO
        {
            CharacterId = character.FindProperty("CharacterId")?.stringValue ?? character.CharacterName,
            CharacterName = character.CharacterName,
            Name = character.Name,
            Gender = character.Gender.ToString(),
            Level = (int)(character.FindProperty("Level")?.floatValue ?? 1),
            Experience = (int)(character.FindProperty("Experience")?.floatValue ?? 0),
            Class = character.Name
        };
    }
}
