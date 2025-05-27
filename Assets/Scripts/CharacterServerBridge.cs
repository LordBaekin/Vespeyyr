using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Data;
using Vespeyr.Network;
using DevionGames.CharacterSystem;

/// <summary>
/// Response from server when saving a character
/// </summary>
[System.Serializable]
public class SaveCharacterResponse
{
    public string characterId;  // lowercase to match server response
    public string message;
}

/// <summary>
/// Bridge between DevionGames Character System and our server API
/// This file is in Assembly-CSharp so it can access both DTOs and server APIs
/// </summary>
public static class CharacterServerBridge
{
    /// <summary>
    /// Create a character on the server and return the server-generated ID
    /// </summary>
    /// <param name="characterName">Player-entered character name</param>
    /// <param name="className">Character class/profession</param>
    /// <param name="genderString">Character gender as string</param>
    /// <returns>Server-generated character ID (GUID string) or null if failed</returns>
    public static async Task<string> CreateCharacterOnServer(string characterName, string className, string genderString)
    {
        try
        {
            string worldKey = PlayerPrefs.GetString("selected_server", "");
            string authToken = PlayerPrefs.GetString("jwt_token", "");

            if (string.IsNullOrEmpty(authToken))
            {
                Debug.LogError("[CharacterServerBridge] No auth token found! Cannot create character on server.");
                return null;
            }
            if (string.IsNullOrEmpty(worldKey))
            {
                Debug.LogError("[CharacterServerBridge] No world key found! Cannot create character on server.");
                return null;
            }

            // Create CharacterDTO for server
            var characterDTO = new CharacterDTO
            {
                CharacterName = characterName,
                Name = className, // This is the class/profession  
                Gender = genderString,
                Level = 1,
                // Add other default properties as needed
            };

            Debug.Log($"[CharacterServerBridge] Creating character '{characterName}' ({className}) on server in world '{worldKey}'...");

            // Call server API
            string response = await DVGApiBridge.SaveCharacter(worldKey, characterDTO);

            if (string.IsNullOrEmpty(response))
            {
                Debug.LogError("[CharacterServerBridge] Server returned empty response!");
                return null;
            }

            // Parse server response using the correct response class
            var serverResponse = JsonUtility.FromJson<SaveCharacterResponse>(response);
            if (!string.IsNullOrEmpty(serverResponse.characterId))
            {
                Debug.Log($"[CharacterServerBridge] Character created successfully with server ID: {serverResponse.characterId}");
                return serverResponse.characterId;
            }
            else
            {
                Debug.LogError($"[CharacterServerBridge] Server response missing characterId: {response}");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CharacterServerBridge] Server character creation failed: {ex.Message}");
            return null;
        }
    }
}