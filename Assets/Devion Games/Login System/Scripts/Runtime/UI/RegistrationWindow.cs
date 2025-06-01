using DevionGames.UIWidgets;
using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TagDebugSystem;

namespace DevionGames.LoginSystem
{
    public class RegistrationWindow : UIWidget
    {
        private const string TAG = Tags.UI;

        public override string[] Callbacks
        {
            get
            {
                var callbacks = new System.Collections.Generic.List<string>(base.Callbacks);
                callbacks.Add("OnAccountCreated");
                callbacks.Add("OnFailedToCreateAccount");
                callbacks.Add("OnUsernameAlreadyExists");
                return callbacks.ToArray();
            }
        }

        [Header("Fields")]
        [SerializeField] protected InputField username;
        [SerializeField] protected InputField password;
        [SerializeField] protected InputField confirmPassword;
        [SerializeField] protected InputField email;
        [SerializeField] protected Toggle termsOfUse;
        [SerializeField] protected Button registerButton;
        [SerializeField] protected Button backButton;
        [SerializeField] protected GameObject loadingIndicator;

        private GameObject errorPanel;
        private Text errorText;
        private Button errorOKButton;

        protected override void OnStart()
        {
            base.OnStart();
            TD.Verbose(TAG, "Initializing RegistrationWindow", this);
            CreateErrorDialogUI();

            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);

            EventHandler.Register("OnAccountCreated", OnAccountCreated);
            EventHandler.Register("OnFailedToCreateAccount", OnFailedToCreateAccount);
            EventHandler.Register("OnUsernameAlreadyExists", OnUsernameAlreadyExists);

            registerButton.onClick.AddListener(CreateAccountUsingFields);

            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    TD.Verbose(TAG, "Back button clicked, returning to login window", this);
                    if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                    {
                        TD.Info(TAG, "Navigating back to login window", this);
                        SceneUIManager.Instance.loginWindow.Show();
                    }
                    else
                    {
                        TD.Error(TAG, "LoginWindow not found in SceneUIManager", this);
                    }
                });
            }

            ClearFields();
        }

        private void CreateErrorDialogUI()
        {
            errorPanel = new GameObject("ErrorPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            errorPanel.transform.SetParent(transform, false);
            var panelRT = errorPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;
            var panelImg = errorPanel.GetComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.75f);

            var textGO = new GameObject("ErrorText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textGO.transform.SetParent(errorPanel.transform, false);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.1f, 0.6f);
            textRT.anchorMax = new Vector2(0.9f, 0.9f);
            textRT.offsetMin = textRT.offsetMax = Vector2.zero;
            errorText = textGO.GetComponent<Text>();
            errorText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            errorText.fontSize = 20;
            errorText.alignment = TextAnchor.MiddleCenter;
            errorText.color = Color.white;
            errorText.horizontalOverflow = HorizontalWrapMode.Wrap;
            errorText.verticalOverflow = VerticalWrapMode.Truncate;

            var btnGO = new GameObject("ErrorOK", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(errorPanel.transform, false);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.4f, 0.2f);
            btnRT.anchorMax = new Vector2(0.6f, 0.3f);
            btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(0.2f, 0.6f, 1f, 1f);
            errorOKButton = btnGO.GetComponent<Button>();

            var okTextGO = new GameObject("OK_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            okTextGO.transform.SetParent(btnGO.transform, false);
            var okRT = okTextGO.GetComponent<RectTransform>();
            okRT.anchorMin = Vector2.zero;
            okRT.anchorMax = Vector2.one;
            okRT.offsetMin = okRT.offsetMax = Vector2.zero;
            var okText = okTextGO.GetComponent<Text>();
            okText.text = "OK";
            okText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            okText.fontSize = 18;
            okText.alignment = TextAnchor.MiddleCenter;
            okText.color = Color.white;

            errorOKButton.onClick.AddListener(() =>
            {
                errorPanel.SetActive(false);
                registerButton.interactable = true;
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
                username.ActivateInputField();
            });

            errorPanel.SetActive(false);
        }

        private void ShowErrorDialog(string message)
        {
            errorText.text = message;
            errorPanel.SetActive(true);
        }

        private void ClearFields()
        {
            username.text = "";
            password.text = "";
            confirmPassword.text = "";
            email.text = "";
            if (termsOfUse != null) termsOfUse.isOn = false;
            registerButton.interactable = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
        }

        private void CreateAccountUsingFields()
        {
            errorPanel.SetActive(false);

            if (string.IsNullOrEmpty(username.text) || string.IsNullOrEmpty(password.text) ||
                string.IsNullOrEmpty(confirmPassword.text) || string.IsNullOrEmpty(email.text))
            {
                ShowErrorDialog("All fields are required.");
                return;
            }
            if (password.text != confirmPassword.text)
            {
                password.text = "";
                confirmPassword.text = "";
                ShowErrorDialog("Passwords do not match.");
                return;
            }
            if (!LoginManager.ValidateEmail(email.text))
            {
                email.text = "";
                ShowErrorDialog("Please enter a valid email address.");
                return;
            }
            if (termsOfUse != null && !termsOfUse.isOn)
            {
                ShowErrorDialog("You must accept the terms of use.");
                return;
            }
            if (password.text.Length < 8)
            {
                password.text = "";
                confirmPassword.text = "";
                ShowErrorDialog("Password must be at least 8 characters long.");
                return;
            }

            registerButton.interactable = false;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            LoginManager.CreateAccount(username.text, password.text, email.text);
        }

        private void OnUsernameAlreadyExists()
        {
            username.text = "";
            registerButton.interactable = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            ShowErrorDialog("Username already exists. Please choose a different username.");
        }

        private void OnFailedToCreateAccount()
        {
            username.text = "";
            string errorMsg = "Failed to create account. Please try again.";
            if (PlayerPrefs.HasKey("last_registration_error"))
            {
                errorMsg = PlayerPrefs.GetString("last_registration_error");
                PlayerPrefs.DeleteKey("last_registration_error");
            }
            registerButton.interactable = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            ShowErrorDialog(errorMsg);
        }

        private void OnAccountCreated()
        {
            Execute("OnAccountCreated", new CallbackEventData());
            if (LoginManager.Notifications.accountCreated != null)
            {
                LoginManager.Notifications.accountCreated.Show(result =>
                {
                    ClearFields();
                    if (!string.IsNullOrEmpty(LoginManager.GetAuthToken()) && LoginManager.DefaultSettings.loadSceneOnLogin)
                    {
                        SceneManager.LoadScene(LoginManager.DefaultSettings.sceneToLoad);
                    }
                    else if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                    {
                        SceneUIManager.Instance.loginWindow.Show();
                    }
                }, "OK");
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.DisplayDialog("Success", "Account created successfully! Please log in.", "OK");
#endif
                if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                {
                    SceneUIManager.Instance.loginWindow.Show();
                }
            }
            registerButton.interactable = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            Close();
        }

        protected override void OnDestroy()
        {
            EventHandler.Unregister("OnAccountCreated", OnAccountCreated);
            EventHandler.Unregister("OnFailedToCreateAccount", OnFailedToCreateAccount);
            EventHandler.Unregister("OnUsernameAlreadyExists", OnUsernameAlreadyExists);
            base.OnDestroy();
        }
    }
}
