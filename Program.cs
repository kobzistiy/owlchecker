using System;
using System.Reflection;
using System.IO;
using System.Linq;

string owenPath = args[0];

// Обработчик для автоматического поиска зависимостей в папках Owen Logic
AppDomain.CurrentDomain.AssemblyResolve += (sender, resolveArgs) => {
    string dllName = new AssemblyName(resolveArgs.Name).Name + ".dll";
    
    // Список путей для поиска
    string[] searchPaths = {
        owenPath,
        Path.Combine(owenPath, "ST"),
        Path.Combine(owenPath, "ProjectJsonConverter"),
        Path.Combine(owenPath, "ProjectJsonConverter", "ST")
    };

    foreach (var path in searchPaths) {
        string fullPath = Path.Combine(path, dllName);
        if (File.Exists(fullPath)) {
            return Assembly.LoadFrom(fullPath);
        }
    }
    return null;
};

try {
    string dllPath = Path.Combine(owenPath, "ST", "STLanguage.dll");
    var asm = Assembly.LoadFrom(dllPath);
    
    Console.WriteLine($"--- Types in {asm.FullName} ---");
    
    // Теперь типы должны загрузиться без ошибок
    var types = asm.GetTypes();
    foreach (var type in types.Where(t => t.IsPublic)) {
        Console.WriteLine(type.FullName);
    }
} catch (ReflectionTypeLoadException ex) {
    Console.WriteLine("Loader Errors:");
    foreach (var loaderEx in ex.LoaderExceptions) {
        Console.WriteLine($" - {loaderEx?.Message}");
    }
} catch (Exception ex) {
    Console.WriteLine($"Error: {ex.Message}");
}
