using System;

namespace Mirae
{
    public enum NetworkBlockName
    {
        None = 0,
        LocalStarting = 1,
        Starting = 2,
        F1 = 3,
        F2 = 4,
        F3 = 5
    }

    public enum BlockType { None, If, While, CloseBracket, Condition, FunctionCall, Execute }
    public abstract class IBlock
    {
        public byte Id { get; private set; }
        public abstract BlockType BlockType { get; }
        public IBlock Next { get; private set; }
        public virtual object Context { get; protected set; }

        public IBlock(byte id, IBlock prev)
        {
            Id = id;
            if (prev != null)
                prev.Next = this;
        }
    }
    public abstract class IOpenBracektBlock : IBlock
    {
        public IOpenBracektBlock(byte id, IBlock prev) : base(id, prev) { }

        public CloseBracektBlock CloseBracekt { get; private set; }
        public void SetCloseBracekt(CloseBracektBlock closeBracekt)
        {
            CloseBracekt = closeBracekt;
        }
    }

    public delegate bool FuncCondition(object condition);
    public delegate bool FuncCondition<T>(T condition);

    public class IfBlock : IOpenBracektBlock
    {
        public IfBlock(byte id, IBlock prev, ConditionBlock condition) : base(id, prev)
        {
            Condition = condition;
        }

        public override BlockType BlockType { get { return BlockType.If; } }

        public ConditionBlock Condition { get; private set; }
    }

    public class WhileBlock : IfBlock
    {
        public WhileBlock(byte id, IBlock prev, ConditionBlock condition) : base(id, prev, condition)
        {
        }

        public override BlockType BlockType { get { return BlockType.While; } }
    }

    public class CloseBracektBlock : IBlock
    {
        public CloseBracektBlock(byte id, IBlock prev, IOpenBracektBlock openBlock) : base(id, prev)
        {
            OpenBracektBlock = openBlock;
        }

        public override BlockType BlockType { get { return BlockType.CloseBracket; } }
        public IOpenBracektBlock OpenBracektBlock { get; set; }
    }

    public class ConditionBlock : IBlock
    {
        public ConditionBlock(byte id, object context, FuncCondition conditionFunction) : base(id, null)
        {
            Context = context;
            ConditionFunction = conditionFunction;
        }

        public override BlockType BlockType { get { return BlockType.Condition; } }
        public FuncCondition ConditionFunction { get; private set; }
    }

    public class ExecuteBlock : IBlock
    {
        public ExecuteBlock(byte id, IBlock prev, Action callback) : base(id, prev)
        {
            Callback = callback;
        }

        public override BlockType BlockType { get { return BlockType.Execute; } }

        public Action Callback { get; private set; }
    }

    public class FunctionCallBlock : IBlock
    {
        public FunctionCallBlock(byte id, IBlock prev, NetworkBlockName targetName) : base(id, prev)
        {
            TargetName = targetName;
        }

        public override BlockType BlockType { get { return BlockType.Execute; } }

        public NetworkBlockName TargetName { get; private set; }
    }

}
