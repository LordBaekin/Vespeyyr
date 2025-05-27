// Filename: CoherenceWorldBridge.cs
// Place in Assets/Scripts/Network/

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Coherence.Cloud;
using TagDebugSystem;

namespace Vespeyr.Network
{
    public static class CoherenceWorldBridge
    {
        private const string TAG = "CoherenceWorldBridge";

        /// <summary>
        /// Fetches worlds from the current player account's cloud services.
        /// </summary>
        /// <param name="playerAccount">A Coherence.Cloud.PlayerAccount, must be logged in</param>
        /// <returns>List of ServerInfo objects representing the available worlds.</returns>
        public static async Task<List<ServerInfo>> ListWorldsAsync(PlayerAccount playerAccount)
        {
            var result = new List<ServerInfo>();

            if (playerAccount == null)
            {
                TD.Error(TAG, "[ListWorldsAsync] PlayerAccount is NULL – aborting fetch.");
                return result;
            }
            if (playerAccount.Services == null)
            {
                TD.Error(TAG, "[ListWorldsAsync] PlayerAccount.Services is NULL – cannot fetch worlds.");
                return result;
            }

            // Use Username if available, otherwise .ToString()
            string userDisplay = !string.IsNullOrEmpty(playerAccount.Username) ? playerAccount.Username : playerAccount.ToString();
            TD.Info(TAG, $"[ListWorldsAsync] Starting fetch for user: '{userDisplay}'");

            IReadOnlyList<WorldData> worlds = null;
            try
            {
                TD.Verbose(TAG, "[ListWorldsAsync] Calling playerAccount.Services.Worlds.FetchWorldsAsync()...");
                worlds = await playerAccount.Services.Worlds.FetchWorldsAsync();
                TD.Info(TAG, $"[ListWorldsAsync] FetchWorldsAsync returned {(worlds == null ? "NULL" : worlds.Count.ToString())} worlds.");
            }
            catch (System.Exception ex)
            {
                TD.Error(TAG, $"[ListWorldsAsync] Exception fetching worlds: {ex.GetType().Name}: {ex.Message}");
                return result;
            }

            if (worlds == null)
            {
                TD.Warning(TAG, "[ListWorldsAsync] No worlds returned (NULL).");
                return result;
            }
            if (worlds.Count == 0)
            {
                TD.Warning(TAG, "[ListWorldsAsync] Worlds list is empty (0 worlds).");
                return result;
            }

            TD.Info(TAG, $"[ListWorldsAsync] Enumerating {worlds.Count} worlds...");
            int idx = 0;
            foreach (var w in worlds)
            {
                idx++;
                var info = ServerInfo.FromWorldData(w);

                if (info == null)
                {
                    TD.Warning(TAG, $"[ListWorldsAsync] WorldData #{idx} returned NULL ServerInfo – skipping.");
                    continue;
                }

                TD.Verbose(TAG, $"[ListWorldsAsync] [{idx}] World: ID='{info.id}', Name='{info.name}', Region='{info.region}', Status='{info.status}'");

                result.Add(info);
            }

            TD.Info(TAG, $"[ListWorldsAsync] Returning {result.Count} server(s) to caller.");
            return result;
        }

    }
}
