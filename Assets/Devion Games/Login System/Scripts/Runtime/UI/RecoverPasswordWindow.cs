using DevionGames.UIWidgets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TagDebugSystem;

namespace DevionGames.LoginSystem
{
    public class RecoverPasswordWindow : UIWidget
    {
        private const string TAG = Tags.UI;

        public override string[] Callbacks
        {
            get
            {
                List<string> callbacks = new List<string>(base.Callbacks);
                callbacks.Add("OnPasswordRecovered");
                callbacks.Add("OnFailedToRecoverPassword");
                return callbacks.ToArray();
            }
        }

        [Header("Reference")]
        [SerializeField] protected InputField email;
        [SerializeField] protected Button recoverButton;
        [SerializeField] protected GameObject loadingIndicator;
        [SerializeField] protected Button backButton;

        protected string lastServerError;

        protected override void OnStart()
        {
            base.OnStart();
            TD.Verbose(TAG, "Initializing RecoverPasswordWindow", this);

            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
                TD.Verbose(TAG, "Loading indicator initialized to hidden state", this);
            }

            EventHandler.Register("OnPasswordRecovered", OnPasswordRecovered);
            EventHandler.Register("OnFailedToRecoverPassword", OnFailedToRecoverPassword);

            recoverButton.onClick.AddListener(RecoverPasswordUsingFields);
            TD.Verbose(TAG, "Recover button listener added", this);

            if (backButton != null)
            {
                backButton.onClick.AddListener(() => {
                    TD.Verbose(TAG, "Back button clicked, returning to login window", this);
                    if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                    {
                        TD.Info(TAG, "Navigating back to login window", this);
                        SceneUIManager.Instance.loginWindow.Show();
                        Close();
                    }
                    else
                    {
                        TD.Error(TAG, "SceneUIManager.Instance.loginWindow is null", this);
                    }
                });
            }

            email.text = "";
            TD.Verbose(TAG, "Email field cleared on window initialization", this);
        }

        private void RecoverPasswordUsingFields()
        {
            TD.Info(TAG, "Password recovery attempt initiated", this);
            lastServerError = null;

            if (!LoginManager.ValidateEmail(email.text))
            {
                TD.Warning(TAG, $"Invalid email format: {email.text}", this);
                LoginManager.Notifications.invalidEmail.Show(delegate (int result) { Show(); }, "OK");
                Close();
                return;
            }

            recoverButton.interactable = false;
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
                TD.Verbose(TAG, "Loading indicator activated", this);
            }

            TD.Info(TAG, $"Requesting password recovery for email: {email.text}", this);
            LoginManager.RecoverPassword(email.text);
        }

        private void OnPasswordRecovered()
        {
            TD.Info(TAG, "Password recovery request successful", this);
            Execute("OnPasswordRecovered", new CallbackEventData());

            LoginManager.Notifications.passwordRecovered.Show(
                delegate (int result)
                {
                    TD.Info(TAG, "Recovery notification closed, returning to login window", this);
                    if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                    {
                        SceneUIManager.Instance.loginWindow.Show();
                    }
                },
                "OK",
                "If your email exists in our system, you'll receive instructions to reset your password. Please check your inbox."
            );

            recoverButton.interactable = true;
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
                TD.Verbose(TAG, "Loading indicator deactivated", this);
            }

            email.text = "";
            TD.Verbose(TAG, "Email field cleared for privacy", this);
        }

        private void OnFailedToRecoverPassword()
        {
            TD.Warning(TAG, "Password recovery request failed", this);
            Execute("OnFailedToRecoverPassword", new CallbackEventData());

            LoginManager.Notifications.passwordRecovered.Show(
                delegate (int result)
                {
                    TD.Info(TAG, "Recovery notification closed, returning to login window", this);
                    if (SceneUIManager.Instance != null && SceneUIManager.Instance.loginWindow != null)
                    {
                        SceneUIManager.Instance.loginWindow.Show();
                    }
                },
                "OK",
                "If your email exists in our system, you'll receive instructions to reset your password. Please check your inbox."
            );

            recoverButton.interactable = true;
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(false);
                TD.Verbose(TAG, "Loading indicator deactivated", this);
            }

            email.text = "";
            TD.Verbose(TAG, "Email field cleared for privacy", this);
        }

        protected override void OnDestroy()
        {
            TD.Info(TAG, "RecoverPasswordWindow being destroyed, unregistering event handlers", this);
            EventHandler.Unregister("OnPasswordRecovered", OnPasswordRecovered);
            EventHandler.Unregister("OnFailedToRecoverPassword", OnFailedToRecoverPassword);
            base.OnDestroy();
        }
    }
}
