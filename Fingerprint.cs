using System.Security.Cryptography;
using System.Text;

namespace CPPIADecompiler;

// to skip a bunch of boilerplate code we don't care about, so we fingerprint it based on the class structure and member signatures
public static class Fingerprint
{
    public static string Of(CppiaModule module, CppiaClass cls)
    {
        var text = new StringBuilder();
        Canonical(module, cls, text);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString()));
        return Convert.ToHexString(hash, 0, 8).ToLowerInvariant();
    }
    
    static void Canonical(CppiaModule module, CppiaClass cls, StringBuilder text)
    {
        text.Append(cls.Kind).Append('|').Append(module.Type(cls.TypeId));
        text.Append('|').Append(module.Type(cls.SuperId));
        foreach (int id in cls.Implements)
            text.Append('+').Append(module.Type(id));
        if (cls.ImplementsDynamic)
            text.Append("+Dynamic");
        text.Append('\n');

        foreach (object member in cls.Members)
        {
            switch (member)
            {
                case CppiaVar variable:
                    text.Append("V|").Append(variable.IsStatic ? '1' : '0')
                        .Append('|').Append(variable.Read).Append('|').Append(variable.Write)
                        .Append('|').Append(variable.IsVirtual ? '1' : '0')
                        .Append('|').Append(module.Str(variable.NameId))
                        .Append('|').Append(module.Type(variable.TypeId))
                        .Append('|');
                    Expr(module, variable.Init, text);
                    text.Append('\n');
                    break;

                case CppiaFunction func:
                    text.Append("F|").Append(func.IsStatic ? '1' : '0')
                        .Append('|').Append(func.IsDynamic ? '1' : '0')
                        .Append('|').Append(module.Str(func.NameId))
                        .Append('|').Append(module.Type(func.ReturnType));
                    foreach (var arg in func.Args)
                        text.Append('|').Append(module.Str(arg.NameId))
                            .Append(':').Append(arg.Optional ? '1' : '0')
                            .Append(':').Append(module.Type(arg.TypeId));
                    text.Append('|');
                    Expr(module, func.Body, text);
                    text.Append('\n');
                    break;

                case CppiaEnumCtor ctor:
                    text.Append("E|").Append(module.Str(ctor.NameId));
                    foreach (var arg in ctor.Args)
                        text.Append('|').Append(module.Str(arg.NameId))
                            .Append(':').Append(module.Type(arg.TypeId));
                    text.Append('\n');
                    break;
            }
        }

        text.Append("M|");
        Expr(module, cls.EnumMeta, text);
    }

    static void Expr(CppiaModule module, CppiaExpr? expr, StringBuilder text)
    {
        if (expr == null)
        {
            text.Append('-');
            return;
        }

        text.Append('(').Append(expr.Op);

        string kinds = OperandKinds(expr.Op);

        for (int i = 0; i < expr.Ops.Count; i++)
        {
            char kind = i < kinds.Length ? kinds[i] : kinds.Length > 0 ? kinds[^1] : 'N';
            int op = expr.Ops[i];

            text.Append(' ').Append(kind switch
            {
                'T' => module.Type(op),
                'S' => module.Str(op),
                'L' => LocalName(module, op),
                _ => op.ToString()
            });
        }

        foreach (var slot in expr.Vars)
            text.Append(" $").Append(module.Str(slot.NameId)).Append(':').Append(module.Type(slot.TypeId));

        foreach (var kid in expr.Kids)
        {
            text.Append(' ');
            Expr(module, kid, text);
        }

        text.Append(')');
    }
    
    static string OperandKinds(string op) => op switch
    {
        "FUN" => "TN",
        "TCAST" or "TODATAARRAY" or "TOINTERFACEARRAY" => "T",
        "TOINTERFACE" => "TT",
        "CALLSTATIC" or "CALLTHIS" or "CALLSUPER" or "CALLMEMBER" or "CREATEENUM" => "TS",
        "FENUM" or "FTHISINST" or "FSTATIC" or "FTHISNAME" or "FLINK" or "FNAME" => "TS",
        "CALLSUPERNEW" or "NEW" or "ADEF" or "ARRAYI" or "CLASSOF" or "RETVAL" or "VARDECLI" => "T",
        "CALLGLOBAL" or "s" or "f" => "S",
        "ENUMI" => "TN",
        "OBJDEF" => "NS",
        "VAR" => "L",
        "POSINFO" => "SNSS",
        _ => "N"
    };

    static string LocalName(CppiaModule module, int id) =>
        module.LocalNames.TryGetValue(id, out string? name) && name.Length > 0 ? name : "_v";
}
