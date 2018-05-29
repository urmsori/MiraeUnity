using System;
using System.Collections.Generic;

namespace Mirae.CodeBlockEngine
{
    public class AlreadyReservedException : Exception
    {
        public enum ExceptionType
        {
            ReservedWord, Duplicated
        }
        private static Dictionary<ExceptionType, string> sMessageList = new Dictionary<ExceptionType, string>()
            {
                { ExceptionType.ReservedWord, "The Block ID is reserved. 9 <= Custom ID <= 254"},
                { ExceptionType.Duplicated, "The Block ID is duplicated" }
            };
        public struct ReservedWord { }
        public struct Custom { }
        public AlreadyReservedException(ExceptionType type) : base(sMessageList[type]) { }
    }

    public class MiraeEnvironment
    {
        public const byte CUSTOM_ID_MINIMUM = 9;
        private static Dictionary<byte, IBlock> sReservedWord = new Dictionary<byte, IBlock>
        {
            { byte.MinValue, null },
            { 1, new IfBlock(1, null, null) },
            { 2, new WhileBlock(2, null, null) },
            { 3, new FunctionCallBlock(3, "첫번째 함수를", null, NetworkBlockName.F1) },
            { 4, new FunctionCallBlock(4, "두번째 함수를", null, NetworkBlockName.F2) },
            { 5, new FunctionCallBlock(5, "세번째 함수를", null, NetworkBlockName.F3) },
            { 8, new CloseBracektBlock(8, null, null) },
            { byte.MaxValue, null },
        };
        private Dictionary<byte, IBlock> mEnvironment = new Dictionary<byte, IBlock>();
        public Dictionary<byte, IBlock> Value { get { return mEnvironment; } }

        public MiraeEnvironment()
        {
            foreach (var env in sReservedWord)
            {
                mEnvironment.Add(env.Key, env.Value);
            }
        }

        public void AddExecuteBlock(byte id, string context, Action callback)
        {
            CheckId(id);

            mEnvironment.Add(id, new ExecuteBlock(id, context, null, callback));
        }

        public void AddConditionBlock(byte id, string context, FuncCondition conditionFunction)
        {
            CheckId(id);

            mEnvironment.Add(id, new ConditionBlock(id, context, () => { return conditionFunction(); }));
        }

        public void AddIfBlock(byte id, string context, FuncCondition conditionFunction)
        {
            CheckId(id);

            var conditionBlock = new ConditionBlock(id, context, () => { return conditionFunction.Invoke(); });
            mEnvironment.Add(id, new IfBlock(id, null, conditionBlock));
        }

        public void AddWhileBlock(byte id, string context, FuncCondition conditionFunction)
        {
            CheckId(id);

            var conditionBlock = new ConditionBlock(id, context, () => { return conditionFunction.Invoke(); });
            mEnvironment.Add(id, new WhileBlock(id, null, conditionBlock));
        }

        public void Clear()
        {
            mEnvironment.Clear();
            foreach (var env in sReservedWord)
            {
                mEnvironment.Add(env.Key, env.Value);
            }
        }

        private void CheckId(byte id)
        {
            if (id < CUSTOM_ID_MINIMUM)
            {
                throw new AlreadyReservedException(AlreadyReservedException.ExceptionType.ReservedWord);
            }
            if (mEnvironment.ContainsKey(id))
            {
                throw new AlreadyReservedException(AlreadyReservedException.ExceptionType.Duplicated);
            }
        }
    }
}
