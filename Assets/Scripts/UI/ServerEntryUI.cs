using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TagDebugSystem;

/// <summary>
/// Represents a single server entry in the server selection list.
/// Displays server information and handles selection.
/// </summary>
public class ServerEntryUI : MonoBehaviour
{
    private const string TAG = Tags.UI;

    [SerializeField] private Text serverNameText; // Using Unity UI Text for compatibility
    [SerializeField] private Text serverStatusText;
    [SerializeField] private Text playerCountText;
    [SerializeField] private Button selectButton;

    private string serverId;
    private string serverName;
    private Action<string, string> onServerSelectedCallback;

    /// <summary>
    /// Sets up the server entry with data and callback.
    /// </summary>
    /// <param name="server">The server information to display</param>
    /// <param name="callback">Callback to invoke when this server is selected</param>
    public void Setup(ServerInfo server, Action<string, string> callback)
    {
        this.serverId = server.id;
        this.serverName = server.name;
        this.onServerSelectedCallback = callback;

        TD.Verbose(TAG, $"Setting up server entry for {server.name} (ID: {server.id})", this);

        // Set UI elements
        if (serverNameText != null)
        {
            serverNameText.text = server.name;
        }
        else
        {
            TD.Warning(TAG, "serverNameText is null", this);
        }

        if (serverStatusText != null)
        {
            serverStatusText.text = server.status;

            // Change color based on status
            if (server.status.ToLower() == "online")
            {
                serverStatusText.color = Color.green;
            }
            else
            {
                serverStatusText.color = Color.red;
            }
        }
        else
        {
            TD.Warning(TAG, "serverStatusText is null", this);
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"{server.playerCount}/{server.maxPlayers}";
        }
        else
        {
            TD.Verbose(TAG, "playerCountText is null (optional)", this);
        }

        // Set button listener
        if (selectButton != null)
        {
            // Clear existing listeners
            selectButton.onClick.RemoveAllListeners();

            // Add new listener
            selectButton.onClick.AddListener(OnSelectClicked);

            // Disable button if server is offline
            selectButton.interactable = server.status.ToLower() == "online";
        }
        else
        {
            TD.Error(TAG, "selectButton is null", this);
        }

        TD.Verbose(TAG, $"Server entry setup complete for {server.name}", this);
    }

    /// <summary>
    /// Handler for the server selection button click.
    /// </summary>
    private void OnSelectClicked()
    {
        TD.Info(TAG, $"Server selected: {serverName} (ID: {serverId})", this);

        // Call the callback with server info
        if (onServerSelectedCallback != null)
        {
            onServerSelectedCallback.Invoke(serverId, serverName);
        }
        else
        {
            TD.Error(TAG, "Server selection callback is null", this);
        }
    }

    /// <summary>
    /// Clean up event listeners when the component is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        TD.Verbose(TAG, $"ServerEntryUI for {serverName} being destroyed", this);

        // Clean up listeners
        if (selectButton != null)
        {
            selectButton.onClick.RemoveListener(OnSelectClicked);
        }
    }
}