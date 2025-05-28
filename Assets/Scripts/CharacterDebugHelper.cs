// Ultra safe character fixer - only uses properties we know exist
// Add this to a GameObject in your Character Select scene

using UnityEngine;
using DevionGames.CharacterSystem;
using System.Reflection;

public class UltraSafeCharacterFixer : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(FixCharacters), 2f);
    }

    private void FixCharacters()
    {
        Debug.Log("🔧 ULTRA SAFE CHARACTER FIXER");

        var characters = FindObjectsOfType<Character>();
        Debug.Log($"Found {characters.Length} characters");

        int fixedCount = 0;

        foreach (var character in characters)
        {
            if (character == null) continue;

            Debug.Log($"Checking: {character.CharacterName}");

            // Only fix the Name property if it's empty
            if (string.IsNullOrEmpty(character.Name))
            {
                character.Name = character.CharacterName;
                Debug.Log($"  ✅ Fixed Name: '{character.Name}'");
                fixedCount++;
            }
            else
            {
                Debug.Log($"  ✅ Name already set: '{character.Name}'");
            }
        }

        Debug.Log($"🔧 Fixed {fixedCount} characters");
        Debug.Log("✅ All characters should now be selectable!");
    }

    [ContextMenu("Fix Characters Now")]
    public void FixNow()
    {
        FixCharacters();
    }

    [ContextMenu("Show Character Info")]
    public void ShowCharacterInfo()
    {
        Debug.Log("=== CHARACTER INFO ===");

        var characters = FindObjectsOfType<Character>();
        for (int i = 0; i < characters.Length; i++)
        {
            var character = characters[i];
            Debug.Log($"[{i}] {character.CharacterName}");
            Debug.Log($"    Name: '{character.Name}'");
            Debug.Log($"    Selectable: {!string.IsNullOrEmpty(character.Name)}");

            // Use reflection to see what other properties exist
            var type = character.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            Debug.Log($"    Available properties: {properties.Length}");
            foreach (var prop in properties)
            {
                if (prop.Name == "CharacterName" || prop.Name == "Name") continue;

                if (prop.CanRead)
                {
                    try
                    {
                        var value = prop.GetValue(character);
                        if (value != null && !string.IsNullOrEmpty(value.ToString()))
                        {
                            Debug.Log($"      {prop.Name}: {value}");
                        }
                    }
                    catch
                    {
                        // Skip properties that can't be read
                    }
                }
            }
        }
    }
}