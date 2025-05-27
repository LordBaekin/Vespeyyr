using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TagDebugSystem;

public class SplashScreen : MonoBehaviour
{
    [Header("Splash Images")]
    public RawImage splashDisplay;
    public List<Texture> splashTextures = new List<Texture>();
    public float splashInterval = 5f;

    [Header("Next Scene Settings")]
    public string nextSceneName = "Start"; // Or MainMenu, whatever your next scene is called

    private void Start()
    {
        // Check assignment
        if (splashDisplay == null || splashTextures == null || splashTextures.Count == 0)
        {
            TD.Warning("SplashScreen", "No splashDisplay or splashTextures assigned! Skipping to next scene.");
            LoadNextScene();
            return;
        }
        StartCoroutine(SplashSequence());
    }

    IEnumerator SplashSequence()
    {
        TD.Info("SplashScreen", "Starting splash sequence");

        for (int i = 0; i < splashTextures.Count; i++)
        {
            try
            {
                splashDisplay.texture = splashTextures[i];
                TD.Info("SplashScreen", $"Displaying splash {i + 1}/{splashTextures.Count}");
            }
            catch (System.Exception ex)
            {
                TD.Error("SplashScreen", $"Failed to display splash {i + 1}: {ex.Message}");
            }

            float elapsed = 0f;
            bool advance = false;

            while (elapsed < splashInterval && !advance)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    TD.Info("SplashScreen", $"User advanced splash with ESC ({i + 1}/{splashTextures.Count})");
                    advance = true;
                    // No yield break! This just skips to next image
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        TD.Info("SplashScreen", "Splash sequence complete.");
        LoadNextScene();
    }


    private void LoadNextScene()
    {
        // Add fade out or sound here if you want
        TD.Info("SplashScreen", $"Loading next scene: {nextSceneName}");
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            TD.Warning("SplashScreen", "No next scene specified!");
    }
}
