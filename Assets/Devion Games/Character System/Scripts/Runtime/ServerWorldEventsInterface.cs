using System.Collections.Generic;

/// <summary>
/// Simple interface bridge file to make ServerWorldEvents accessible from all assemblies
/// </summary>
public static class ServerWorldEventsInterface
{
    /// <summary>
    /// Whether the user is currently authenticated with the server
    /// </summary>
    public static bool IsAuthenticated => ServerWorldEvents.IsAuthenticated;

    /// <summary>
    /// The key of the currently selected world (used as save key)
    /// </summary>
    public static string CurrentWorldKey => ServerWorldEvents.CurrentWorldKey;

    /// <summary>
    /// The display name of the currently selected world
    /// </summary>
    public static string CurrentWorldName => ServerWorldEvents.CurrentWorldName;

    /// <summary>
    /// The unique ID of the currently selected world
    /// </summary>
    public static string CurrentWorldId => ServerWorldEvents.CurrentWorldId;
}

