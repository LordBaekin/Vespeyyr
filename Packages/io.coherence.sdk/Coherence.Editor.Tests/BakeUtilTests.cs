namespace Coherence.Editor.Tests
{
    using System;
    using System.Collections;
    using System.IO;
    using Coherence.Tests;
    using Coherence.Toolkit;
    using NUnit.Framework;
    using Portal;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.TestTools;

    public class BakeUtilTest : CoherenceTest, IPostBuildCleanup
    {
        void IPostBuildCleanup.Cleanup()
        {
            ClearBakedData();
            AssetDatabase.Refresh();
        }

        [Test]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void Should_GenerateSchemaFileInAssets_When_GatheringSchema()
        {
            using(new TempPrefab(out var _))
            {
                Assert.That(BakeUtil.GenerateSchema(out var _, out var _), "Gather passed");
                Assert.That(File.Exists(Paths.gatherSchemaPath));
            }
        }

        [Test]
#if COHERENCE_SKIP_LONG_UNIT_TESTS
        [Ignore("Long running test")]
#endif
        public void Should_GenerateADifferentSchemaFileInAssets_When_GatheringSchemaWithDifferentNetworkSetup()
        {
            using(new TempPrefab(out var prefab))
            {
                var schemaId = BakeUtil.SchemaID;
                prefab.AddComponent<MeshRenderer>();
                CoherenceSyncUtils.AddBinding<MeshRenderer>(prefab, "enabled");
                BakeUtil.GenerateSchema(out var _, out var _);
                Assert.AreNotEqual(schemaId, BakeUtil.SchemaID);
            }
        }
        
        
        private sealed class TempPrefab : IDisposable
        {
            private const string DefaultPrefabName = nameof(BakeUtilTest);
            private const string DefaultPrefabPath = "Assets/" + DefaultPrefabName + ".prefab";

            private AssetPath prefabPath;

            public TempPrefab(out GameObject prefab)
            {

                if (File.Exists(DefaultPrefabPath))
                {
                    AssetUtils.DeleteFile(DefaultPrefabPath);
                }

                ClearBakedData();

                var instance = ObjectFactory.CreateGameObject(DefaultPrefabName, typeof(CoherenceSync));
                prefabPath = AssetUtils.GenerateUniqueAssetPath(DefaultPrefabPath);

                prefab = AssetUtils.CreatePrefab
                (
                    instance,
                    ref prefabPath,
                    InteractionMode.AutomatedAction
                );

                _ = BakeUtil.GenerateSchema(out var _, out var _);
            }

            public void Dispose()
            {
                AssetUtils.DeleteFile(prefabPath);
                AssetDatabase.Refresh();
            }
        }

        private static void ClearBakedData()
        {
            Schemas.InvalidateSchemaCache();

            try
            {
                if (File.Exists(Paths.gatherSchemaPath))
                {
                    File.Delete(Paths.gatherSchemaPath);
                    File.Delete(Paths.gatherSchemaPath + ".meta");
                }

                if (Directory.Exists(Paths.defaultSchemaBakePath))
                {
                    Directory.Delete(Paths.defaultSchemaBakePath, true);
                    File.Delete(Paths.defaultSchemaBakePath + ".meta");
                }

                if (Directory.Exists(BakeUtil.OutputFolder))
                {
                    Directory.Delete(BakeUtil.OutputFolder, true);
                    File.Delete(BakeUtil.OutputFolder + ".meta");
                }

                AssetDatabase.Refresh();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}
