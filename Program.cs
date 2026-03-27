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

void InspectTypeFull(Assembly asm, string typeName) {
    var type = asm.GetType(typeName);
    if (type == null) { Console.WriteLine($"Type {typeName} not found."); return; }
    Console.WriteLine($"\n=== Inspecting {typeName} ===");
    
    Console.WriteLine("\n--- Constructors ---");
    foreach (var ctor in type.GetConstructors()) {
        var paramsInfo = string.Join(", ", ctor.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"{type.Name}({paramsInfo})");
    }

    Console.WriteLine("\n--- Public Methods ---");
    foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(m => !m.IsSpecialName)) {
        var paramsInfo = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"{m.ReturnType.Name} {m.Name}({paramsInfo})");
    }
}

try {
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    InspectTypeFull(asmLang, "Owen.STLanguage.Domain.ParseService");
    InspectTypeFull(asmParser, "StParser.Parsers.Parser");

} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
