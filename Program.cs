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
    Console.WriteLine("Step 1: Loading StParser.dll...");
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    var lexerType = asmParser.GetType("StParser.Lexer");
    var parserType = asmParser.GetType("StParser.Parsers.Parser");
    
    if (lexerType == null) { Console.WriteLine("FAILED: Lexer type not found."); return; }
    if (parserType == null) { Console.WriteLine("FAILED: Parser type not found."); return; }

    Console.WriteLine("Step 2: Creating instances...");
    var lexer = Activator.CreateInstance(lexerType);
    var parser = Activator.CreateInstance(parserType);

    Console.WriteLine("Step 3: Finding methods...");
    // Ищем Tokenize более гибко
    var tokenizeMethod = lexerType.GetMethods().FirstOrDefault(m => m.Name == "Tokenize" && m.GetParameters().Length == 1);
    if (tokenizeMethod == null) { Console.WriteLine("FAILED: Tokenize method not found."); return; }

    Console.WriteLine("Step 4: Reading ST file...");
    string sourceCode = File.ReadAllText(stFile);

    Console.WriteLine("Step 5: Tokenizing...");
    var tokens = tokenizeMethod.Invoke(lexer, new object[] { sourceCode });
    if (tokens == null) { Console.WriteLine("FAILED: Tokenize returned null."); return; }

    Console.WriteLine("Step 6: Finding Parse method...");
    // Метод Parse принимает (IList<Token>, string)
    var parseMethod = parserType.GetMethods().FirstOrDefault(m => m.Name == "Parse" && m.GetParameters().Length == 2);
    if (parseMethod == null) { Console.WriteLine("FAILED: Parse method not found."); return; }

    Console.WriteLine("Step 7: Parsing...");
    var tree = parseMethod.Invoke(parser, new object[] { tokens, sourceCode });

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
        Console.WriteLine("FAILED: Parser returned null tree.");
    }

} catch (Exception ex) {
    Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
    if (ex.InnerException != null) {
        Console.WriteLine($"INNER ERROR: {ex.InnerException.Message}");
        Console.WriteLine($"STACK: {ex.InnerException.StackTrace}");
    } else {
        Console.WriteLine($"STACK: {ex.StackTrace}");
    }
}
