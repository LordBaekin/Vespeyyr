using DevionGames.LoginSystem.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using System.Text;
using System;
using TagDebugSystem;

namespace DevionGames.LoginSystem
{
    [Serializable]
    public class AuthResponse
    {
        // Updated to match server response format - access_token instead of token
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public string id;
        public string username;
        public string message;
        public string error;

        // Backwards compatibility properties
        public string token
        {
            get { return access_token; }
        }
        public string refresh
        {
            get { return refresh_token; }
        }
    }

    [Serializable]
    public class MessageResponse
    {
        public string message;
        public string error;
    }

    /// <summary>
    /// LoginManager handles authentication interactions with the server, including
    /// account creation, login, session management, and password recovery.
    /// </summary>
    public class LoginManager : MonoBehaviour
    {
        // Tag for logging
        private const string TAG = Tags.Authentication;

        private static LoginManager m_Current;

        /// <summary>
        /// The LoginManager singleton object. This object is set inside Awake()
        /// </summary>
        public static LoginManager current
        {
            get
            {
                Assert.IsNotNull(m_Current, "Requires Login Manager.Create one from Tools > Devion Games > Login System > Create Login Manager!");
                return m_Current;
            }
        }

        /// <summary>
        /// Constructs a proper endpoint URL by ensuring the base URL has a trailing slash
        /// </summary>
        /// <param name="endpointName">The endpoint name to append to the base URL</param>
        /// <returns>A properly formatted full URL</returns>
        private static string GetEndpoint(string endpointName)
        {
            string baseUrl = LoginManager.Server.serverAddress.TrimEnd('/') + "/";
            return baseUrl + endpointName;
        }

        /// <summary>
        /// Helper method to check if a web request was successful based on Unity version
        /// </summary>
        private static bool IsRequestFailed(UnityWebRequest www)
        {
#if UNITY_2020_1_OR_NEWER
            return www.result != UnityWebRequest.Result.Success;
#else
            return www.isNetworkError || www.isHttpError;
#endif
        }

        /// <summary>
        /// Gets the JWT token from PlayerPrefs
        /// </summary>
        /// <returns>The stored JWT token or null if not found</returns>
        public static string GetAuthToken()
        {
            return PlayerPrefs.GetString("jwt_token", null);
        }

        /// <summary>
        /// Gets the refresh token from PlayerPrefs
        /// </summary>
        public static string GetRefreshToken()
        {
            return PlayerPrefs.GetString("refresh_token", null);
        }

        /// <summary>
        /// Sets the Authorization header for a request with the stored JWT token
        /// </summary>
        /// <param name="request">The UnityWebRequest to add the header to</param>
        public static void SetAuthHeader(UnityWebRequest request)
        {
            string token = GetAuthToken();
            if (!string.IsNullOrEmpty(token))
            {
                request.SetRequestHeader("Authorization", "Bearer " + token);
            }
        }

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (LoginManager.m_Current != null)
            {
                TD.Warning(TAG, "Multiple LoginManager instances detected. Destroying duplicate!", this);
                Destroy(gameObject);
                return;
            }
            else
            {
                LoginManager.m_Current = this;
                TD.Info(TAG, "LoginManager initialized", this);

                // Share the server base URL with other systems via PlayerPrefs
                PlayerPrefs.SetString("ServerBaseUrl", LoginManager.Server.serverAddress);
                TD.Verbose(TAG, $"Set ServerBaseUrl: {LoginManager.Server.serverAddress}", this);

                // Listen for token expiration events from other systems
                EventHandler.Register("OnAuthTokenExpired", OnAuthTokenExpired);
            }
        }

        private void OnDestroy()
        {
            EventHandler.Unregister("OnAuthTokenExpired", OnAuthTokenExpired);
            TD.Verbose(TAG, "LoginManager destroyed, unregistered from auth token events", this);
        }

        /// <summary>
        /// Handle token expiration events from other systems
        /// </summary>
        private void OnAuthTokenExpired()
        {
            TD.Info(TAG, "Auth token expired event received, attempting refresh", this);
            // When another system detects an expired token, attempt to refresh it
            RefreshToken();
        }

        private void Start()
        {
            if (LoginManager.DefaultSettings.skipLogin)
            {
                TD.Info(TAG, $"Login System is disabled...Loading {LoginManager.DefaultSettings.sceneToLoad} scene", this);
                UnityEngine.SceneManagement.SceneManager.LoadScene(LoginManager.DefaultSettings.sceneToLoad);
            }
        }

        [SerializeField]
        private LoginConfigurations m_Configurations = null;

        /// <summary>
        /// Gets the login configurations. Configurate it inside the editor.
        /// </summary>
        /// <value>The database.</value>
        public static LoginConfigurations Configurations
        {
            get
            {
                if (LoginManager.current != null)
                {
                    Assert.IsNotNull(LoginManager.current.m_Configurations, "Please assign Login Configurations to the Login Manager!");
                    return LoginManager.current.m_Configurations;
                }
                return null;
            }
        }

        private static Default m_DefaultSettings;
        public static Default DefaultSettings
        {
            get
            {
                if (m_DefaultSettings == null)
                {
                    m_DefaultSettings = GetSetting<Default>();
                }
                return m_DefaultSettings;
            }
        }

        private static UI m_UI;
        public static UI UI
        {
            get
            {
                if (m_UI == null)
                {
                    m_UI = GetSetting<UI>();
                }
                return m_UI;
            }
        }

        private static Notifications m_Notifications;
        public static Notifications Notifications
        {
            get
            {
                if (m_Notifications == null)
                {
                    m_Notifications = GetSetting<Notifications>();
                }
                return m_Notifications;
            }
        }

        private static Server m_Server;
        public static Server Server
        {
            get
            {
                if (m_Server == null)
                {
                    m_Server = GetSetting<Server>();
                }
                return m_Server;
            }
        }

        private static T GetSetting<T>() where T : Configuration.Settings
        {
            if (LoginManager.Configurations != null)
            {
                return (T)LoginManager.Configurations.settings.Where(x => x.GetType() == typeof(T)).FirstOrDefault();
            }
            return default(T);
        }

        /// <summary>
        /// Creates a new user account on the server
        /// </summary>
        /// <param name="username">Username</param>
        /// <param name="password">Password</param>
        /// <param name="email">Email address</param>
        public static void CreateAccount(string username, string password, string email)
        {
            if (LoginManager.current != null)
            {
                TD.Info(TAG, $"Starting account creation for username: {username}", LoginManager.current);
                LoginManager.current.StartCoroutine(CreateAccountInternal(username, password, email));
            }
            else
            {
                TD.Error(TAG, "Cannot create account - LoginManager is null", null);
            }
        }

        private static IEnumerator CreateAccountInternal(string username, string password, string email)
        {
            TD.Verbose(TAG, $"[CreateAccountInternal] Starting account creation for username: {username}", LoginManager.current);

            if (LoginManager.Configurations == null)
            {
                TD.Error(TAG, "[CreateAccountInternal] Configurations not found!", LoginManager.current);
                EventHandler.Execute("OnFailedToCreateAccount");
                yield break;
            }

            // Prepare JSON payload - manually construct to avoid JsonUtility issues with anonymous types
            var uri = GetEndpoint(LoginManager.Server.createAccount);
            TD.Verbose(TAG, $"[CreateAccountInternal] Using endpoint: {uri}", LoginManager.current);

            string payload = $"{{\"username\":\"{username}\",\"password\":\"{password}\",\"email\":\"{email}\"}}";
            TD.Verbose(TAG, "[CreateAccountInternal] Payload prepared (password hidden)", LoginManager.current);

            using (var www = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");
                TD.Verbose(TAG, "[CreateAccountInternal] Request prepared, sending...", LoginManager.current);

                yield return www.SendWebRequest();
                TD.Verbose(TAG, $"[CreateAccountInternal] Request completed with code: {www.responseCode}", LoginManager.current);

                if (IsRequestFailed(www))
                {
                    TD.Error(TAG, $"[CreateAccountInternal] Error: {www.error}", LoginManager.current);
                    TD.Error(TAG, $"[CreateAccountInternal] Response body: {www.downloadHandler.text}", LoginManager.current);

                    // Try to extract error from response even in failed request
                    try
                    {
                        var errorResponse = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        if (!string.IsNullOrEmpty(errorResponse.error))
                        {
                            TD.Error(TAG, $"[CreateAccountInternal] Error message from JSON: {errorResponse.error}", LoginManager.current);

                            // Store the error for the UI
                            PlayerPrefs.SetString("last_registration_error", errorResponse.error);
                            PlayerPrefs.Save();

                            // Check for username conflict (this can happen even with non-200 responses)
                            if (errorResponse.error.Contains("Username already exists"))
                            {
                                TD.Warning(TAG, "[CreateAccountInternal] Username conflict detected!", LoginManager.current);
                                EventHandler.Execute("OnUsernameAlreadyExists");
                                yield break;
                            }
                        }
                    }
                    catch (Exception parseEx)
                    {
                        TD.Error(TAG, $"[CreateAccountInternal] Failed to parse error response: {parseEx.Message}", LoginManager.current);
                    }

                    EventHandler.Execute("OnFailedToCreateAccount");
                }
                else
                {
                    // Log the full response for debugging
                    TD.Verbose(TAG, $"[CreateAccountInternal] Full response: {www.downloadHandler.text}", LoginManager.current);

                    // Check for error message first
                    try
                    {
                        var response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        TD.Verbose(TAG, "[CreateAccountInternal] Response parsed successfully", LoginManager.current);

                        // Check for error
                        if (!string.IsNullOrEmpty(response.error))
                        {
                            TD.Warning(TAG, $"[CreateAccountInternal] Server error: {response.error}", LoginManager.current);

                            // Store the error for the UI
                            PlayerPrefs.SetString("last_registration_error", response.error);
                            PlayerPrefs.Save();

                            // Check for username conflict 
                            if (response.error.Contains("Username already exists"))
                            {
                                TD.Warning(TAG, "[CreateAccountInternal] Username conflict detected!", LoginManager.current);
                                EventHandler.Execute("OnUsernameAlreadyExists");
                                yield break;
                            }

                            EventHandler.Execute("OnFailedToCreateAccount");
                            yield break;
                        }

                        // Valid token = success
                        // Check for both old (token) and new (access_token) property names
                        if (!string.IsNullOrEmpty(response.access_token))
                        {
                            TD.Info(TAG, "[CreateAccountInternal] Token received (access_token format)", LoginManager.current);
                            PlayerPrefs.SetString("jwt_token", response.access_token);
                            if (!string.IsNullOrEmpty(response.refresh_token))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh_token);
                            }
                            PlayerPrefs.Save();

                            TD.Info(TAG, "[CreateAccountInternal] Account creation successful, token stored", LoginManager.current);
                            EventHandler.Execute("OnAccountCreated");
                        }
                        else if (!string.IsNullOrEmpty(response.token)) // Fallback to old format
                        {
                            TD.Info(TAG, "[CreateAccountInternal] Token received (old token format)", LoginManager.current);
                            PlayerPrefs.SetString("jwt_token", response.token);
                            if (!string.IsNullOrEmpty(response.refresh))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh);
                            }
                            PlayerPrefs.Save();

                            TD.Info(TAG, "[CreateAccountInternal] Account creation successful, token stored (old format)", LoginManager.current);
                            // Try parsing raw text manually to check for email confirmation flag
                            try
                            {
                                if (www.downloadHandler.text.Contains("\"email_sent\""))
                                {
                                    // Crude match to avoid full JSON parse
                                    bool emailSent = www.downloadHandler.text.Contains("\"email_sent\":true");
                                    if (emailSent)
                                    {
                                        TD.Info(TAG, "[CreateAccountInternal] ✅ Confirmation email was sent successfully.", LoginManager.current);
                                    }
                                    else
                                    {
                                        TD.Warning(TAG, "[CreateAccountInternal] ⚠️ 'email_sent' key present but value is false.", LoginManager.current);
                                    }
                                }
                                else
                                {
                                    TD.Warning(TAG, "[CreateAccountInternal] ⚠️ 'email_sent' key not found in response.", LoginManager.current);
                                }
                            }
                            catch (Exception ex)
                            {
                                TD.Warning(TAG, $"[CreateAccountInternal] ⚠️ Failed to verify email_sent flag: {ex.Message}", LoginManager.current);
                            }




                            EventHandler.Execute("OnAccountCreated");
                        }
                        else
                        {
                            TD.Warning(TAG, "[CreateAccountInternal] No token in response", LoginManager.current);
                            // Try to manually parse the JSON to look for differently named token field
                            bool tokenParsed = TryParseTokenManually(www.downloadHandler.text, "CreateAccount");
                            if (!tokenParsed)
                            {
                                TD.Error(TAG, "[CreateAccountInternal] Failed to parse token manually", LoginManager.current);
                                EventHandler.Execute("OnFailedToCreateAccount");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TD.Error(TAG, $"[CreateAccountInternal] JSON parse error: {ex.Message}", LoginManager.current);
                        TD.Error(TAG, $"[CreateAccountInternal] Response was: {www.downloadHandler.text}", LoginManager.current);
                        // Try to manually parse the JSON as a last resort
                        bool tokenParsed = TryParseTokenManually(www.downloadHandler.text, "CreateAccount");
                        if (!tokenParsed)
                        {
                            TD.Error(TAG, "[CreateAccountInternal] Failed to parse token manually", LoginManager.current);
                            EventHandler.Execute("OnFailedToCreateAccount");
                        }
                    }
                }
            }
            TD.Verbose(TAG, "[CreateAccountInternal] Method completed", LoginManager.current);
        }

        // Helper method to try to extract token from JSON response even if JsonUtility fails
        private static bool TryParseTokenManually(string jsonText, string operation)
        {
            // Simple string-based parsing as a last resort
            bool success = false;

            try
            {
                TD.Verbose(TAG, $"[{operation}] Attempting manual token extraction", LoginManager.current);

                // Format 1: "access_token":"value"
                int tokenIndex = jsonText.IndexOf("\"access_token\":");
                if (tokenIndex >= 0)
                {
                    int startIndex = jsonText.IndexOf('"', tokenIndex + 14) + 1;
                    int endIndex = jsonText.IndexOf('"', startIndex);
                    if (startIndex > 0 && endIndex > startIndex)
                    {
                        string token = jsonText.Substring(startIndex, endIndex - startIndex);
                        PlayerPrefs.SetString("jwt_token", token);

                        // Try to find refresh token too
                        int refreshIndex = jsonText.IndexOf("\"refresh_token\":");
                        if (refreshIndex >= 0)
                        {
                            int refreshStart = jsonText.IndexOf('"', refreshIndex + 16) + 1;
                            int refreshEnd = jsonText.IndexOf('"', refreshStart);
                            if (refreshStart > 0 && refreshEnd > refreshStart)
                            {
                                string refresh = jsonText.Substring(refreshStart, refreshEnd - refreshStart);
                                PlayerPrefs.SetString("refresh_token", refresh);
                            }
                        }

                        PlayerPrefs.Save();
                        TD.Info(TAG, $"[{operation}] Successfully extracted token manually", LoginManager.current);
                        success = true;
                    }
                }
                // Format 2: "token":"value" (old format)
                else
                {
                    tokenIndex = jsonText.IndexOf("\"token\":");
                    if (tokenIndex >= 0)
                    {
                        int startIndex = jsonText.IndexOf('"', tokenIndex + 8) + 1;
                        int endIndex = jsonText.IndexOf('"', startIndex);
                        if (startIndex > 0 && endIndex > startIndex)
                        {
                            string token = jsonText.Substring(startIndex, endIndex - startIndex);
                            PlayerPrefs.SetString("jwt_token", token);

                            // Try to find refresh token too
                            int refreshIndex = jsonText.IndexOf("\"refresh\":");
                            if (refreshIndex >= 0)
                            {
                                int refreshStart = jsonText.IndexOf('"', refreshIndex + 10) + 1;
                                int refreshEnd = jsonText.IndexOf('"', refreshStart);
                                if (refreshStart > 0 && refreshEnd > refreshStart)
                                {
                                    string refresh = jsonText.Substring(refreshStart, refreshEnd - refreshStart);
                                    PlayerPrefs.SetString("refresh_token", refresh);
                                }
                            }

                            PlayerPrefs.Save();
                            TD.Info(TAG, $"[{operation}] Successfully extracted token manually (old format)", LoginManager.current);
                            success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TD.Error(TAG, $"[{operation}] Error while trying to manually parse token: {ex.Message}", LoginManager.current);
            }

            if (success)
            {
                // Trigger success event
                if (operation == "CreateAccount")
                    EventHandler.Execute("OnAccountCreated");
                else if (operation == "LoginAccount")
                    EventHandler.Execute("OnLogin");
                else if (operation == "RefreshToken")
                    EventHandler.Execute("OnTokenRefreshed");
            }

            return success;
        }

        /// <summary>
        /// Logins the account.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        public static void LoginAccount(string username, string password)
        {
            if (LoginManager.current != null)
            {
                TD.Info(TAG, $"Attempting login for user: {username}", LoginManager.current);
                LoginManager.current.StartCoroutine(LoginAccountInternal(username, password));
            }
            else
            {
                TD.Error(TAG, "Cannot login - LoginManager is null", null);
            }
        }

        private static IEnumerator LoginAccountInternal(string username, string password)
        {
            if (LoginManager.Configurations == null)
            {
                TD.Error(TAG, "Login failed - Configurations not found", LoginManager.current);
                EventHandler.Execute("OnFailedToLogin");
                yield break;
            }

            TD.Verbose(TAG, $"[LoginAccount] Trying to login using username: {username}", LoginManager.current);

            // Prepare JSON payload - manually construct to avoid JsonUtility issues with anonymous types
            var uri = GetEndpoint(LoginManager.Server.login);
            TD.Verbose(TAG, $"[LoginAccount] Using endpoint: {uri}", LoginManager.current);

            string payload = $"{{\"username\":\"{username}\",\"password\":\"{password}\"}}";

            using (var www = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                TD.Verbose(TAG, "[LoginAccount] Sending login request...", LoginManager.current);
                yield return www.SendWebRequest();
                TD.Verbose(TAG, $"[LoginAccount] Request completed with code: {www.responseCode}", LoginManager.current);

                if (IsRequestFailed(www))
                {
                    TD.Error(TAG, $"[LoginAccount] Error: {www.error}", LoginManager.current);
                    TD.Error(TAG, $"[LoginAccount] Response body: {www.downloadHandler.text}", LoginManager.current);
                    EventHandler.Execute("OnFailedToLogin");
                }
                else
                {
                    // Log the full response for debugging
                    TD.Verbose(TAG, $"[LoginAccount] Full response: {www.downloadHandler.text}", LoginManager.current);

                    try
                    {
                        var response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        TD.Verbose(TAG, "[LoginAccount] Response parsed successfully", LoginManager.current);

                        // Check for error
                        if (!string.IsNullOrEmpty(response.error))
                        {
                            TD.Warning(TAG, $"[LoginAccount] Server error: {response.error}", LoginManager.current);
                            EventHandler.Execute("OnFailedToLogin");
                            yield break;
                        }

                        // Valid token = success
                        // Check for both old (token) and new (access_token) property names
                        if (!string.IsNullOrEmpty(response.access_token))
                        {
                            // Store the username using the server's account key (for compatibility)
                            PlayerPrefs.SetString(LoginManager.Server.accountKey, username);

                            // Store JWT tokens
                            PlayerPrefs.SetString("jwt_token", response.access_token);
                            if (!string.IsNullOrEmpty(response.refresh_token))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh_token);
                            }
                            PlayerPrefs.Save();

                            // Update the server URL in PlayerPrefs for other systems
                            PlayerPrefs.SetString("ServerBaseUrl", LoginManager.Server.serverAddress);

                            TD.Info(TAG, "[LoginAccount] Login successful, token stored", LoginManager.current);
                            EventHandler.Execute("OnLogin");
                        }
                        else if (!string.IsNullOrEmpty(response.token)) // Fallback to old format
                        {
                            // Store the username using the server's account key (for compatibility)
                            PlayerPrefs.SetString(LoginManager.Server.accountKey, username);

                            // Store JWT tokens
                            PlayerPrefs.SetString("jwt_token", response.token);
                            if (!string.IsNullOrEmpty(response.refresh))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh);
                            }
                            PlayerPrefs.Save();

                            // Update the server URL in PlayerPrefs for other systems
                            PlayerPrefs.SetString("ServerBaseUrl", LoginManager.Server.serverAddress);

                            TD.Info(TAG, "[LoginAccount] Login successful, token stored (old format)", LoginManager.current);
                            EventHandler.Execute("OnLogin");
                        }
                        else
                        {
                            TD.Warning(TAG, "[LoginAccount] No token in response", LoginManager.current);
                            // Try to manually parse the JSON to look for differently named token field
                            TryParseTokenManually(www.downloadHandler.text, "LoginAccount");
                            EventHandler.Execute("OnFailedToLogin");
                        }
                    }
                    catch (Exception ex)
                    {
                        TD.Error(TAG, $"[LoginAccount] JSON parse error: {ex.Message}", LoginManager.current);
                        TD.Error(TAG, $"Response was: {www.downloadHandler.text}", LoginManager.current);
                        // Try to manually parse the JSON as a last resort
                        TryParseTokenManually(www.downloadHandler.text, "LoginAccount");
                        EventHandler.Execute("OnFailedToLogin");
                    }
                }
            }
        }

        /// <summary>
        /// Recovers the password.
        /// </summary>
        /// <param name="email">Email.</param>
        public static void RecoverPassword(string email)
        {
            if (LoginManager.current != null)
            {
                TD.Info(TAG, $"Starting password recovery for email: {email}", LoginManager.current);
                LoginManager.current.StartCoroutine(RecoverPasswordInternal(email));
            }
            else
            {
                TD.Error(TAG, "Cannot recover password - LoginManager is null", null);
            }
        }

        private static IEnumerator RecoverPasswordInternal(string email)
        {
            if (LoginManager.Configurations == null)
            {
                TD.Error(TAG, "Recover password failed - Configurations not found", LoginManager.current);
                EventHandler.Execute("OnFailedToRecoverPassword");
                yield break;
            }

            TD.Verbose(TAG, $"[RecoverPassword] Trying to recover password using email: {email}", LoginManager.current);

            // Prepare JSON payload - manually construct to avoid JsonUtility issues with anonymous types
            var uri = GetEndpoint(LoginManager.Server.recoverPassword);
            TD.Verbose(TAG, $"[RecoverPassword] Using endpoint: {uri}", LoginManager.current);

            string payload = $"{{\"email\":\"{email}\"}}";

            using (var www = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                TD.Verbose(TAG, "[RecoverPassword] Sending password recovery request...", LoginManager.current);
                yield return www.SendWebRequest();
                TD.Verbose(TAG, $"[RecoverPassword] Request completed with code: {www.responseCode}", LoginManager.current);

                if (IsRequestFailed(www))
                {
                    TD.Error(TAG, $"[RecoverPassword] Error: {www.error}", LoginManager.current);
                    TD.Error(TAG, $"[RecoverPassword] Response body: {www.downloadHandler.text}", LoginManager.current);
                    EventHandler.Execute("OnFailedToRecoverPassword");
                }
                else
                {
                    try
                    {
                        // For password reset requests, your server sends a message success response
                        // Parse the response to check for success
                        var responseData = JsonUtility.FromJson<MessageResponse>(www.downloadHandler.text);
                        TD.Verbose(TAG, $"[RecoverPassword] Response parsed: {www.downloadHandler.text}", LoginManager.current);

                        // Check for error
                        if (!string.IsNullOrEmpty(responseData.error))
                        {
                            TD.Warning(TAG, $"[RecoverPassword] Server error: {responseData.error}", LoginManager.current);
                            EventHandler.Execute("OnFailedToRecoverPassword");
                            yield break;
                        }

                        // Success case - message exists
                        if (!string.IsNullOrEmpty(responseData.message))
                        {
                            TD.Info(TAG, "[RecoverPassword] Password recovery email sent successfully", LoginManager.current);
                            EventHandler.Execute("OnPasswordRecovered");
                        }
                        else
                        {
                            TD.Warning(TAG, "[RecoverPassword] No success message in response", LoginManager.current);
                            EventHandler.Execute("OnFailedToRecoverPassword");
                        }
                    }
                    catch (Exception ex)
                    {
                        TD.Error(TAG, $"[RecoverPassword] JSON parse error: {ex.Message}", LoginManager.current);

                        // If JSON parsing fails, try the legacy approach as fallback
                        if (www.downloadHandler.text.Contains("true") || www.downloadHandler.text.Contains("message"))
                        {
                            TD.Info(TAG, "[RecoverPassword] Password recovery email sent successfully (legacy detection)", LoginManager.current);
                            EventHandler.Execute("OnPasswordRecovered");
                        }
                        else
                        {
                            TD.Error(TAG, $"Response was: {www.downloadHandler.text}", LoginManager.current);
                            EventHandler.Execute("OnFailedToRecoverPassword");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Resets the password.
        /// </summary>
        /// <param name="token">The reset token received via email.</param>
        /// <param name="password">The new password to set.</param>
        public static void ResetPassword(string token, string password)
        {
            if (LoginManager.current != null)
            {
                TD.Info(TAG, "Starting password reset with token", LoginManager.current);
                LoginManager.current.StartCoroutine(ResetPasswordInternal(token, password));
            }
            else
            {
                TD.Error(TAG, "Cannot reset password - LoginManager is null", null);
            }
        }

        private static IEnumerator ResetPasswordInternal(string token, string password)
        {
            if (LoginManager.Configurations == null)
            {
                TD.Error(TAG, "Reset password failed - Configurations not found", LoginManager.current);
                EventHandler.Execute("OnFailedToResetPassword");
                yield break;
            }

            TD.Verbose(TAG, "[ResetPassword] Trying to reset password with token", LoginManager.current);

            // Prepare JSON payload - manually construct to avoid JsonUtility issues with anonymous types
            var uri = GetEndpoint(LoginManager.Server.resetPassword);
            TD.Verbose(TAG, $"[ResetPassword] Using endpoint: {uri}", LoginManager.current);

            string payload = $"{{\"token\":\"{token}\",\"new_password\":\"{password}\"}}";

            using (var www = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                TD.Verbose(TAG, "[ResetPassword] Sending password reset request...", LoginManager.current);
                yield return www.SendWebRequest();
                TD.Verbose(TAG, $"[ResetPassword] Request completed with code: {www.responseCode}", LoginManager.current);

                if (IsRequestFailed(www))
                {
                    TD.Error(TAG, $"[ResetPassword] Error: {www.error}", LoginManager.current);
                    TD.Error(TAG, $"[ResetPassword] Response body: {www.downloadHandler.text}", LoginManager.current);
                    EventHandler.Execute("OnFailedToResetPassword");
                }
                else
                {
                    try
                    {
                        // For password reset, your server sends a success message
                        var responseData = JsonUtility.FromJson<MessageResponse>(www.downloadHandler.text);
                        TD.Verbose(TAG, $"[ResetPassword] Response parsed: {www.downloadHandler.text}", LoginManager.current);

                        // Check for error
                        if (!string.IsNullOrEmpty(responseData.error))
                        {
                            TD.Warning(TAG, $"[ResetPassword] Server error: {responseData.error}", LoginManager.current);
                            EventHandler.Execute("OnFailedToResetPassword");
                            yield break;
                        }

                        // Success case - message exists
                        if (!string.IsNullOrEmpty(responseData.message))
                        {
                            TD.Info(TAG, "[ResetPassword] Password reset successful", LoginManager.current);
                            EventHandler.Execute("OnPasswordResetted");
                        }
                        else
                        {
                            TD.Warning(TAG, "[ResetPassword] No success message in response", LoginManager.current);
                            EventHandler.Execute("OnFailedToResetPassword");
                        }
                    }
                    catch (Exception ex)
                    {
                        TD.Error(TAG, $"[ResetPassword] JSON parse error: {ex.Message}", LoginManager.current);

                        // If JSON parsing fails, try the legacy approach as fallback
                        if (www.downloadHandler.text.Contains("true") || www.downloadHandler.text.Contains("message"))
                        {
                            TD.Info(TAG, "[ResetPassword] Password reset successful (legacy detection)", LoginManager.current);
                            EventHandler.Execute("OnPasswordResetted");
                        }
                        else
                        {
                            TD.Error(TAG, $"[ResetPassword] Response was: {www.downloadHandler.text}", LoginManager.current);
                            EventHandler.Execute("OnFailedToResetPassword");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validates the email.
        /// </summary>
        /// <returns><c>true</c>, if email was validated, <c>false</c> otherwise.</returns>
        /// <param name="email">Email.</param>
        public static bool ValidateEmail(string email)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$");
            System.Text.RegularExpressions.Match match = regex.Match(email);

            if (match.Success)
            {
                TD.Verbose(TAG, $"Email validation was successful for email: {email}", LoginManager.current);
            }
            else
            {
                TD.Warning(TAG, $"Email validation failed for email: {email}", LoginManager.current);
            }

            return match.Success;
        }

        /// <summary>
        /// Refreshes the JWT token using the stored refresh token
        /// </summary>
        public static void RefreshToken()
        {
            if (LoginManager.current != null)
            {
                string refreshToken = GetRefreshToken();
                if (!string.IsNullOrEmpty(refreshToken))
                {
                    TD.Info(TAG, "Starting token refresh process", LoginManager.current);
                    LoginManager.current.StartCoroutine(RefreshTokenInternal(refreshToken));
                }
                else
                {
                    TD.Warning(TAG, "No refresh token available to refresh the session", LoginManager.current);
                    EventHandler.Execute("OnSessionExpired");
                }
            }
            else
            {
                TD.Error(TAG, "Cannot refresh token - LoginManager is null", null);
            }
        }

        private static IEnumerator RefreshTokenInternal(string refreshToken)
        {
            if (LoginManager.Configurations == null)
            {
                TD.Error(TAG, "Unable to refresh token: configurations not found", LoginManager.current);
                EventHandler.Execute("OnSessionExpired");
                yield break;
            }

            TD.Verbose(TAG, "[RefreshToken] Attempting to refresh authentication token", LoginManager.current);

            // Prepare JSON payload
            var uri = GetEndpoint(LoginManager.Server.refreshToken);
            TD.Verbose(TAG, $"[RefreshToken] Using endpoint: {uri}", LoginManager.current);

            string payload = $"{{\"refresh_token\":\"{refreshToken}\"}}";

            using (var www = new UnityWebRequest(uri, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(payload);
                www.uploadHandler = new UploadHandlerRaw(bodyRaw);
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                TD.Verbose(TAG, "[RefreshToken] Sending token refresh request...", LoginManager.current);
                yield return www.SendWebRequest();
                TD.Verbose(TAG, $"[RefreshToken] Request completed with code: {www.responseCode}", LoginManager.current);

                if (IsRequestFailed(www))
                {
                    TD.Error(TAG, $"[RefreshToken] Error: {www.error}", LoginManager.current);
                    TD.Error(TAG, $"[RefreshToken] Response body: {www.downloadHandler.text}", LoginManager.current);
                    EventHandler.Execute("OnSessionExpired");
                }
                else
                {
                    // Log the full response for debugging
                    TD.Verbose(TAG, $"[RefreshToken] Full response: {www.downloadHandler.text}", LoginManager.current);

                    try
                    {
                        var response = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
                        TD.Verbose(TAG, "[RefreshToken] Response parsed successfully", LoginManager.current);

                        // Check for error
                        if (!string.IsNullOrEmpty(response.error))
                        {
                            TD.Warning(TAG, $"[RefreshToken] Server error: {response.error}", LoginManager.current);
                            EventHandler.Execute("OnSessionExpired");
                            yield break;
                        }

                        // Valid token = success
                        // Check for both old (token) and new (access_token) property names
                        if (!string.IsNullOrEmpty(response.access_token))
                        {
                            // Store JWT tokens
                            PlayerPrefs.SetString("jwt_token", response.access_token);
                            if (!string.IsNullOrEmpty(response.refresh_token))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh_token);
                            }

                            // Set flag for other systems
                            PlayerPrefs.SetString("jwt_token_refreshed", "true");

                            PlayerPrefs.Save();

                            TD.Info(TAG, "[RefreshToken] Token refreshed successfully", LoginManager.current);
                            EventHandler.Execute("OnTokenRefreshed");
                        }
                        else if (!string.IsNullOrEmpty(response.token)) // Fallback to old format
                        {
                            // Store JWT tokens
                            PlayerPrefs.SetString("jwt_token", response.token);
                            if (!string.IsNullOrEmpty(response.refresh))
                            {
                                PlayerPrefs.SetString("refresh_token", response.refresh);
                            }

                            // Set flag for other systems
                            PlayerPrefs.SetString("jwt_token_refreshed", "true");

                            PlayerPrefs.Save();

                            TD.Info(TAG, "[RefreshToken] Token refreshed successfully (old format)", LoginManager.current);
                            EventHandler.Execute("OnTokenRefreshed");
                        }
                        else
                        {
                            TD.Warning(TAG, "[RefreshToken] No token in response", LoginManager.current);
                            // Try to manually parse the JSON to look for differently named token field
                            bool tokenParsed = TryParseTokenManually(www.downloadHandler.text, "RefreshToken");
                            if (!tokenParsed)
                            {
                                TD.Error(TAG, "[RefreshToken] Failed to parse token manually", LoginManager.current);
                                EventHandler.Execute("OnSessionExpired");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        TD.Error(TAG, $"[RefreshToken] JSON parse error: {ex.Message}", LoginManager.current);
                        TD.Error(TAG, $"Response was: {www.downloadHandler.text}", LoginManager.current);
                        // Try to manually parse the JSON as a last resort
                        bool tokenParsed = TryParseTokenManually(www.downloadHandler.text, "RefreshToken");
                        if (!tokenParsed)
                        {
                            TD.Error(TAG, "[RefreshToken] Failed to parse token manually", LoginManager.current);
                            EventHandler.Execute("OnSessionExpired");
                        }
                    }
                }
            }
        }
    }
}