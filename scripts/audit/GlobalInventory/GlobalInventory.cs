using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// Audit-only: reads pinned Git blobs; writes inventories, never application sources.
internal static class GlobalInventory
{
    private const string Revision = "aa0e503c1b6f241dd14d0f01224d211f2a20f008";
    private const string GlobalName = "DS4Windows.Global";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private static string root = @"G:\Cursor_Folder\DS4Windows-Vader4Pro";

    private static string Git(params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = new UTF8Encoding(false, true),
            UseShellExecute = false
        };
        start.ArgumentList.Add("-C"); start.ArgumentList.Add(root);
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start)!;
        var errorTask = process.StandardError.ReadToEndAsync();
        var value = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) throw new InvalidOperationException(errorTask.Result);
        return value;
    }

    private static string Scope(string path) => path.Contains("/obj/") || path.Contains("/bin/") ? "build-artifact" :
        path.StartsWith("DS4Windows/") ? "production" :
        path.StartsWith("DS4WindowsTests/") || path.StartsWith("StandaloneTests/") ? "test" :
        path.StartsWith("externals/") ? "external" : "other";
    private static bool IsGlobal(ISymbol? symbol) => symbol?.ContainingSymbol is INamedTypeSymbol owner && owner.ToDisplayString() == GlobalName;
    private static string SymbolId(ISymbol? symbol) => symbol?.GetDocumentationCommentId() ?? symbol?.ToDisplayString() ?? "";
    private static object Position(SyntaxNode node)
    {
        var pos = node.SyntaxTree.GetLineSpan(node.Span).StartLinePosition;
        return new { line = pos.Line + 1, column = pos.Character + 1, offset = node.SpanStart, length = node.Span.Length };
    }
    private static string Context(SyntaxNode node) => node.Ancestors().OfType<MemberDeclarationSyntax>()
        .Select(n => n switch
        {
            MethodDeclarationSyntax m => m.Identifier.Text,
            ConstructorDeclarationSyntax c => c.Identifier.Text + ".ctor",
            PropertyDeclarationSyntax p => p.Identifier.Text,
            FieldDeclarationSyntax f => f.Declaration.Variables.First().Identifier.Text,
            EventDeclarationSyntax e => e.Identifier.Text,
            _ => ""
        }).FirstOrDefault(n => n.Length > 0) ?? "<type>";
    private static string Usage(SimpleNameSyntax node)
    {
        SyntaxNode expression = node.Parent is MemberAccessExpressionSyntax ma && ma.Name == node ? ma : node;
        if (expression.Parent is InvocationExpressionSyntax invocation && invocation.Expression == expression) return "call";
        while (expression.Parent is ElementAccessExpressionSyntax element && element.Expression == expression) expression = element;
        if (expression.Parent is AssignmentExpressionSyntax assignment && assignment.Left == expression)
            return assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) ? "write" : "read-write";
        if (expression.Parent is PostfixUnaryExpressionSyntax || expression.Parent is PrefixUnaryExpressionSyntax prefix &&
            (prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression))) return "read-write";
        if (expression.Parent is ArgumentSyntax arg && arg.RefKindKeyword.RawKind != 0) return "ref-out";
        if (node.Ancestors().OfType<InvocationExpressionSyntax>().Any(i => i.Expression.ToString() == "nameof")) return "nameof";
        return "read-or-method-group";
    }
    private static IEnumerable<MetadataReference> References()
    {
        var paths = new List<string>();
        // 1. Trusted platform assemblies (system references)
        var trusted = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "").Split(Path.PathSeparator)
            .Where(File.Exists).ToList();
        paths.AddRange(trusted);

        // 2. Project references from .csproj (PackageReference and Reference)
        var csprojPath = Path.Combine(root, "DS4Windows/DS4WinWPF.csproj");
        if (File.Exists(csprojPath))
        {
            var csprojText = File.ReadAllText(csprojPath);
            // Extract PackageReference Include values
            foreach (Match match in System.Text.RegularExpressions.Regex.Matches(csprojText, @"<PackageReference\s+Include=""([^""]+)"""))
            {
                var packageName = match.Groups[1].Value;
                // Try to locate package assembly in NuGet package folders
                var packageDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget/packages", packageName.ToLower());
                if (Directory.Exists(packageDir))
                {
                    var versionDir = Directory.GetDirectories(packageDir).OrderByDescending(d => d, StringComparer.Ordinal).FirstOrDefault();
                    if (versionDir != null)
                    {
                        var libDir = Path.Combine(versionDir, "lib", "net8.0");
                        if (!Directory.Exists(libDir)) libDir = Path.Combine(versionDir, "lib", "net8.0-windows10.0.19041.0");
                        if (Directory.Exists(libDir)) paths.AddRange(Directory.GetFiles(libDir, "*.dll"));
                    }
                }
            }
            // Extract Reference HintPath values
            foreach (Match match in System.Text.RegularExpressions.Regex.Matches(csprojText, @"<Reference\s+[^>]*<HintPath>([^<]+)</HintPath>"))
            {
                var hintPath = match.Groups[1].Value;
                var fullPath = Path.IsPathRooted(hintPath) ? hintPath : Path.Combine(root, hintPath);
                if (File.Exists(fullPath)) paths.Add(fullPath);
            }
        }

        // 3. Build output DLLs (from successful dotnet build)
        var output = Path.Combine(root, "DS4Windows/bin/x64/Debug");
        if (Directory.Exists(output)) paths.AddRange(Directory.GetFiles(output, "*.dll"));

        // 4. Windows Desktop App reference framework (WPF references)
        var windowsRefs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "dotnet/packs/Microsoft.WindowsDesktop.App.Ref");
        if (Directory.Exists(windowsRefs))
        {
            var version = Directory.GetDirectories(windowsRefs).Where(p => Path.GetFileName(p).StartsWith("8."))
                .OrderByDescending(p => p, StringComparer.Ordinal).FirstOrDefault();
            if (version != null) paths.AddRange(Directory.GetFiles(version, "*.dll", SearchOption.AllDirectories));
        }

        // 5. Deduplicate by file name (prefer build output over framework references for same assembly)
        foreach (var file in paths.Where(p => Path.GetFileNameWithoutExtension(p) != "DS4Windows")
                     .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase).Select(g => g.First()))
        {
            MetadataReference? reference = null;
            try { reference = MetadataReference.CreateFromFile(file); } catch (BadImageFormatException) { }
            if (reference != null) yield return reference;
        }
    }

    private static void SelfTest(MetadataReference[] metadata)
    {
        const string fixture = """
        using static DS4Windows.Global;
        namespace DS4Windows { public class Global {
          public const int Constant = 4;
          public static int Value, Second;
          public static int Method(int x) => x;
          public static int Method(string x) => x.Length;
          public static int Property
            => Value;
        } }
        class Sample {
          void Run() { GlobalAlias(); var x = DS4Windows.Global.Value + DS4Windows.Global.Value;
            var y = $"{Value}"; var z = nameof(Value); Method(1);
            // Global.Value
            var text = "Global.Value";
          }
          void GlobalAlias() { int Value = 0; Value++; }
        #if DEBUG
          int Branch() => Value;
        #else
          int Branch() => Second;
        #endif
        }
        """;
        foreach (var debug in new[] { true, false })
        {
            var tree = CSharpSyntaxTree.ParseText(fixture, new CSharpParseOptions(preprocessorSymbols: debug ? new[] { "DEBUG" } : Array.Empty<string>()));
            var compilation = CSharpCompilation.Create("Fixture", new[] { tree }, metadata,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var model = compilation.GetSemanticModel(tree);
            var refs = tree.GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().Where(n => IsGlobal(model.GetSymbolInfo(n).Symbol)).ToList();
            if (refs.Count != 7 || compilation.GetTypeByMetadataName(GlobalName)!.GetMembers().Count(s => !s.IsImplicitlyDeclared && s is not IMethodSymbol { MethodKind: MethodKind.PropertyGet }) != 6)
                throw new InvalidOperationException($"Fixture failed: debug={debug}, references={refs.Count}; " + string.Join("; ", refs.Select(n => n + "=" + model.GetSymbolInfo(n).Symbol?.Kind + ":" + model.GetSymbolInfo(n).Symbol?.ToDisplayString())));
            if (tree.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error)) throw new InvalidOperationException("Fixture syntax error");
        }
    }

    public static int Main(string[] args)
    {
        if (args.Length > 0) root = Path.GetFullPath(args[0]);
        var metadata = References().ToArray();
        SelfTest(metadata);
        if (args.Contains("--self-test")) { Console.WriteLine("PASS: Global inventory fixtures (DEBUG / alternate branch)"); return 0; }
        var outDir = Path.Combine(root, "docs-forDIMG/MadeByAgent/Phase6-Step1-Audit");
        Directory.CreateDirectory(outDir);
        var entries = Git("ls-tree", "-r", "-z", Revision).Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.Split('\t', 2)).Where(e => e[1].EndsWith(".cs", StringComparison.Ordinal)).OrderBy(e => e[1], StringComparer.Ordinal).ToList();
        var sources = entries.ToDictionary(e => e[1], e => Git("show", Revision + ":" + e[1]).TrimStart('\uFEFF'));
        void Write(string name, object value) => File.WriteAllText(Path.Combine(outDir, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));
        Write("manifest.json", entries.Select(e => new { path = e[1], blob = e[0].Split(' ')[2], scope = Scope(e[1]) }));
        Write("metadata.json", metadata.Select(m => m.Display));
        var members = new Dictionary<string, object>();
        var totals = new List<object>();
        using var candidates = new StreamWriter(Path.Combine(outDir, "candidates.jsonl"), false, new UTF8Encoding(false));
        using var raw = new StreamWriter(Path.Combine(outDir, "raw-global-text.jsonl"), false, new UTF8Encoding(false));
        using var diagnostics = new StreamWriter(Path.Combine(outDir, "diagnostics.jsonl"), false, new UTF8Encoding(false));
        foreach (var variant in new[] { "DEBUG_WIN64", "RELEASE_WIN64", "NO_SYMBOLS" })
        {
            var defines = variant == "DEBUG_WIN64" ? new[] { "DEBUG", "TRACE", "WIN64" } : variant == "RELEASE_WIN64" ? new[] { "WIN64" } : Array.Empty<string>();
            var options = new CSharpParseOptions(LanguageVersion.CSharp12, preprocessorSymbols: defines);
            var trees = sources.ToDictionary(p => p.Key, p => CSharpSyntaxTree.ParseText(p.Value, options, p.Key, Encoding.UTF8));
            var production = trees.Where(p => Scope(p.Key) == "production").Select(p => p.Value).ToArray();
            var compilation = CSharpCompilation.Create("DS4Windows-Audit", production, metadata,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            var global = compilation.GetTypeByMetadataName(GlobalName) ?? throw new InvalidOperationException("Global type missing");
            var globalMembers = global.GetMembers().Where(s => !s.IsImplicitlyDeclared && !(s is IMethodSymbol method && method.AssociatedSymbol != null)).ToList();
            var names = globalMembers.Select(s => s.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var symbol in globalMembers)
            {
                var syntax = symbol.DeclaringSyntaxReferences.First().GetSyntax();
                var id = SymbolId(symbol);
                members.TryAdd(id, new
                {
                    id,
                    name = symbol.Name,
                    kind = symbol.Kind.ToString(),
                    visibility = symbol.DeclaredAccessibility.ToString(),
                    isStatic = symbol.IsStatic,
                    isConst = symbol is IFieldSymbol { IsConst: true },
                    isReadOnly = symbol is IFieldSymbol { IsReadOnly: true },
                    signature = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat),
                    file = syntax.SyntaxTree.FilePath,
                    position = Position(syntax),
                    declaration = syntax.ToString()
                });
            }
            var stats = new Dictionary<string, int>();
            void Count(string status) => stats[status] = stats.GetValueOrDefault(status) + 1;
            foreach (var pair in trees)
            {
                var path = pair.Key; var tree = pair.Value; var syntaxRoot = tree.GetRoot();
                var scope = Scope(path);
                // Other projects may define test doubles with identical names. Do not merge them into production.
                var model = scope == "production" ? compilation.GetSemanticModel(tree, true) : null;
                foreach (var name in syntaxRoot.DescendantNodes().OfType<SimpleNameSyntax>())
                {
                    var access = name.Parent as MemberAccessExpressionSyntax;
                    bool qualified = access?.Name == name && (access.Expression.ToString() == "Global" || access.Expression.ToString().EndsWith(".Global", StringComparison.Ordinal));
                    if (!names.Contains(name.Identifier.ValueText) && !qualified) continue;
                    var info = model?.GetSymbolInfo(name) ?? default;
                    var symbol = info.Symbol;
                    var allGlobalCandidates = info.CandidateSymbols.Length > 0 && info.CandidateSymbols.All(IsGlobal);
                    var withinGlobal = name.Ancestors().OfType<TypeDeclarationSyntax>().Any(t => t.Identifier.ValueText == "Global" && t.SyntaxTree.FilePath == "DS4Windows/DS4Control/ScpUtil.cs");
                    var staticImport = syntaxRoot.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(u => u.StaticKeyword.RawKind != 0 && u.Name?.ToString() == GlobalName);
                    var aliasGlobal = access?.Name == name && model?.GetSymbolInfo(access.Expression).Symbol?.ToDisplayString() == GlobalName;
                    var possibleU = access?.Name != name && !(name.Parent is QualifiedNameSyntax) && (withinGlobal || staticImport);
                    string status;
                    if (IsGlobal(symbol)) status = "global-symbol";
                    else if (allGlobalCandidates) status = "global-overload-candidates";
                    else if (symbol != null) status = "other-symbol";
                    else if (qualified || aliasGlobal || possibleU) status = scope == "production" ? "unresolved" : "outside-production-candidate";
                    else status = "unbound-other-context";
                    Count(status);
                    candidates.WriteLine(JsonSerializer.Serialize(new
                    {
                        id = path + ":" + name.SpanStart,
                        variant,
                        file = path,
                        scope,
                        member = name.Identifier.ValueText,
                        position = Position(name),
                        status,
                        symbol = SymbolId(symbol),
                        candidateSymbols = info.CandidateSymbols.Select(SymbolId),
                        reason = info.CandidateReason.ToString(),
                        form = qualified || aliasGlobal ? "Q" : "U",
                        context = Context(name),
                        containingType = name.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault()?.Identifier.Text,
                        inLambda = name.Ancestors().Any(n => n is AnonymousFunctionExpressionSyntax),
                        usage = Usage(name),
                        text = tree.GetText().Lines.GetLineFromPosition(name.SpanStart).ToString()
                    }, JsonOptions));
                }
                foreach (Match match in Regex.Matches(sources[path], @"\bGlobal\s*\."))
                {
                    var token = syntaxRoot.FindToken(match.Index, findInsideTrivia: true);
                    var trivia = syntaxRoot.FindTrivia(match.Index, findInsideTrivia: true);
                    var activeName = syntaxRoot.DescendantNodes().OfType<IdentifierNameSyntax>().FirstOrDefault(n => n.SpanStart == match.Index && n.Identifier.ValueText == "Global");
                    var disposition = activeName != null ? "code" : trivia.IsKind(SyntaxKind.DisabledTextTrivia) ? "inactive" :
                        trivia.RawKind != 0 ? "trivia-" + trivia.Kind() : "token-" + token.Kind();
                    raw.WriteLine(JsonSerializer.Serialize(new
                    {
                        id = path + ":" + match.Index,
                        variant,
                        file = path,
                        scope,
                        offset = match.Index,
                        line = tree.GetText().Lines.GetLineFromPosition(match.Index).LineNumber + 1,
                        disposition
                    }, JsonOptions));
                }
                foreach (var diagnostic in tree.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error))
                    diagnostics.WriteLine(JsonSerializer.Serialize(new { variant, phase = "parse", file = path, message = diagnostic.ToString() }));
            }
            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
            foreach (var diagnostic in errors) diagnostics.WriteLine(JsonSerializer.Serialize(new { variant, phase = "compilation", message = diagnostic.ToString() }));
            totals.Add(new { variant, defines, stats, compilationErrors = errors.Count, members = globalMembers.Count });
        }
        Write("members.json", members.Values);
        Write("summary.json", new
        {
            revision = Revision,
            sourceTree = Git("rev-parse", Revision + ":DS4Windows").Trim(),
            trackedCSharpFiles = sources.Count,
            productionFiles = sources.Keys.Count(p => Scope(p) == "production"),
            memberCount = members.Count,
            selfTests = "passed",
            totals,
            limitations = new[] { "Partial semantic model, not an MSBuild build. Compilation diagnostics must be reviewed.",
                "Outside-production references require separate project/stub ownership review.", "Three preprocessor variants only; inactive text is retained separately.",
                "Candidate ledger includes same-name unrelated identifiers; do not count all candidates as references.",
                "No DI classification or hot-path correctness is inferred by this extractor." }
        });
        Console.WriteLine(JsonSerializer.Serialize(new { output = outDir, memberCount = members.Count, files = sources.Count, totals }, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }
}
