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

// Умный резолвер зависимостей
AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
    string dllName = new AssemblyName(resolveArgs.Name).Name + ".dll";
    string[] searchPaths = { owenPath, Path.Combine(owenPath, "ST"), Path.Combine(owenPath, "ProjectJsonConverter") };
    foreach (var path in searchPaths) {
        string fullPath = Path.Combine(path, dllName);
        if (File.Exists(fullPath)) return Assembly.LoadFrom(fullPath);
    }
    return null;
};

void DumpDiagnostics(Assembly asm) {
    Console.WriteLine("\n=== EMERGENCY DIAGNOSTICS ===");
    string[] typesToDump = { "StParser.Lexer", "StParser.Parsers.Parser", "StParser.ParseSyntaxTree", "StParser.ProgramError" };
    foreach (var typeName in typesToDump) {
        var t = asm.GetType(typeName);
        if (t == null) { Console.WriteLine($"Type {typeName} not found."); continue; }
        Console.WriteLine($"\nType: {typeName}");
        Console.WriteLine("  Methods:");
        foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => !m.IsSpecialName))
            Console.WriteLine($"    - {m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})");
        Console.WriteLine("  Properties:");
        foreach (var p in t.GetProperties())
            Console.WriteLine($"    - {p.PropertyType.Name} {p.Name}");
    }
}

try {
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));
    
    var lexerType = asmParser.GetType("StParser.Lexer");
    var parserType = asmParser.GetType("StParser.Parsers.Parser");
    var treeType = asmParser.GetType("StParser.ParseSyntaxTree");

    // 1. Инициализация
    var lexer = Activator.CreateInstance(lexerType);
    var parser = Activator.CreateInstance(parserType);

    // 2. Поиск методов по сигнатуре (Analyze принимает string, Parse принимает IEnumerable и string)
    var analyzeMethod = lexerType.GetMethods().FirstOrDefault(m => m.Name == "Analyze" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
    
    // Читаем код
    string sourceCode = File.ReadAllText(stFile);

    // 3. Токенизация
    var tokens = analyzeMethod.Invoke(lexer, new object[] { sourceCode });

    // 4. Парсинг
    var parseMethod = parserType.GetMethods().FirstOrDefault(m => m.Name == "Parse" && m.GetParameters().Length == 2);
    var tree = parseMethod.Invoke(parser, new object[] { tokens, sourceCode });

    // 5. Вывод ошибок
    if (tree != null) {
        var errorsProp = tree.GetType().GetProperty("Errors");
        var errors = errorsProp?.GetValue(tree) as IEnumerable;

        int errorCount = 0;
        if (errors != null) {
            foreach (var err in errors) {
                errorCount++;
                var line = err.GetType().GetProperty("Line")?.GetValue(err);
                var col = err.GetType().GetProperty("Column")?.GetValue(err);
                var msg = err.GetType().GetProperty("Message")?.GetValue(err);
                Console.WriteLine($"LINE {line}, COL {col}: {msg}");
            }
        }

        if (errorCount == 0) {
            Console.WriteLine("SUCCESS: No syntax errors found.");
        } else {
            Console.WriteLine($"FAILED: {errorCount} errors found.");
        }
    } else {
        Console.WriteLine("CRITICAL: Parser returned null tree.");
        DumpDiagnostics(asmParser);
    }

} catch (Exception ex) {
    Console.WriteLine($"\n!!! CRITICAL ERROR: {ex.Message} !!!");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
    
    // Пытаемся вывести диагностику даже при падении
    try {
        var asm = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));
        DumpDiagnostics(asm);
    } catch { }
}
