// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using Coherence;
    using Connection;
    using Entities;
    using Log;
    using UnityEngine;
    using Logger = Log.Logger;

    [NonBindable]
    public abstract class CoherenceQuery : CoherenceBehaviour
    {
        /// <inheritdoc cref="CoherenceBridgeResolver{T}"/>
        public event CoherenceBridgeResolver<CoherenceQuery> BridgeResolve;

        public Entity EntityID { get; set; }

        protected CoherenceBridge bridge;
        protected IClient Client => bridge.Client;
        protected Logger Logger { get; private set; }

        protected bool IsConnected => bridge != null && bridge.IsConnected;

        private void Start()
        {
            Logger = Log.GetLogger<CoherenceQuery>(gameObject.scene);

            if (!CoherenceBridgeStore.TryGetBridge(gameObject.scene, BridgeResolve, this, out bridge))
            {
                enabled = false;
                return;
            }

            bridge.OnAfterFloatingOriginShifted += OnFloatingOriginShiftedInternal;
            bridge.onConnected.AddListener(OnConnected);
            bridge.onDisconnected.AddListener(OnDisconnected);

            if (IsConnected)
            {
                OnConnected(bridge);
            }
        }

        private void OnConnected(CoherenceBridge _)
        {
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            var coherenceSync = GetComponentInParent<CoherenceSync>();
            if (coherenceSync?.EntityState != null)
            {
                EntityID = coherenceSync.EntityState.EntityID;
            }

            CreateQuery();
        }

        private void OnDisconnected(CoherenceBridge _, ConnectionCloseReason __) => EntityID = Entity.InvalidRelative;

        private void Update()
        {
            if (!IsConnected)
            {
                return;
            }

            if (NeedsUpdate)
            {
                UpdateQuery();
            }
        }

        private void OnEnable()
        {
            if (!IsConnected)
            {
                return;
            }

            UpdateQuery();
        }

        private void OnDisable()
        {
            if (!IsConnected)
            {
                return;
            }

            UpdateQuery(false);
        }

        private void OnDestroy()
        {
            if (bridge == null)
            {
                return;
            }

            bridge.OnAfterFloatingOriginShifted -= OnFloatingOriginShiftedInternal;
            bridge.onConnected.RemoveListener(OnConnected);
            bridge.onDisconnected.RemoveListener(OnDisconnected);
        }

        private void OnFloatingOriginShiftedInternal(FloatingOriginShiftArgs args)
        {
            if (!IsConnected)
            {
                return;
            }

            OnFloatingOriginShifted(args);
        }

        protected virtual void OnFloatingOriginShifted(FloatingOriginShiftArgs args) { }
        protected abstract void CreateQuery();
        protected abstract bool NeedsUpdate { get; }
        protected abstract void UpdateQuery(bool queryActive = true);
    }
}
