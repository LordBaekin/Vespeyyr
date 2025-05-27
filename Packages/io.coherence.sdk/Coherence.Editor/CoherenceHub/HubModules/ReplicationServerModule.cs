// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using Coherence.Toolkit.ReplicationServer;
    using Features;
    using Portal;
    using ReplicationServer;
    using Toolkit;
    using UnityEditor;
    using UnityEngine;

    [HubModule(Priority = 80)]
    public class ReplicationServerModule : HubModule
    {
        public override string ModuleName => "Replication Server";

        public static class ModuleGUIContents
        {
            public static readonly GUIContent Rooms = EditorGUIUtility.TrTextContentWithIcon("Run for Rooms",
                "Start a terminal with a Replication Server for Rooms.",
                Icons.GetPath("Coherence.Terminal"));

            public static readonly GUIContent Worlds = EditorGUIUtility.TrTextContentWithIcon("Run for Worlds",
                "Start a terminal with a Replication Server for Worlds.",
                Icons.GetPath("Coherence.Terminal"));
            public static readonly GUIContent WhatAreReplicationServers = new("In order for clients to communicate with each other, they need a replication server. A replication server can either run locally or in the cloud. The responsibility of the server is to replicate the state of the world across the network." +
                                                               "\nIf a new schema has been created, you also need to restart the replication server.");
            public static readonly GUIContent OpenStorage = new(
#if UNITY_EDITOR_WIN
                "Show Storage in Explorer"
#elif UNITY_EDITOR_OSX
                "Reveal Storage in Finder"
#else
                "Open Storage Folder"
#endif
            );
            public static readonly GUIContent UseWorldPersistence = new("Enable",
                "Enables serialization of persistent data into a storage file (JSON), which is loaded back when a World Replication Server is restarted.");
            public static readonly GUIContent PersistenceStoragePath = new("Storage",
                "File on disk where the Replication Server persistent entities are stored.");
            public static readonly GUIContent PersistenceStorageSaveRate = new("Save Rate (seconds)",
                "How often should the replication server serialize the world information into the storage.");
            public static readonly GUIContent PersistenceStorageBackup = new("Create Backup Now",
                "Create a backup of the world persistence storage.");
            public static readonly GUIContent PersistenceResetToDefaults = new("Reset to Defaults",
                "Reset all persistence-related settings to their default values.");
        }

        public override HelpSection Help => new()
        {
            title = new GUIContent("What are Replication Servers?"),
            content = ModuleGUIContents.WhatAreReplicationServers,
        };

        public override void OnModuleEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            Refresh();
        }

        public override void OnModuleDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
        }

        private void Refresh()
        {
            if (!string.IsNullOrEmpty(ProjectSettings.instance.LoginToken))
            {
                PortalLogin.FetchOrgs();
            }
        }

        private void OnProjectChanged()
        {
            Refresh();
        }

        public override void OnGUI()
        {
            CoherenceHubLayout.DrawSection("Replication Server", DrawReplicationServers);

            var backupWorldDataFeature = FeaturesManager.GetFeature(FeatureFlags.BackupWorldData);
            if (backupWorldDataFeature.IsEnabled || backupWorldDataFeature.IsUserConfigurable)
            {
                CoherenceHubLayout.DrawSection("Backup World Data", DrawSettingsForWorlds);
            }

            CoherenceHubLayout.DrawSection("Online Resources", DrawLinks);
        }

        private void DrawSettingsForWorlds()
        {
            var backupWorldDataFeature = FeaturesManager.GetFeature(FeatureFlags.BackupWorldData);
            if (backupWorldDataFeature.IsUserConfigurable)
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    EditorGUILayout.HelpBox(
                        "This feature has been deprecated. Disabling it is a permanent action.",
                        MessageType.Warning);
                    if (GUILayout.Button("Disable Feature"))
                    {
                        PersistenceUtils.UseWorldPersistence = false;
                    }
                }
            }

            EditorGUI.BeginChangeCheck();
            var usePersistence = EditorGUILayout.Toggle(ModuleGUIContents.UseWorldPersistence,
                PersistenceUtils.UseWorldPersistence);
            if (EditorGUI.EndChangeCheck())
            {
                PersistenceUtils.UseWorldPersistence = usePersistence;
            }

            using (new EditorGUI.DisabledGroupScope(!usePersistence))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    CoherenceHubLayout.DrawDiskPath(
                        PersistenceUtils.StoragePath,
                        ModuleGUIContents.PersistenceStoragePath.tooltip,
                        () =>
                        {
                            var filePath = PersistenceUtils.StoragePath;
                            var defaultPath = File.Exists(filePath) ? filePath : Paths.libraryRootPath;
                            return EditorUtility.SaveFilePanel("Persistence Storage", defaultPath,
                                Paths.defaultPersistentStorageFileName + "." + Paths.persistentStorageFileExtension,
                                Paths.persistentStorageFileExtension);
                        },
                        path => PersistenceUtils.StoragePath = path);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var rate = EditorGUILayout.IntField(ModuleGUIContents.PersistenceStorageSaveRate,
                        PersistenceUtils.SaveRateInSeconds);
                    if (EditorGUI.EndChangeCheck())
                    {
                        PersistenceUtils.SaveRateInSeconds = Mathf.Max(rate, 1);
                    }
                }

                using (new EditorGUI.DisabledScope(!PersistenceUtils.CanBackup))
                using (new EditorGUILayout.HorizontalScope())
                {
                    var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(r, ModuleGUIContents.PersistenceStorageBackup, EditorStyles.miniButton))
                    {
                        if (Host is EditorWindow)
                        {
                            var success = PersistenceUtils.Backup(out var backupPath);
                        }
                    }
                }

                using (new EditorGUI.DisabledGroupScope(!File.Exists(PersistenceUtils.StoragePath)))
                {
                    var revealInFinderRect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(revealInFinderRect, ModuleGUIContents.OpenStorage, EditorStyles.miniButton))
                    {
                        EditorUtility.RevealInFinder(PersistenceUtils.StoragePath);
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.Space();

                using (new EditorGUILayout.HorizontalScope())
                {
                    var r = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect(false,
                        EditorGUIUtility.singleLineHeight, EditorStyles.miniButton, GUILayout.ExpandWidth(true)));
                    if (GUI.Button(r, new GUIContent(ModuleGUIContents.PersistenceResetToDefaults),
                            EditorStyles.miniButton))
                    {
                        PersistenceUtils.StoragePath = null;
                        PersistenceUtils.SaveRateInSeconds = Constants.defaultPersistenceSaveRateInSeconds;
                        GUIUtility.hotControl = 0;
                        GUIUtility.keyboardControl = 0;
                        GUIUtility.ExitGUI();
                    }
                }
            }

            EditorGUILayout.Space();

            EditorGUILayout.HelpBox("After changing any setting, please remember to restart your replication server.",
                MessageType.Info);
        }

        private void DrawReplicationServers()
        {
            if (BakeUtil.Outdated)
            {
                EditorGUILayout.HelpBox("Bake is outdated. It is recommended to bake before you start a Replication Server.", MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.Rooms, ContentUtils.GUIStyles.bigButton))
                {
                    EditorLauncher.RunRoomsReplicationServerInTerminal();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(ContentUtils.GUIContents.clipboard, ContentUtils.GUIStyles.iconButton))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalRoomsConfig());
                    GUIUtility.systemCopyBuffer = command;
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Command copied to clipboard"));
                }
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(ModuleGUIContents.Worlds, ContentUtils.GUIStyles.bigButton))
                {
                    EditorLauncher.RunWorldsReplicationServerInTerminal();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(ContentUtils.GUIContents.clipboard, ContentUtils.GUIStyles.iconButton))
                {
                    var command = Launcher.ToCommand(EditorLauncher.CreateLocalWorldConfig());
                    GUIUtility.systemCopyBuffer = command;
                    EditorWindow.focusedWindow.ShowNotification(new GUIContent("Command copied to clipboard"));
                }
            }
        }

        public void DrawLinks()
        {
            CoherenceHubLayout.DrawLink(new GUIContent("Testing Multiplayer Locally"), DocumentationLinks.GetDocsUrl(DocumentationKeys.LocalServers));
        }
    }
}
