namespace CPPIADecompiler;

public sealed class CppiaParser
{
    static readonly HashSet<string> Binops = new()
    {
        "+", "-", "*", "/", "%", "&", "|", "^", "<<", ">>", ">>>", "&&", "||",
        "<", "<=", ">", ">=", "==", "!="
    };

    static readonly HashSet<string> Assigns = new()
    {
        "SET", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", "<<=", ">>=", ">>>="
    };

    static readonly HashSet<string> Unops = new()
    {
        "NEG", "!", "~", "ISNULL", "NOTNULL", "THROW",
        "CAST", "NOCAST", "CASTINT", "CASTBOOL", "TODYNARRAY"
    };

    static readonly HashSet<string> Crements = new() { "++", "+++", "--", "---" };

    readonly CppiaStream stream;
    readonly CppiaModule module = new();

    public CppiaParser(byte[] data)
    {
        stream = new CppiaStream(data);
    }

    public int ClassCount { get; private set; }

    public CppiaModule Parse()
    {
        ReadHeader();
        ReadClasses();
        ReadMain();
        ReadResources();
        return module;
    }
    
    public bool AtEnd() => !stream.HasMore();

    public int Position => stream.Position;

    // header

    void ReadHeader()
    {
        string magic = stream.Token();
        if (magic == "CPPIB")
            throw new CppiaException("this is a CPPIB file, only the CPPIA text format is supported", stream.CurrentLine());
        if (magic != "CPPIA")
            throw new CppiaException($"not a cppia file, magic was \"{magic}\"", stream.CurrentLine());

        int stringCount = stream.Int();
        if (stringCount < 0)
            throw new CppiaException($"bad string count {stringCount}", stream.CurrentLine());
        module.Strings.Capacity = stringCount;
        for (int i = 0; i < stringCount; i++)
            module.Strings.Add(stream.ReadString());

        int typeCount = stream.Int();
        if (typeCount < 0)
            throw new CppiaException($"bad type count {typeCount}", stream.CurrentLine());
        module.Types.Capacity = typeCount;
        for (int i = 0; i < typeCount; i++)
            module.Types.Add(stream.ReadString());

        ClassCount = stream.Int();
        if (ClassCount < 0)
            throw new CppiaException($"bad class count {ClassCount}", stream.CurrentLine());
    }

    void ReadClasses()
    {
        for (int i = 0; i < ClassCount; i++)
            module.Classes.Add(ReadClass());
    }

    // 1:1 from hxcpp basically
    
    CppiaClass ReadClass()
    {
        var cls = new CppiaClass();

        string tag = stream.Token();
        cls.Kind = tag switch
        {
            "CLASS" => ClassKind.Class,
            "INTERFACE" => ClassKind.Interface,
            "ENUM" => ClassKind.Enum,
            _ => throw new CppiaException($"bad class type \"{tag}\"", stream.CurrentLine())
        };

        cls.TypeId = stream.Int();

        if (cls.Kind != ClassKind.Enum)
        {
            cls.SuperId = stream.Int();
            int implementCount = stream.Int();
            for (int i = 0; i < implementCount; i++)
                cls.Implements.Add(stream.Int());
        }

        int fieldCount = stream.Int();
        for (int i = 0; i < fieldCount; i++)
        {
            if (cls.Kind == ClassKind.Enum)
            {
                var ctor = ReadEnumCtor();
                cls.EnumCtors.Add(ctor);
                cls.Members.Add(ctor);
                continue;
            }

            string field = stream.Token();
            switch (field)
            {
                case "FUNCTION":
                {
                    var func = ReadFunction(cls.Kind != ClassKind.Interface);
                    cls.Functions.Add(func);
                    cls.Members.Add(func);
                    break;
                }
                case "VAR":
                {
                    var variable = ReadVar();
                    cls.Vars.Add(variable);
                    cls.Members.Add(variable);
                    break;
                }
                case "IMPLDYNAMIC":
                    cls.ImplementsDynamic = true;
                    break;
                case "INLINE":
                    break;
                default:
                    throw new CppiaException($"unknown field type \"{field}\"", stream.CurrentLine());
            }
        }

        if (cls.Kind == ClassKind.Enum && stream.Bool())
            cls.EnumMeta = ParseExpr();

        return cls;
    }

    CppiaEnumCtor ReadEnumCtor()
    {
        var ctor = new CppiaEnumCtor { NameId = stream.Int() };
        int argCount = stream.Int();
        for (int i = 0; i < argCount; i++)
            ctor.Args.Add(new CppiaArg { NameId = stream.Int(), TypeId = stream.Int() });
        return ctor;
    }

    CppiaFunction ReadFunction(bool expectBody)
    {
        var func = new CppiaFunction
        {
            IsStatic = stream.Bool(),
            IsDynamic = stream.Int() != 0,
            NameId = stream.Int(),
            ReturnType = stream.Int()
        };

        int argCount = stream.Int();
        for (int i = 0; i < argCount; i++)
        {
            func.Args.Add(new CppiaArg
            {
                NameId = stream.Int(),
                Optional = stream.Bool(),
                TypeId = stream.Int()
            });
        }

        if (expectBody)
            func.Body = ParseExpr();

        return func;
    }

    CppiaVar ReadVar()
    {
        var variable = new CppiaVar
        {
            IsStatic = stream.Bool(),
            Read = ReadAccess(),
            Write = ReadAccess(),
            IsVirtual = stream.Bool(),
            NameId = stream.Int(),
            TypeId = stream.Int()
        };

        if (stream.Int() != 0)
            variable.Init = ParseExpr();

        return variable;
    }

    Access ReadAccess()
    {
        string tok = stream.Token();
        return tok switch
        {
            "N" => Access.Normal,
            "n" => Access.None,
            "R" => Access.Resolve,
            "C" => Access.Call,
            "V" => Access.CallNative,
            _ => throw new CppiaException($"bad access code \"{tok}\"", stream.CurrentLine())
        };
    }
    
    // main and resources

    void ReadMain()
    {
        string tok = stream.Token();
        if (tok == "MAIN")
            module.Main = ParseExpr();
        else if (tok != "NOMAIN")
            throw new CppiaException($"no main specified, got \"{tok}\"", stream.CurrentLine());
    }

    void ReadResources()
    {
        string tok = stream.Token();
        if (tok != "RESOURCES")
            throw new CppiaException($"no resources tag, got \"{tok}\"", stream.CurrentLine());

        int count = stream.Int();
        for (int i = 0; i < count; i++)
        {
            string reso = stream.Token();
            if (reso != "RESO")
                throw new CppiaException($"no reso tag, got \"{reso}\"", stream.CurrentLine());
            module.Resources.Add(new CppiaResource { NameId = stream.Int(), Length = stream.Int() });
        }

        if (count > 0)
        {
            stream.SkipChar();
            foreach (var resource in module.Resources)
                resource.Data = stream.ReadBytes(resource.Length);
        }
    }

    // expressions

    StackVar ReadStackVar()
    {
        var slot = new StackVar
        {
            NameId = stream.Int(),
            Id = stream.Int(),
            Capture = stream.Bool(),
            TypeId = stream.Int()
        };
        module.LocalNames[slot.Id] = module.Str(slot.NameId);
        return slot;
    }

    CppiaConst ReadConst()
    {
        string tok = stream.Token();
        return tok switch
        {
            "true" => new CppiaConst { Kind = ConstKind.Int, Value = 1 },
            "false" => new CppiaConst { Kind = ConstKind.Int, Value = 0 },
            "NULL" => new CppiaConst { Kind = ConstKind.Null },
            "THIS" => new CppiaConst { Kind = ConstKind.This },
            "SUPER" => new CppiaConst { Kind = ConstKind.Super },
            "i" => new CppiaConst { Kind = ConstKind.Int, Value = stream.Int() },
            "f" => new CppiaConst { Kind = ConstKind.Float, Value = stream.Int() },
            "s" => new CppiaConst { Kind = ConstKind.String, Value = stream.Int() },
            _ => throw new CppiaException($"unknown const value \"{tok}\"", stream.CurrentLine())
        };
    }

    CppiaExpr ParseExpr()
    {
        int fileId = stream.Int();
        int line = stream.Int();
        string tok = stream.Token();

        var expr = new CppiaExpr(tok) { FileId = fileId, Line = line };
        FillExpr(expr, tok);
        return expr;
    }

    void FillExpr(CppiaExpr expr, string tok)
    {
        switch (tok)
        {
            case "FUN":
            {
                expr.Ops.Add(stream.Int());
                int argCount = stream.Int();
                expr.Ops.Add(argCount);
                for (int i = 0; i < argCount; i++)
                {
                    expr.Vars.Add(ReadStackVar());
                    expr.Defaults.Add(stream.Bool() ? ReadConst() : null);
                }
                expr.Kids.Add(ParseExpr());
                return;
            }

            case "BLOCK":
                ParseKids(expr, stream.Int());
                return;

            case "IF":
                ParseKids(expr, 2);
                return;

            case "IFELSE":
                ParseKids(expr, 3);
                return;

            case "TCAST":
            case "TODATAARRAY":
            case "TOINTERFACEARRAY":
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 1);
                return;

            case "TOINTERFACE":
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 1);
                return;

            case "CALLSTATIC":
            case "CALLTHIS":
            case "CALLSUPER":
            case "CREATEENUM":
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                ParseKids(expr, stream.Int());
                return;

            case "CALLSUPERNEW":
            case "NEW":
            case "ADEF":
            case "CALLGLOBAL":
                expr.Ops.Add(stream.Int());
                ParseKids(expr, stream.Int());
                return;

            case "CALLMEMBER":
            {
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                int argCount = stream.Int();
                ParseKids(expr, 1);
                ParseKids(expr, argCount);
                return;
            }

            case "CALL":
            {
                int argCount = stream.Int();
                ParseKids(expr, 1);
                ParseKids(expr, argCount);
                return;
            }

            case "FENUM":
            case "FTHISINST":
            case "FSTATIC":
            case "FTHISNAME":
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                return;

            case "FLINK":
            case "FNAME":
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 1);
                return;

            case "ARRAYI":
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 2);
                return;

            case "ENUMI":
                expr.Ops.Add(stream.Int());
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 1);
                return;

            case "OBJDEF":
            {
                int fieldCount = stream.Int();
                expr.Ops.Add(fieldCount);
                for (int i = 0; i < fieldCount; i++)
                    expr.Ops.Add(stream.Int());
                ParseKids(expr, fieldCount);
                return;
            }

            case "VAR":
            case "i":
            case "f":
            case "s":
            case "CLASSOF":
                expr.Ops.Add(stream.Int());
                return;

            case "TVARS":
            {
                int count = stream.Int();
                for (int i = 0; i < count; i++)
                    expr.Kids.Add(ParseVarDecl());
                return;
            }

            case "WHILE":
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 2);
                return;

            case "FOR":
                expr.Vars.Add(ReadStackVar());
                ParseKids(expr, 2);
                return;

            case "RETVAL":
                expr.Ops.Add(stream.Int());
                ParseKids(expr, 1);
                return;

            case "POSINFO":
                for (int i = 0; i < 4; i++)
                    expr.Ops.Add(stream.Int());
                return;

            case "SWITCH":
            {
                int caseCount = stream.Int();
                int hasDefault = stream.Int();
                expr.Ops.Add(caseCount);
                expr.Ops.Add(hasDefault);
                ParseKids(expr, 1);
                for (int i = 0; i < caseCount; i++)
                {
                    int condCount = stream.Int();
                    expr.Ops.Add(condCount);
                    ParseKids(expr, condCount);
                    ParseKids(expr, 1);
                }
                if (hasDefault != 0)
                    ParseKids(expr, 1);
                return;
            }

            case "TRY":
            {
                int catchCount = stream.Int();
                expr.Ops.Add(catchCount);
                ParseKids(expr, 1);
                for (int i = 0; i < catchCount; i++)
                {
                    expr.Vars.Add(ReadStackVar());
                    ParseKids(expr, 1);
                }
                return;
            }

            case "RETURN":
            case "BREAK":
            case "CONTINUE":
            case "THIS":
            case "NULL":
            case "true":
            case "false":
                return;
        }

        if (Unops.Contains(tok) || Crements.Contains(tok))
        {
            ParseKids(expr, 1);
            return;
        }

        if (Binops.Contains(tok) || Assigns.Contains(tok))
        {
            ParseKids(expr, 2);
            return;
        }

        throw new CppiaException($"invalid expression \"{tok}\"", stream.CurrentLine());
    }
    
    CppiaExpr ParseVarDecl()
    {
        string tok = stream.Token();
        var expr = new CppiaExpr(tok);

        switch (tok)
        {
            case "VARDECL":
                expr.Vars.Add(ReadStackVar());
                return expr;
            case "VARDECLI":
                expr.Vars.Add(ReadStackVar());
                expr.Ops.Add(stream.Int());
                expr.Kids.Add(ParseExpr());
                return expr;
            default:
                throw new CppiaException($"expected VARDECL in TVARS, got \"{tok}\"", stream.CurrentLine());
        }
    }

    void ParseKids(CppiaExpr expr, int count)
    {
        for (int i = 0; i < count; i++)
            expr.Kids.Add(ParseExpr());
    }
}
