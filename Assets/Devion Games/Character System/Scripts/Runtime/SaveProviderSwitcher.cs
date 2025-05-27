using UnityEngine;

public class SaveProviderSwitcher : MonoBehaviour
{
    public SaveProviderSelectorSO providerAsset; // Assign in inspector

    void OnGUI()
    {
        if (providerAsset == null) return;

        GUILayout.BeginArea(new Rect(10, 10, 200, 120), "Save Provider", GUI.skin.window);
        GUILayout.Label("Current: " + providerAsset.currentProvider);
        if (GUILayout.Button("PlayerPrefs")) providerAsset.currentProvider = SaveProviderSelectorSO.SaveProvider.PlayerPrefs;
        if (GUILayout.Button("Server")) providerAsset.currentProvider = SaveProviderSelectorSO.SaveProvider.Server;
        if (GUILayout.Button("Both")) providerAsset.currentProvider = SaveProviderSelectorSO.SaveProvider.Both;
        GUILayout.EndArea();
    }
}