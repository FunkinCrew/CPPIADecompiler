using System.Text;
using CPPIADecompiler;

var options = CommandLine.Parse(args);
if (options == null)
{
    CommandLine.PrintUsage();
    return 1;
}

byte[] data;
try
{
    data = File.ReadAllBytes(options.Input);
}
catch (Exception error)
{
    Console.Error.WriteLine($"could not read {options.Input}: {error.Message}");
    return 1;
}

CppiaModule module;
var parser = new CppiaParser(data);
try
{
    module = parser.Parse();
}
catch (CppiaException error)
{
    Console.Error.WriteLine($"parse failed at line {error.Line}: {error.Message}");
    return 1;
}

if (!parser.AtEnd())
{
    Console.Error.WriteLine($"warning: {data.Length - parser.Position} bytes left after the end of the module, the parse desynced");
}

if (options.DumpTables)
{
    Console.WriteLine($"strings: {module.Strings.Count}");
    for (int i = 0; i < module.Strings.Count; i++)
        Console.WriteLine($"  [{i}] {Quote(module.Strings[i])}");

    Console.WriteLine($"types: {module.Types.Count}");
    for (int i = 0; i < module.Types.Count; i++)
        Console.WriteLine($"  [{i}] {module.Types[i]} -> {module.Type(i)}");

    Console.WriteLine($"classes: {module.Classes.Count}");
    foreach (var cls in module.Classes)
        Console.WriteLine($"  {cls.Kind} {module.Type(cls.TypeId)}");

    return 0;
}

if (options.Fingerprints)
{
    foreach (var cls in module.Classes)
        Console.WriteLine($"{Fingerprint.Of(module, cls)}  {module.Type(cls.TypeId)}");
    return 0;
}

var writer = new HaxeWriter(module);

var classes = new List<CppiaClass>();
var skipped = new List<string>();

foreach (var cls in module.Classes)
{
    string name = module.Type(cls.TypeId);

    if (!options.All && Boilerplate.IsKnownName(name))
    {
        string print = Fingerprint.Of(module, cls);

        if (Boilerplate.Matches(name, print))
        {
            skipped.Add(name);
            continue;
        }

        Console.Error.WriteLine($"warning: {name} does not match known generated code (fingerprint {print}), writing it out");
    }

    classes.Add(cls);
}

if (skipped.Count > 0)
    Console.Error.WriteLine($"skipped {skipped.Count} known generated types: {string.Join(", ", skipped)}");

if (options.OutDir == null)
{
    var text = new StringBuilder();
    foreach (var cls in classes)
    {
        text.Append("// ==== ").Append(module.Type(cls.TypeId)).Append(" ====\n");
        text.Append(writer.Write(cls));
        text.Append('\n');
    }
    Console.Out.Write(text.ToString());
    return 0;
}

Directory.CreateDirectory(options.OutDir);

foreach (var cls in classes)
{
    string relative = TypeNames.FilePath(module.Type(cls.TypeId));
    string path = Path.Combine(options.OutDir, relative);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
    File.WriteAllText(path, writer.Write(cls));
}

Console.WriteLine($"wrote {classes.Count} type definitions to {options.OutDir}");
return 0;

static string Quote(string value) =>
    "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t") + "\"";

namespace CPPIADecompiler
{
    sealed class Options
    {
        public string Input = "";
        public string? OutDir;
        public bool DumpTables;
        public bool Ast;
        public bool All;
        public bool Fingerprints;
    }

    static class CommandLine
    {
        public static Options? Parse(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "-o":
                    case "--out":
                        if (++i >= args.Length) return null;
                        options.OutDir = args[i];
                        break;
                    case "--dump-tables":
                        options.DumpTables = true;
                        break;
                    case "--ast":
                        options.Ast = true;
                        break;
                    case "--all":
                        options.All = true;
                        break;
                    case "--fingerprints":
                        options.Fingerprints = true;
                        break;
                    default:
                        if (arg.StartsWith("-", StringComparison.Ordinal)) return null;
                        if (options.Input.Length > 0) return null;
                        options.Input = arg;
                        break;
                }
            }

            return options.Input.Length > 0 ? options : null;
        }

        public static void PrintUsage()
        {
            Console.Error.WriteLine("usage: CPPIADecompiler <file.cppia> [-o <dir>] [--all] [--dump-tables]");
            Console.Error.WriteLine("  --all keeps the generated types that are skipped by default");
            Console.Error.WriteLine("  with no -o the whole module is written to stdout");
        }
    }
}
