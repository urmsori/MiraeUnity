using Mirae.CodeBlockEngine;
using Mirae.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

namespace Mirae
{
    public enum MiraeBranchType { Condition, If, While }

    [Serializable]
    public class MiraeConnectEvent : UnityEvent<bool> { }
    [Serializable]
    public class MiraeBuildEvent : UnityEvent<MiraeBuildResult> { }
    [Serializable]
    public class MiraeRuntimeEvent : UnityEvent<MiraeRuntimeResult> { }
    [Serializable]
    public class MiraeExecuteEvent : UnityEvent { }
    [Serializable]
    public class MiraeConditionEvent : SerializableCallback<bool> { }

    [Serializable]
    public class MiraeIdExecuteEventPair
    {
        public byte id = 9;
        public string context = "";
        public MiraeExecuteEvent callback = null;
    }
    [Serializable]
    public class MiraeIdConditionEventPair
    {
        public byte id = 9;
        public MiraeBranchType type = MiraeBranchType.If;
        public string context = "";
        public MiraeConditionEvent callback = null;
    }


    public class MiraeService : MonoBehaviour
    {
        public bool connectOnStart = true;
        public MiraeConnectEvent onConnectDone;
        public MiraeBuildEvent onBuildCode;
        public MiraeRuntimeEvent onRunCode;
        public MiraeIdExecuteEventPair[] executeEvents;
        public MiraeIdConditionEventPair[] conditionEvents;

        public MiraeEnvironment Environment { get; private set; }

        MiraeBlockCodeEngine mEngine = null;

        private MiraeNetwork mNetwork = new MiraeNetwork();
        private Queue<bool> mReadyQueue = new Queue<bool>();
        private Queue<IEnumerable<MiraeNetworkBlock>> mReadQueue = new Queue<IEnumerable<MiraeNetworkBlock>>();
        private object mReadyQueueLock = new object();
        private object mReadQueueLock = new object();

        public MiraeService()
        {
            Environment = new MiraeEnvironment();
        }

        private void AddInitCallbacks()
        {
            foreach (var pair in executeEvents)
            {
                Environment.AddExecuteBlock(pair.id, pair.context, () => { if (pair.callback != null) pair.callback.Invoke(); });
            }
            foreach (var pair in conditionEvents)
            {
                switch (pair.type)
                {
                    case MiraeBranchType.Condition:
                        Environment.AddConditionBlock(pair.id, pair.context, () => { return pair.callback.Invoke(); });
                        break;
                    case MiraeBranchType.If:
                        Environment.AddIfBlock(pair.id, pair.context, () => { return pair.callback.Invoke(); });
                        break;
                    case MiraeBranchType.While:
                        Environment.AddWhileBlock(pair.id, pair.context, () => { return pair.callback.Invoke(); });
                        break;
                    default:
                        break;
                }

            }
        }

        private void Awake()
        {
            AddInitCallbacks();
            mNetwork.ConnectDone += MNetwork_Ready;
            mNetwork.ButtonPushed += MNetwork_ButtonPushed;
        }

        private void Start()
        {
            if (connectOnStart)
                Connect();
        }

        private void MNetwork_ButtonPushed(IEnumerable<MiraeNetworkBlock> blocks)
        {
            lock (mReadQueueLock)
            {
                mReadQueue.Enqueue(blocks);
            }
        }

        private void MNetwork_Ready(bool success)
        {
            lock (mReadyQueueLock)
            {
                mReadyQueue.Enqueue(success);
            }
        }

        private void Update()
        {
            bool updated = false;
            do
            {
                updated = false;
                bool success = false;
                lock (mReadyQueueLock)
                {
                    if (mReadyQueue.Count > 0)
                    {
                        updated = true;
                        success = mReadyQueue.Dequeue();
                    }
                }
                if (updated)
                {
                    mEngine = new MiraeBlockCodeEngine(Environment);
                    if (onConnectDone != null)
                        onConnectDone.Invoke(success);
                }
            } while (updated);

            do
            {
                updated = false;
                IEnumerable<MiraeNetworkBlock> blocks = null;
                lock (mReadQueueLock)
                {
                    if (mReadQueue.Count > 0)
                    {
                        updated = true;
                        blocks = mReadQueue.Dequeue();
                    }
                }
                if (updated)
                {
                    var buildResult = mEngine.Build(blocks);
                    if (onBuildCode != null)
                        onBuildCode.Invoke(buildResult);
                }
            } while (updated);
        }

        public void Connect()
        {
            mNetwork.ConnectAsync();
        }

        public void RunOnce()
        {
            if (mEngine == null)
                return;
            var result = mEngine.RunOnce();
            if (onRunCode != null)
                onRunCode.Invoke(result);
        }
    }
}