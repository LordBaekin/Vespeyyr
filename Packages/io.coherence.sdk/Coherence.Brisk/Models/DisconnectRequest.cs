// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Brisk.Models
{
    using Brook;
    using Connection;

    public class DisconnectRequest : IOobMessage
    {
        public bool IsReliable { get; set; }
        public OobMessageType Type => OobMessageType.DisconnectRequest;

        public ConnectionCloseReason Reason { get; }

        public DisconnectRequest(ConnectionCloseReason reason)
        {
            Reason = reason;
        }

        public override string ToString()
        {
            return $"{nameof(DisconnectRequest)}: [ Reason: {Reason} ]";
        }

        public void Serialize(IOutOctetStream outStream, uint _)
        {
            outStream.WriteUint8((byte)Reason);
        }

        public static DisconnectRequest Deserialize(IInOctetStream inStream, uint _)
        {
            var reason = (ConnectionCloseReason)inStream.ReadUint8();
            return new DisconnectRequest(reason);
        }
    }
}
