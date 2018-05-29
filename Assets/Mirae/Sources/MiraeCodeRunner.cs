using System;
using System.Collections.Generic;

namespace Mirae
{
    public enum MiraeRuntimeError
    {
        Success,
        NotBuild,
        WrongNetworkBlockName,
        UnknownBlock,
        NullConditionFunction,
        NullCallback,
        UnknownOpenBracekt,
        WrongConditionPosition,
    }
    public enum MiraeRuntimeResultType
    {
        Running,
        End,
        Error,
    }

    public struct MiraeRuntimeResult
    {
        public MiraeRuntimeResult(MiraeRuntimeResultType result, MiraeRuntimeError error, ResultTarget target)
        {
            Result = result;
            Error = error;
            Target = target;
        }

        public class ResultTarget
        {
            public ResultTarget(NetworkBlockName name, byte blockId)
            {
                Name = name;
                BlockId = blockId;
            }

            public NetworkBlockName Name { get; private set; }
            public byte BlockId { get; private set; }
        }

        public MiraeRuntimeResultType Result { get; private set; }
        public MiraeRuntimeError Error { get; private set; }
        public ResultTarget Target { get; private set; }
    }

}

namespace Mirae.CodeBlockEngine
{
    public class MiraeCodeRunner
    {
        private Dictionary<NetworkBlockName, List<IBlock>> mBlockCodes;
        private NetworkBlockName mMainName;

        private NetworkBlockName mCurrentName = NetworkBlockName.None;
        private Stack<IBlock> mFunctionStack = new Stack<IBlock>();
        private MiraeRuntimeResultType mRunType = MiraeRuntimeResultType.End;

        private IBlock mCurrentBlock = null;

        public MiraeCodeRunner(Dictionary<NetworkBlockName, List<IBlock>> blockCodes, NetworkBlockName startingBlockName)
        {
            mBlockCodes = blockCodes;
            mMainName = startingBlockName;
            mCurrentName = startingBlockName;
        }

        public MiraeRuntimeResult Next()
        {
            if (!mBlockCodes.ContainsKey(mCurrentName))
            {
                // Wrong BlockName Error
                mRunType = MiraeRuntimeResultType.Error;
                return new MiraeRuntimeResult(MiraeRuntimeResultType.Error, MiraeRuntimeError.WrongNetworkBlockName, new MiraeRuntimeResult.ResultTarget(mCurrentName, 0));
            }

            #region CheckCurrentBlock
            if (mCurrentBlock == null)
            {
                var codes = mBlockCodes[mCurrentName];
                if (codes.Count == 0)
                {
                    if (IsCodeEnd())
                    {
                        // End
                        mRunType = MiraeRuntimeResultType.End;
                        return new MiraeRuntimeResult(MiraeRuntimeResultType.End, MiraeRuntimeError.Success, null);
                    }
                }
                else
                {
                    if (mRunType != MiraeRuntimeResultType.Running)
                        mCurrentBlock = codes[0];
                }
            }
            #endregion

            if (mCurrentBlock == null)
            {
                if (IsCodeEnd())
                {
                    // End
                    mRunType = MiraeRuntimeResultType.End;
                    return new MiraeRuntimeResult(MiraeRuntimeResultType.End, MiraeRuntimeError.Success, null);
                }
            }

            MiraeRuntimeError error = MiraeRuntimeError.Success;
            var block = mCurrentBlock;
            switch (block.BlockType)
            {
                case BlockType.If:
                    error = RunIf(block as IfBlock);
                    break;
                case BlockType.While:
                    error = RunWhile(block as WhileBlock);
                    break;
                case BlockType.CloseBracket:
                    error = RunCloseBracekt(block as CloseBracektBlock);
                    break;
                case BlockType.Condition:
                    // WrongConditionPosition Error
                    mRunType = MiraeRuntimeResultType.Error;
                    return new MiraeRuntimeResult(MiraeRuntimeResultType.Error, MiraeRuntimeError.WrongConditionPosition, new MiraeRuntimeResult.ResultTarget(mCurrentName, block.Id));
                case BlockType.FunctionCall:
                    RunFunctionCall(block as FunctionCallBlock);
                    break;
                case BlockType.Execute:
                    error = RunExecute(block as ExecuteBlock);
                    break;
                default:
                    // Unknown Block Error
                    mRunType = MiraeRuntimeResultType.Error;
                    return new MiraeRuntimeResult(MiraeRuntimeResultType.Error, MiraeRuntimeError.UnknownBlock, new MiraeRuntimeResult.ResultTarget(mCurrentName, block.Id));
            }
            if (error != MiraeRuntimeError.Success)
            {
                // error
                mRunType = MiraeRuntimeResultType.Error;
                return new MiraeRuntimeResult(MiraeRuntimeResultType.Error, error, new MiraeRuntimeResult.ResultTarget(mCurrentName, block.Id));
            }
            else
            {
                mRunType = MiraeRuntimeResultType.Running;
                if (block == null)
                {
                    return new MiraeRuntimeResult(MiraeRuntimeResultType.Running, MiraeRuntimeError.Success, new MiraeRuntimeResult.ResultTarget(mCurrentName, 0));
                }

                if (block.BlockType == BlockType.Execute)
                {
                    return new MiraeRuntimeResult(MiraeRuntimeResultType.Running, MiraeRuntimeError.Success, new MiraeRuntimeResult.ResultTarget(mCurrentName, block.Id));
                }
                else
                {
                    return Next();
                }
            }
        }

        private MiraeRuntimeError RunExecute(ExecuteBlock block)
        {
            if (block.Callback == null)
            {
                return MiraeRuntimeError.NullCallback;
            }

            block.Callback.Invoke();
            mCurrentBlock = block.Next;

            return MiraeRuntimeError.Success;
        }

        private void RunFunctionCall(FunctionCallBlock block)
        {
            mCurrentName = block.TargetName;
            mFunctionStack.Push(block);
            mCurrentBlock = null;
        }

        private MiraeRuntimeError RunCloseBracekt(CloseBracektBlock closeBlock)
        {
            var openBlock = closeBlock.OpenBracektBlock;

            switch (openBlock.BlockType)
            {
                case BlockType.If:
                    mCurrentBlock = closeBlock.Next;
                    break;
                case BlockType.While:
                    return RunWhile(openBlock as WhileBlock);
                default:
                    return MiraeRuntimeError.UnknownOpenBracekt;
            }

            return MiraeRuntimeError.Success;
        }

        private bool IsCodeEnd()
        {
            do
            {
                if (mCurrentName == mMainName)
                {
                    // End
                    return true;
                }
                else
                {
                    // Function End
                    var block = mFunctionStack.Pop();
                    mCurrentBlock = block.Next;
                }
            } while (mCurrentBlock == null);
            return false;
        }

        private MiraeRuntimeError RunIf(IfBlock block)
        {
            if (block.Condition.ConditionFunction == null)
            {
                return MiraeRuntimeError.NullConditionFunction;
            }

            var conditionResult = block.Condition.ConditionFunction.Invoke();
            if (conditionResult)
            {
                mCurrentBlock = block.Next;
            }
            else
            {
                mCurrentBlock = block.CloseBracekt.Next;
            }

            return MiraeRuntimeError.Success;
        }

        private MiraeRuntimeError RunWhile(WhileBlock block)
        {
            return RunIf(block);
        }

    }
}
