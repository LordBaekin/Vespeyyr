using UnityEngine;
using DevionGames.LoginSystem.Configuration;
using DevionGames.UIWidgets;
using DevionGames.LoginSystem;
using TagDebugSystem;

public class SceneUIManager : MonoBehaviour
{
    public static SceneUIManager Instance { get; private set; }

    private const string TAG = Tags.UI;

    [Header("Drag your in-scene windows & widgets here")]
    public LoginWindow loginWindow;
    public RegistrationWindow registrationWindow;
    public RecoverPasswordWindow recoverPasswordWindow;
    public DialogBox dialogBox;
    public Tooltip tooltip;

    [Header("Panels for login scene only")]
    public GameObject loginPanel;
    public GameObject registrationPanel;
    public GameObject recoverPasswordPanel;
    public GameObject serverPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            TD.Warning(TAG, "Multiple SceneUIManager instances detected. Destroying duplicate.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TD.Info(TAG, "SceneUIManager initialized", this);
        ValidateReferences();
    }

    private void ValidateReferences()
    {
        if (loginWindow == null) TD.Warning(TAG, "LoginWindow reference is missing", this);
        if (registrationWindow == null) TD.Warning(TAG, "RegistrationWindow reference is missing", this);
        if (recoverPasswordWindow == null) TD.Warning(TAG, "RecoverPasswordWindow reference is missing", this);
        if (dialogBox == null) TD.Warning(TAG, "DialogBox reference is missing", this);
        if (tooltip == null) TD.Warning(TAG, "Tooltip reference is missing", this);

        if (loginPanel == null) TD.Warning(TAG, "LoginPanel reference is missing", this);
        if (registrationPanel == null) TD.Warning(TAG, "RegistrationPanel reference is missing", this);
        if (recoverPasswordPanel == null) TD.Warning(TAG, "RecoverPasswordPanel reference is missing", this);
        if (serverPanel == null) TD.Warning(TAG, "ServerPanel reference is missing", this);
    }
}
