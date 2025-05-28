using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DevionGames.CharacterSystem;
using DevionGames;
using Assets.Scripts.Data;

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

    private void Awake()
    {
        if (enableServerIntercept)
        {
            // Register for character system events
            EventHandler.Register("OnCharacterManagerLoadCharacters", InterceptCharacterLoad);
            EventHandler.Register<Character>("OnCharacterManagerCreateCharacter", InterceptCharacterCreate);
            EventHandler.Register<Character>("OnCharacterManagerDeleteCharacter", InterceptCharacterDelete);

            Debug.Log("[CharacterServerInterceptor] Registered for character events");
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

    /// <summary>
    /// Intercept character loading and handle server operations
    /// </summary>
    private async void InterceptCharacterLoad()
    {
        if (isLoadingFromServer) return; // Prevent recursion

        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider == SaveProviderSelectorSO.SaveProvider.Server)
        {
            // Server only mode - load from server
            Debug.Log("[CharacterServerInterceptor] Server-only mode: loading from server");

            isLoadingFromServer = true;
            try
            {
                await LoadCharactersFromServer();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterServerInterceptor] Server load failed: {ex.Message}");
            }
            finally
            {
                isLoadingFromServer = false;
            }
        }
        else if (provider == SaveProviderSelectorSO.SaveProvider.Both)
        {
            // Both mode - try server first, then let normal flow handle local
            Debug.Log("[CharacterServerInterceptor] Both mode: trying server first");

            isLoadingFromServer = true;
            try
            {
                bool serverSuccess = await LoadCharactersFromServer();

                if (!serverSuccess)
                {
                    Debug.Log("[CharacterServerInterceptor] Server load failed, allowing normal local load to proceed");
                    // Let the normal CharacterManager.LoadCharacters continue with local loading
                    CallNormalLoadCharacters();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterServerInterceptor] Server load failed: {ex.Message}");
                // Let the normal CharacterManager.LoadCharacters continue with local loading
                CallNormalLoadCharacters();
            }
            finally
            {
                isLoadingFromServer = false;
            }
        }
        else
        {
            // PlayerPrefs only mode - let normal flow proceed
            Debug.Log("[CharacterServerInterceptor] PlayerPrefs-only mode: delegating to normal load");
            CallNormalLoadCharacters();
        }
    }

    /// <summary>
    /// Call the normal CharacterManager local loading process
    /// </summary>
    private void CallNormalLoadCharacters()
    {
        DevionGamesAdapter.LoadCharacterData((data) => {
            if (string.IsNullOrEmpty(data))
            {
                Debug.Log("[CharacterServerInterceptor] No local character data found");
                return;
            }

            // Fire the normal processing event that CharacterManager would handle
            EventHandler.Execute("OnCharacterManagerProcessData", data);
        });
    }

    /// <summary>
    /// Intercept character creation and save to server
    /// </summary>
    private async void InterceptCharacterCreate(Character character)
    {
        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            Debug.Log($"[CharacterServerInterceptor] Saving created character to server: {character.CharacterName}");

            try
            {
                string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
                if (!string.IsNullOrEmpty(worldKey))
                {
                    var dto = ConvertToDTO(character);
                    await DVGApiBridge.SaveCharacter(worldKey, dto);
                    Debug.Log($"[CharacterServerInterceptor] Character {character.CharacterName} saved to server successfully");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterServerInterceptor] Failed to save character to server: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Intercept character deletion and remove from server
    /// </summary>
    private async void InterceptCharacterDelete(Character character)
    {
        var provider = DevionGamesAdapter.GetCurrentProvider();

        if (provider != SaveProviderSelectorSO.SaveProvider.PlayerPrefs)
        {
            Debug.Log($"[CharacterServerInterceptor] Deleting character from server: {character.CharacterName}");

            try
            {
                string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
                string characterId = character.FindProperty("CharacterId")?.stringValue ?? character.CharacterName;

                if (!string.IsNullOrEmpty(worldKey) && !string.IsNullOrEmpty(characterId))
                {
                    bool success = await DVGApiBridge.DeleteCharacter(worldKey, characterId);
                    if (success)
                    {
                        Debug.Log($"[CharacterServerInterceptor] Character {characterId} deleted from server successfully");
                    }
                    else
                    {
                        Debug.LogWarning($"[CharacterServerInterceptor] Failed to delete character {characterId} from server");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CharacterServerInterceptor] Failed to delete character from server: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Load characters from server and inject into DevionGames system
    /// </summary>
    private async System.Threading.Tasks.Task<bool> LoadCharactersFromServer()
    {
        string worldKey = ServerWorldEvents.CurrentWorldKey ?? PlayerPrefs.GetString("selected_server", "");
        if (string.IsNullOrEmpty(worldKey))
        {
            Debug.LogWarning("[CharacterServerInterceptor] No world key available for server load");
            return false;
        }

        try
        {
            var serverCharacters = await DVGApiBridge.GetCharacters(worldKey);

            if (serverCharacters != null && serverCharacters.Count > 0)
            {
                Debug.Log($"[CharacterServerInterceptor] Loaded {serverCharacters.Count} characters from server");

                // Convert server DTOs to Character objects and inject into DevionGames system
                foreach (var dto in serverCharacters)
                {
                    var character = ConvertFromDTO(dto);

                    if (character != null)
                    {
                        // Fire the character loaded event to populate the UI
                        EventHandler.Execute("OnCharacterLoaded", character);
                    }
                    else
                    {
                        Debug.LogError($"[CharacterServerInterceptor] Failed to convert character: {dto.CharacterName}");
                    }
                }
                return true;
            }
            else
            {
                Debug.Log("[CharacterServerInterceptor] No characters found on server");
                return false;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterServerInterceptor] Server load exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Convert server DTO to a live Character instance by cloning the DB template,
    /// then applying all server fields (name, id, level, xp, gender).
    /// </summary>
    private Character ConvertFromDTO(CharacterDTO dto)
    {
        // 1) Debug header
        if (debugCharacterConversion)
        {
            Debug.Log($"[CharacterServerInterceptor] Converting character: {dto.CharacterName} " +
                      $"(ClassKey: {dto.Name ?? dto.Class ?? dto.PrefabName}, Gender: {dto.Gender})");
        }

        // 2) Figure out which key to use for template lookup
        string classKey =
            !string.IsNullOrEmpty(dto.Name) ? dto.Name :
            !string.IsNullOrEmpty(dto.Class) ? dto.Class :
            !string.IsNullOrEmpty(dto.PrefabName) ? dto.PrefabName :
            "Unknown";

        // 3) Parse gender
        DevionGames.CharacterSystem.Gender genderValue = DevionGames.CharacterSystem.Gender.Male;
        if (!string.IsNullOrEmpty(dto.Gender) &&
            !System.Enum.TryParse(dto.Gender, true, out genderValue))
        {
            Debug.LogWarning($"[CharacterServerInterceptor] Invalid gender '{dto.Gender}' for {dto.CharacterName}, defaulting to Male");
            genderValue = DevionGames.CharacterSystem.Gender.Male;
        }

        // 4) Attempt to find an exact template in your DB
        Character template = FindBestMatchingTemplate(classKey, genderValue);
        Character character;

        if (template != null)
        {
            // Clone the template so we carry over its SO.name, Prefab, portrait, description, etc.
            character = ScriptableObject.Instantiate(template);
            if (debugCharacterConversion)
            {
                Debug.Log($"[CharacterServerInterceptor] Instantiated template for {classKey}/{genderValue}: {template.name}");
            }
        }
        else
        {
            Debug.LogWarning($"[CharacterServerInterceptor] No template for {classKey}/{genderValue}. Trying gender fallback.");

            // Try any template of same gender
            var fallback = CharacterManager.Database.items
                                         .FirstOrDefault(x => x.Gender == genderValue);
            if (fallback != null)
            {
                character = ScriptableObject.Instantiate(fallback);
                Debug.Log($"[CharacterServerInterceptor] Using fallback template from {fallback.name} for {dto.CharacterName}");
            }
            else
            {
                // As a last resort, create a blank Character SO
                character = ScriptableObject.CreateInstance<Character>();
                Debug.LogError($"[CharacterServerInterceptor] No fallback template found for gender {genderValue}. Created blank Character SO.");
            }
        }

        // 5) Now apply all server‐side fields onto our new instance

        // Display name
        character.CharacterName = dto.CharacterName;

        // Server ID
        character.SetProperty("CharacterId", dto.CharacterId);

        // Level & XP
        if (dto.Level > 0)
        {
            character.SetProperty("Level", dto.Level);
        }
        if (dto.Experience > 0)
        {
            character.SetProperty("Experience", dto.Experience);
        }

        // Overwrite Gender on the instance (in case template had something else)
        character.Gender = genderValue;

        // 6) Sanity check
        if (character.Prefab == null)
        {
            Debug.LogError($"[CharacterServerInterceptor] Character {dto.CharacterName} has no Prefab! UI instantiation will fail.");
        }

        if (debugCharacterConversion)
        {
            Debug.Log($"[CharacterServerInterceptor] Conversion complete: " +
                      $"{character.CharacterName} | Prefab: {(character.Prefab != null ? character.Prefab.name : "NULL")} | " +
                      $"Image: {(character.CreateCharacterImage != null ? "Present" : "NULL")}");
        }

        return character;
    }


    /// <summary>
    /// Find the best matching template for a character class and gender
    /// </summary>
    private Character FindBestMatchingTemplate(string characterClass, DevionGames.CharacterSystem.Gender gender)
    {
        if (CharacterManager.Database == null || CharacterManager.Database.items == null)
        {
            Debug.LogError("[CharacterServerInterceptor] CharacterManager.Database is null!");
            return null;
        }

        // Try exact match first
        var exactMatch = CharacterManager.Database.items.FirstOrDefault(x =>
            x.Name == characterClass && x.Gender == gender);

        if (exactMatch != null)
        {
            return exactMatch;
        }

        // Try case-insensitive match
        var caseInsensitiveMatch = CharacterManager.Database.items.FirstOrDefault(x =>
            string.Equals(x.Name, characterClass, System.StringComparison.OrdinalIgnoreCase) && x.Gender == gender);

        if (caseInsensitiveMatch != null)
        {
            return caseInsensitiveMatch;
        }

        // Try partial match (contains)
        var partialMatch = CharacterManager.Database.items.FirstOrDefault(x =>
            x.Name.Contains(characterClass) && x.Gender == gender);

        if (partialMatch != null)
        {
            return partialMatch;
        }

        // Try reverse partial match
        var reversePartialMatch = CharacterManager.Database.items.FirstOrDefault(x =>
            characterClass.Contains(x.Name) && x.Gender == gender);

        if (reversePartialMatch != null)
        {
            return reversePartialMatch;
        }

        // Log available templates for debugging
        if (debugCharacterConversion)
        {
            Debug.Log($"[CharacterServerInterceptor] Available templates for debugging:");
            foreach (var item in CharacterManager.Database.items)
            {
                Debug.Log($"  - {item.Name} ({item.Gender})");
            }
        }

        return null;
    }

    /// <summary>
    /// Convert Character to DTO for server
    /// </summary>
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