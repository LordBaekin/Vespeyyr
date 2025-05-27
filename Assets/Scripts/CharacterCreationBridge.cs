// Create this file: Assets/Scripts/CharacterCreationBridge.cs
using UnityEngine;
using System.Collections;
using DevionGames.CharacterSystem;

/// <summary>
/// MonoBehaviour bridge that both assemblies can access
/// Place this on a GameObject in your scene
/// </summary>
public class CharacterCreationBridge : MonoBehaviour
{
    public static CharacterCreationBridge Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Create character on server, then create locally
    /// Called from DevionGames CreateCharacterWindow
    /// </summary>
    public void CreateCharacterWithServer(System.Collections.Generic.Dictionary<string, object> parameters)
    {
        string characterName = parameters["characterName"] as string;
        string className = parameters["className"] as string;
        string genderString = parameters["genderString"] as string;
        Character characterTemplate = parameters["characterTemplate"] as Character;

        Debug.Log($"[CharacterCreationBridge] Received parameters - Name: {characterName}, Class: {className}, Gender: {genderString}");

        StartCoroutine(CreateCharacterCoroutine(characterName, className, genderString, characterTemplate));
    }

    private IEnumerator CreateCharacterCoroutine(string characterName, string className, string genderString, Character characterTemplate)
    {
        Debug.Log($"[CharacterCreationBridge] Creating character '{characterName}' on server...");

        // =============================================================================
        // CHARACTER OBJECT DEBUGGING
        // =============================================================================
        Debug.Log("=== CHARACTER OBJECT ANALYSIS ===");
        Debug.Log($"Character Type: {characterTemplate.GetType().FullName}");
        Debug.Log($"Character Name: {characterTemplate.CharacterName}");

        // Check existing properties in the DevionGames property system
        Debug.Log("EXISTING DEVION GAMES PROPERTIES:");
        var existingProperties = characterTemplate.GetProperties();
        if (existingProperties != null && existingProperties.Length > 0)
        {
            foreach (var prop in existingProperties)
            {
                Debug.Log($"  Property: {prop.Name} = {prop.GetValue()}");
            }
        }
        else
        {
            Debug.Log("  No existing properties found");
        }

        // Check if CharacterId property already exists
        var existingCharacterIdProp = characterTemplate.FindProperty("CharacterId");
        if (existingCharacterIdProp != null)
        {
            Debug.Log($"  Existing CharacterId property found: {existingCharacterIdProp.stringValue}");
        }
        else
        {
            Debug.Log("  No existing CharacterId property found");
        }

        // =============================================================================
        // SERVER CALL
        // =============================================================================
        Debug.Log("=== CALLING SERVER ===");
        var serverTask = CharacterServerBridge.CreateCharacterOnServer(characterName, className, genderString);

        // Wait for the server call to complete
        while (!serverTask.IsCompleted)
        {
            yield return null;
        }

        string characterId = serverTask.Result;  // This is the characterId from server

        Debug.Log("=== SERVER RESPONSE ===");
        Debug.Log($"Server Task Status: {serverTask.Status}");
        Debug.Log($"Server Returned CharacterId: '{characterId}'");
        Debug.Log($"CharacterId is null or empty: {string.IsNullOrEmpty(characterId)}");

        if (!string.IsNullOrEmpty(characterId))
        {
            Debug.Log($"[CharacterCreationBridge] SUCCESS! Server returned characterId: {characterId}");

            // =============================================================================
            // SET CHARACTER ID USING DEVION GAMES PROPERTY SYSTEM
            // =============================================================================
            Debug.Log("=== SETTING CHARACTER ID VIA PROPERTY SYSTEM ===");

            bool characterIdWasSet = false;

            try
            {
                // Use DevionGames property system - this is what CharacterManager.StartPlayScene() expects!
                characterTemplate.SetProperty("CharacterId", characterId);
                Debug.Log($"✅ Called SetProperty('CharacterId', '{characterId}')");

                // Verify it was set correctly
                var verifyProperty = characterTemplate.FindProperty("CharacterId");
                if (verifyProperty != null)
                {
                    string retrievedValue = verifyProperty.stringValue;
                    Debug.Log($"✅ SUCCESS: CharacterId property set and verified: '{retrievedValue}'");

                    if (retrievedValue == characterId)
                    {
                        characterIdWasSet = true;
                        Debug.Log($"✅ PERFECT: Retrieved value matches server ID exactly");
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ WARNING: Retrieved value '{retrievedValue}' doesn't match server ID '{characterId}'");
                    }
                }
                else
                {
                    Debug.LogError($"❌ ERROR: FindProperty('CharacterId') returned null after SetProperty!");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"❌ FAILED to set CharacterId property: {ex.Message}");
                Debug.LogError($"Exception stack trace: {ex.StackTrace}");
            }

            // =============================================================================
            // BACKUP STORAGE
            // =============================================================================
            // Store the ID mapping as backup (in case something goes wrong)
            PlayerPrefs.SetString($"CharacterID_{characterName}", characterId);
            PlayerPrefs.Save();
            Debug.Log($"💾 Stored backup mapping: CharacterID_{characterName} = {characterId}");

            // =============================================================================
            // FINAL VERIFICATION
            // =============================================================================
            Debug.Log("=== FINAL VERIFICATION ===");
            Debug.Log($"Character ID was successfully set via property system: {characterIdWasSet}");

            // Show all properties one more time to confirm
            Debug.Log("FINAL PROPERTY STATE:");
            var finalProperties = characterTemplate.GetProperties();
            if (finalProperties != null && finalProperties.Length > 0)
            {
                foreach (var prop in finalProperties)
                {
                    Debug.Log($"  Final Property: {prop.Name} = {prop.GetValue()}");
                }
            }

            // =============================================================================
            // CREATE CHARACTER LOCALLY
            // =============================================================================
            if (characterIdWasSet)
            {
                Debug.Log("=== CREATING CHARACTER LOCALLY ===");
                CharacterManager.CreateCharacter(characterTemplate);
                Debug.Log("✅ CharacterManager.CreateCharacter() called successfully");
                Debug.Log($"🎉 Character '{characterName}' created with server ID: {characterId}");
            }
            else
            {
                Debug.LogError("❌ Cannot create character - CharacterId was not set properly!");
                DevionGames.EventHandler.Execute("OnFailedToCreateCharacter", characterTemplate);
            }
        }
        else
        {
            Debug.LogError("❌ [CharacterCreationBridge] Server character creation failed! No characterId returned.");
            Debug.LogError($"Server task result was: '{characterId}'");
            Debug.LogError($"Server task status was: {serverTask.Status}");

            if (serverTask.Exception != null)
            {
                Debug.LogError($"Server task exception: {serverTask.Exception}");
            }

            DevionGames.EventHandler.Execute("OnFailedToCreateCharacter", characterTemplate);
        }
    }
}