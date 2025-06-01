using DevionGames.UIWidgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TagDebugSystem;

namespace DevionGames.LoginSystem
{
    public class LoginWindow : UIWidget
    {
        private const string TAG = Tags.UI;

        public override string[] Callbacks
        {
            get
            {
                List<string> callbacks = new List<string>(base.Callbacks);
                callbacks.Add("OnLogin");
                callbacks.Add("OnFailedToLogin");
                callbacks.Add("OnSessionExpired");
                return callbacks.ToArray();
            }
        }

        [Header("Reference")]
        [SerializeField] protected InputField username;
        [SerializeField] protected InputField password;
        [SerializeField] protected Toggle rememberMe;
        [SerializeField] protected Button loginButton;
        [SerializeField] protected GameObject loadingIndicator;

        [Header("Navigation")]
        [SerializeField] protected Button registerButton;
        [SerializeField] protected Button forgotPasswordButton;

        protected override void OnStart()
        {
            base.OnStart();
            TD.Verbose(TAG, "Initializing LoginWindow", this);

            // Safety check for required fields
            if (username == null || password == null || loginButton == null)
            {
                TD.Error(TAG, "Missing required UI fields (username/password/loginButton)", this);
                return;
            }

            username.text = PlayerPrefs.GetString("username", string.Empty);
            password.text = string.Empty;

            if (rememberMe != null)
            {
                rememberMe.isOn = !string.IsNullOrEmpty(username.text);
            }

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
            }

            EventHandler.Register("OnLogin", OnLogin);
            EventHandler.Register("OnFailedToLogin", OnFailedToLogin);
            EventHandler.Register("OnSessionExpired", OnSessionExpired);

            loginButton.onClick.AddListener(LoginUsingFields);

            if (registerButton != null)
            {
                registerButton.onClick.AddListener(() =>
                {
                    TD.Verbose(TAG, "Register button clicked", this);
                    var regWindow = SceneUIManager.Instance?.registrationWindow;
                    if (regWindow != null)
                    {
                        TD.Info(TAG, "Showing registration window", this);
                        Close();
                        regWindow.Show();
                    }
                    else
                    {
                        TD.Error(TAG, "SceneUIManager.Instance.registrationWindow is null", this);
                    }
                });
            }

            if (forgotPasswordButton != null)
            {
                forgotPasswordButton.onClick.AddListener(() =>
                {
                    TD.Verbose(TAG, "Forgot password button clicked", this);
                    var recoverWindow = SceneUIManager.Instance?.recoverPasswordWindow;
                    if (recoverWindow != null)
                    {
                        TD.Info(TAG, "Showing password recovery window", this);
                        Close();
                        recoverWindow.Show();
                    }
                    else
                    {
                        TD.Error(TAG, "SceneUIManager.Instance.recoverPasswordWindow is null", this);
                    }
                });
            }

            string token = LoginManager.GetAuthToken();
            if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(username.text))
            {
                TD.Info(TAG, "Found token. Attempting token refresh...", this);
                StartCoroutine(AttemptTokenRefresh());
            }
        }

        private IEnumerator AttemptTokenRefresh()
        {
            loginButton.interactable = false;
            loadingIndicator?.SetActive(true);

            LoginManager.RefreshToken();

            float timer = 0f;
            bool refreshComplete = false;

            System.Action onSuccess = () => {
                TD.Info(TAG, "Token refresh successful", this);
                refreshComplete = true;
                OnLogin();
            };

            System.Action onFailure = () => {
                TD.Warning(TAG, "Token refresh failed", this);
                refreshComplete = true;
                loginButton.interactable = true;
                loadingIndicator?.SetActive(false);
            };

            EventHandler.Register("OnTokenRefreshed", onSuccess);
            EventHandler.Register("OnSessionExpired", onFailure);

            while (!refreshComplete && timer < 5f)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            EventHandler.Unregister("OnTokenRefreshed", onSuccess);
            EventHandler.Unregister("OnSessionExpired", onFailure);

            if (!refreshComplete)
            {
                TD.Warning(TAG, "Token refresh timed out", this);
                loginButton.interactable = true;
                loadingIndicator?.SetActive(false);
            }
        }

        public void LoginUsingFields()
        {
            TD.Info(TAG, $"Login attempt for user: {username.text}", this);
            LoginManager.LoginAccount(username.text, password.text);

            loginButton.interactable = false;
            loadingIndicator?.SetActive(true);
        }

        private void OnLogin()
        {
            TD.Info(TAG, "Login successful", this);

            if (rememberMe != null && rememberMe.isOn)
            {
                PlayerPrefs.SetString("username", username.text);
                PlayerPrefs.DeleteKey("password");
            }
            else
            {
                PlayerPrefs.DeleteKey("username");
                PlayerPrefs.DeleteKey("password");
            }

            Execute("OnLogin", new CallbackEventData());

            var flowManager = FlowManager.Instance;
            if (flowManager != null && flowManager.serverSelectionPanel != null)
            {
                TD.Info(TAG, "Showing server selection panel", this);
                flowManager.ShowOnly(flowManager.serverSelectionPanel);
            }
            else
            {
                TD.Warning(TAG, "FlowManager or serverSelectionPanel is null", this);
            }
        }

        private void OnFailedToLogin()
        {
            TD.Warning(TAG, "Login failed", this);

            Execute("OnFailedToLogin", new CallbackEventData());

            password.text = "";
            Show();

            LoginManager.Notifications.loginFailed.Show((int result) =>
            {
                Show();
            }, "OK");

            loginButton.interactable = true;
            loadingIndicator?.SetActive(false);
        }

        private void OnSessionExpired()
        {
            TD.Warning(TAG, "Session expired", this);
            Show();
            Execute("OnSessionExpired", new CallbackEventData());

            LoginManager.Notifications.loginFailed.Show((int result) =>
            {
                Show();
            }, "OK", "Session expired. Please log in again.");

            loginButton.interactable = true;
            loadingIndicator?.SetActive(false);
        }

        protected override void OnDestroy()
        {
            TD.Info(TAG, "LoginWindow destroyed, unregistering events", this);

            EventHandler.Unregister("OnLogin", OnLogin);
            EventHandler.Unregister("OnFailedToLogin", OnFailedToLogin);
            EventHandler.Unregister("OnSessionExpired", OnSessionExpired);

            base.OnDestroy();
        }
    }
}
