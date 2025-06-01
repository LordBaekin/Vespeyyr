using DevionGames.CharacterSystem.Configuration;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using System;
using TagDebugSystem;
using DevionGames.StatSystem;           // For StatsHandler and Attribute
using DevionGames;                      // For EventHandler
using DevionGames.UIWidgets;            // For NotificationOptions and NotificationExtension
using UnityEngine.Events;               // For UnityAction

namespace DevionGames.CharacterSystem
{
    public class CharacterManager : MonoBehaviour
    {
        public bool dontDestroyOnLoad = true;

        private static CharacterManager m_Current;
        public static CharacterManager current
        {
            get
            {
                Assert.IsNotNull(m_Current, "Requires a Character Manager. Create one from Tools > Devion Games > Character System > Create Character Manager!");
                return m_Current;
            }
        }

        [SerializeField]
        private CharacterDatabase m_Database = null;

        public static CharacterDatabase Database
        {
            get
            {
                if (CharacterManager.current != null)
                {
                    Assert.IsNotNull(CharacterManager.current.m_Database, "Please assign CharacterDatabase to the Character Manager!");
                    return CharacterManager.current.m_Database;
                }
                return null;
            }
        }

        private static Default m_DefaultSettings;
        public static Default DefaultSettings => m_DefaultSettings ??= GetSetting<Default>();

        private static UI m_UI;
        public static UI UI => m_UI ??= GetSetting<UI>();

        private static Notifications m_Notifications;
        public static Notifications Notifications => m_Notifications ??= GetSetting<Notifications>();

        private static SavingLoading m_SavingLoading;
        public static SavingLoading SavingLoading => m_SavingLoading ??= GetSetting<SavingLoading>();

        private static T GetSetting<T>() where T : Configuration.Settings
        {
            if (CharacterManager.Database != null)
            {
                return (T)CharacterManager.Database.settings.FirstOrDefault(x => x.GetType() == typeof(T));
            }
            return default;
        }

        private Character m_SelectedCharacter;
        public Character SelectedCharacter => this.m_SelectedCharacter;

        private void Awake()
        {
            if (CharacterManager.m_Current != null)
            {
                TD.Warning(Tags.CharacterSystem, "Multiple Character Manager in scene. Destroying instance.");
                Destroy(gameObject);
                return;
            }

            CharacterManager.m_Current = this;

            if (dontDestroyOnLoad)
            {
                if (transform.parent != null)
                {
                    TD.Warning(Tags.CharacterSystem, "Character Manager with DontDestroyOnLoad can't be a child transform. Unparenting.");
                    transform.parent = null;
                }
                DontDestroyOnLoad(gameObject);
            }

            // Register for processing incoming character data
            EventHandler.Register<string>("OnCharacterManagerProcessData", ProcessCharacterData);
            // Register for experience-gained events (fired from Level.cs)
            EventHandler.Register<int>("OnExperienceGained", OnExperienceGained);

            TD.Info(Tags.CharacterSystem, "Character Manager initialized.");
        }

        private void OnDestroy()
        {
            // Unregister from processing character data
            EventHandler.Unregister<string>("OnCharacterManagerProcessData", ProcessCharacterData);
            // Unregister from experience-gained events
            EventHandler.Unregister<int>("OnExperienceGained", OnExperienceGained);

            TD.Info(Tags.CharacterSystem, "Character Manager destroyed and unregistered.");
        }

        private static void ProcessCharacterData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                TD.Warning(Tags.CharacterSystem, "No character data to process.");
                return;
            }

            List<object> l = MiniJSON.Deserialize(data) as List<object>;
            for (int i = 0; i < l.Count; i++)
            {
                Dictionary<string, object> characterData = l[i] as Dictionary<string, object>;
                EventHandler.Execute("OnCharacterDataLoaded", characterData);
            }

            List<Character> list = JsonSerializer.Deserialize<Character>(data);
            for (int i = 0; i < list.Count; i++)
            {
                Character character = list[i];
                TD.Info(Tags.CharacterSystem, $"Character loaded: {character.CharacterName}");
                EventHandler.Execute("OnCharacterLoaded", character);
            }
        }

        /// <summary>
        /// Called when EXP increases (fired by Level.cs).
        /// Displays a "+N EXP" popup via UIWidgets' NotificationExtension.Show.
        /// </summary>
        private void OnExperienceGained(int amount)
        {
            // Build NotificationOptions instance:
            var options = new NotificationOptions
            {
                text = $"+{amount} EXP",
                color = Color.cyan
            };
            // Cast null to UnityAction<int> so the compiler picks the correct overload:
            options.Show((UnityAction<int>)null, options.text);
        }

        public static void StartPlayScene(Character selected)
        {
            CharacterManager.current.m_SelectedCharacter = selected;

            string characterId = selected.FindProperty("CharacterId")?.stringValue;
            string characterName = selected.CharacterName;

            if (string.IsNullOrEmpty(characterId))
            {
                characterId = characterName;
                TD.Warning(Tags.CharacterSystem, $"No CharacterId found for character '{characterName}', using name as ID");
            }

            string worldKey = ServerWorldEvents.CurrentWorldKey;
            if (string.IsNullOrEmpty(worldKey))
            {
                worldKey = PlayerPrefs.GetString("selected_server", "");
                if (!string.IsNullOrEmpty(worldKey))
                {
                    string worldName = PlayerPrefs.GetString("selected_server_name", worldKey);
                    ServerWorldEvents.SetCurrentWorld(worldKey, worldName);
                    DevionGamesAdapter.SetWorldContext(worldKey);
                }
            }

            TD.Info(Tags.CharacterSystem, $"Set context for characterId={characterId}, characterName={characterName}, prefab={selected.Name}");
            DevionGamesAdapter.SetCharacterContext(characterId, characterName);

            string token = PlayerPrefs.GetString("jwt_token", "");
            if (!string.IsNullOrEmpty(token))
            {
                DevionGamesAdapter.SetAuthToken(token);
            }

            PlayerPrefs.SetString("Player", selected.CharacterName);
            PlayerPrefs.SetString("Profession", selected.Name);

            string scene = selected.FindProperty("Scene")?.stringValue ?? CharacterManager.DefaultSettings.playScene;

            TD.Info(Tags.CharacterSystem, $"Loading scene '{scene}' for character '{selected.CharacterName}'");

            UnityEngine.SceneManagement.SceneManager.activeSceneChanged += ChangedActiveScene;
            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        private static void ChangedActiveScene(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Vector3 position = CharacterManager.current.m_SelectedCharacter.FindProperty("Spawnpoint").vector3Value;
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                if (player.name == CharacterManager.current.m_SelectedCharacter.Prefab.name)
                {
                    TD.Info(Tags.CharacterSystem, "Player already in scene with correct prefab.");
                    return;
                }
                TD.Warning(Tags.CharacterSystem, "Replacing existing Player in scene.");
                DestroyImmediate(player);
            }

            // Instantiate the character’s prefab:
            player = GameObject.Instantiate(CharacterManager.current.m_SelectedCharacter.Prefab, position, Quaternion.identity);
            player.name = player.name.Replace("(Clone)", "").Trim();
            TD.Info(Tags.CharacterSystem, $"Instantiated player: {player.name} at {position}");

            // ── Copy “Experience” into the Stat System ──
            var handler = player.GetComponent<StatsHandler>();
            if (handler != null)
            {
                var expProp = CharacterManager.current.m_SelectedCharacter.FindProperty("Experience");
                var expAttr = handler.GetStat("Experience") as DevionGames.StatSystem.Attribute;

                if (expAttr != null && expProp != null)
                {
                    expAttr.CurrentValue = expProp.floatValue;
                    TD.Info(Tags.CharacterSystem, $"Set runtime EXP to {expProp.floatValue} for player '{player.name}'.");
                }
                else
                {
                    TD.Warning(Tags.CharacterSystem, "Could not find Attribute 'Experience' on StatsHandler.");
                }
            }
            else
            {
                TD.Warning(Tags.CharacterSystem, "No StatsHandler component found on player prefab.");
            }
            // ────────────────────────────────────────────────────
        }

        public static void CreateCharacter(Character character)
        {
            TD.Info(Tags.CharacterSystem, $"CreateCharacter called for: {character.CharacterName}");
            EventHandler.Execute("OnCharacterManagerCreateCharacter", character);

            DevionGamesAdapter.LoadCharacterData((existingData) => {
                List<Character> list = string.IsNullOrEmpty(existingData) ?
                    new List<Character>() :
                    JsonSerializer.Deserialize<Character>(existingData);

                if (list.Any(c => c.CharacterName == character.CharacterName))
                {
                    TD.Warning(Tags.CharacterSystem, $"Character creation failed: duplicate name '{character.CharacterName}'");
                    EventHandler.Execute("OnFailedToCreateCharacter", character);
                    return;
                }

                list.Add(character);
                string data = JsonSerializer.Serialize(list.ToArray());
                DevionGamesAdapter.SaveCharacterData(data);

                TD.Info(Tags.CharacterSystem, $"Character created: {character.CharacterName}");
                EventHandler.Execute("OnCharacterCreated", character);
            });
        }

        public static void LoadCharacters()
        {
            TD.Info(Tags.CharacterSystem, "LoadCharacters() called.");
            EventHandler.Execute("OnCharacterManagerLoadCharacters");
        }

        public static void DeleteCharacter(Character character)
        {
            TD.Info(Tags.CharacterSystem, $"DeleteCharacter() called for: {character.CharacterName}");
            EventHandler.Execute("OnCharacterManagerDeleteCharacter", character);

            DevionGamesAdapter.LoadCharacterData((existingData) => {
                if (string.IsNullOrEmpty(existingData)) return;

                List<Character> list = JsonSerializer.Deserialize<Character>(existingData);
                string data = JsonSerializer.Serialize(list.Where(x => x.CharacterName != character.CharacterName).ToArray());
                DevionGamesAdapter.SaveCharacterData(data);

                DeleteInventorySystemForCharacter(character.CharacterName);
                DeleteStatSystemForCharacter(character.CharacterName);
                TD.Info(Tags.CharacterSystem, $"Character deleted: {character.CharacterName}");
                EventHandler.Execute("OnCharacterDeleted", character);
            });
        }

        private static void DeleteInventorySystemForCharacter(string character)
        {
            TD.Info(Tags.CharacterSystem, $"Deleting Inventory data for: {character}");
            PlayerPrefs.DeleteKey(character + ".UI");
            List<string> scenes = PlayerPrefs.GetString(character + ".Scenes").Split(';').ToList();
            scenes.RemoveAll(string.IsNullOrEmpty);
            for (int i = 0; i < scenes.Count; i++)
            {
                PlayerPrefs.DeleteKey(character + "." + scenes[i]);
            }
            PlayerPrefs.DeleteKey(character + ".Scenes");
        }

        private static void DeleteStatSystemForCharacter(string character)
        {
            TD.Info(Tags.CharacterSystem, $"Deleting Stat data for: {character}");
            PlayerPrefs.DeleteKey(character + ".Stats");
            List<string> keys = PlayerPrefs.GetString("StatSystemSavedKeys").Split(';').ToList();
            keys.RemoveAll(string.IsNullOrEmpty);
            List<string> allKeys = new List<string>(keys);
            allKeys.Remove(character);
            PlayerPrefs.SetString("StatSystemSavedKeys", string.Join(";", allKeys));
        }
    }
}
