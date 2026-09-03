using System.Text;

namespace CPPIADecompiler;

public sealed class ExprWriter
{
    const int PrecAssign = 1;
    const int PrecTernary = 2;
    const int PrecOr = 3;
    const int PrecAnd = 4;
    const int PrecCompare = 5;
    const int PrecBitOr = 6;
    const int PrecBitAnd = 7;
    const int PrecShift = 8;
    const int PrecAdd = 9;
    const int PrecMul = 10;
    const int PrecUnary = 11;
    const int PrecPrimary = 13;

    static readonly Dictionary<string, int> Precedence = new()
    {
        ["||"] = PrecOr,
        ["&&"] = PrecAnd,
        ["=="] = PrecCompare, ["!="] = PrecCompare,
        ["<"] = PrecCompare, ["<="] = PrecCompare, [">"] = PrecCompare, [">="] = PrecCompare,
        ["|"] = PrecBitOr, ["^"] = PrecBitOr,
        ["&"] = PrecBitAnd,
        ["<<"] = PrecShift, [">>"] = PrecShift, [">>>"] = PrecShift,
        ["+"] = PrecAdd, ["-"] = PrecAdd,
        ["*"] = PrecMul, ["/"] = PrecMul, ["%"] = PrecMul
    };

    static readonly Dictionary<string, string> AssignOps = new()
    {
        ["SET"] = "=",
        ["+="] = "+=", ["-="] = "-=", ["*="] = "*=", ["/="] = "/=", ["%="] = "%=",
        ["&="] = "&=", ["|="] = "|=", ["^="] = "^=",
        ["<<="] = "<<=", [">>="] = ">>=", [">>>="] = ">>>="
    };

    readonly CppiaModule module;
    readonly Func<int, string> use;

    public ExprWriter(CppiaModule module, Func<int, string> use)
    {
        this.module = module;
        this.use = use;
    }

    public void WriteBody(CppiaExpr fun, StringBuilder text, int indent)
    {
        if (fun.Op != "FUN" || fun.Kids.Count == 0)
            return;

        var body = fun.Kids[0];
        if (body.Op == "BLOCK")
            WriteStatements(body, text, indent);
        else
            WriteStatement(body, text, indent);
    }

    void WriteStatements(CppiaExpr block, StringBuilder text, int indent)
    {
        foreach (var kid in block.Kids)
            WriteStatement(kid, text, indent);
    }

    void WriteBlock(CppiaExpr expr, StringBuilder text, int indent)
    {
        Indent(text, indent).Append("{\n");
        if (expr.Op == "BLOCK")
            WriteStatements(expr, text, indent + 1);
        else
            WriteStatement(expr, text, indent + 1);
        Indent(text, indent).Append("}\n");
    }

    void WriteStatement(CppiaExpr expr, StringBuilder text, int indent)
    {
        switch (expr.Op)
        {
            case "BLOCK":
                WriteBlock(expr, text, indent);
                return;

            case "TVARS":
                foreach (var decl in expr.Kids)
                    WriteVarDecl(decl, text, indent);
                return;

            case "IF":
                Indent(text, indent).Append("if (").Append(Expr(expr[0])).Append(")\n");
                WriteBlock(expr[1], text, indent);
                return;

            case "IFELSE":
                Indent(text, indent).Append("if (").Append(Expr(expr[0])).Append(")\n");
                WriteBlock(expr[1], text, indent);
                Indent(text, indent).Append("else\n");
                WriteBlock(expr[2], text, indent);
                return;

            case "WHILE":
                if (expr.Op0 != 0)
                {
                    Indent(text, indent).Append("while (").Append(Expr(expr[0])).Append(")\n");
                    WriteBlock(expr[1], text, indent);
                }
                else
                {
                    Indent(text, indent).Append("do\n");
                    WriteBlock(expr[1], text, indent);
                    Indent(text, indent).Append("while (").Append(Expr(expr[0])).Append(");\n");
                }
                return;

            case "FOR":
                Indent(text, indent).Append("for (").Append(Local(expr.Vars[0]))
                    .Append(" in ").Append(Expr(expr[0])).Append(")\n");
                WriteBlock(expr[1], text, indent);
                return;

            case "RETURN":
                Indent(text, indent).Append("return;\n");
                return;

            case "RETVAL":
                Indent(text, indent).Append("return ").Append(Expr(expr[0])).Append(";\n");
                return;

            case "THROW":
                Indent(text, indent).Append("throw ").Append(Expr(expr[0])).Append(";\n");
                return;

            case "BREAK":
                Indent(text, indent).Append("break;\n");
                return;

            case "CONTINUE":
                Indent(text, indent).Append("continue;\n");
                return;
        }

        Indent(text, indent).Append(Expr(expr)).Append(";\n");
    }

    void WriteVarDecl(CppiaExpr decl, StringBuilder text, int indent)
    {
        var slot = decl.Vars[0];
        Indent(text, indent).Append("var ").Append(Local(slot));

        if (slot.TypeId != 0)
            text.Append(':').Append(use(slot.TypeId));

        if (decl.Op == "VARDECLI")
            text.Append(" = ").Append(Expr(decl[0]));

        text.Append(";\n");
    }

    public string Expr(CppiaExpr expr) => Expr(expr, 0);

    string Expr(CppiaExpr expr, int parent)
    {
        switch (expr.Op)
        {
            case "i":
                return expr.Op0.ToString();

            case "f":
                return module.Str(expr.Op0);

            case "s":
                return Literals.Quote(module.Str(expr.Op0));

            case "true":
                return "true";

            case "false":
                return "false";

            case "NULL":
                return "null";

            case "THIS":
                return "this";

            case "VAR":
                return LocalName(expr.Op0);

            case "FTHISINST":
            case "FTHISNAME":
                return "this." + module.Str(expr.Op1);

            case "FSTATIC":
                return use(expr.Op0) + "." + module.Str(expr.Op1);

            case "FNAME":
            case "FLINK":
                return Expr(expr[0], PrecPrimary) + "." + module.Str(expr.Op1);

            case "ARRAYI":
                return Expr(expr[0], PrecPrimary) + "[" + Expr(expr[1]) + "]";

            case "CALLTHIS":
                return "this." + module.Str(expr.Op1) + Args(expr.Kids, 0);

            case "CALLSUPER":
                return "super." + module.Str(expr.Op1) + Args(expr.Kids, 0);

            case "CALLSUPERNEW":
                return "super" + Args(expr.Kids, 0);

            case "CALLSTATIC":
                return use(expr.Op0) + "." + module.Str(expr.Op1) + Args(expr.Kids, 0);

            case "CALLMEMBER":
                return Expr(expr[0], PrecPrimary) + "." + module.Str(expr.Op1) + Args(expr.Kids, 1);

            case "CALLGLOBAL":
                return module.Str(expr.Op0) + Args(expr.Kids, 0);

            case "CALL":
                return Expr(expr[0], PrecPrimary) + Args(expr.Kids, 1);

            case "NEW":
                return "new " + use(expr.Op0) + Args(expr.Kids, 0);

            case "ADEF":
                return "[" + string.Join(", ", expr.Kids.Select(kid => Expr(kid))) + "]";

            case "ISNULL":
                return Wrap(Expr(expr[0], PrecCompare) + " == null", PrecCompare, parent);

            case "NOTNULL":
                return Wrap(Expr(expr[0], PrecCompare) + " != null", PrecCompare, parent);

            case "NEG":
                return Wrap("-" + Expr(expr[0], PrecUnary), PrecUnary, parent);

            case "!":
                return Wrap("!" + Expr(expr[0], PrecUnary), PrecUnary, parent);

            case "~":
                return Wrap("~" + Expr(expr[0], PrecUnary), PrecUnary, parent);

            case "++":
                return Wrap("++" + Expr(expr[0], PrecUnary), PrecUnary, parent);

            case "--":
                return Wrap("--" + Expr(expr[0], PrecUnary), PrecUnary, parent);

            case "+++":
                return Wrap(Expr(expr[0], PrecUnary) + "++", PrecUnary, parent);

            case "---":
                return Wrap(Expr(expr[0], PrecUnary) + "--", PrecUnary, parent);

            case "IFELSE":
                return Wrap($"{Expr(expr[0], PrecTernary + 1)} ? {Expr(expr[1], PrecTernary)} : {Expr(expr[2], PrecTernary)}", PrecTernary, parent);

            case "THROW":
                return "throw " + Expr(expr[0]);

            case "CLASSOF":
                return use(expr.Op0);

            case "OBJDEF":
            {
                var fields = new List<string>();
                for (int i = 0; i < expr.Op0; i++)
                    fields.Add(module.Str(expr.Ops[i + 1]) + " : " + Expr(expr[i]));
                return "{ " + string.Join(", ", fields) + " }";
            }

            case "POSINFO":
                return "{ fileName : " + Literals.Quote(module.Str(expr.Ops[0]))
                    + ", lineNumber : " + expr.Ops[1]
                    + ", className : " + Literals.Quote(module.Str(expr.Ops[2]))
                    + ", methodName : " + Literals.Quote(module.Str(expr.Ops[3])) + " }";
        }

        if (AssignOps.TryGetValue(expr.Op, out string? assign))
            return Wrap($"{Expr(expr[0], PrecAssign + 1)} {assign} {Expr(expr[1], PrecAssign)}", PrecAssign, parent);

        if (Precedence.TryGetValue(expr.Op, out int prec))
            return Wrap($"{Expr(expr[0], prec)} {expr.Op} {Expr(expr[1], prec + 1)}", prec, parent);

        return Unhandled(expr);
    }

    string Args(List<CppiaExpr> kids, int from)
    {
        var parts = new List<string>();
        for (int i = from; i < kids.Count; i++)
            parts.Add(Expr(kids[i]));
        return "(" + string.Join(", ", parts) + ")";
    }

    static string Wrap(string text, int prec, int parent) => prec < parent ? "(" + text + ")" : text;

    string Unhandled(CppiaExpr expr)
    {
        var text = new StringBuilder("/* ").Append(expr.Op);
        foreach (int op in expr.Ops)
            text.Append(' ').Append(op);
        foreach (var kid in expr.Kids)
            text.Append(' ').Append(Expr(kid));
        return text.Append(" */").ToString();
    }

    string Local(StackVar slot)
    {
        string name = module.Str(slot.NameId);
        return name.Length > 0 ? name : "_v" + slot.Id;
    }

    string LocalName(int id) =>
        module.LocalNames.TryGetValue(id, out string? name) && name.Length > 0 ? name : "_v" + id;

    static StringBuilder Indent(StringBuilder text, int indent) => text.Append('\t', indent);
}
