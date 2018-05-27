using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System;
using System.Threading;

namespace Mirae.Network
{
    public class MiraeLockedBuffer
    {
        private object mLock = new object();
        private List<byte> mBuffer = new List<byte>();
        public void Enqueue(byte[] bytes, int length)
        {
            byte[] newBytes = new byte[length];
            for (int i = 0; i < length; i++)
            {
                newBytes[i] = bytes[i];
            }

            lock (mLock)
            {
                mBuffer.AddRange(newBytes);
            }
        }
        public void Enqueue(byte abyte)
        {
            lock (mLock)
            {
                mBuffer.Add(abyte);
            }
        }
        public byte[] DequeueAll()
        {
            lock (mLock)
            {
                var ret = mBuffer.ToArray();
                mBuffer.Clear();
                return ret;
            }
        }
        public bool TryDequeue(out byte data)
        {
            lock (mLock)
            {
                if (mBuffer.Count > 0)
                {
                    data = mBuffer[0];
                    mBuffer.RemoveAt(0);
                    return true;
                }
                else
                {
                    data = 0;
                    return false;
                }
            }
        }
        public bool TryDequeue(int length, out List<byte> data)
        {
            lock (mLock)
            {
                if (mBuffer.Count >= length)
                {
                    data = mBuffer.GetRange(0, length);
                    mBuffer.RemoveRange(0, length);
                    return true;
                }
                else
                {
                    data = null;
                    return false;
                }
            }
        }
    }

    public class MiraeTcpClient
    {
        private class ClientObject
        {
            private const int RAWBUFFER_SIZE = 10;

            public TcpClient Tcp { get; private set; }
            public string Address { get; private set; }
            public Timer Timer { get; set; }

            public ClientObject(TcpClient client, string address)
            {
                Tcp = client;
                Address = address;
                RawBuffer = new byte[RAWBUFFER_SIZE];
                Buffer = new MiraeLockedBuffer();
            }
            public byte[] RawBuffer { get; private set; }
            public MiraeLockedBuffer Buffer { get; private set; }

            public void CopyRawBufferToBuffer(int count)
            {
                Buffer.Enqueue(RawBuffer, count);
            }
        }

        public enum ClientState
        {
            None,
            State, Connected, Disconnected,
            Error, WrongAddressFormat, UnknownConnectError, ConnectError, UnknownReceiveError, ReceiveError
        }
        public delegate void DelStateChanged(string address, ClientState state);
        public delegate void DelRead(string address, MiraeLockedBuffer buffer);

        private Dictionary<string, ClientObject> mClients = new Dictionary<string, ClientObject>();

        public event DelStateChanged StateChanged = new DelStateChanged((x, y) => { });
        public event DelRead OnRead = new DelRead((x, y) => { });

        public void ConnectAsync(IEnumerable<string> addresses, int port, int timeoutMils)
        {
            foreach (var address in addresses)
            {
                IPAddress ipout;
                if (!IPAddress.TryParse(address, out ipout))
                {
                    StateChanged.Invoke(address, ClientState.WrongAddressFormat);
                    return;
                }

                TcpClient tcp = new TcpClient();
                var clientObject = new ClientObject(tcp, address);
                if (mClients.ContainsKey(address))
                {
                    try
                    {
                        if (mClients[address].Tcp != null && mClients[address].Tcp.Connected)
                            mClients[address].Tcp.Close();
                    }
                    catch { }
                    mClients[address] = clientObject;
                }
                else
                {
                    mClients.Add(address, clientObject);
                }

                var connectResult = tcp.BeginConnect(ipout, port, ConnectHandler, clientObject);
                clientObject.Timer = new Timer(ConnectionTimeoutHandler, new KeyValuePair<ClientObject, IAsyncResult>(clientObject, connectResult), timeoutMils, Timeout.Infinite);
            }
        }

        private void ConnectionTimeoutHandler(object state)
        {
            if (state == null)
                return;
            var keyValue = (KeyValuePair<ClientObject, IAsyncResult>)state;
            var clientObject = keyValue.Key;
            var connectResult = keyValue.Value;
            if (!clientObject.Tcp.Connected)
            {
                try
                {
                    clientObject.Tcp.Close();
                }
                catch { }
                finally
                {
                    StateChanged.Invoke(clientObject.Address, ClientState.ConnectError);
                }
            }

            clientObject.Timer.Dispose();
            clientObject.Timer = null;
        }

        private void ConnectHandler(IAsyncResult ar)
        {
            var clientObject = ar.AsyncState as ClientObject;
            if (clientObject == null)
            {
                StateChanged.Invoke("", ClientState.UnknownConnectError);
                return;
            }

            var tcp = clientObject.Tcp;
            if (!tcp.Connected)
            {
                StateChanged.Invoke(clientObject.Address, ClientState.ConnectError);
                return;
            }

            if (!ar.IsCompleted)
            {
                StateChanged.Invoke(clientObject.Address, ClientState.Disconnected);
                return;
            }
            try
            {
                tcp.EndConnect(ar);
            }
            catch { }

            StateChanged.Invoke(clientObject.Address, ClientState.Connected);
            StartRead(clientObject, tcp);
        }

        private void StartRead(ClientObject clientObject, TcpClient tcp)
        {
            tcp.GetStream().BeginRead(clientObject.RawBuffer, 0, clientObject.RawBuffer.Length, ReadHandler, clientObject);
        }

        private void ReadHandler(IAsyncResult ar)
        {
            var clientObject = ar.AsyncState as ClientObject;
            if (clientObject == null)
            {
                StateChanged.Invoke("", ClientState.UnknownReceiveError);
                return;
            }

            var tcp = clientObject.Tcp;
            if (!tcp.Connected)
            {
                StateChanged.Invoke(clientObject.Address, ClientState.ReceiveError);
                return;
            }

            if (!ar.IsCompleted)
            {
                StateChanged.Invoke(clientObject.Address, ClientState.Disconnected);
                return;
            }

            int countRead = 0;
            try
            {
                countRead = tcp.GetStream().EndRead(ar);
            }
            catch
            {
            }

            if (countRead > 0)
            {
                clientObject.CopyRawBufferToBuffer(countRead);
                OnRead.Invoke(clientObject.Address, clientObject.Buffer);
            }

            StartRead(clientObject, tcp);
        }
    }
}