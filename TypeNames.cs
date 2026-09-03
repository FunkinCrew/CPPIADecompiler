using System.Text;

namespace CPPIADecompiler;

public static class TypeNames
{
    public static string Normalize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "Dynamic";

        string name = Dotted(raw);

        if (name == "Array")
            return "Array<Dynamic>";

        if (name.StartsWith("Array.", StringComparison.Ordinal))
        {
            string element = name.Substring("Array.".Length);
            return $"Array<{Primitive(Dotted(element))}>";
        }

        return Primitive(name);
    }

    static string Dotted(string raw)
    {
        string name = raw.Replace("::", ".");
        while (name.StartsWith(".", StringComparison.Ordinal))
            name = name.Substring(1);
        return name;
    }

    static string Primitive(string name) => name switch
    {
        "" => "Dynamic",
        "int" => "Int",
        "unsigned char" => "Int",
        "bool" => "Bool",
        "float" => "Float",
        "double" => "Float",
        "Any" => "Dynamic",
        "Object" => "Dynamic",
        "void" => "Void",
        "cpp.Int64" => "haxe.Int64",
        _ => name
    };
    
    public static string PackageOf(string dotted)
    {
        int last = LastTypeSeparator(dotted);
        return last < 0 ? "" : dotted.Substring(0, last);
    }
    
    public static string ShortName(string dotted)
    {
        int last = LastTypeSeparator(dotted);
        return last < 0 ? dotted : dotted.Substring(last + 1);
    }

    static int LastTypeSeparator(string dotted)
    {
        int last = -1;
        for (int i = 0; i < dotted.Length; i++)
        {
            if (dotted[i] != '.')
                continue;
            if (i + 1 < dotted.Length && char.IsUpper(dotted[i + 1]))
                return i;
            last = i;
        }
        return last;
    }
    
    public static string FilePath(string dotted)
    {
        var builder = new StringBuilder();
        string package = PackageOf(dotted);
        if (package.Length > 0)
        {
            builder.Append(package.Replace('.', '/'));
            builder.Append('/');
        }
        builder.Append(ShortName(dotted).Replace('.', '_'));
        builder.Append(".hx");
        return builder.ToString();
    }
}
