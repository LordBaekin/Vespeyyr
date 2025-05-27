using UnityEngine;
using TagDebugSystem;

/// <summary>
/// Centralized save key management for consistent world and character-specific data persistence
/// Place this in your main Scripts folder (Assembly-CSharp) so all assemblies can access it
/// </summary>
public static class SaveKeyManager
{
    private const string TAG = Tags.SaveSystem;

    /// <summary>
    /// Gets the world-specific save key for systems that save per-world (characters, etc.)
    /// Format: "world_{worldKey}"
    /// </summary>
    public static string GetWorldSaveKey()
    {
        string worldKey = PlayerPrefs.GetString("CurrentWorldKey", "default");

        if (string.IsNullOrEmpty(worldKey) || worldKey == "default")
        {
            TD.Warning(TAG, "Using default world key for saving");
            return "world_default";
        }

        return $"world_{SanitizeForSaveKey(worldKey)}";
    }

    /// <summary>
    /// Gets the character-specific save key for systems that save per-character (stats, inventory, quests)
    /// Format: "world_{worldKey}_char_{characterName}"
    /// </summary>
    public static string GetCharacterSaveKey(string characterName = null)
    {
        string worldKey = PlayerPrefs.GetString("CurrentWorldKey", "default");

        if (string.IsNullOrEmpty(characterName))
        {
            characterName = PlayerPrefs.GetString("CurrentCharacterName", "");
        }

        if (string.IsNullOrEmpty(characterName))
        {
            TD.Warning(TAG, "No character name available, using world key only");
            return GetWorldSaveKey();
        }

        // Sanitize both world key and character name for use in save keys
        string sanitizedWorldKey = SanitizeForSaveKey(worldKey);
        string sanitizedCharacterName = SanitizeForSaveKey(characterName);

        if (string.IsNullOrEmpty(worldKey) || worldKey == "default")
        {
            return $"world_default_char_{sanitizedCharacterName}";
        }

        return $"world_{sanitizedWorldKey}_char_{sanitizedCharacterName}";
    }

    /// <summary>
    /// Gets the account-level save key for systems that save per-account across worlds
    /// Format: "account_{username}"
    /// </summary>
    public static string GetAccountSaveKey()
    {
        string username = PlayerPrefs.GetString("username", "");

        if (string.IsNullOrEmpty(username))
        {
            username = PlayerPrefs.GetString("Account", "default_user");
        }

        string sanitizedUsername = SanitizeForSaveKey(username);
        return $"account_{sanitizedUsername}";
    }

    /// <summary>
    /// Sets the current character name for save key generation
    /// </summary>
    public static void SetCurrentCharacter(string characterName)
    {
        if (!string.IsNullOrEmpty(characterName))
        {
            PlayerPrefs.SetString("CurrentCharacterName", characterName);
            PlayerPrefs.Save();
            TD.Info(TAG, $"Current character set to: {characterName}");
        }
    }

    /// <summary>
    /// Gets the current world key
    /// </summary>
    public static string GetCurrentWorldKey()
    {
        return PlayerPrefs.GetString("CurrentWorldKey", "default");
    }

    /// <summary>
    /// Gets the current character name
    /// </summary>
    public static string GetCurrentCharacterName()
    {
        return PlayerPrefs.GetString("CurrentCharacterName", "");
    }

    /// <summary>
    /// Sanitizes a string for use in save keys by removing invalid characters
    /// </summary>
    private static string SanitizeForSaveKey(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unnamed";

        // Remove or replace invalid characters
        string sanitized = input.Replace(" ", "_")
                               .Replace(".", "_")
                               .Replace("/", "_")
                               .Replace("\\", "_")
                               .Replace(":", "_")
                               .Replace("*", "_")
                               .Replace("?", "_")
                               .Replace("\"", "_")
                               .Replace("<", "_")
                               .Replace(">", "_")
                               .Replace("|", "_");

        // Ensure it's not too long
        if (sanitized.Length > 50)
        {
            sanitized = sanitized.Substring(0, 50);
        }

        return sanitized.ToLowerInvariant();
    }

    /// <summary>
    /// Helper to get the save key for a specific system
    /// </summary>
    public static string GetSystemSaveKey(SaveKeyType keyType, string characterName = null)
    {
        switch (keyType)
        {
            case SaveKeyType.World:
                return GetWorldSaveKey();
            case SaveKeyType.Character:
                return GetCharacterSaveKey(characterName);
            case SaveKeyType.Account:
                return GetAccountSaveKey();
            default:
                TD.Warning(TAG, $"Unknown save key type: {keyType}");
                return GetWorldSaveKey();
        }
    }

    /// <summary>
    /// Logs the current save key configuration for debugging
    /// </summary>
    public static void LogCurrentConfiguration()
    {
        TD.Info(TAG, "=== Save Key Configuration ===");
        TD.Info(TAG, $"World Key: {GetCurrentWorldKey()}");
        TD.Info(TAG, $"Character Name: {GetCurrentCharacterName()}");
        TD.Info(TAG, $"World Save Key: {GetWorldSaveKey()}");
        TD.Info(TAG, $"Character Save Key: {GetCharacterSaveKey()}");
        TD.Info(TAG, $"Account Save Key: {GetAccountSaveKey()}");
        TD.Info(TAG, "==============================");
    }
}

/// <summary>
/// Types of save keys available
/// </summary>
public enum SaveKeyType
{
    /// <summary>
    /// World-specific data (characters list, world settings)
    /// </summary>
    World,

    /// <summary>
    /// Character-specific data (stats, inventory, quests)
    /// </summary>
    Character,

    /// <summary>
    /// Account-wide data (settings, preferences)
    /// </summary>
    Account
}