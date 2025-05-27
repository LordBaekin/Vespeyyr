// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor
{
    using System;
    using System.IO;
    using System.Linq;
    using Coherence.UI;
    using UnityEditor;
    using UnityEditor.Callbacks;
    using UnityEditor.PackageManager;
    using UnityEditor.PackageManager.UI;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;
    using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#if HAS_UGUI
    using UnityEngine.EventSystems;
#endif

    internal static class UIUtils
    {
        public const string sampleUiName = "coherence Sample UI";
        public const string sampleUiPath = Paths.uiPrefabsPath + "/" + sampleUiName + ".prefab";

        private static Sample[] CoherenceSamples => coherenceSamplesCache ??= GetCachedPackageSamples();

        private static Sample[] coherenceSamplesCache;

        private static Sample[] GetCachedPackageSamples()
        {
            var info = PackageInfo.FindForAssembly(typeof(UIUtils).Assembly);
            return Sample.FindByPackage(info.name, info.version).ToArray();
        }

        private static bool EnsureDependency(SamplePackageDependency dependency)
        {
            switch (dependency)
            {
                case SamplePackageDependency.None:
                    return true;
                case SamplePackageDependency.Ugui:
#if HAS_UGUI
                    return true;
#else
                        return PromptPackageInstall("com.unity.ugui");
#endif
                default:
                    throw new ArgumentOutOfRangeException(nameof(dependency), dependency, null);
            }
        }

        private static bool PromptPackageInstall(string packageName)
        {
            if (!EditorUtility.DisplayDialog("Package required",
                    "This requires the package " + packageName +
                    " to be installed, clicking OK will install it in your project.", "OK"))
            {
                return false;
            }

            var req = Client.Add(packageName);
            return req.Status == StatusCode.Success;
        }

        internal static void ImportAndPingFromPackageSample(SampleDialogAsset dialogAsset, GameObject parentGameObject)
        {
            if (ImportPrefabFromPackageSample(dialogAsset, parentGameObject, out var sample))
            {
                var path = PathUtils.GetRelativePath(sample.importPath);
                EditorApplication.delayCall = () =>
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                    if (!obj)
                    {
                        return;
                    }

                    EditorGUIUtility.PingObject(obj);
                    Selection.activeObject = obj;
                };
            }
        }

        internal static bool ImportPrefabFromPackageSample(SampleDialogAsset dialogAsset, GameObject parentGameObject, out Sample sample)
        {
            sample = default;
            if (!EnsureDependency(dialogAsset.Dependency))
            {
                Debug.LogError("Unable to install this sample, failed to satisfy dependencies");
                return false;
            }

            if (CoherenceSamples.Length == 0)
            {
                Debug.LogError("No samples are available in the coherence package");
                return false;
            }

            var sampleIndex = Array.FindIndex(CoherenceSamples, s => s.displayName == dialogAsset.SampleDisplayName);
            if (sampleIndex == -1)
            {
                Debug.LogError("Couldn't find matching sample to import in the coherence package: " +
                               dialogAsset.SampleDisplayName);
                return false;
            }

            var importedSample = false;
            sample = CoherenceSamples[sampleIndex];
            if (!sample.isImported)
            {
                importedSample = true;
                Debug.Log("Importing sample " + sample.displayName);
                if (!sample.Import())
                {
                    Debug.LogError("Error importing sample " + sample.displayName);
                    return false;
                }
            }

            if (importedSample)
            {
                SessionState.SetBool(AddSampleUiAfterDomainReloadKey, true);
            }

            if (string.IsNullOrEmpty(dialogAsset.PrefabFileName))
            {
                return true;
            }

            var prefabBaseDir = sample.importPath.Substring(Application.dataPath.Length + 1);
            var prefabPath = Path.Combine("Assets", prefabBaseDir, dialogAsset.PrefabFileName);

            if (!importedSample)
            {
                EditorApplication.delayCall += () =>
                {
                    AddSampleUI(parentGameObject, prefabPath);
                };
            }
            else
            {
                SessionState.SetString(SampleUiPrefabPathKey, prefabPath);
            }

            return true;
        }

        private static string AddSampleUiAfterDomainReloadKey => "io.coherence.addsampleui";
        private static string SampleUiPrefabPathKey => "io.coherence.addsampleuiprefab";

        [DidReloadScripts]
        private static void AddSampleUiAfterDomainReload()
        {
            var performActions = SessionState.GetBool(AddSampleUiAfterDomainReloadKey, false);

            if (!performActions)
            {
                return;
            }

            if (EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AddSampleUiAfterDomainReload;
                return;
            }

            AssetDatabase.SaveAssets();

            SessionState.SetBool(AddSampleUiAfterDomainReloadKey, false);
            var prefab = SessionState.GetString(SampleUiPrefabPathKey, string.Empty);

            SessionState.SetString(SampleUiPrefabPathKey, string.Empty);

            if (!string.IsNullOrEmpty(prefab))
            {
                AddSampleUI(null, prefab);
            }
        }

        private static void AddSampleUI(GameObject parentGameObject, string prefabPath)
        {
#if HAS_UGUI
            var prefabToInstantiate = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var go = parentGameObject
                ? PrefabUtility.InstantiatePrefab(prefabToInstantiate, parentGameObject.scene) as GameObject
                : PrefabUtility.InstantiatePrefab(prefabToInstantiate) as GameObject;

            var prefabName = Path.GetFileName(prefabPath);
            Undo.RegisterCreatedObjectUndo(go, prefabName);
            if (parentGameObject)
            {
                Undo.SetTransformParent(go.transform, parentGameObject.transform, string.Empty);
            }

            GameObjectCreationCommands.Place(go, parentGameObject);
            Selection.activeGameObject = go;
            if (!Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include))
            {
                _ = ObjectFactory.CreateGameObject(go.scene, HideFlags.None, "EventSystem", typeof(EventSystem),
                    typeof(StandaloneInputModule));
            }
#endif
        }

        public static void AddInterfaceInstanceToScene(Scene scene)
        {
#if HAS_UGUI
            var go =
                PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(sampleUiPath), scene) as
                    GameObject;
            Undo.RegisterCreatedObjectUndo(go, sampleUiName);

            var es = ObjectFactory.CreateGameObject(scene, HideFlags.None, "EventSystem", typeof(EventSystem),
                typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, sampleUiName);
#else
            MessageQueue.AddToQueue(StatusTrackerConstructor.Scopes.ProjectStatus,
                EditorTasks.StartTask(StatusTrackerConstructor.StatusTrackerIDs.SampleUIAdded),
                () => CoherenceHubLayout.DrawLabel(new GUIContent(
                    "Error: Unity UI package not installed")));
#endif
        }
    }
}
