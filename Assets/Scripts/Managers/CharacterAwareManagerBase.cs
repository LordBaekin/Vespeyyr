using UnityEngine;
using System.Collections;
using TagDebugSystem;
using DevionGames; // ← ADD THIS LINE for EventHandler access

/// <summary>
/// Base class for managers that need character-aware save/load functionality.
/// Handles common patterns for character selection, world awareness, and data persistence.
/// Place this in your main Assembly-CSharp to avoid circular dependencies.
/// </summary>
public abstract class CharacterAwareManagerBase : MonoBehaviour
{
    [Header("Character Aware Manager")]
    public bool dontDestroyOnLoad = true;

    // Abstract properties that derived classes must implement
    protected abstract string ManagerName { get; }
    protected abstract string LogTag { get; }
    protected abstract bool UseServerPersistence { get; }
    protected abstract bool AutoSaveEnabled { get; }
    protected abstract float AutoSaveInterval { get; }

    // State tracking
    private bool isLoggedIn = false;
    private bool isWorldSelected = false;
    private bool isCharacterSelected = false;
    private string currentWorldKey = null;
    private string currentCharacterName = null;
    private bool autoSaveRunning = false;

    // Base URL for server operations
    protected static string serverBaseUrl = "http://localhost:5000/";

    protected virtual void Awake()
    {
        // Handle singleton behavior if needed
        HandleSingletonSetup();

        // Persist across scenes if enabled
        if (dontDestroyOnLoad)
        {
            if (transform.parent != null)
            {
                TD.Verbose(LogTag, $"{ManagerName} with DontDestroyOnLoad can't be a child transform. Unparenting.", this);
                transform.parent = null;
            }
            DontDestroyOnLoad(gameObject);
        }

        // Get server URL from PlayerPrefs
        string savedServerUrl = PlayerPrefs.GetString("ServerBaseUrl", null);
        if (!string.IsNullOrEmpty(savedServerUrl))
        {
            serverBaseUrl = savedServerUrl;
        }

        // Check current authentication state
        isLoggedIn = ServerWorldEvents.IsAuthenticated;
        currentWorldKey = ServerWorldEvents.CurrentWorldKey;
        isWorldSelected = !string.IsNullOrEmpty(currentWorldKey);
        currentCharacterName = SaveKeyManager.GetCurrentCharacterName();
        isCharacterSelected = !string.IsNullOrEmpty(currentCharacterName);

        // Register for events
        RegisterForEvents();

        TD.Info(LogTag, $"{ManagerName} initialized. Auth={isLoggedIn}, World={currentWorldKey ?? "<none>"}, Character={currentCharacterName ?? "<none>"}");

        // If we're already in a complete state, load data
        if (isLoggedIn && isCharacterSelected)
        {
            TD.Info(LogTag, $"Already authenticated and character selected, loading data for {currentCharacterName}");
            LoadForCurrentCharacter();
        }
        else if (isLoggedIn && isWorldSelected)
        {
            TD.Info(LogTag, $"Already authenticated and world selected, waiting for character selection");
        }
    }

    protected virtual void OnDestroy()
    {
        UnregisterFromEvents();
    }

    #region Event Management

    protected virtual void RegisterForEvents()
    {
        EventHandler.Register("OnLogin", OnLoginReady);
        EventHandler.Register<string>("OnWorldSelected", OnWorldSelected);
        EventHandler.Register<string>("OnCharacterSelected", OnCharacterSelected);
        EventHandler.Register("OnSessionExpired", OnSessionExpired);

        TD.Verbose(LogTag, $"{ManagerName} registered for character awareness events");
    }

    protected virtual void UnregisterFromEvents()
    {
        EventHandler.Unregister("OnLogin", OnLoginReady);
        EventHandler.Unregister<string>("OnWorldSelected", OnWorldSelected);
        EventHandler.Unregister<string>("OnCharacterSelected", OnCharacterSelected);
        EventHandler.Unregister("OnSessionExpired", OnSessionExpired);

        TD.Verbose(LogTag, $"{ManagerName} unregistered from events");
    }

    #endregion

    #region Event Handlers

    protected virtual void OnLoginReady()
    {
        TD.Info(LogTag, $"{ManagerName}: OnLoginReady called, authentication complete");
        isLoggedIn = true;

        // Start auto-save if enabled and not already running
        if (AutoSaveEnabled && !autoSaveRunning)
        {
            StartCoroutine(AutoSaveCoroutine());
        }

        // Don't load data yet - wait for character selection
    }

    protected virtual void OnWorldSelected(string worldKey)
    {
        currentWorldKey = worldKey;
        isWorldSelected = true;

        TD.Info(LogTag, $"{ManagerName}: World selected: {worldKey}");

        // Still don't load - wait for character selection
    }

    protected virtual void OnCharacterSelected(string characterName)
    {
        TD.Info(LogTag, $"{ManagerName}: Character selected: {characterName}, loading their data");

        // Update character context
        currentCharacterName = characterName;
        isCharacterSelected = true;
        SaveKeyManager.SetCurrentCharacter(characterName);

        // Now we can load character-specific data
        if (isLoggedIn && isWorldSelected)
        {
            LoadForCurrentCharacter();
        }
    }

    protected virtual void OnSessionExpired()
    {
        TD.Warning(LogTag, $"{ManagerName}: Session expired, switching to local storage");
        // Derived classes can override to handle server persistence changes
    }

    #endregion

    #region Character-Aware Save/Load

    /// <summary>
    /// Save data using the appropriate character-specific key
    /// </summary>
    public void SaveForCurrentCharacter()
    {
        if (!isCharacterSelected)
        {
            TD.Warning(LogTag, $"{ManagerName}: Cannot save - no character selected");
            return;
        }

        string saveKey = SaveKeyManager.GetCharacterSaveKey();
        TD.Verbose(LogTag, $"{ManagerName}: Saving with character key: {saveKey}");

        // Call the derived class implementation
        SaveWithKey(saveKey);
    }

    /// <summary>
    /// Load data using the appropriate character-specific key
    /// </summary>
    public void LoadForCurrentCharacter()
    {
        if (!isCharacterSelected)
        {
            TD.Warning(LogTag, $"{ManagerName}: Cannot load - no character selected");
            return;
        }

        string saveKey = SaveKeyManager.GetCharacterSaveKey();
        TD.Verbose(LogTag, $"{ManagerName}: Loading with character key: {saveKey}");

        // Call the derived class implementation
        LoadWithKey(saveKey);
    }

    /// <summary>
    /// Save data using the world-specific key (for data that's shared across characters in a world)
    /// </summary>
    public void SaveForCurrentWorld()
    {
        if (!isWorldSelected)
        {
            TD.Warning(LogTag, $"{ManagerName}: Cannot save - no world selected");
            return;
        }

        string saveKey = SaveKeyManager.GetWorldSaveKey();
        TD.Verbose(LogTag, $"{ManagerName}: Saving with world key: {saveKey}");

        SaveWithKey(saveKey);
    }

    /// <summary>
    /// Load data using the world-specific key
    /// </summary>
    public void LoadForCurrentWorld()
    {
        if (!isWorldSelected)
        {
            TD.Warning(LogTag, $"{ManagerName}: Cannot load - no world selected");
            return;
        }

        string saveKey = SaveKeyManager.GetWorldSaveKey();
        TD.Verbose(LogTag, $"{ManagerName}: Loading with world key: {saveKey}");

        LoadWithKey(saveKey);
    }

    #endregion

    #region Auto-Save

    private IEnumerator AutoSaveCoroutine()
    {
        autoSaveRunning = true;
        TD.Info(LogTag, $"{ManagerName}: Starting auto-save with {AutoSaveInterval}s interval");

        while (autoSaveRunning && AutoSaveEnabled)
        {
            yield return new WaitForSeconds(AutoSaveInterval);

            if (isCharacterSelected)
            {
                TD.Verbose(LogTag, $"{ManagerName}: Auto-save triggered");
                SaveForCurrentCharacter();
            }
        }

        TD.Info(LogTag, $"{ManagerName}: Auto-save stopped");
    }

    protected void StopAutoSave()
    {
        autoSaveRunning = false;
    }

    #endregion

    #region Abstract Methods - Derived Classes Must Implement

    /// <summary>
    /// Derived classes implement this to handle the actual save operation
    /// </summary>
    protected abstract void SaveWithKey(string saveKey);

    /// <summary>
    /// Derived classes implement this to handle the actual load operation
    /// </summary>
    protected abstract void LoadWithKey(string saveKey);

    /// <summary>
    /// Derived classes can override this to handle singleton setup
    /// </summary>
    protected virtual void HandleSingletonSetup()
    {
        // Default implementation does nothing
        // Derived classes can enforce singleton pattern here
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Get the JWT token for server operations
    /// </summary>
    protected static string GetAuthToken()
    {
        return PlayerPrefs.GetString("jwt_token", null);
    }

    /// <summary>
    /// Check if we should use server persistence
    /// </summary>
    protected bool ShouldUseServerPersistence()
    {
        return UseServerPersistence && !string.IsNullOrEmpty(GetAuthToken());
    }

    /// <summary>
    /// Get current state summary for debugging
    /// </summary>
    protected string GetStateInfo()
    {
        return $"Auth={isLoggedIn}, World={currentWorldKey ?? "<none>"}, Character={currentCharacterName ?? "<none>"}";
    }

    #endregion
}