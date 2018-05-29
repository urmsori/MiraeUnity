using System.Collections;
using System.Collections.Generic;

namespace Mirae.CodeBlockEngine
{
    public static class MiraeContextConverter
    {
        public static string Convert(Dictionary<NetworkBlockName, List<IBlock>> blockCodes)
        {
            string ret = "";
            int tabCount = 0;
            foreach (var networkBlock in blockCodes)
            {
                ret += string.Format("<{0}>\n", networkBlock.Key);
                foreach (var block in networkBlock.Value)
                {
                    switch (block.BlockType)
                    {
                        case BlockType.None:
                            break;
                        case BlockType.If:
                            InsertTab(ref ret, tabCount);
                            InsertIfBlock(ref ret, block as IfBlock);
                            tabCount++;
                            break;
                        case BlockType.While:
                            InsertTab(ref ret, tabCount);
                            InsertIfBlock(ref ret, block as IfBlock);
                            tabCount++;
                            break;
                        case BlockType.CloseBracket:
                            switch ((block as CloseBracektBlock).OpenBracektBlock.BlockType)
                            {
                                case BlockType.If:
                                    break;
                                case BlockType.While:
                                    InsertTab(ref ret, tabCount);
                                    ret += "'만약에'로 다시 돌아가요\n";
                                    break;
                                default:
                                    break;
                            }
                            tabCount--;
                            break;
                        case BlockType.Condition:
                            break;
                        case BlockType.FunctionCall:
                            InsertTab(ref ret, tabCount);
                            ret += block.Context + "\n";
                            break;
                        case BlockType.Execute:
                            InsertTab(ref ret, tabCount);
                            ret += block.Context + "\n";
                            break;
                        default:
                            break;
                    }
                }
            }
            return ret;
        }

        public static void InsertIfBlock(ref string src, IfBlock block)
        {
            src += "만약에 ";
            src += block.Condition.Context;
            src += "\n";
        }

        public static void InsertTab(ref string src, int tabCount)
        {
            for (int i = 0; i < tabCount; i++)
            {
                src += "\t";
            }
        }
    }
}