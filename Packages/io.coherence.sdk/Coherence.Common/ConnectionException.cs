// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Connection
{
    using System;

    public class ConnectionException : Exception
    {
        public ConnectionException(string message) : base(message) { }
        public ConnectionException(string message, Exception innerException) : base(message, innerException) { }

        public override string Message => InnerException == null ? base.Message : $"{InnerException.Message}: {base.Message}";
    }

    public class ConnectionClosedException : ConnectionException
    {
        // The InnerException is likely a TCP SocketException with an error code of
        // SocketError.ConnectionReset or SocketError.Shutdown.
        public ConnectionClosedException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ConnectionTimeoutException : ConnectionException
    {
        public TimeSpan After { get; }

        public override string Message => $"{base.Message}, Timeout: {After:g}";

        public ConnectionTimeoutException(TimeSpan after)
            : this(after, null, null) { }

        public ConnectionTimeoutException(TimeSpan after, string message)
            : this(after, message, null) { }

        public ConnectionTimeoutException(TimeSpan after, string message, Exception innerException)
            : base(message, innerException)
        {
            After = after;
        }
    }

    public class ConnectionDeniedException : ConnectionException
    {
        public ConnectionCloseReason CloseReason { get; }

        public override string Message
        {
            get
            {
                string message = base.Message;
                return !string.IsNullOrEmpty(message)
                    ? $"{base.Message}, DenyReason: {CloseReason}"
                    : $"DenyReason: {CloseReason}";
            }
        }

        public ConnectionDeniedException(ConnectionCloseReason closeReason)
            : this(closeReason, null, null) { }

        public ConnectionDeniedException(ConnectionCloseReason closeReason, string message)
            : this(closeReason, message, null) { }

        public ConnectionDeniedException(ConnectionCloseReason closeReason, string message, Exception innerException)
            : base(message, innerException)
        {
            CloseReason = closeReason;
        }
    }
}
