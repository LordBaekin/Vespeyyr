// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Editor.Features
{
    internal sealed class BackupWorldDataFeature : IFeature
    {
        public bool IsEnabled { get; }
        public bool IsUserConfigurable => PersistenceUtils.UseWorldPersistence;

        public BackupWorldDataFeature()
        {
            IsEnabled =
#if COHERENCE_ENABLE_BACKUP_WORLD_DATA
                true;
#else
                false;
#endif
        }
    }
}
