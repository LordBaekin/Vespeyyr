using UnityEngine;
using TagDebugSystem;

/// <summary>
/// Debug helper script to verify character data is being set correctly
/// Attach this to any GameObject in the Main Scene to debug character loading issues
/// </summary>
public class CharacterDebugHelper : MonoBehaviour
{
    private const string TAG = Tags.Character;

    [Header("Debug Options")]
    public bool logOnStart = true;
    public bool logEverySecond = false;

    void Start()
    {
        if (logOnStart)
        {
            LogCharacterData("Start()");
        }

        if (logEverySecond)
        {
            InvokeRepeating(nameof(LogCharacterDataRepeating), 1f, 1f);
        }
    }

    void LogCharacterDataRepeating()
    {
        LogCharacterData("Periodic Check");
    }

    [ContextMenu("Log Character Data")]
    public void LogCharacterData(string context = "Manual Check")
    {
        TD.Info(TAG, $"=== CHARACTER DEBUG - {context} ===", this);

        // Check all relevant PlayerPrefs
        string[] keys = {
            "CurrentCharacterID",
            "Player",
            "Profession",
            "CurrentWorldKey",
            "SavingKey",
            "jwt_token",
            "Coherence_AuthToken",
            "Coherence_WorldKey"
        };

        foreach (string key in keys)
        {
            string value = PlayerPrefs.GetString(key, "<not set>");
            if (key.Contains("token") && value != "<not set>")
            {
                // Don't log full tokens for security
                value = $"<token present: {value.Length} chars>";
            }
            TD.Info(TAG, $"  {key} = {value}", this);
        }

        // Check CharacterManager state
        try
        {
            if (DevionGames.CharacterSystem.CharacterManager.current != null)
            {
                var selectedChar = DevionGames.CharacterSystem.CharacterManager.current.SelectedCharacter;
                if (selectedChar != null)
                {
                    TD.Info(TAG, $"CharacterManager.SelectedCharacter = {selectedChar.CharacterName} ({selectedChar.Name})", this);
                }
                else
                {
                    TD.Warning(TAG, "CharacterManager.SelectedCharacter = null", this);
                }
            }
            else
            {
                TD.Warning(TAG, "CharacterManager.current = null", this);
            }
        }
        catch (System.Exception ex)
        {
            TD.Error(TAG, $"Error checking CharacterManager: {ex.Message}", this);
        }

        // Check for player objects in scene
        GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
        TD.Info(TAG, $"Player objects in scene: {playerObjects.Length}", this);
        for (int i = 0; i < playerObjects.Length; i++)
        {
            TD.Info(TAG, $"  Player {i}: {playerObjects[i].name} at {playerObjects[i].transform.position}", this);
        }

        TD.Info(TAG, "=== END CHARACTER DEBUG ===", this);
    }

    [ContextMenu("Fix Missing Character ID")]
    public void FixMissingCharacterID()
    {
        string currentCharID = PlayerPrefs.GetString("CurrentCharacterID", "");
        string playerName = PlayerPrefs.GetString("Player", "");

        if (string.IsNullOrEmpty(currentCharID) && !string.IsNullOrEmpty(playerName))
        {
            PlayerPrefs.SetString("CurrentCharacterID", playerName);
            PlayerPrefs.Save();
            TD.Info(TAG, $"Fixed missing CurrentCharacterID by setting it to: {playerName}", this);
        }
        else if (string.IsNullOrEmpty(currentCharID))
        {
            TD.Warning(TAG, "Cannot fix CurrentCharacterID - no Player name available either", this);
        }
        else
        {
            TD.Info(TAG, $"CurrentCharacterID already set: {currentCharID}", this);
        }
    }
}