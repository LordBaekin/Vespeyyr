// FlowManager.cs with full TagDebugSystem logging integrated
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Vespeyr.Network;
using Assets.Scripts.Data;
using TagDebugSystem;
using DevionGames.CharacterSystem;
using DevionGames.LoginSystem;
using Coherence.Cloud;

public class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    [System.Serializable]
    public class SceneReference { public string sceneName; }

    [Header("Scene Names (editable in Inspector, add/remove as needed)")]
    public List<SceneReference> scenes = new List<SceneReference>();
    [Header("Scene Role Names")]
    public string bootstrapScene = "Bootstrap";
    public string startScene = "Start";
    public string loginScene = "Login";
    public string selectCharacterScene = "Select Character";
    public string createCharacterScene = "Create Character";
    public string mainScene = "Main Scene";

    [HideInInspector] public GameObject loginPanel, registrationPanel, recoverPasswordPanel, serverSelectionPanel;
    [HideInInspector] public CharacterWindow characterWindow;
    [HideInInspector] public GameObject characterPanel;
    [HideInInspector] public CharacterWindowBridge characterWindowBridge;
    [HideInInspector] public ServerWindow serverWindow;
    [HideInInspector] public GameObject splashScreenUI, startMenuUI;

    [Header("Player Account (set after login)")]
    public PlayerAccount playerAccount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        TD.Info("FlowManager", "Instance created and set persistent");
    }

    private void Start()
    {
        TD.Info("FlowManager", "Calling RegisterSceneObjects from Start");
        RegisterSceneObjects(SceneManager.GetActiveScene().name);
    }

    private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RegisterSceneObjects(scene.name);

    private void RegisterSceneObjects(string sceneName)
    {
        loginPanel = registrationPanel = recoverPasswordPanel = serverSelectionPanel = null;
        characterWindow = null; characterPanel = null; characterWindowBridge = null; serverWindow = null;
        splashScreenUI = null; startMenuUI = null;

        TD.Info("FlowManager", $"Registering scene objects for: {sceneName}");

        if (sceneName == bootstrapScene)
            splashScreenUI = GameObject.FindWithTag("SplashScreenUI") ?? GameObject.Find("SplashScreenUI");
        else if (sceneName == startScene)
            startMenuUI = GameObject.FindWithTag("StartMenuUI") ?? GameObject.Find("StartMenuUI");
        else if (sceneName == loginScene)
        {
            loginPanel = GameObject.Find("Login UI");
            registrationPanel = GameObject.Find("RegistrationUI");
            recoverPasswordPanel = GameObject.Find("RecoverPasswordUI");
            serverSelectionPanel = GameObject.Find("ServerUI");
            serverWindow = FindObjectOfType<ServerWindow>(true);
            if (serverWindow != null) serverWindow.SetFlowManager(this);
        }
        else if (sceneName == selectCharacterScene)
        {
            characterWindow = FindObjectOfType<CharacterWindow>(true);
            characterPanel = GameObject.Find("SelectCharacterUI");
            characterWindowBridge = FindObjectOfType<CharacterWindowBridge>(true);
            serverWindow = FindObjectOfType<ServerWindow>(true);
            if (serverWindow != null) serverWindow.SetFlowManager(this);
        }

        TD.Info("FlowManager", $"Registered scene objects for '{sceneName}'");
    }

    public void ShowSplashScreen() => SceneManager.LoadScene(bootstrapScene);
    public void ShowStart() => SceneManager.LoadScene(startScene);
    public void ShowLoginScene() => SceneManager.LoadScene(loginScene);
    public void ShowCharacterSelectScene() => SceneManager.LoadScene(selectCharacterScene);
    public void ShowCreateCharacterScene() => SceneManager.LoadScene(createCharacterScene);
    public void ShowMainGame() => SceneManager.LoadScene(mainScene);

    public void ShowLoginPanel()
    {
        TD.Info("FlowManager", "Showing login panel");
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registrationPanel != null) registrationPanel.SetActive(false);
        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);
        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);
    }

    public void ShowRegistrationPanel()
    {
        TD.Info("FlowManager", "Showing registration panel");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registrationPanel != null) registrationPanel.SetActive(true);
        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);
        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);
    }

    public void ShowRecoverPasswordPanel()
    {
        TD.Info("FlowManager", "Showing recover password panel");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registrationPanel != null) registrationPanel.SetActive(false);
        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(true);
        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);
    }

    public void ShowServerSelectionPanel()
    {
        TD.Info("FlowManager", "Showing server selection panel");
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registrationPanel != null) registrationPanel.SetActive(false);
        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);
        if (serverSelectionPanel != null)
        {
            serverSelectionPanel.SetActive(true);
            if (serverWindow != null) serverWindow.RefreshServerList();
        }
    }
    public void ShowOnly(GameObject target)
    {
        foreach (var panel in new[] { loginPanel, registrationPanel, recoverPasswordPanel, serverSelectionPanel })
        {
            if (panel != null) panel.SetActive(panel == target);
        }
    }
    public void ShowCharacterPanel()
    {
        TD.Info("FlowManager", "Showing character panel");
        if (characterPanel != null) characterPanel.SetActive(true);
        if (serverWindow != null) serverWindow.gameObject.SetActive(false);
    }

    public async void RequestServerList()
    {
        TD.Info("FlowManager", "[RequestServerList] Fetching server list from Coherence...");
        if (playerAccount == null)
        {
            TD.Error("FlowManager", "[RequestServerList] PlayerAccount is null! Cannot fetch servers.");
            if (serverWindow != null) serverWindow.PopulateServerList(new List<ServerInfo>());
            return;
        }

        var servers = await CoherenceWorldBridge.ListWorldsAsync(playerAccount);
        TD.Info("FlowManager", $"[RequestServerList] Worlds fetched: {servers.Count}");
        if (serverWindow != null) serverWindow.PopulateServerList(servers);
    }

    public void OnServerSelected(string serverId, string serverName)
    {
        TD.Info("FlowManager", $"[OnServerSelected] Server picked: {serverName} ({serverId})");
        ServerWorldEvents.SetCurrentWorld(serverId, serverName);
        DevionGamesAdapter.SetWorldContext(serverId);
        PlayerPrefs.SetString("selected_server", serverId);
        PlayerPrefs.SetString("selected_server_name", serverName);
        TD.Info("FlowManager", $"World context set: {serverId}");
        ShowCharacterSelectScene();
    }

    public void LogoutToServerSelect()
    {
        TD.Info("FlowManager", "[LogoutToServerSelect] Logging out and showing server list");
        DVGApiBridge.SetToken("");
        DVGApiBridge.SetRefresh("");
        DevionGamesAdapter.ClearCharacterContext();
        DevionGamesAdapter.ClearWorldContext();
        PlayerPrefs.DeleteKey("selected_server");
        PlayerPrefs.DeleteKey("selected_server_name");

        ShowServerSelectionPanel();
    }
    public void LogoutToCharacterSelect()
    {
        TD.Info("FlowManager", "[LogoutToCharacterSelect] Logging out to character selection screen.");

        // Clear character context only
        DevionGamesAdapter.ClearCharacterContext();

        // Retain world context and token, just reset character session
        PopulateCharacters(ServerWorldEvents.CurrentWorldKey);
        ShowCharacterSelectScene();
    }


    public void LogoutToLogin()
    {
        TD.Info("FlowManager", "[LogoutToLogin] Logging out fully to login scene.");

        // Clear token and session
        DVGApiBridge.SetToken("");
        DVGApiBridge.SetRefresh("");

        // Clear character and world context
        DevionGamesAdapter.ClearCharacterContext();
        DevionGamesAdapter.ClearWorldContext();
        PlayerPrefs.DeleteKey("selected_server");
        PlayerPrefs.DeleteKey("selected_server_name");

        // Clear PlayerAccount state
        playerAccount = null;

        // Return to login
        ShowLoginScene();
    }


    public async void PopulateCharacters(string worldKey)
    {
        TD.Info("FlowManager", $"[PopulateCharacters] Fetching characters for world: {worldKey}");
        var chars = await DVGApiBridge.GetCharacters(worldKey);
        if (characterWindowBridge != null)
        {
            TD.Info("FlowManager", $"[PopulateCharacters] Populating character list with {chars.Count} entries");
            characterWindowBridge.PopulateCharacterList(chars);
        }
        else
        {
            TD.Error("FlowManager", "CharacterWindowBridge reference missing on FlowManager!");
        }
    }

    public void OnCharacterSelected(CharacterDTO character)
    {
        TD.Info("FlowManager", $"Character selected: {character.CharacterName} ({character.CharacterId})");
        string characterId = character.CharacterId ?? character.CharacterName;
        string characterName = character.CharacterName;
        DevionGamesAdapter.SetCharacterContext(characterId, characterName);
        DevionGamesAdapter.SetAuthToken(DVGApiBridge.GetToken());
        TD.Info("FlowManager", $"Character context set: ID={characterId}, Name={characterName}");
        // Game loading logic happens next...
    }

    public async void OnCharacterDelete(CharacterDTO character)
    {
        string worldKey = PlayerPrefs.GetString("selected_server", "");
        TD.Info("FlowManager", $"[OnCharacterDelete] Deleting character {character.CharacterName} ({character.CharacterId}) from world {worldKey}");
        await DVGApiBridge.DeleteCharacter(worldKey, character.CharacterId);
        PopulateCharacters(worldKey);
    }

    private new T FindObjectOfType<T>(bool includeInactive) where T : Object
    {
#if UNITY_2022_1_OR_NEWER
        var objs = Object.FindObjectsByType<T>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        return objs.Length > 0 ? objs[0] : null;
#else
        var objs = Object.FindObjectsOfType<T>(includeInactive);
        return objs.Length > 0 ? objs[0] : null;
#endif
    }
}
