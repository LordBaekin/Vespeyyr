using UnityEngine;

/// <summary>
/// Data structure representing server information for display and selection.
/// </summary>
[System.Serializable]
public class ServerInfo
{
    /// <summary>
    /// Unique identifier for the server/world.
    /// </summary>
    public string id;

    /// <summary>
    /// Display name of the server/world.
    /// </summary>
    public string name;

    /// <summary>
    /// Current server status (e.g., "Online", "Offline", "Maintenance").
    /// </summary>
    public string status = "Online";

    /// <summary>
    /// Current number of connected players.
    /// </summary>
    public int playerCount;

    /// <summary>
    /// Maximum number of players allowed on this server.
    /// </summary>
    public int maxPlayers = 100;

    /// <summary>
    /// Optional description of the server/world.
    /// </summary>
    public string description;

    /// <summary>
    /// Region where the server is hosted.
    /// </summary>
    public string region;

    /// <summary>
    /// Creates a string representation of the server info.
    /// </summary>
    /// <returns>A formatted string with the server details</returns>
    public override string ToString()
    {
        return $"Server[{id}]: {name} - {status}, Players: {playerCount}/{maxPlayers}, Region: {region}";
    }

    /// <summary>
    /// Create a ServerInfo from a Coherence WorldData object.
    /// </summary>
    /// <param name="worldData">The Coherence WorldData to convert</param>
    /// <returns>A new ServerInfo instance</returns>
    public static ServerInfo FromWorldData(Coherence.Cloud.WorldData worldData)
    {
        return new ServerInfo
        {
            id = worldData.WorldId.ToString(),       // Use correct property
            name = worldData.Name,                   // This is correct
            status = "Online",                       // Set as online by default since there's no status field
            playerCount = 0,                         // Default player count (not available in API)
            maxPlayers = 100,                        // Default max players (not available in API)
            description = string.Empty,              // No description in API
            region = worldData.Region ?? string.Empty // Region is available
        };
    }
}