using System.Text;

namespace CPPIADecompiler;

public sealed class HaxeWriter
{
    readonly CppiaModule module;
    
    readonly Dictionary<string, string> shortNames = new();

    public HaxeWriter(CppiaModule module)
    {
        this.module = module;

        var seen = new Dictionary<string, string?>();
        foreach (string raw in module.Types)
        {
            string full = TypeNames.Normalize(raw);
            if (full.Contains('<'))
                continue;
            string shortName = TypeNames.ShortName(full);
            if (shortName == full)
                continue;
            if (seen.TryGetValue(shortName, out string? existing))
            {
                if (existing != full)
                    seen[shortName] = null;
            }
            else
            {
                seen[shortName] = full;
            }
        }

        foreach (var pair in seen)
            if (pair.Value != null)
                shortNames[pair.Key] = pair.Value;
    }

    public string Write(CppiaClass cls)
    {
        string full = module.Type(cls.TypeId);
        string package = TypeNames.PackageOf(full);

        var body = new StringBuilder();
        var imports = new SortedSet<string>(StringComparer.Ordinal);

        WriteDeclaration(cls, body, imports, package);

        var text = new StringBuilder();
        if (package.Length > 0)
            text.Append("package ").Append(package).Append(";\n\n");
        else
            text.Append("package;\n\n");

        if (imports.Count > 0)
        {
            foreach (string import in imports)
                text.Append("import ").Append(import).Append(";\n");
            text.Append('\n');
        }

        text.Append(body);
        return text.ToString();
    }

    void WriteDeclaration(CppiaClass cls, StringBuilder text, SortedSet<string> imports, string package)
    {
        string name = TypeNames.ShortName(module.Type(cls.TypeId));

        if (cls.Kind == ClassKind.Enum)
        {
            text.Append("enum ").Append(name).Append("\n{\n");
            foreach (var ctor in cls.EnumCtors)
                WriteEnumCtor(ctor, text, imports, package);
            text.Append("}\n");
            return;
        }

        text.Append(cls.Kind == ClassKind.Interface ? "interface " : "class ").Append(name);

        if (cls.SuperId != 0)
            text.Append(" extends ").Append(Use(cls.SuperId, imports, package));

        foreach (int id in cls.Implements)
            text.Append(" implements ").Append(Use(id, imports, package));

        if (cls.ImplementsDynamic)
            text.Append(" implements Dynamic");

        text.Append("\n{\n");

        bool first = true;
        foreach (object member in cls.Members)
        {
            if (!first)
                text.Append('\n');
            first = false;

            if (member is CppiaVar variable)
                WriteVar(variable, text, imports, package);
            else if (member is CppiaFunction func)
                WriteFunction(cls, func, text, imports, package);
        }

        text.Append("}\n");
    }

    void WriteEnumCtor(CppiaEnumCtor ctor, StringBuilder text, SortedSet<string> imports, string package)
    {
        text.Append('\t').Append(module.Str(ctor.NameId));

        if (ctor.Args.Count > 0)
        {
            text.Append('(');
            for (int i = 0; i < ctor.Args.Count; i++)
            {
                if (i > 0)
                    text.Append(", ");
                var arg = ctor.Args[i];
                text.Append(module.Str(arg.NameId)).Append(':').Append(Use(arg.TypeId, imports, package));
            }
            text.Append(')');
        }

        text.Append(";\n");
    }

    void WriteVar(CppiaVar variable, StringBuilder text, SortedSet<string> imports, string package)
    {
        string name = module.Str(variable.NameId);
        bool property = variable.Read != Access.Normal || variable.Write != Access.Normal;

        if (property && !variable.IsVirtual)
            text.Append("\t@:isVar\n");

        text.Append('\t');
        if (variable.IsStatic)
            text.Append("static ");
        text.Append("var ").Append(name);

        if (property)
            text.Append('(').Append(AccessName(variable.Read, "get")).Append(", ").Append(AccessName(variable.Write, "set")).Append(')');

        text.Append(':').Append(Use(variable.TypeId, imports, package));

        if (variable.Init != null)
            text.Append(" = ").Append(new ExprWriter(module, typeId => Use(typeId, imports, package)).Expr(variable.Init));

        text.Append(";\n");
    }

    void WriteFunction(CppiaClass cls, CppiaFunction func, StringBuilder text, SortedSet<string> imports, string package)
    {
        string name = module.Str(func.NameId);
        bool isConstructor = func.IsStatic && name == "new";

        text.Append('\t');
        if (isConstructor)
            text.Append("public ");
        else if (func.IsStatic)
            text.Append("static ");
        if (func.IsDynamic)
            text.Append("dynamic ");
        text.Append("function ").Append(name).Append('(');

        for (int i = 0; i < func.Args.Count; i++)
        {
            if (i > 0)
                text.Append(", ");
            var arg = func.Args[i];
            var fallback = DefaultOf(func.Body, i);

            if (arg.Optional && fallback == null)
                text.Append('?');
            text.Append(module.Str(arg.NameId)).Append(':').Append(Use(arg.TypeId, imports, package));
            if (fallback != null)
                text.Append(" = ").Append(RenderConst(fallback));
        }

        text.Append(')');
        if (!isConstructor)
            text.Append(':').Append(Use(func.ReturnType, imports, package));

        if (cls.Kind == ClassKind.Interface || func.Body == null)
        {
            text.Append(";\n");
            return;
        }

        text.Append("\n\t{\n");
        new ExprWriter(module, typeId => Use(typeId, imports, package)).WriteBody(func.Body, text, 2);
        text.Append("\t}\n");
    }

    static CppiaConst? DefaultOf(CppiaExpr? fun, int index)
    {
        if (fun == null || fun.Op != "FUN" || index >= fun.Defaults.Count)
            return null;
        return fun.Defaults[index];
    }

    string RenderConst(CppiaConst value) => value.Kind switch
    {
        ConstKind.Int => value.Value.ToString(),
        ConstKind.Float => module.Str(value.Value),
        ConstKind.String => Literals.Quote(module.Str(value.Value)),
        ConstKind.This => "this",
        ConstKind.Super => "super",
        _ => "null"
    };

    static string AccessName(Access access, string call) => access switch
    {
        Access.Normal => "default",
        Access.None => "null",
        Access.Resolve => "dynamic",
        _ => call
    };
    
    string Use(int typeId, SortedSet<string> imports, string package)
    {
        string full = module.Type(typeId);
        return UseName(full, imports, package);
    }

    string UseName(string full, SortedSet<string> imports, string package)
    {
        int open = full.IndexOf('<');
        if (open >= 0 && full.EndsWith(">", StringComparison.Ordinal))
        {
            string outer = full.Substring(0, open);
            string inner = full.Substring(open + 1, full.Length - open - 2);
            return $"{UseName(outer, imports, package)}<{UseName(inner, imports, package)}>";
        }

        string shortName = TypeNames.ShortName(full);
        if (shortName == full)
            return full;

        if (!shortNames.TryGetValue(shortName, out string? owner) || owner != full)
            return full;

        string typePackage = TypeNames.PackageOf(full);
        if (typePackage != package)
            imports.Add(full);

        return shortName;
    }
}
