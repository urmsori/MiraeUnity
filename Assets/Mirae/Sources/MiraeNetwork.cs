using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Mirae.Network
{
    public enum MiraeNetworkState
    {
        None,
        Connected,
        Disconnected
    }

    internal enum Protocol
    {
        ProtocolEnd = byte.MaxValue,
    }

    internal class ParseBuffer
    {
        private MiraeLockedBuffer mBuffer = new MiraeLockedBuffer();

        public event Action<ABlockData[]> OnRead;

        public void Add(byte data)
        {
            switch (data)
            {
                case (byte)Protocol.ProtocolEnd:
                    var dataArray = mBuffer.DequeueAll();
                    var blockData = RawDataToBlockData(dataArray);
                    OnRead.Invoke(blockData);
                    break;
                default:
                    mBuffer.Enqueue(data);
                    break;
            }
        }
        public bool TryRemove(out byte data)
        {
            return mBuffer.TryDequeue(out data);
        }

        public ABlockData[] RawDataToBlockData(byte[] data)
        {
            ABlockData[] ret = new ABlockData[data.Length / 2];
            for (int i = 0; i < data.Length / 2; i++)
            {
                ret[i] = new ABlockData(data[i * 2], data[i * 2 + 1]);
            }
            return ret;
        }
    }

    public struct ABlockData
    {
        public byte Id { get; private set; }
        public byte ConditionId { get; private set; }

        public ABlockData(byte id, byte conditionId)
        {
            Id = id;
            ConditionId = conditionId;
        }
    }

    public class MiraeNetworkBlock : IEnumerable<ABlockData>
    {
        public NetworkBlockName Name { get; private set; }
        public MiraeNetworkState State { get; set; }
        public ABlockData[] Data { get; private set; }
        public event Action<MiraeNetworkBlock> OnRead;

        internal bool Updated { get; set; }
        internal ParseBuffer RawBuffer { get; private set; }

        public MiraeNetworkBlock(NetworkBlockName name)
        {
            Name = name;
            State = MiraeNetworkState.None;
            RawBuffer = new ParseBuffer();
            Data = new ABlockData[0];
            Updated = false;
            RawBuffer.OnRead += RawBuffer_OnRead;
        }

        private void RawBuffer_OnRead(ABlockData[] data)
        {
            Updated = true;
            Data = data;
            OnRead.Invoke(this);
        }

        public IEnumerator<ABlockData> GetEnumerator()
        {
            return Data.AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Data.GetEnumerator();
        }
    }

    public class MiraeNetwork
    {
        private const int PORT = 7979;

        private MiraeTcpClient mClient = new MiraeTcpClient();
        private Dictionary<string, NetworkBlockName> mAddressMapper = new Dictionary<string, NetworkBlockName>();
        private List<MiraeNetworkBlock> mReadyList = new List<MiraeNetworkBlock>();
        private Dictionary<NetworkBlockName, MiraeNetworkBlock> mNetworkBlocks = new Dictionary<NetworkBlockName, MiraeNetworkBlock>();
        private bool mStartConnect = false;

        public delegate void DelButtonPushed(IEnumerable<MiraeNetworkBlock> blocks);
        public event Action<bool> ConnectDone = (x) => { };
        public event DelButtonPushed ButtonPushed = (x) => { };

        public MiraeNetwork()
        {
            mClient.StateChanged += MClient_StateChanged;
            mClient.OnRead += MClient_OnRead;
        }

        public void ConnectAsync()
        {
            mStartConnect = true;

            mAddressMapper.Clear();
            mNetworkBlocks.Clear();
            mReadyList.Clear();

            var enumValues = Enum.GetValues(typeof(NetworkBlockName));
            List<string> addressList = new List<string>();
            foreach (var enumValue in enumValues)
            {
                var name = (NetworkBlockName)enumValue;
                if (name == NetworkBlockName.None)
                    continue;

                var addr = GetAddress(name);

                var block = new MiraeNetworkBlock(name);
                block.OnRead += Block_OnRead;

                mAddressMapper.Add(addr, name);
                addressList.Add(addr);
                mNetworkBlocks.Add(name, block);
            }

            mClient.ConnectAsync(addressList, PORT, 1000);
        }

        private void MClient_StateChanged(string address, MiraeTcpClient.ClientState state)
        {
            if (!mAddressMapper.ContainsKey(address))
                return;
            var name = mAddressMapper[address];

            switch (state)
            {
                case MiraeTcpClient.ClientState.Connected:
                    mNetworkBlocks[name].State = MiraeNetworkState.Connected;
                    break;
                default:
                    mNetworkBlocks[name].State = MiraeNetworkState.Disconnected;
                    break;
            }

            CheckAllConnectionDone();
        }

        private bool CheckAllConnectionDone()
        {
            foreach (var block in mNetworkBlocks)
            {
                if (block.Value.State == MiraeNetworkState.None)
                    return false;
            }

            foreach (var block in mNetworkBlocks)
            {
                if (block.Value.State == MiraeNetworkState.Connected)
                    mReadyList.Add(block.Value);
            }

            if (mStartConnect)
            {
                mStartConnect = false;
                ConnectDone.Invoke(mReadyList.Count > 0);
            }
            return true;
        }

        private void MClient_OnRead(string address, MiraeLockedBuffer buffer)
        {
            if (!mAddressMapper.ContainsKey(address))
                return;
            var color = mAddressMapper[address];

            var data = buffer.DequeueAll();
            foreach (var a in data)
            {
                mNetworkBlocks[color].RawBuffer.Add(a);
            }
        }


        private void Block_OnRead(MiraeNetworkBlock obj)
        {
            CheckAllReadDone();
        }

        private bool CheckAllReadDone()
        {
            foreach (var block in mReadyList)
            {
                if (!block.Updated)
                    return false;
            }

            foreach (var block in mReadyList)
            {
                block.Updated = false;
            }

            ButtonPushed.Invoke(mReadyList);
            return true;
        }

        private string GetAddress(NetworkBlockName name)
        {
            switch (name)
            {
                case NetworkBlockName.None:
                    throw new Exception("Wrong NetworkBlockName");
                case NetworkBlockName.LocalStarting:
                    return "127.0.0.1";
                default:
                    return "192.168.0." + (int)name;
            }
        }
    }
}
