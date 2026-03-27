using System;
using System.Reflection;
using System.IO;
using System.Linq;
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
    if (type == null) return;
    Console.WriteLine($"\n--- Methods in {typeName} ---");
    foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
        var paramsInfo = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
        Console.WriteLine($"{m.ReturnType.Name} {m.Name}({paramsInfo})");
    }
}

try {
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    Console.WriteLine("--- Types in StParser.dll ---");
    foreach (var t in asmParser.GetTypes().Where(t => t.IsPublic)) Console.WriteLine(t.FullName);

    InspectType(asmLang, "Owen.STLanguage.Domain.ParseService");
    InspectType(asmLang, "Owen.STLanguage.Application.BuildSyntaxTreeService");
    InspectType(asmParser, "StParser.StParser"); // Предполагаю наличие такого класса

} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
