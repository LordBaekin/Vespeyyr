using UnityEngine;
using System.Reflection;

public class DeepComponentInspector : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"[DeepComponentInspector] --- Components on '{gameObject.name}' ---");

        foreach (var comp in GetComponents<Component>())
        {
            if (comp == null) continue;
            Debug.Log($"Component: {comp.GetType().Name}");

            // Print public fields
            var fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (var field in fields)
            {
                // Print all public fields and private fields with [SerializeField]
                bool isSerialized = field.IsPublic || field.GetCustomAttribute<SerializeField>() != null;
                if (isSerialized)
                {
                    object value = field.GetValue(comp);
                    Debug.Log($"    Field: {field.Name} = {value}");
                }
            }

            // Print public properties (skip Unity internals, indexers, and non-readable)
            var props = comp.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
            foreach (var prop in props)
            {
                if (!prop.CanRead) continue;
                if (prop.GetIndexParameters().Length > 0) continue;
                if (prop.Name == "transform" || prop.Name == "gameObject" || prop.Name == "rigidbody") continue;
                object value;
                try { value = prop.GetValue(comp); }
                catch { continue; }
                Debug.Log($"    Property: {prop.Name} = {value}");
            }
        }
    }
}
