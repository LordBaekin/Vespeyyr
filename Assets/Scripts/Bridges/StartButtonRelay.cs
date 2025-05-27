// Place this script in Assets/Scripts/StartButtonRelay.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonRelay : MonoBehaviour
{
    public string loginSceneName = "Login"; // Set this in the Inspector if needed

    public void OnStartGameClicked()
    {
        SceneManager.LoadScene(loginSceneName);
    }
}
