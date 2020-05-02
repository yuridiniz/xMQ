using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using xMQ.Protocol;
using xMQ.SocketsType;

namespace xMQ
{
    public abstract class SocketProtocolController
    {
        protected ProtocolHandler ProtocolHandler { get; set; }

        internal ISocket Socket { get => socket; }

        protected ISocket socket;

        private Uri serverUri;
        private Uri pairAddressUri;

        public SocketProtocolController()
        {
            ProtocolHandler = new ProtocolHandler();
        }

        public void AddProtocolCommand(ProtocolCommand customProtocol)
        {
            var nextCode = ProtocolHandler.SupportedProtocol.Count;
            if(nextCode != 0)
                nextCode = ProtocolHandler.SupportedProtocol.Keys.Max() + 1;

            if (nextCode > byte.MaxValue)
                throw new InvalidOperationException("Limite de protocolos atingido");

            customProtocol.CODE = (byte)nextCode;
            ProtocolHandler.SupportedProtocol.Add(customProtocol.CODE, customProtocol);
        }

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
            serverUri = ValidateAddress(serverAddress);
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
            pairAddressUri = ValidateAddress(pairAddress);
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

        public virtual bool TryReconnect(int timeout = 1000) => Reconnect(timeout, true);
        public virtual void Reconnect(int timeout = 1000) => Reconnect(timeout, false);

        protected virtual bool Reconnect(int timeout, bool silence)
        {
            socket?.Close();
            socket = null;

            if (pairAddressUri != null)
                return Connect(pairAddressUri.ToString(), timeout, silence);
            else if(serverUri != null)
                return Bind(pairAddressUri.ToString(), silence);

            if (!silence)
                throw new Exception("Nenhuma conexão foi iniciada para ser restabelecida");

            return false;
        }

        public bool Close()
        {
            try
            {
                socket?.Close();
                socket?.Dispose();
                socket = null;

                return true;
            }
            catch (SocketException)
            {
                socket?.Dispose();
                socket = null;
                return false;
            }
        }

        internal int HandleMesage(ISocket remote)
        {
            try
            {
                return OnRemoteMessage(remote);
            } catch (Exception ex)
            {
                return -1;
            }
        }

        internal void HandleConnection(ISocket remote)
        {
            try
            {
                OnRemoteConnected(remote);
            }
            catch (Exception ex)
            {
            }
        }

        internal void HandleError(ISocket remote)
        {
            try
            {
                OnError(remote);
            } catch(Exception ex)
            {

            }
        }

        protected abstract int OnRemoteMessage(ISocket remote);

        protected abstract void OnRemoteConnected(ISocket remote);

        protected abstract void OnError(ISocket remote);
    }
}
