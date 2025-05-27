// Assets/Scripts/LoginSceneInitializer.cs
using UnityEngine;

public class LoginSceneInitializer : MonoBehaviour
{
    void Start()
    {
        if (FlowManager.Instance != null)
        {
            FlowManager.Instance.ShowLoginPanel();
        }
        else
        {
            Debug.LogError("FlowManager.Instance is null in LoginSceneInitializer!");
        }
    }
}
