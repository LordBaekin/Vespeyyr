using System.Collections;
using UnityEngine;
using Coherence.Toolkit;
using UnityEngine.SceneManagement;
using TagDebugSystem;

/// <summary>
/// BridgeDebugger finds and logs information about all CoherenceBridge instances at startup.
/// This helps inspect the configuration and state of Coherence networking in the scene.
/// </summary>
public class BridgeDebugger : MonoBehaviour
{
    // Tag for logging
    private const string TAG = Tags.Network;

    /// <summary>
    /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
    /// Waits one frame for all bridges to initialize before finding and logging them.
    /// </summary>
    private IEnumerator Start()
    {
        TD.Verbose(TAG, "Starting BridgeDebugger scan...", this);

        // let all bridges Awake/Start first
        yield return null;

        // use the new API—keep the default sort (InstanceID) so order is predictable
        var bridges = Object.FindObjectsByType<CoherenceBridge>(
            FindObjectsSortMode.InstanceID
        );

        TD.Info(TAG, $"Found {bridges.Length} CoherenceBridge(s)", this);

        if (bridges.Length == 0)
        {
            TD.Warning(TAG, "No CoherenceBridge components found in the scene. Networking functionality may be unavailable.", this);
            yield break;
        }

        // Log details about each bridge
        foreach (var b in bridges)
        {
            TD.Verbose(TAG,
                $"Bridge '{b.gameObject.name}': " +
                $"scene='{b.Scene.name}', " +
                $"IsMain={b.IsMain}, " +
                $"AutoLoginAsGuest={b.AutoLoginAsGuest}, " +
                $"PlayerAccountAutoConnect={b.PlayerAccountAutoConnect}",
                this
            );

            // Log additional warnings about possible misconfiguration
            if (!b.IsMain)
            {
                TD.Warning(TAG, $"Bridge '{b.gameObject.name}' is not set as the main bridge. Only the main bridge will connect to Coherence servers.", this);
            }
        }

        // Check if there's more than one main bridge, which would be a problem
        var mainBridges = System.Array.FindAll(bridges, bridge => bridge.IsMain);
        if (mainBridges.Length > 1)
        {
            TD.Error(TAG, $"Found {mainBridges.Length} main bridges. Only one CoherenceBridge should have IsMain=true.", this);

            foreach (var mainBridge in mainBridges)
            {
                TD.Error(TAG, $"Main bridge conflict: '{mainBridge.gameObject.name}' in scene '{mainBridge.Scene.name}'", this);
            }
        }
        else if (mainBridges.Length == 0)
        {
            TD.Warning(TAG, "No main bridge found. Define one CoherenceBridge with IsMain=true for proper networking.", this);
        }

        TD.Verbose(TAG, "BridgeDebugger scan completed", this);
    }
}