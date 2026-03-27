using System;
using System.Reflection;
using System.IO;
using System.Linq;

string owenPath = args[0];
string dllPath = Path.Combine(owenPath, "ST", "STLanguage.dll");

try {
    var asm = Assembly.LoadFrom(dllPath);
    Console.WriteLine($"--- Types in {asm.FullName} ---");
    foreach (var type in asm.GetTypes().Where(t => t.IsPublic)) {
        Console.WriteLine(type.FullName);
    }
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
