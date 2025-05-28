// Safe character debug that doesn't assume API structure
// Add this to a GameObject in your Character Select scene

using UnityEngine;
using DevionGames.CharacterSystem;
using System.Reflection;
using System.Linq;

public class SafeCharacterDebug : MonoBehaviour
{
    private void Start()
    {
        Invoke(nameof(DebugCharacterSystem), 2f);
    }

    private void DebugCharacterSystem()
    {
        Debug.Log("=== SAFE CHARACTER DEBUG ===");

        // 1. Find CharacterWindow
        var characterWindow = FindObjectOfType<CharacterWindow>();
        if (characterWindow == null)
        {
            Debug.LogError("❌ No CharacterWindow found");
            return;
        }

        Debug.Log($"✅ CharacterWindow found: {characterWindow.name}");

        // 2. Use reflection to see what properties/fields are available
        var windowType = characterWindow.GetType();
        Debug.Log($"🔍 CharacterWindow type: {windowType.Name}");

        // Get all public fields
        var fields = windowType.GetFields(BindingFlags.Public | BindingFlags.Instance);
        Debug.Log($"📝 Public fields ({fields.Length}):");
        foreach (var field in fields)
        {
            try
            {
                var value = field.GetValue(characterWindow);
                Debug.Log($"   {field.Name} ({field.FieldType.Name}): {value}");
            }
            catch (System.Exception ex)
            {
                Debug.Log($"   {field.Name} ({field.FieldType.Name}): ERROR - {ex.Message}");
            }
        }

        // Get all public properties
        var properties = windowType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        Debug.Log($"🏷️ Public properties ({properties.Length}):");
        foreach (var prop in properties)
        {
            if (prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(characterWindow);
                    Debug.Log($"   {prop.Name} ({prop.PropertyType.Name}): {value}");
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"   {prop.Name} ({prop.PropertyType.Name}): ERROR - {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"   {prop.Name} ({prop.PropertyType.Name}): [Write-only]");
            }
        }

        // 3. Look for character-related components in the scene
        Debug.Log($"🎭 Looking for character-related components...");

        var characterSlots = FindObjectsOfType<CharacterSlot>();
        Debug.Log($"   CharacterSlot components: {characterSlots.Length}");

        for (int i = 0; i < characterSlots.Length; i++)
        {
            var slot = characterSlots[i];
            Debug.Log($"     Slot[{i}]: {slot.name}");

            // Check if slot has a Character property/field
            var slotType = slot.GetType();
            var characterField = slotType.GetField("Character", BindingFlags.Public | BindingFlags.Instance);
            var characterProp = slotType.GetProperty("Character", BindingFlags.Public | BindingFlags.Instance);

            if (characterField != null)
            {
                var character = characterField.GetValue(slot);
                Debug.Log($"       Character (field): {character?.GetType()?.Name ?? "null"}");
                if (character != null)
                {
                    DebugCharacterObject(character, i);
                }
            }
            else if (characterProp != null && characterProp.CanRead)
            {
                var character = characterProp.GetValue(slot);
                Debug.Log($"       Character (property): {character?.GetType()?.Name ?? "null"}");
                if (character != null)
                {
                    DebugCharacterObject(character, i);
                }
            }
            else
            {
                Debug.Log($"       No Character field/property found");
            }
        }

        // 4. Look for Character components directly
        var characters = FindObjectsOfType<Character>();
        Debug.Log($"   Character components in scene: {characters.Length}");

        for (int i = 0; i < characters.Length; i++)
        {
            Debug.Log($"     Character[{i}]: {characters[i].name}");
            DebugCharacterObject(characters[i], i);
        }

        Debug.Log("=== END SAFE DEBUG ===");
    }

    private void DebugCharacterObject(object character, int index)
    {
        if (character == null) return;

        var charType = character.GetType();

        // Look for common character properties
        string[] commonProps = { "CharacterName", "Name", "Race", "Class", "Level", "PrefabName" };

        foreach (var propName in commonProps)
        {
            var prop = charType.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
            var field = charType.GetField(propName, BindingFlags.Public | BindingFlags.Instance);

            if (prop != null && prop.CanRead)
            {
                try
                {
                    var value = prop.GetValue(character);
                    Debug.Log($"         {propName}: {value}");
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"         {propName}: ERROR - {ex.Message}");
                }
            }
            else if (field != null)
            {
                try
                {
                    var value = field.GetValue(character);
                    Debug.Log($"         {propName}: {value}");
                }
                catch (System.Exception ex)
                {
                    Debug.Log($"         {propName}: ERROR - {ex.Message}");
                }
            }
        }
    }

    [ContextMenu("Debug Character System Now")]
    public void DebugNow()
    {
        DebugCharacterSystem();
    }

    [ContextMenu("List All Character System Components")]
    public void ListAllCharacterComponents()
    {
        Debug.Log("=== ALL CHARACTER SYSTEM COMPONENTS ===");

        // Find all DevionGames.CharacterSystem components
        var allComponents = FindObjectsOfType<MonoBehaviour>()
            .Where(c => c.GetType().Namespace?.Contains("CharacterSystem") == true)
            .ToArray();

        Debug.Log($"Found {allComponents.Length} CharacterSystem components:");

        foreach (var comp in allComponents)
        {
            Debug.Log($"   {comp.GetType().Name} on '{comp.name}'");
        }

        Debug.Log("=== END COMPONENT LIST ===");
    }
}