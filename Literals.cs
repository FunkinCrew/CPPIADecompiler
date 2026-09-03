using System.Text;

namespace CPPIADecompiler;

public static class Literals
{
    public static string Quote(string value)
    {
        var text = new StringBuilder("\"");

        foreach (char c in value)
        {
            switch (c)
            {
                case '"':
                    text.Append("\\\"");
                    break;
                case '\\':
                    text.Append("\\\\");
                    break;
                case '\n':
                    text.Append("\\n");
                    break;
                case '\r':
                    text.Append("\\r");
                    break;
                case '\t':
                    text.Append("\\t");
                    break;
                default:
                    if (c < 32)
                        text.Append("\\x").Append(((int)c).ToString("x2"));
                    else
                        text.Append(c);
                    break;
            }
        }

        text.Append('"');
        return text.ToString();
    }
}
