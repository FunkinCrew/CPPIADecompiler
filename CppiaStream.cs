using System.Text;

namespace CPPIADecompiler;

public sealed class CppiaStream
{
    readonly byte[] data;
    int pos;

    public CppiaStream(byte[] data)
    {
        this.data = data;
    }

    public int Position => pos;
    public int Length => data.Length;
    
    void SkipWhitespace()
    {
        while (true)
        {
            while (pos < data.Length && data[pos] <= 32)
                pos++;

            if (pos < data.Length && data[pos] == (byte)'#')
            {
                while (pos < data.Length && data[pos] != (byte)'\n')
                    pos++;
            }
            else
            {
                break;
            }
        }
    }

    public bool HasMore()
    {
        SkipWhitespace();
        return pos < data.Length;
    }

    public string Token()
    {
        SkipWhitespace();
        int start = pos;
        while (pos < data.Length && data[pos] > 32)
            pos++;
        if (pos == start)
            throw new CppiaException($"expected a token at byte {start}", LineOf(start));
        return Encoding.UTF8.GetString(data, start, pos - start);
    }
    
    public int Int()
    {
        SkipWhitespace();
        int start = pos;
        long result = 0;
        int sign = 1;
        int digits = 0;

        while (pos < data.Length && data[pos] > 32)
        {
            byte c = data[pos];
            if (c == (byte)'-')
            {
                sign = -1;
            }
            else
            {
                int digit = c - '0';
                if (digit < 0 || digit > 9)
                    throw new CppiaException($"expected a digit at byte {pos}", LineOf(pos));
                result = result * 10 + digit;
                digits++;
            }
            pos++;
        }

        if (digits == 0)
            throw new CppiaException($"expected a number at byte {start}", LineOf(start));

        return (int)(result * sign);
    }

    public bool Bool()
    {
        int value = Int();
        if (value > 1)
            throw new CppiaException($"invalid bool {value} at byte {pos}", LineOf(pos));
        return value != 0;
    }
    
    public string ReadString()
    {
        int len = Int();
        if (len < 0)
            throw new CppiaException($"bad string length {len} at byte {pos}", LineOf(pos));

        pos++;

        int start = pos;
        pos += len;
        if (pos > data.Length)
            throw new CppiaException("ran off the end of the file", LineOf(start));

        return Encoding.UTF8.GetString(data, start, len);
    }

    public byte[] ReadBytes(int count)
    {
        if (pos + count > data.Length)
            throw new CppiaException("ran off the end of the file", LineOf(pos));
        byte[] result = new byte[count];
        Array.Copy(data, pos, result, 0, count);
        pos += count;
        return result;
    }

    public void SkipChar()
    {
        if (pos < data.Length)
            pos++;
    }
    
    public int LineOf(int offset)
    {
        int line = 1;
        int end = Math.Min(offset, data.Length);
        for (int i = 0; i < end; i++)
            if (data[i] == (byte)'\n')
                line++;
        return line;
    }

    public int CurrentLine() => LineOf(pos);
}

public sealed class CppiaException : Exception
{
    public int Line { get; }

    public CppiaException(string message, int line = 0) : base(message)
    {
        Line = line;
    }
}
