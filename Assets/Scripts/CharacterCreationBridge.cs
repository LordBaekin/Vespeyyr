// Create this file: Assets/Scripts/CharacterCreationBridge.cs
using UnityEngine;
using System.Collections;
using DevionGames.CharacterSystem;
using TagDebugSystem;

/// <summary>
/// MonoBehaviour bridge that both assemblies can access
/// Place this on a GameObject in your scene
/// </summary>
public class CharacterCreationBridge : MonoBehaviour
{
    public static CharacterCreationBridge Instance { get; private set; }
    private const string TAG = "CharacterSystem";

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

    public void CreateCharacterWithServer(System.Collections.Generic.Dictionary<string, object> parameters)
    {
        string characterName = parameters["characterName"] as string;
        string className = parameters["className"] as string;
        string genderString = parameters["genderString"] as string;
        Character characterTemplate = parameters["characterTemplate"] as Character;

        TD.Info(TAG, $"Received parameters - Name: {characterName}, Class: {className}, Gender: {genderString}", this);

        StartCoroutine(CreateCharacterCoroutine(characterName, className, genderString, characterTemplate));
    }

    private IEnumerator CreateCharacterCoroutine(string characterName, string className, string genderString, Character characterTemplate)
    {
        TD.Info(TAG, $"Creating character '{characterName}' on server...", this);
        TD.Info(TAG, "=== CHARACTER OBJECT ANALYSIS ===", this);
        TD.Info(TAG, $"Character Type: {characterTemplate.GetType().FullName}", this);
        TD.Info(TAG, $"Character Name: {characterTemplate.CharacterName}", this);

        TD.Info(TAG, "EXISTING DEVION GAMES PROPERTIES:", this);
        var existingProperties = characterTemplate.GetProperties();
        if (existingProperties != null && existingProperties.Length > 0)
        {
            foreach (var prop in existingProperties)
            {
                TD.Info(TAG, $"  Property: {prop.Name} = {prop.GetValue()}", this);
            }
        }
        else
        {
            TD.Info(TAG, "  No existing properties found", this);
        }

        var existingCharacterIdProp = characterTemplate.FindProperty("CharacterId");
        if (existingCharacterIdProp != null)
        {
            TD.Info(TAG, $"  Existing CharacterId property found: {existingCharacterIdProp.stringValue}", this);
        }
        else
        {
            TD.Info(TAG, "  No existing CharacterId property found", this);
        }

        TD.Info(TAG, "=== CALLING SERVER ===", this);
        var serverTask = CharacterServerBridge.CreateCharacterOnServer(characterName, className, genderString);

        while (!serverTask.IsCompleted)
        {
            yield return null;
        }

        string characterId = serverTask.Result;

        TD.Info(TAG, "=== SERVER RESPONSE ===", this);
        TD.Info(TAG, $"Server Task Status: {serverTask.Status}", this);
        TD.Info(TAG, $"Server Returned CharacterId: '{characterId}'", this);
        TD.Info(TAG, $"CharacterId is null or empty: {string.IsNullOrEmpty(characterId)}", this);

        if (!string.IsNullOrEmpty(characterId))
        {
            TD.Info(TAG, $"SUCCESS! Server returned characterId: {characterId}", this);
            TD.Info(TAG, "=== SETTING CHARACTER ID VIA PROPERTY SYSTEM ===", this);
            bool characterIdWasSet = false;

            try
            {
                characterTemplate.SetProperty("CharacterId", characterId);
                TD.Info(TAG, $"✅ Called SetProperty('CharacterId', '{characterId}')", this);

                var verifyProperty = characterTemplate.FindProperty("CharacterId");
                if (verifyProperty != null)
                {
                    string retrievedValue = verifyProperty.stringValue;
                    TD.Info(TAG, $"✅ SUCCESS: CharacterId property set and verified: '{retrievedValue}'", this);

                    if (retrievedValue == characterId)
                    {
                        characterIdWasSet = true;
                        TD.Info(TAG, "✅ PERFECT: Retrieved value matches server ID exactly", this);
                    }
                    else
                    {
                        TD.Warning(TAG, $"⚠️ WARNING: Retrieved value '{retrievedValue}' doesn't match server ID '{characterId}'", this);
                    }
                }
                else
                {
                    TD.Error(TAG, "❌ ERROR: FindProperty('CharacterId') returned null after SetProperty!", this);
                }
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"❌ FAILED to set CharacterId property: {ex.Message}", this);
                TD.Error(TAG, $"Exception stack trace: {ex.StackTrace}", this);
            }

            PlayerPrefs.SetString($"CharacterID_{characterName}", characterId);
            PlayerPrefs.Save();
            TD.Info(TAG, $"💾 Stored backup mapping: CharacterID_{characterName} = {characterId}", this);

            TD.Info(TAG, "=== FINAL VERIFICATION ===", this);
            TD.Info(TAG, $"Character ID was successfully set via property system: {characterIdWasSet}", this);

            TD.Info(TAG, "FINAL PROPERTY STATE:", this);
            var finalProperties = characterTemplate.GetProperties();
            if (finalProperties != null && finalProperties.Length > 0)
            {
                foreach (var prop in finalProperties)
                {
                    TD.Info(TAG, $"  Final Property: {prop.Name} = {prop.GetValue()}", this);
                }
            }

            if (characterIdWasSet)
            {
                TD.Info(TAG, "=== CREATING CHARACTER LOCALLY ===", this);
                CharacterManager.CreateCharacter(characterTemplate);
                TD.Info(TAG, "✅ CharacterManager.CreateCharacter() called successfully", this);
                TD.Info(TAG, $"🎉 Character '{characterName}' created with server ID: {characterId}", this);
            }
            else
            {
                TD.Error(TAG, "❌ Cannot create character - CharacterId was not set properly!", this);
                DevionGames.EventHandler.Execute("OnFailedToCreateCharacter", characterTemplate);
            }
        }
        else
        {
            TD.Error(TAG, "❌ Server character creation failed! No characterId returned.", this);
            TD.Error(TAG, $"Server task result was: '{characterId}'", this);
            TD.Error(TAG, $"Server task status was: {serverTask.Status}", this);

            if (serverTask.Exception != null)
            {
                TD.Error(TAG, $"Server task exception: {serverTask.Exception}", this);
            }

            DevionGames.EventHandler.Execute("OnFailedToCreateCharacter", characterTemplate);
        }
    }
}
