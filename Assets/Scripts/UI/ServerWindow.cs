// Filename: ServerWindow.cs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TagDebugSystem;

public class ServerWindow : MonoBehaviour
{
    private const string TAG = Tags.UI;

    [Header("UI References")]
    [SerializeField] private Transform serverListContent;
    [SerializeField] private GameObject serverEntryPrefab;
    [SerializeField] private Button backButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject loadingIndicator;
    [Header("Logout Controls")]
    [SerializeField] private Button logoutButton;

    private FlowManager flowManager;

    private string selectedServerId = null;
    private string selectedServerName = null;

    public void SetFlowManager(FlowManager manager) { flowManager = manager; }

    private void Awake()
    {
        TD.Verbose(TAG, "ServerWindow initializing", this);

        // Button listeners (single responsibility: ServerWindow owns all UI)
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
        if (logoutButton != null)
            logoutButton.onClick.AddListener(OnLogoutClicked);
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextClicked);
            nextButton.interactable = false; // until a server is selected
        }

        TD.Info(TAG, "ServerWindow initialized successfully", this);
    }
    private void OnEnable()
    {
        TD.Info(TAG, "ServerWindow activated, refreshing server list", this);
        EnsureFlowManagerReference();
        RefreshServerList();
    }
    private void OnDestroy()
    {
        TD.Info(TAG, "ServerWindow being destroyed, unregistering event handlers", this);
        if (backButton != null) backButton.onClick.RemoveListener(OnBackClicked);
        if (logoutButton != null) logoutButton.onClick.RemoveListener(OnLogoutClicked);
        if (nextButton != null) nextButton.onClick.RemoveListener(OnNextClicked);
    }

    // --- Robust reference re-linking ---
    private void EnsureFlowManagerReference()
    {
        if (flowManager != null) return;
        flowManager = FlowManager.Instance;
        if (flowManager == null)
            TD.Error(TAG, "FlowManager is null and cannot be found.", this);
    }

    // --- UI Logic: called ONLY by ServerWindow, not by FlowManager ---
    public void RefreshServerList()
    {
        TD.Info(TAG, "[RefreshServerList] Requesting new server list...", this);
        SetLoading(true);
        SetStatus("Fetching available servers...");
        EnsureFlowManagerReference();
        if (flowManager != null)
        {
            flowManager.RequestServerList();
        }
        else
        {
            TD.Error(TAG, "[RefreshServerList] No FlowManager available.", this);
            SetStatus("Unable to fetch server list.");
            SetLoading(false);
        }
    }

    /// <summary>
    /// Populates the server list UI with server data. This would be called by the FlowManager when the list is ready.
    /// </summary>
    public void PopulateServerList(List<ServerInfo> servers)
    {
        TD.Info(TAG, $"[PopulateServerList] Populating with {servers.Count} servers", this);

        if (serverListContent != null)
        {
            foreach (Transform child in serverListContent)
                Destroy(child.gameObject);
            TD.Verbose(TAG, "[PopulateServerList] Cleared existing server entries before populating", this);
        }
        else
        {
            TD.Error(TAG, "[PopulateServerList] serverListContent is null", this);
            SetStatus("UI error: serverListContent missing!");
            SetLoading(false);
            return;
        }

        if (servers == null || servers.Count == 0)
        {
            TD.Warning(TAG, "[PopulateServerList] No servers available to display", this);
            SetStatus("No available servers found");
            SetLoading(false);
            return;
        }

        for (int i = 0; i < servers.Count; i++)
        {
            var server = servers[i];
            TD.Info(TAG, $"[PopulateServerList] Server {i}: Name={server.name}, ID={server.id}", this);

            if (serverEntryPrefab == null)
            {
                TD.Error(TAG, "[PopulateServerList] serverEntryPrefab is null", this);
                continue;
            }

            GameObject entryObject = Instantiate(serverEntryPrefab, serverListContent);
            ServerEntryUI entry = entryObject.GetComponent<ServerEntryUI>();

            if (entry != null)
            {
                entry.Setup(server, OnServerEntrySelected);
                TD.Verbose(TAG, $"[PopulateServerList] Created entry for {server.name} ({server.id})", this);
            }
            else
            {
                TD.Error(TAG, "[PopulateServerList] ServerEntryUI component missing on server entry prefab.", this);
                // Fallback - assign via Text/Button
                Text entryText = entryObject.GetComponentInChildren<Text>();
                Button entryButton = entryObject.GetComponent<Button>();

                if (entryText != null)
                    entryText.text = server.name;
                if (entryButton != null)
                    entryButton.onClick.AddListener(() => OnServerEntrySelected(server.id, server.name));
            }
        }
        SetStatus("");
        SetLoading(false);
    }

    private void OnServerEntrySelected(string serverId, string serverName)
    {
        TD.Info(TAG, $"[OnServerEntrySelected] {serverName} ({serverId})", this);

        selectedServerId = serverId;
        selectedServerName = serverName;

        if (nextButton != null) nextButton.interactable = true;
    }

    private void OnNextClicked()
    {
        TD.Info(TAG, "[OnNextClicked] Next button pressed.", this);

        if (string.IsNullOrEmpty(selectedServerId))
        {
            TD.Warning(TAG, "[OnNextClicked] No server selected.", this);
            SetStatus("Please select a server first");
            return;
        }
        EnsureFlowManagerReference();
        if (flowManager != null)
        {
            flowManager.OnServerSelected(selectedServerId, selectedServerName);
        }
        else
        {
            TD.Error(TAG, "[OnNextClicked] FlowManager is null!", this);
            SetStatus("FlowManager missing. Please restart.");
        }
    }
    private void OnBackClicked()
    {
        TD.Info(TAG, "[OnBackClicked] Back button pressed.", this);
        EnsureFlowManagerReference();
        if (flowManager != null)
        {
            flowManager.ShowLoginPanel();
        }
        else
        {
            TD.Error(TAG, "[OnBackClicked] FlowManager is null!", this);
            SetStatus("FlowManager missing. Please restart.");
        }
    }
    private void OnLogoutClicked()
    {
        TD.Info(TAG, "[OnLogoutClicked] Logout button pressed.", this);
        EnsureFlowManagerReference();
        if (flowManager != null)
        {
            flowManager.LogoutToServerSelect();
        }
        else
        {
            TD.Error(TAG, "[OnLogoutClicked] FlowManager is null!", this);
            SetStatus("FlowManager missing. Please restart.");
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            TD.Verbose(TAG, $"[SetStatus] {message}", this);
        }
    }
    private void SetLoading(bool isLoading)
    {
        if (loadingIndicator != null)
        {
            loadingIndicator.SetActive(isLoading);
            TD.Verbose(TAG, $"[SetLoading] {(isLoading ? "shown" : "hidden")}", this);
        }
    }
}
