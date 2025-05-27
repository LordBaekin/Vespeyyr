using UnityEngine;

[CreateAssetMenu(fileName = "SaveProviderSelectorSO", menuName = "Game/SaveProviderSelectorSO")]
public class SaveProviderSelectorSO : ScriptableObject
{
    public enum SaveProvider { PlayerPrefs, Server, Both }

    [Header("Change this at runtime or in the inspector!")]
    public SaveProvider currentProvider = SaveProvider.PlayerPrefs;

    // Optional: Provide a global static accessor (not required, but handy)
    public static SaveProviderSelectorSO Instance
    {
        get
        {
            if (_instance == null)
                _instance = Resources.Load<SaveProviderSelectorSO>("SaveProviderSelectorSO");
            return _instance;
        }
    }
    private static SaveProviderSelectorSO _instance;
}