using UnityEngine;
using UnityEngine.UI;
using Vespeyr.Network;
using TagDebugSystem;
using Coherence.Cloud; // Import for PlayerAccount
namespace DevionGames.CharacterSystem
{
        public class VespeyrLoginRelay : MonoBehaviour
    {
        public InputField usernameField;
        public InputField passwordField;
        public Button loginButton;
        public GameObject loginPanel;           // Inspector assignment
        public GameObject serverSelectionPanel; // Inspector assignment

        private void Awake()
        {
            loginButton.onClick.AddListener(OnLoginClicked);
        }

        private async void OnLoginClicked()
        {
            string username = usernameField.text.Trim();
            string password = passwordField.text;

            TD.Info("VespeyrLoginRelay", $"Attempting login for user: {username}");

            // Backend login first
            var authResponse = await DVGApiBridge.LoginAndGetJwtAsync(username, password);

            if (authResponse != null && !string.IsNullOrEmpty(authResponse.access_token))
            {
                // Save tokens immediately on success
                DVGApiBridge.SetToken(authResponse.access_token);
                DVGApiBridge.SetRefresh(authResponse.refresh_token);
                TD.Info("VespeyrLoginRelay", "JWT token saved successfully");

                // Also set for DevionGamesAdapter if needed
                DevionGamesAdapter.SetAuthToken(authResponse.access_token);

                TD.Info("VespeyrLoginRelay", "Backend login success, now logging in to Coherence...");

                // Continue with Coherence login
                var loginOperation = await CoherenceCloud.LoginWithJwt(authResponse.access_token);

                if (loginOperation != null && !loginOperation.HasFailed && loginOperation.Result != null)
                {
                    TD.Info("VespeyrLoginRelay", "Coherence login successful, advancing UI flow.");

                    FlowManager.Instance.playerAccount = loginOperation.Result;

                    // Inspector-driven panel assignment
                    if (serverSelectionPanel != null)
                        serverSelectionPanel.SetActive(true);
                    if (loginPanel != null)
                        loginPanel.SetActive(false);

                    loginButton.interactable = true;
                }
                else
                {
                    string err = loginOperation != null && loginOperation.HasFailed
                        ? loginOperation.Error?.ToString()
                        : "Unknown error";
                    TD.Error("VespeyrLoginRelay", $"Coherence login failed: {err}");
                    loginButton.interactable = true;
                }
            }
            else
            {
                TD.Error("VespeyrLoginRelay", $"Login failed: {authResponse?.error ?? "No response"}");
                loginButton.interactable = true;
            }
        }

    }
}