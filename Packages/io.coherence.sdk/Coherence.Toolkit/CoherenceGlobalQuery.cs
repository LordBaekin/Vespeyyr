// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using UnityEngine;
    using Entities;

    /// <summary>
    /// Filters what entities are replicated to the client based being global.
    /// </summary>
    /// <remarks>
    /// This query does not take position of entities into account,
    /// use <see cref="Coherence.Toolkit.CoherenceLiveQuery"/> for that.
    /// </remarks>
    [AddComponentMenu("coherence/Queries/Coherence Global Query")]
    [DefaultExecutionOrder(ScriptExecutionOrder.CoherenceQuery)]
    [NonBindable]
    [HelpURL("https://docs.coherence.io/v/1.6/manual/components/coherenceglobalquery")]
    public sealed class CoherenceGlobalQuery : CoherenceQuery
    {
        private bool createdEntityID;

        // for components, we don't expose direct creation of instances - add as component instead
        private CoherenceGlobalQuery()
        {
        }

        protected override void CreateQuery()
        {
            if (EntityID == Entity.InvalidRelative)
            {
                CreateQueryImpl();
            }
            else
            {
                UpdateQuery();
            }
        }

        private void CreateQueryImpl()
        {
            EntityID = Impl.CreateGlobalQuery(Client);
            createdEntityID = true;
        }

        protected override bool NeedsUpdate => false;

        protected override void UpdateQuery(bool queryActive = true)
        {
            if (queryActive)
            {
                if (EntityID == Entity.InvalidRelative)
                {
                    CreateQueryImpl();
                }
                else
                {
                    Impl.AddGlobalQuery(Client, EntityID);
                }
            }
            else
            {
                if (EntityID != Entity.InvalidRelative && createdEntityID)
                {
                    Client.DestroyEntity(EntityID);
                    EntityID = Entity.InvalidRelative;
                    createdEntityID = false;
                }
            }
        }
    }
}
