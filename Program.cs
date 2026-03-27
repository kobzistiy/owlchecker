using System;
using System.Reflection;
using System.IO;
using System.Linq;

string owenPath = args[0];

AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
    string dllName = new AssemblyName(resolveArgs.Name).Name + ".dll";
    string[] searchPaths = { owenPath, Path.Combine(owenPath, "ST") };
    foreach (var path in searchPaths) {
        string fullPath = Path.Combine(path, dllName);
        if (File.Exists(fullPath)) return Assembly.LoadFrom(fullPath);
    }
    return null;
};

try {
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));
    var lexerType = asmParser.GetType("StParser.Lexer");
    
    Console.WriteLine($"=== Methods in {lexerType.FullName} ===");
    foreach (var m in lexerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
        var paramsInfo = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"{m.ReturnType.Name} {m.Name}({paramsInfo})");
    }
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
