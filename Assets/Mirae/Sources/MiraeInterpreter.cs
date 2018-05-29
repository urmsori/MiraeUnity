using Mirae.Network;
using System;
using System.Collections.Generic;

namespace Mirae
{
    public enum MiraeBuildError
    {
        Success,
        UnknownError,
        NoStartingBlock,
        UnknownBlock,
        NoCondition,
        UndefinedBlock,
        WrongBlockType,
        NoLeftBracekt,
        NoRightBracekt,
        WrongConditionPosition,
    }

    public struct MiraeBuildResult
    {
        public MiraeBuildResult(MiraeBuildError result, ErrorTarget target, Dictionary<NetworkBlockName, List<IBlock>> blockCodes)
        {
            Result = result;
            ErrorDetail = target;
            BlockCodes = blockCodes;
            StartingBlockName = NetworkBlockName.None;
            Context = "";
        }
        public MiraeBuildResult(Dictionary<NetworkBlockName, List<IBlock>> blockCodes, NetworkBlockName startingBlockName, string context)
        {
            Result = MiraeBuildError.Success;
            ErrorDetail = null;
            BlockCodes = blockCodes;
            StartingBlockName = startingBlockName;
            Context = context;
        }

        public class ErrorTarget
        {
            public ErrorTarget(NetworkBlockName name, byte blockId)
            {
                Name = name;
                BlockId = blockId;
            }

            public NetworkBlockName Name { get; private set; }
            public byte BlockId { get; private set; }
        }

        public MiraeBuildError Result { get; private set; }
        public ErrorTarget ErrorDetail { get; private set; }
        public NetworkBlockName StartingBlockName { get; private set; }
        public Dictionary<NetworkBlockName, List<IBlock>> BlockCodes { get; private set; }
        public string Context { get; private set; }
    }
}

namespace Mirae.CodeBlockEngine
{
    public class MiraeInterpreter
    {
        private enum StartingBlockType { None, Local, Remote }

        private Dictionary<byte, IBlock> mEnvironments = null;

        public MiraeInterpreter(MiraeEnvironment environments)
        {
            mEnvironments = environments.Value;
        }

        public MiraeBuildResult Build(IEnumerable<MiraeNetworkBlock> networkBlocks)
        {
            MiraeNetworkBlock startingBlock = null;
            foreach (var networkBlock in networkBlocks)
            {
                if (networkBlock.Name == NetworkBlockName.LocalStarting)
                {
                    startingBlock = networkBlock;
                }
                if (networkBlock.Name == NetworkBlockName.Starting && startingBlock == null)
                {
                    startingBlock = networkBlock;
                }
            }

            if (startingBlock == null)
            {
                // Error
                return new MiraeBuildResult(MiraeBuildError.NoStartingBlock, null, null);
            }

            Dictionary<NetworkBlockName, List<IBlock>> dicBlocks = new Dictionary<NetworkBlockName, List<IBlock>>();
            Stack<IOpenBracektBlock> openBracektBlockStack = new Stack<IOpenBracektBlock>();
            foreach (var networkBlock in networkBlocks)
            {
                openBracektBlockStack.Clear();

                List<IBlock> blocks = new List<IBlock>();
                IBlock prevBlock = null;

                foreach (var current in networkBlock)
                {
                    IBlock currentBlock = null;

                    var id = current.Id;
                    if (!mEnvironments.ContainsKey(id))
                    {
                        return new MiraeBuildResult(MiraeBuildError.UndefinedBlock, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                    }
                    var env = mEnvironments[id];

                    switch (env.BlockType)
                    {
                        case BlockType.If:
                            {
                                MiraeBuildError error;
                                error = BuildIfBlock(env as IfBlock, prevBlock, current.ConditionId, out currentBlock);
                                if (error > MiraeBuildError.Success)
                                {
                                    // Error
                                    return new MiraeBuildResult(error, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }

                                openBracektBlockStack.Push(currentBlock as IOpenBracektBlock);
                            }
                            break;
                        case BlockType.While:
                            {
                                MiraeBuildError error;
                                error = BuildWhileBlock(env as WhileBlock, prevBlock, current.ConditionId, out currentBlock);
                                if (error > MiraeBuildError.Success)
                                {
                                    // Error
                                    return new MiraeBuildResult(error, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }

                                openBracektBlockStack.Push(currentBlock as IOpenBracektBlock);
                            }
                            break;
                        case BlockType.CloseBracket:
                            {
                                if (openBracektBlockStack.Count == 0)
                                {
                                    // Bracekt Error
                                    return new MiraeBuildResult(MiraeBuildError.NoLeftBracekt, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }

                                var openBracektBlock = openBracektBlockStack.Pop();
                                MiraeBuildError error = BuildCloseBracekt(env.Id, prevBlock, openBracektBlock, out currentBlock);
                                if (error > MiraeBuildError.Success)
                                {
                                    // Error
                                    return new MiraeBuildResult(error, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }
                            }
                            break;
                        case BlockType.Condition:
                            {
                                // Error
                                return new MiraeBuildResult(MiraeBuildError.WrongConditionPosition, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                            }
                        case BlockType.FunctionCall:
                            {
                                MiraeBuildError error = BuildFunctionCallBlock(env as FunctionCallBlock, prevBlock, out currentBlock);
                                if (error > MiraeBuildError.Success)
                                {
                                    // Error
                                    return new MiraeBuildResult(error, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }
                            }
                            break;
                        case BlockType.Execute:
                            {
                                MiraeBuildError error = BuildExecuteBlock(env as ExecuteBlock, prevBlock, out currentBlock);
                                if (error > MiraeBuildError.Success)
                                {
                                    // Error
                                    return new MiraeBuildResult(error, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    
                    if (currentBlock == null)
                        return new MiraeBuildResult(MiraeBuildError.UnknownBlock, new MiraeBuildResult.ErrorTarget(networkBlock.Name, current.Id), null);

                    // After Create Block
                    blocks.Add(currentBlock);

                    prevBlock = currentBlock;
                }// foreach (var current in block)

                if (openBracektBlockStack.Count > 0)
                {
                    // Bracekt Error
                    return new MiraeBuildResult(MiraeBuildError.NoRightBracekt, new MiraeBuildResult.ErrorTarget(networkBlock.Name, 0), null);
                }

                dicBlocks.Add(networkBlock.Name, blocks);
            }// foreach (var block in networkBlocks)

            var context = MiraeContextConverter.Convert(dicBlocks);
            return new MiraeBuildResult(dicBlocks, startingBlock.Name, context);
        }

        private MiraeBuildError BuildExecuteBlock(ExecuteBlock environment, IBlock prev, out IBlock block)
        {
            block = new ExecuteBlock(environment.Id, environment.Context, prev, environment.Callback);
            return MiraeBuildError.Success;
        }

        private MiraeBuildError BuildCloseBracekt(byte id, IBlock prev, IOpenBracektBlock openBracektBlock, out IBlock block)
        {
            block = new CloseBracektBlock(id, prev, openBracektBlock);
            openBracektBlock.SetCloseBracekt(block as CloseBracektBlock);
            return MiraeBuildError.Success;
        }

        private MiraeBuildError BuildConditionBlock(byte id, out ConditionBlock block)
        {
            if (!mEnvironments.ContainsKey(id))
            {
                block = null;
                return MiraeBuildError.UndefinedBlock;
            }

            var env = mEnvironments[id];
            if (env.BlockType != BlockType.Condition)
            {
                block = null;
                return MiraeBuildError.WrongBlockType;
            }

            var conditionBlock = env as ConditionBlock;
            block = new ConditionBlock(conditionBlock.Id, conditionBlock.Context, conditionBlock.ConditionFunction);
            return MiraeBuildError.Success;
        }

        private MiraeBuildError BuildIfBlock(IfBlock environment, IBlock prev, byte conditionBlockId, out IBlock block)
        {
            ConditionBlock conditionBlock = environment.Condition;
            if (conditionBlock == null)
            {
                MiraeBuildError error = BuildConditionBlock(conditionBlockId, out conditionBlock);
                if (error > MiraeBuildError.Success)
                {
                    block = null;
                    return error;
                }
            }

            block = new IfBlock(environment.Id, prev, conditionBlock);
            return MiraeBuildError.Success;
        }

        private MiraeBuildError BuildWhileBlock(WhileBlock environment, IBlock prev, byte conditionBlockId, out IBlock block)
        {
            ConditionBlock conditionBlock = environment.Condition;
            if (conditionBlock == null)
            {
                MiraeBuildError error = BuildConditionBlock(conditionBlockId, out conditionBlock);
                if (error > MiraeBuildError.Success)
                {
                    block = null;
                    return error;
                }
            }

            block = new WhileBlock(environment.Id, prev, conditionBlock);
            return MiraeBuildError.Success;
        }

        private MiraeBuildError BuildFunctionCallBlock(FunctionCallBlock environment, IBlock prev, out IBlock block)
        {
            block = new FunctionCallBlock(environment.Id, environment.Context, prev, environment.TargetName);
            return MiraeBuildError.Success;
        }

    }
}
