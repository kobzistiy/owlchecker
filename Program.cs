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

try {
    Console.WriteLine("Loading Owen Logic Core Engine...");
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    // 1. Создаем StaticAnalyzer (он нужен для ParseService)
    var staticAnalyzerType = asmLang.GetType("Owen.STLanguage.Domain.Analyzers.StaticAnalyzer");
    // У StaticAnalyzer может быть конструктор с параметрами, попробуем найти самый простой
    var staticAnalyzer = Activator.CreateInstance(staticAnalyzerType);

    // 2. Создаем ParseService, передавая ему StaticAnalyzer
    var parseServiceType = asmLang.GetType("Owen.STLanguage.Domain.ParseService");
    var parseService = Activator.CreateInstance(parseServiceType, new object[] { staticAnalyzer });

    // 3. Читаем код
    string sourceCode = File.ReadAllText(stFile);

    // 4. Вызываем ParseSourceCode
    Console.WriteLine("Running Deep Semantic Analysis...");
    var parseMethod = parseServiceType.GetMethod("ParseSourceCode", new[] { typeof(string) });
    var tree = parseMethod.Invoke(parseService, new object[] { sourceCode });

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
            Console.WriteLine("SUCCESS: Full check passed.");
        } else {
            Console.WriteLine($"FAILED: {errorCount} errors found.");
        }
    }

} catch (Exception ex) {
    Console.WriteLine($"\n!!! ERROR: {ex.Message} !!!");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
    
    // Если не удалось запустить сложный анализатор, откатываемся к простому, но с фиксом комментариев
    Console.WriteLine("\nFalling back to Syntax-only check...");
    // (Тут будет код из прошлой версии, если этот упадет)
}
