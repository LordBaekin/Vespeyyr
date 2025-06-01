using System.Threading.Tasks;
using UnityEngine;
using Assets.Scripts.Data;
using Vespeyr.Network;
using DevionGames.CharacterSystem;
using TagDebugSystem;

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
    private const string TAG = "CharacterServer";

    /// <summary>
    /// Create a character on the server and return the server-generated ID
    /// </summary>
    public static async Task<string> CreateCharacterOnServer(string characterName, string className, string genderString)
    {
        try
        {
            string worldKey = PlayerPrefs.GetString("selected_server", "");
            string authToken = PlayerPrefs.GetString("jwt_token", "");

            if (string.IsNullOrEmpty(authToken))
            {
                TD.Error(TAG, "No auth token found! Cannot create character on server.");
                return null;
            }
            if (string.IsNullOrEmpty(worldKey))
            {
                TD.Error(TAG, "No world key found! Cannot create character on server.");
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

            TD.Info(TAG, $"Creating character '{characterName}' ({className}) on server in world '{worldKey}'...");

            // Call server API
            string response = await DVGApiBridge.SaveCharacter(worldKey, characterDTO);

            if (string.IsNullOrEmpty(response))
            {
                TD.Error(TAG, "Server returned empty response!");
                return null;
            }

            // Parse server response
            var serverResponse = JsonUtility.FromJson<SaveCharacterResponse>(response);
            if (!string.IsNullOrEmpty(serverResponse.characterId))
            {
                TD.Info(TAG, $"Character created successfully with server ID: {serverResponse.characterId}");
                return serverResponse.characterId;
            }
            else
            {
                TD.Error(TAG, $"Server response missing characterId: {response}");
                return null;
            }
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Server character creation failed: {ex.Message}");
            return null;
        }
    }
}
