// Filename: FlowManager.cs

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



    // UI and window references (auto-assigned)

    [HideInInspector] public GameObject loginPanel, registrationPanel, recoverPasswordPanel, serverSelectionPanel;

    [HideInInspector] public CharacterWindow characterWindow;

    [HideInInspector] public GameObject characterPanel;

    [HideInInspector] public CharacterWindowBridge characterWindowBridge;

    [HideInInspector] public ServerWindow serverWindow;

    [HideInInspector] public GameObject splashScreenUI, startMenuUI;



    [Header("Player Account (set after login)")]

    public Coherence.Cloud.PlayerAccount playerAccount;



    // Singleton setup

    private void Awake()

    {

        if (Instance != null && Instance != this)

        {

            Destroy(this.gameObject);

            return;

        }

        Instance = this;

        DontDestroyOnLoad(this.gameObject);

    }

    private void Start() { RegisterSceneObjects(SceneManager.GetActiveScene().name); }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }

    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)

    {

        RegisterSceneObjects(scene.name);

    }



    private void RegisterSceneObjects(string sceneName)

    {

        loginPanel = registrationPanel = recoverPasswordPanel = serverSelectionPanel = null;

        characterWindow = null; characterPanel = null; characterWindowBridge = null; serverWindow = null;

        splashScreenUI = null; startMenuUI = null;



        if (sceneName == bootstrapScene)

            splashScreenUI = GameObject.FindWithTag("SplashScreenUI") ?? GameObject.Find("SplashScreenUI");

        else if (sceneName == startScene)

            startMenuUI = GameObject.FindWithTag("StartMenuUI") ?? GameObject.Find("StartMenuUI");

        else if (sceneName == loginScene)

        {

            loginPanel = GameObject.Find("LoginPanel");

            registrationPanel = GameObject.Find("RegistrationPanel");

            recoverPasswordPanel = GameObject.Find("RecoverPasswordPanel");

            serverSelectionPanel = GameObject.Find("ServerSelectionPanel");

            // Assign serverWindow if it's in the login scene

            serverWindow = FindObjectOfType<ServerWindow>(true);

            if (serverWindow != null) serverWindow.SetFlowManager(this);

        }

        else if (sceneName == selectCharacterScene)

        {

            characterWindow = FindObjectOfType<CharacterWindow>(true);

            characterPanel = GameObject.Find("CharacterPanel");

            characterWindowBridge = FindObjectOfType<CharacterWindowBridge>(true);

            serverWindow = FindObjectOfType<ServerWindow>(true);

            if (serverWindow != null) serverWindow.SetFlowManager(this);

        }

        TD.Info("FlowManager", $"Registered scene objects for '{sceneName}'");

    }



    // Scene navigation

    public void ShowSplashScreen() => SceneManager.LoadScene(bootstrapScene);

    public void ShowStart() => SceneManager.LoadScene(startScene);

    public void ShowLoginScene() => SceneManager.LoadScene(loginScene);

    public void ShowCharacterSelectScene() => SceneManager.LoadScene(selectCharacterScene);

    public void ShowCreateCharacterScene() => SceneManager.LoadScene(createCharacterScene);

    public void ShowMainGame() => SceneManager.LoadScene(mainScene);



    // Panel visibility

    public void ShowLoginPanel()

    {

        if (loginPanel != null) loginPanel.SetActive(true);

        if (registrationPanel != null) registrationPanel.SetActive(false);

        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);

        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);

    }

    public void ShowRegistrationPanel()

    {

        if (loginPanel != null) loginPanel.SetActive(false);

        if (registrationPanel != null) registrationPanel.SetActive(true);

        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);

        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);

    }

    public void ShowRecoverPasswordPanel()

    {

        if (loginPanel != null) loginPanel.SetActive(false);

        if (registrationPanel != null) registrationPanel.SetActive(false);

        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(true);

        if (serverSelectionPanel != null) serverSelectionPanel.SetActive(false);

    }

    public void ShowServerSelectionPanel()

    {

        if (loginPanel != null) loginPanel.SetActive(false);

        if (registrationPanel != null) registrationPanel.SetActive(false);

        if (recoverPasswordPanel != null) recoverPasswordPanel.SetActive(false);

        if (serverSelectionPanel != null)

        {

            serverSelectionPanel.SetActive(true);

            // ServerWindow handles UI button events; FlowManager only supplies data

            if (serverWindow != null) serverWindow.RefreshServerList();

        }

    }



    public void ShowCharacterPanel()

    {

        if (characterPanel != null) characterPanel.SetActive(true);

        if (serverWindow != null) serverWindow.gameObject.SetActive(false);

    }



    // API logic—these are called by ServerWindow, never by UI events directly

    public async void RequestServerList()

    {

        TD.Info("FlowManager", "[RequestServerList] Fetching server list from Coherence...");

        if (playerAccount == null)

        {

            TD.Error("FlowManager", "[RequestServerList] PlayerAccount is null! Cannot fetch servers.");

            if (serverWindow != null) serverWindow.PopulateServerList(new List<ServerInfo>()); // clear out

            return;

        }

        var servers = await CoherenceWorldBridge.ListWorldsAsync(playerAccount);

        TD.Info("FlowManager", $"[RequestServerList] Worlds fetched: {servers.Count}");

        if (serverWindow != null) serverWindow.PopulateServerList(servers);

    }



    public void OnServerSelected(string serverId, string serverName)

    {

        TD.Info("FlowManager", $"[OnServerSelected] Server picked: {serverName} ({serverId})");



        // ✅ FIX: Properly notify ServerWorldEvents system

        ServerWorldEvents.SetCurrentWorld(serverId, serverName);



        // ✅ FIX: Also set the world context in DevionGamesAdapter

        DevionGamesAdapter.SetWorldContext(serverId);



        // Keep existing PlayerPrefs for backward compatibility

        PlayerPrefs.SetString("selected_server", serverId);

        PlayerPrefs.SetString("selected_server_name", serverName);



        TD.Info("FlowManager", $"World context set: {serverId}");



        ShowCharacterSelectScene();

    }



    public void LogoutToServerSelect()

    {

        DVGApiBridge.SetToken(""); DVGApiBridge.SetRefresh("");

        ShowServerSelectionPanel();

    }



    // --- Character list logic (for character select scene) ---

    public async void PopulateCharacters(string worldKey)

    {

        var chars = await DVGApiBridge.GetCharacters(worldKey);

        if (characterWindowBridge != null)

        {

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



        // ✅ Extract the values from DTO and pass as strings to avoid assembly reference issues

        string characterId = character.CharacterId ?? character.CharacterName; // Fallback if ID is null

        string characterName = character.CharacterName;



        // Set character context with both values

        DevionGamesAdapter.SetCharacterContext(characterId, characterName);

        DevionGamesAdapter.SetAuthToken(DVGApiBridge.GetToken());



        TD.Info("FlowManager", $"Character context set: ID={characterId}, Name={characterName}");



        // TODO: Load into world, spawn player, etc.

    }



    public async void OnCharacterDelete(CharacterDTO character)

    {

        string worldKey = PlayerPrefs.GetString("selected_server", "");

        await DVGApiBridge.DeleteCharacter(worldKey, character.CharacterId);

        PopulateCharacters(worldKey);

    }



    // Util for finding objects (for Unity 2022+ compatibility)

    private new T FindObjectOfType<T>(bool includeInactive) where T : Object

    {

#if UNITY_2022_1_OR_NEWER

        var objs = Object.FindObjectsByType<T>(

            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,

            FindObjectsSortMode.None

        );

        return objs.Length > 0 ? objs[0] : null;

#else

        var objs = Object.FindObjectsOfType<T>(includeInactive);

        return objs.Length > 0 ? objs[0] : null;

#endif

    }

}
