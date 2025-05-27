// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using Brisk;
    using Brisk.Models;
    using Brook;
    using Common;
    using System;
    using System.Collections.Generic;

    internal interface IOutConnection
    {
        bool CanSend { get; }
        bool UseDebugStreams { get; }
        OutPacket CreatePacket(bool reliable);
        void Send(OutPacket packet);
    }

    internal interface IConnection : IOutConnection
    {
        event Action<ConnectResponse> OnConnect;
        event Action<ConnectionCloseReason> OnDisconnect;
        event Action<ConnectionException> OnError;
        event Action<DeliveryInfo> OnDeliveryInfo;

        ClientID ClientID { get; }
        ConnectionState State { get; }
        Ping Ping { get; }
        byte SendFrequency { get; }
        uint InitialScene { get; set; }
        string TransportDescription { get; }

        void Update();

        void Connect(EndpointData data, ConnectionType connectionType, bool clientAsSimulator, ConnectionSettings settings);
        void Disconnect(ConnectionCloseReason connectionCloseReason, bool serverInitiated);

        void Receive(List<InPacket> buffer);
    }
}
