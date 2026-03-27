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

if (!File.Exists(stFile)) {
    Console.WriteLine($"Error: ST file {stFile} not found.");
    return;
}

// Резолвер зависимостей
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
    // 1. Загружаем библиотеку парсера
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    // 2. Получаем типы
    var lexerType = asmParser.GetType("StParser.Lexer");
    var parserType = asmParser.GetType("StParser.Parsers.Parser");
    
    if (lexerType == null || parserType == null) {
        Console.WriteLine("Error: Required types (Lexer/Parser) not found in StParser.dll.");
        return;
    }

    // 3. Создаем экземпляры
    var lexer = Activator.CreateInstance(lexerType);
    var parser = Activator.CreateInstance(parserType);

    // 4. Читаем код
    string sourceCode = File.ReadAllText(stFile);

    // 5. Токенизация (метод Tokenize)
    var tokenizeMethod = lexerType.GetMethod("Tokenize", new[] { typeof(string) });
    var tokens = tokenizeMethod.Invoke(lexer, new object[] { sourceCode });

    // 6. Парсинг (метод Parse)
    var parseMethod = parserType.GetMethod("Parse", new[] { tokens.GetType(), typeof(string) });
    var tree = parseMethod.Invoke(parser, new object[] { tokens, sourceCode });

    // 7. Извлекаем ошибки
    if (tree != null) {
        var errorsProp = tree.GetType().GetProperty("Errors");
        var errors = errorsProp.GetValue(tree) as IEnumerable;

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
    }

} catch (Exception ex) {
    Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
}
