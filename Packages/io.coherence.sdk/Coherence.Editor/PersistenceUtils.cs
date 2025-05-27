namespace Coherence.Editor
{
    using System;
    using System.IO;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class PersistenceUtils
    {
        private const string keyUseWorldPersistence = "Coherence.UseWorldPersistence";
        private const string keyStoragePath = "Coherence.PersistenceStoragePath";
        private const string keySaveRate = "Coherence.PersistenceSaveRate";
        private const string keyBackupOnEnterPlayMode = "Coherence.PersistenceBackupOnEnterPlayMode";

        static PersistenceUtils()
        {
            EditorApplication.delayCall += UpdateBackupPersistenceMenuItem;
        }

        private static void UpdateBackupPersistenceMenuItem()
        {
            Menu.SetChecked(CoherenceMainMenu.BackupWorldDataMenuItem, UseWorldPersistence);
        }

        public static bool UseWorldPersistence
        {
            get => UserSettings.GetBool(keyUseWorldPersistence, false);
            set
            {
                UserSettings.SetBool(keyUseWorldPersistence, value);
                Menu.SetChecked(CoherenceMainMenu.BackupWorldDataMenuItem, value);
            }
        }

        public static string StoragePath
        {
            get => UserSettings.GetString(keyStoragePath, Path.GetFullPath(Paths.defaultPersistentStoragePath));
            set => UserSettings.SetString(keyStoragePath, value);
        }

        public static int SaveRateInSeconds
        {
            get => UserSettings.GetInt(keySaveRate, Constants.defaultPersistenceSaveRateInSeconds);
            set => UserSettings.SetInt(keySaveRate, value);
        }

        public static bool CanBackup => File.Exists(StoragePath);

        public static bool Backup()
        {
            return Backup(out _);
        }

        public static bool Backup(out string backupPath)
        {
            backupPath = default;

            if (!CanBackup)
            {
                return false;
            }

            try
            {
                var file = StoragePath;
                var path = Path.GetDirectoryName(file);
                var fileNoExtension = Path.GetFileNameWithoutExtension(file);

                if (!File.Exists(file))
                {
                    return false;
                }

                backupPath = Path.Combine(path,
                    $"{fileNoExtension}.{DateTime.Now.ToString("yyyyMMdd-HHmmss")}.{Paths.persistentStorageFileExtension}");
                File.Copy(file, backupPath);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
    }
}
