using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

if (args.Length < 2) {
    Console.WriteLine("Usage: OwenStChecker.exe <OwenLogicPath> <StFilePath>");
    return;
}

string owenPath = args[0];
string stFile = args[1];

AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
    string dllName = new AssemblyName(resolveArgs.Name).Name + ".dll";
    string[] searchPaths = { owenPath, Path.Combine(owenPath, "ST"), Path.Combine(owenPath, "ProjectJsonConverter") };
    foreach (var path in searchPaths) {
        string fullPath = Path.Combine(path, dllName);
        if (File.Exists(fullPath)) return Assembly.LoadFrom(fullPath);
    }
    return null;
};

void RunSyntaxOnly(string owenPath, string stFile) {
    Console.WriteLine("--- Running Syntax-Only Check ---");
    try {
        var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));
        var lexerType = asmParser.GetType("StParser.Lexer");
        var parserType = asmParser.GetType("StParser.Parsers.Parser");
        
        var lexer = Activator.CreateInstance(lexerType);
        var parser = Activator.CreateInstance(parserType);

        string sourceCode = File.ReadAllText(stFile);

        // ОЧЕНЬ ВАЖНО: Owen Logic Lexer может возвращать комментарии как токены.
        // Мы попробуем отфильтровать их перед парсингом, если парсер на них ругается.
        var analyzeMethod = lexerType.GetMethod("Analyze", new[] { typeof(string) });
        var tokens = analyzeMethod.Invoke(lexer, new object[] { sourceCode }) as IEnumerable;

        if (tokens != null) {
            // Пытаемся вызвать парсинг
            var parseMethod = parserType.GetMethods().FirstOrDefault(m => m.Name == "Parse" && m.GetParameters().Length == 2);
            var tree = parseMethod.Invoke(parser, new object[] { tokens, sourceCode });

            if (tree != null) {
                var errors = tree.GetType().GetProperty("Errors")?.GetValue(tree) as IEnumerable;
                int count = 0;
                if (errors != null) {
                    foreach (var err in errors) {
                        count++;
                        var line = err.GetType().GetProperty("Line")?.GetValue(err);
                        var msg = err.GetType().GetProperty("Message")?.GetValue(err);
                        Console.WriteLine($"LINE {line}: {msg}");
                    }
                }
                if (count == 0) Console.WriteLine("SUCCESS: Syntax is valid.");
                else Console.WriteLine($"FAILED: {count} syntax errors.");
            }
        }
    } catch (Exception ex) {
        Console.WriteLine($"Syntax Check Error: {ex.Message}");
    }
}

try {
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var staticAnalyzerType = asmLang.GetType("Owen.STLanguage.Domain.Analyzers.StaticAnalyzer");

    Console.WriteLine($"--- Inspecting StaticAnalyzer constructors ---");
    foreach (var ctor in staticAnalyzerType.GetConstructors()) {
        Console.WriteLine("Constructor found with params: " + string.Join(", ", ctor.GetParameters().Select(p => p.ParameterType.Name)));
    }

    // Пока мы не знаем как создать StaticAnalyzer со всеми зависимостями, 
    // запускаем гарантированный синтаксический чек
    RunSyntaxOnly(owenPath, stFile);

} catch (Exception ex) {
    Console.WriteLine($"\n!!! ERROR: {ex.Message} !!!");
    RunSyntaxOnly(owenPath, stFile);
}
