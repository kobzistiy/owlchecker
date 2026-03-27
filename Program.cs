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

void RunCleanCheck(string owenPath, string stFile) {
    try {
        var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));
        var lexerType = asmParser.GetType("StParser.Lexer");
        var parserType = asmParser.GetType("StParser.Parsers.Parser");
        var tokenType = asmParser.GetType("StParser.Token");
        
        var lexer = Activator.CreateInstance(lexerType);
        var parser = Activator.CreateInstance(parserType);
        string sourceCode = File.ReadAllText(stFile);

        var analyzeMethod = lexerType.GetMethod("Analyze", new[] { typeof(string) });
        var rawTokens = analyzeMethod.Invoke(lexer, new object[] { sourceCode }) as IEnumerable;

        if (rawTokens != null) {
            // Фильтруем комментарии. В Owen Logic тип токена обычно имеет свойство Type или TokenType.
            // Мы просто проверим свойство "Type", если оно есть, и отсечем комментарии по имени.
            var filteredTokensList = Activator.CreateInstance(typeof(List<>).MakeGenericType(tokenType));
            var addMethod = filteredTokensList.GetType().GetMethod("Add");

            foreach (var token in rawTokens) {
                var tTypeProp = token.GetType().GetProperty("Type");
                string typeName = tTypeProp?.GetValue(token)?.ToString() ?? "";
                
                // Игнорируем токены, похожие на комментарии
                if (!typeName.Contains("Comment") && !typeName.Contains("CommentBlock")) {
                    addMethod.Invoke(filteredTokensList, new object[] { token });
                }
            }

            var parseMethod = parserType.GetMethods().FirstOrDefault(m => m.Name == "Parse" && m.GetParameters().Length == 2);
            var tree = parseMethod.Invoke(parser, new object[] { filteredTokensList, sourceCode });

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
                if (count == 0) Console.WriteLine("SUCCESS: ST is valid (comments ignored).");
                else Console.WriteLine($"FAILED: {count} errors found.");
            }
        }
    } catch (Exception ex) {
        Console.WriteLine($"Checker Error: {ex.Message}");
    }
}

try {
    // Выполняем проверку
    RunCleanCheck(owenPath, stFile);
} catch (Exception ex) {
    Console.WriteLine($"CRITICAL: {ex.Message}");
}
