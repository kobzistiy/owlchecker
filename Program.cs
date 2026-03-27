using System;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

string owenPath = args[0];

AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
    string dllName = new AssemblyName(resolveArgs.Name).Name + ".dll";
    string[] searchPaths = { owenPath, Path.Combine(owenPath, "ST"), Path.Combine(owenPath, "ProjectJsonConverter") };
    foreach (var path in searchPaths) {
        string fullPath = Path.Combine(path, dllName);
        if (File.Exists(fullPath)) return Assembly.LoadFrom(fullPath);
    }
    return null;
};

void InspectType(Assembly asm, string typeName) {
    var type = asm.GetType(typeName);
    if (type == null) { Console.WriteLine($"Type {typeName} not found."); return; }
    Console.WriteLine($"\n--- Properties in {typeName} ---");
    foreach (var p in type.GetProperties()) Console.WriteLine($"{p.PropertyType.Name} {p.Name}");
    Console.WriteLine($"\n--- Methods in {typeName} ---");
    foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => !m.IsSpecialName)) {
        var paramsInfo = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"{m.ReturnType.Name} {m.Name}({paramsInfo})");
    }
}

try {
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    InspectType(asmParser, "StParser.ParseSyntaxTree");
    InspectType(asmParser, "StParser.ProgramError");

    // Пробный парсинг
    Console.WriteLine("\n--- Testing ParseSourceCode with error ---");
    var parseServiceType = asmLang.GetType("Owen.STLanguage.Domain.ParseService");
    var parseService = Activator.CreateInstance(parseServiceType);
    var parseMethod = parseServiceType.GetMethod("ParseSourceCode", new[] { typeof(string) });
    
    // Код с ошибкой (пропущена точка с запятой)
    string testCode = "FUNCTION_BLOCK Test VAR x : INT END_VAR x := 5 END_FUNCTION_BLOCK"; 
    var tree = parseMethod.Invoke(parseService, new object[] { testCode });

    if (tree != null) {
        Console.WriteLine($"Tree type: {tree.GetType().FullName}");
        var errorsProp = tree.GetType().GetProperty("Errors");
        if (errorsProp != null) {
            var errors = errorsProp.GetValue(tree) as IEnumerable;
            if (errors != null) {
                foreach (var err in errors) {
                    var msg = err.GetType().GetProperty("Message")?.GetValue(err);
                    var line = err.GetType().GetProperty("Line")?.GetValue(err);
                    Console.WriteLine($"Detected Error: {msg} at line {line}");
                }
            }
        }
    }

} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
    if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
}
