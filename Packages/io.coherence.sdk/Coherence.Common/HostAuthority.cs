// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Common
{
    using System;

    [Flags]
    public enum HostAuthority
    {
        CreateEntities = 1 << 0,
        ValidateConnection = 1 << 1,
        KickConnection = 1 << 2,
    }

    public static class HostAuthorityEx
    {
        public static bool Can(this HostAuthority authority, HostAuthority feature)
        {
            return (authority & feature) != 0;
        }
    }
}
