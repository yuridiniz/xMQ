using System;
using System.Collections.Generic;
using System.Text;
using xMQ.SocketsType;

namespace xMQ
{
    public abstract class SocketProtocolController
    {
        protected ISocket socket;

        protected Uri ValidateAddress(string serverAddress)
        {
            if (socket != null)
                throw new NotSupportedException("Unable to start another connection on a connection that has already started");

            Uri serverUri;
            if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out serverUri))
                throw new ArgumentException("serverAddress has a invalid value, use the format protocol://ip:port. Exemple: tcp://127.0.0.1:5000, see documentations for more protocol information");

            if (serverUri.Port <= 0 || serverUri.Port > 65535)
                throw new ArgumentException("Port range is not valid, enter a value between 1 and 65535");

            return serverUri;
        }

        protected ISocket GetSocketConnectionProtocol(Uri serverUri)
        {
            ISocket socketConnection;
            if (serverUri.Scheme == TcpSocket.SCHEME)
            {
                socketConnection = new TcpSocket(serverUri, this);
            }
            else if (serverUri.Scheme == UdpSocket.SCHEME)
            {
                socketConnection = new UdpSocket(serverUri);
            }
            else if (serverUri.Scheme == TcpSocket.SCHEME)
            {
                socketConnection = new IpcSocket(serverUri.Host);
            }
            else
            {
                throw new NotSupportedException($"Scheme '{serverUri.Scheme}' is not valid, see documentations for more protocol information");
            }

            return socketConnection;
        }

        public virtual bool TryBind(string serverAddress) => Bind(serverAddress, true);
        public virtual void Bind(string serverAddress) => Bind(serverAddress, false);

        protected virtual bool Bind(string serverAddress, bool silence)
        {
            Uri serverUri = ValidateAddress(serverAddress);
            var _socket = GetSocketConnectionProtocol(serverUri);

            try
            {
                _socket.Bind();
                socket = _socket;

                return true;
            }
            catch (Exception ex)
            {
                if (!silence)
                    throw ex;

                return false;
            }
        }

        public virtual bool TryConnect(string pairAddress, int timeout = 1000) => Connect(pairAddress, timeout, true);
        public virtual void Connect(string pairAddress, int timeout = 1000) => Connect(pairAddress, timeout, false);

        protected virtual bool Connect(string pairAddress, int timeout, bool silence)
        {
            Uri pairAddressUri = ValidateAddress(pairAddress);
            var _socket = GetSocketConnectionProtocol(pairAddressUri);

            try
            {
                _socket.Connect(timeout);
                socket = _socket;

                return true;
            }
            catch (Exception ex)
            {
                socket?.Close();
                socket = null;

                if (!silence)
                    throw ex;

                return false;
            }
        }

        internal void HandleMesage(ISocket remote)
        {
            OnRemoteMessage(remote);
        }

        internal void HandleConnection(ISocket remote)
        {
            OnRemoteConnected(remote);
        }

        internal void HandleError(ISocket remote)
        {
            OnError(remote);
        }

        protected abstract void OnRemoteMessage(ISocket remote);

        protected abstract void OnRemoteConnected(ISocket remote);

        protected abstract void OnError(ISocket remote);
    }
}
