using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowExec
{
    public static class CommandLine
    {
        public static List<string> Parse(string input)
        {
            List<string> tokens = new List<string>();
            StringBuilder currentToken = new StringBuilder();
            bool inSingleQuote = false;
            bool inDoubleQuote = false;
            bool escapeNext = false;

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (escapeNext)
                {
                    // 转义字符：直接添加下一个字符（无论是什么）
                    currentToken.Append(c);
                    escapeNext = false;
                    continue;
                }

                switch (c)
                {
                    case '\\':
                        // 设置转义标志（下一个字符将被直接添加）
                        escapeNext = true;
                        break;

                    case '\'':
                        if (!inDoubleQuote)
                        {
                            inSingleQuote = !inSingleQuote; // 切换单引号状态
                        }
                        else
                        {
                            currentToken.Append(c); // 双引号内的单引号作为普通字符
                        }
                        break;

                    case '"':
                        if (!inSingleQuote)
                        {
                            inDoubleQuote = !inDoubleQuote; // 切换双引号状态
                        }
                        else
                        {
                            currentToken.Append(c); // 单引号内的双引号作为普通字符
                        }
                        break;

                    case ' ':
                        if (inSingleQuote || inDoubleQuote)
                        {
                            // 引号内的空格保留
                            currentToken.Append(c);
                        }
                        else if (currentToken.Length > 0)
                        {
                            // 结束当前令牌
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                        }
                        break;

                    default:
                        // 所有其他字符直接添加到当前令牌
                        currentToken.Append(c);
                        break;
                }
            }

            // 处理最后一个令牌
            if (currentToken.Length > 0)
            {
                tokens.Add(currentToken.ToString());
            }

            return tokens;
        }
    }
}
