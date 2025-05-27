// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Simulator
{
    using Cloud;
    using Common;
    using Toolkit;
    using Connection;
    using Log;
    using Logger = Log.Logger;
    using System.Collections;
    using System.Linq;
    using Cloud.Coroutines;
    using UnityEngine;

    [AddComponentMenu("coherence/Simulators/Auto Simulator Connection")]
    [HelpURL("https://docs.coherence.io/v/1.6/manual/simulation-server/client-vs-simulator-logic#connecting-simulators-automatically-to-rs-autosimulatorconnection-component")]
    public class AutoSimulatorConnection : MonoBehaviour
    {
        public float reconnectTime = 3f;
        [Tooltip("Number of connection attempts before trying to resolve the endpoint again.\n\nOnly applies to Worlds.")]
        public int attemptsBeforeRefetch = 3;
        [Tooltip("Try to log in to Cloud as guest. If the bridge handles login already, or it's logged in already, this step will be skipped.\n\nOnly applies to Worlds.")]
        public bool autoLoginToCloud = true;

        public EndpointData Endpoint => endpoint;

        private readonly Logger logger = Log.GetLogger<AutoSimulatorConnection>();
        private EndpointData endpoint;
        private Coroutine reconnectCoroutine;
        private CoherenceBridge bridge;
        private IClient client;
        private EndpointData lastConnectedEndpoint;
        private int currentReconnectAttempts;
        private bool quitting;

        private EndpointData CommandLineRoomEndpoint => new()
        {
            roomId = (ushort)SimulatorUtility.RoomId,
            uniqueRoomId = SimulatorUtility.UniqueRoomId,
            host = SimulatorUtility.Ip,
            port = SimulatorUtility.Port,
            runtimeKey = RuntimeSettings.Instance.RuntimeKey,
            schemaId = RuntimeSettings.Instance.SchemaID,
            worldId = SimulatorUtility.WorldId,
            authToken = SimulatorUtility.AuthToken,
            region = SimulatorUtility.Region,
        };
        private bool IsSimulator => SimulatorUtility.IsSimulator || client?.ConnectionType == ConnectionType.Simulator;
        private bool UsingWorld => SimulatorUtility.SimulatorType == SimulatorUtility.Type.World;

        private IEnumerator Start()
        {
            logger.Context = gameObject;
            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, out bridge))
            {
                logger.Error(Error.SimulatorAutoConnectBridge, $"This scene does not contain a {nameof(CoherenceBridge)}.");
                yield break;
            }

            client = bridge.Client;
            client.OnConnected += NetworkOnConnected;
            client.OnDisconnected += NetworkOnDisconnected;
            client.OnConnectedEndpoint += OnConnectedEndpoint;

            if (SimulatorUtility.IsInvokedAsSimulator)
            {
                var args = string.Join(' ', System.Environment.GetCommandLineArgs());
                logger.Info($"Invoked as hosted simulator. Arguments: {args}");

                endpoint = CommandLineRoomEndpoint;
                logger.Info($"Taking endpoint from command-line args: {endpoint}");
                if (UsingWorld)
                {
                    yield return StartCoroutine(ResolveWorldEndpoint());
                }
            }
            else
            {
                logger.Info($"Not invoked as hosted simulator, waiting for {nameof(CoherenceBridge)} to connect.");

                while (!bridge.IsConnected)
                {
                    yield return null;
                }

                endpoint = lastConnectedEndpoint;
                if (bridge.Client.ConnectionType != ConnectionType.Simulator)
                {
                    logger.Info($"Disabling: scene not connected as simulator.");
                    enabled = false;
                    yield break;
                }
            }

            var isValid = endpoint.Validate().isValid;
            if (!isValid)
            {
                logger.Error(Error.SimulatorAutoConnectEndpoint, $"Disabling: Failed to resolve endpoint: {endpoint}.");
                enabled = false;
                yield break;
            }

            StartReconnect();
        }

        private void OnConnectedEndpoint(EndpointData endpointData)
        {
            lastConnectedEndpoint = endpointData;
        }

        private void OnApplicationQuit()
        {
            quitting = true;
        }

        private void OnDestroy()
        {
            if (client != null)
            {
                client.OnConnected -= NetworkOnConnected;
                client.OnDisconnected -= NetworkOnDisconnected;
                client.OnConnectedEndpoint -= OnConnectedEndpoint;
            }
        }

        private void NetworkOnConnected(ClientID _)
        {
            if (IsSimulator)
            {
                logger.Info("Connection successful.");
                currentReconnectAttempts = 0;
            }
        }

        private void NetworkOnDisconnected(ConnectionCloseReason connectionCloseReason)
        {
            if (!quitting && client.ConnectionType == ConnectionType.Simulator)
            {
                logger.Error(Error.SimulatorAutoConnectDisconnected, ("reason", connectionCloseReason));
                StartReconnect();
            }
        }

        private void StartReconnect()
        {
            if (reconnectCoroutine != null)
            {
                StopCoroutine(reconnectCoroutine);
            }

            reconnectCoroutine = StartCoroutine(Reconnect());
        }

        private IEnumerator Reconnect()
        {
            var connected = client.IsConnected();
            while (!connected)
            {
                if (client.IsDisconnected())
                {
                    logger.Info("Attempting reconnect to: " + endpoint);

                    Connect();
                    currentReconnectAttempts++;
                }

                connected = client.IsConnected();

                if (connected)
                {
                    logger.Info("Connected.");
                    currentReconnectAttempts = 0;
                    yield break;
                }

                if (UsingWorld && attemptsBeforeRefetch > 0 && currentReconnectAttempts > attemptsBeforeRefetch)
                {
                    yield return new WaitForSeconds(reconnectTime);
                    yield return ResolveWorldEndpoint();
                    StartReconnect();
                    yield break;
                }

                yield return new WaitForSeconds(reconnectTime);
                connected = client.IsConnected();
            }
        }

        private void Connect()
        {
            logger.Info($"Connecting as simulator to endpoint {endpoint}",
                ("slug", RuntimeSettings.Instance.SimulatorSlug),
                ("sdkVersion", RuntimeSettings.Instance.VersionInfo.SdkRevisionOrVersion),
                ("rsVersion", RuntimeSettings.Instance.VersionInfo.Engine));

            var settings = ConnectionSettings.Default;
            settings.UseDebugStreams = RuntimeSettings.Instance.UseDebugStreams;

            // if version is uninitialized, use the one in runtime settings
            if (string.IsNullOrEmpty(endpoint.rsVersion))
            {
                endpoint.rsVersion = RuntimeSettings.Instance.VersionInfo.Engine;
            }

            client.Connect(endpoint, settings, ConnectionType.Simulator);
        }

        private IEnumerator ResolveWorldEndpoint()
        {
            EndpointData newEndpoint;
            if (SimulatorUtility.Region == SimulatorUtility.LocalRegionParameter)
            {
                newEndpoint = new EndpointData
                {
                    host = SimulatorUtility.Ip,
                    port = RuntimeSettings.Instance.IsWebGL ? RuntimeSettings.Instance.LocalWorldWebPort : RuntimeSettings.Instance.LocalWorldUDPPort,
                    worldId = SimulatorUtility.WorldId,
                    runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                    schemaId = RuntimeSettings.Instance.SchemaID,
                    authToken = SimulatorUtility.AuthToken,
                    region = SimulatorUtility.Region,
                };
                endpoint = newEndpoint;
                logger.Info($"Resolved local world endpoint: {endpoint}");
            }
            else
            {
                var worldsService = bridge.CloudService.Worlds;
                if (worldsService.IsLoggedIn)
                {
                    logger.Info("Cloud logged in. Skipping logging.");
                }
                else
                {
                    if (autoLoginToCloud)
                    {
                        if (bridge.PlayerAccountAutoConnect is not CoherenceBridgePlayerAccount.AutoLoginAsGuest)
                        {
                            logger.Info("Logging as guest in Cloud...");
                            bridge.CloudService.GameServices.AuthService.LoginAsGuest().Then(task =>
                                {
                                    if (task.IsFaulted)
                                    {
                                        logger.Error(Error.SimulatorAutoConnectCloudFailed, $"Cloud login failed: {task.Exception?.Message}");
                                    }
                                    else
                                    {
                                        logger.Info($"Logged in as guest: {task.Result.PlayerAccount}. ");
                                    }
                                });
                        }
                        else
                        {
                            logger.Info($"Configured to auto-login, but {nameof(CoherenceBridge)} is set to auto-login as guest. Skipping logging in as part of this component.");
                        }
                    }
                }

                if (!worldsService.IsLoggedIn)
                {
                    logger.Info("Waiting for login...");
                    while (!worldsService.IsLoggedIn)
                    {
                        yield return null;
                    }
                }

                logger.Info("Fetching worlds...");
                var fetchWorldsReq = worldsService.WaitForFetchWorlds(endpoint.region, RuntimeSettings.Instance.SimulatorSlug);
                yield return fetchWorldsReq;

                var response = fetchWorldsReq.RequestResponse;
                if (response.Status == RequestStatus.Fail)
                {
                    logger.Error(Error.SimulatorAutoConnectWorldIDFailed, $"World fetching failed: {response.Exception.Message}");
                    yield break;
                }

                var worlds = response.Result;
                if (worlds.Count == 0)
                {
                    logger.Info("Worlds fetching OK: no results.");
                    yield break;
                }

                logger.Info($"Worlds feting OK: {string.Join(',', worlds.Select(w => w.WorldId))}");

                foreach (var worldData in worlds)
                {
                    if (worldData.WorldId == endpoint.worldId)
                    {
                        newEndpoint = new EndpointData
                        {
                            host = worldData.Host.Ip,
                            port = worldData.Host.UDPPort,
                            worldId = worldData.WorldId,
                            runtimeKey = RuntimeSettings.Instance.RuntimeKey,
                            schemaId = RuntimeSettings.Instance.SchemaID,
                            authToken = SimulatorUtility.AuthToken,
                            region = SimulatorUtility.Region,
                        };
                        endpoint = newEndpoint;
                        logger.Info($"Found world that matches id '{endpoint.worldId}'. New endpoint = {endpoint}.");
                    }
                }
            }

        }
    }
}
