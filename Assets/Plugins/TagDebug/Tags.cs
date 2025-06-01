using System.Collections.Generic;
using UnityEngine;

namespace TagDebugSystem
{
    /// <summary>
    /// Central registry for all debug tags used in the application.
    /// This ensures consistent tag usage across the codebase.
    /// </summary>
    public static class Tags
    {
        // System Category
        public const string System = "System";
        public const string Initialization = "Initialization";
        public const string Configuration = "Configuration";
        public const string Performance = "Performance";

        // UI Category
        public const string UI = "UI";
        public const string Menu = "Menu";
        public const string Dialog = "Dialog";
        public const string HUD = "HUD";

        // Flow Category (for FlowManager etc.)
        public const string FlowManager = "FlowManager";
        public const string Scene = "Scene";
        public const string Login = "Login";
        public const string Authentication = "Authentication";
        public const string World = "World";
        public const string Character = "Character";
        public const string ServerWorld = "ServerWorld";

        // Network Category
        public const string Network = "Network";
        public const string Server = "Server";
        public const string Client = "Client";
        public const string Connection = "Connection";

        // Audio Category
        public const string Audio = "Audio";
        public const string Music = "Music";
        public const string SFX = "SFX";

        // Gameplay Category
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Combat = "Combat";
        public const string Inventory = "Inventory";
        public const string Quest = "Quest";
        public const string Stats = "Stats";
        public const string StatSystem = "StatSystem";
        public const string StatsManager = "StatsManager";
        public const string CharacterSystem = "CharacterSystem";

        // Data Management Category
        public const string SaveSystem = "SaveSystem";
        public const string LoadSystem = "LoadSystem";
        public const string DataPersistence = "DataPersistence";

        // Input Category
        public const string Input = "Input";
        public const string Controller = "Controller";
        public const string Touch = "Touch";

        // AI Category
        public const string AI = "AI";
        public const string Pathfinding = "Pathfinding";
        public const string Behavior = "Behavior";

        // Physics Category
        public const string Physics = "Physics";
        public const string Collision = "Collision";
        public const string Rigidbody = "Rigidbody";

        // VFX Category  
        public const string VFX = "VFX";
        public const string Particles = "Particles";
        public const string Shader = "Shader";

        // Dictionary to store custom tags that aren't defined as constants
        private static Dictionary<string, string> _customTags = new Dictionary<string, string>();

        /// <summary>
        /// Gets a tag by name, or returns the name itself if not found.
        /// This allows for graceful handling of unknown tags without errors.
        /// </summary>
        /// <param name="tagName">The name of the tag</param>
        /// <returns>The tag string to use</returns>
        public static string GetTag(string tagName)
        {
            // Try simple direct matching with known tags first
            switch (tagName)
            {
                // System
                case "System": return System;
                case "Initialization": return Initialization;
                case "Configuration": return Configuration;
                case "Performance": return Performance;

                // UI
                case "UI": return UI;
                case "Menu": return Menu;
                case "Dialog": return Dialog;
                case "HUD": return HUD;

                // Flow
                case "FlowManager": return FlowManager;
                case "Scene": return Scene;
                case "Login": return Login;
                case "Authentication": return Authentication;
                case "World": return World;
                case "Character": return Character;
                case "ServerWorld": return ServerWorld;

                // Network
                case "Network": return Network;
                case "Server": return Server;
                case "Client": return Client;
                case "Connection": return Connection;

                // Audio
                case "Audio": return Audio;
                case "Music": return Music;
                case "SFX": return SFX;

                // Gameplay
                case "Player": return Player;
                case "Enemy": return Enemy;
                case "Combat": return Combat;
                case "Inventory": return Inventory;
                case "Quest": return Quest;
                case "Stats": return Stats;
                case "StatSystem": return StatSystem;
                case "StatsManager": return StatsManager;

                // Data Management
                case "SaveSystem": return SaveSystem;
                case "LoadSystem": return LoadSystem;
                case "DataPersistence": return DataPersistence;

                // Input
                case "Input": return Input;
                case "Controller": return Controller;
                case "Touch": return Touch;

                // AI
                case "AI": return AI;
                case "Pathfinding": return Pathfinding;
                case "Behavior": return Behavior;

                // Physics
                case "Physics": return Physics;
                case "Collision": return Collision;
                case "Rigidbody": return Rigidbody;

                // VFX
                case "VFX": return VFX;
                case "Particles": return Particles;
                case "Shader": return Shader;
            }

            // If it's not a predefined tag, check our custom tags dictionary
            if (!_customTags.TryGetValue(tagName, out string tag))
            {
                // First time we've seen this tag, log it
                Debug.Log($"[TagDebug] Using custom tag: {tagName}");
                _customTags[tagName] = tagName;
                tag = tagName;
            }

            return tag;
        }

        /// <summary>
        /// Initialize all tags with appropriate categories
        /// </summary>
        public static void RegisterAllTags()
        {
            // System Category
            Logger.RegisterCategory("System", new string[] {
                System, Initialization, Configuration, Performance
            });

            // UI Category
            Logger.RegisterCategory("UI", new string[] {
                UI, Menu, Dialog, HUD
            });

            // Flow Category
            Logger.RegisterCategory("Flow", new string[] {
                FlowManager, Scene, Login, Authentication, World, Character, ServerWorld
            });

            // Network Category
            Logger.RegisterCategory("Network", new string[] {
                Network, Server, Client, Connection
            });

            // Audio Category
            Logger.RegisterCategory("Audio", new string[] {
                Audio, Music, SFX
            });

            // Gameplay Category
            Logger.RegisterCategory("Gameplay", new string[] {
                Player, Enemy, Combat, Inventory, Quest, Stats, StatSystem, StatsManager
            });

            // Data Management Category
            Logger.RegisterCategory("DataManagement", new string[] {
                SaveSystem, LoadSystem, DataPersistence
            });

            // Input Category
            Logger.RegisterCategory("Input", new string[] {
                Input, Controller, Touch
            });

            // AI Category
            Logger.RegisterCategory("AI", new string[] {
                AI, Pathfinding, Behavior
            });

            // Physics Category
            Logger.RegisterCategory("Physics", new string[] {
                Physics, Collision, Rigidbody
            });

            // VFX Category
            Logger.RegisterCategory("VFX", new string[] {
                VFX, Particles, Shader
            });
        }

        /// <summary>
        /// Get all available tags grouped by category for debugging/inspection
        /// </summary>
        public static Dictionary<string, string[]> GetAllTagsByCategory()
        {
            return new Dictionary<string, string[]>
            {
                ["System"] = new string[] { System, Initialization, Configuration, Performance },
                ["UI"] = new string[] { UI, Menu, Dialog, HUD },
                ["Flow"] = new string[] { FlowManager, Scene, Login, Authentication, World, Character, ServerWorld },
                ["Network"] = new string[] { Network, Server, Client, Connection },
                ["Audio"] = new string[] { Audio, Music, SFX },
                ["Gameplay"] = new string[] { Player, Enemy, Combat, Inventory, Quest, Stats, StatSystem, StatsManager },
                ["DataManagement"] = new string[] { SaveSystem, LoadSystem, DataPersistence },
                ["Input"] = new string[] { Input, Controller, Touch },
                ["AI"] = new string[] { AI, Pathfinding, Behavior },
                ["Physics"] = new string[] { Physics, Collision, Rigidbody },
                ["VFX"] = new string[] { VFX, Particles, Shader }
            };
        }

        /// <summary>
        /// Check if a tag is registered in the system
        /// </summary>
        /// <param name="tagName">The tag to check</param>
        /// <returns>True if the tag is a predefined tag, false if it's custom</returns>
        public static bool IsRegisteredTag(string tagName)
        {
            var allTags = GetAllTagsByCategory();
            foreach (var category in allTags.Values)
            {
                foreach (var tag in category)
                {
                    if (tag == tagName)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the category that a tag belongs to
        /// </summary>
        /// <param name="tagName">The tag to find the category for</param>
        /// <returns>The category name, or "Custom" if not found</returns>
        public static string GetTagCategory(string tagName)
        {
            var allTags = GetAllTagsByCategory();
            foreach (var kvp in allTags)
            {
                foreach (var tag in kvp.Value)
                {
                    if (tag == tagName)
                        return kvp.Key;
                }
            }
            return "Custom";
        }
    }
}