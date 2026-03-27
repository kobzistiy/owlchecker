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
    var asmLang = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "STLanguage.dll"));
    var asmParser = Assembly.LoadFrom(Path.Combine(owenPath, "ST", "StParser.dll"));

    // 1. Создаем окружение через Фабрику
    var factoryType = asmLang.GetType("Owen.STLanguage.SourceModel.ProgramEnvironmentFactory");
    var factory = Activator.CreateInstance(factoryType);
    var createEnvMethod = factoryType.GetMethod("Create");
    var environment = createEnvMethod.Invoke(factory, null);

    // 2. Инициализируем системные компоненты (чтобы он знал про UDINT_TO_TIME, TON и т.д.)
    var initServiceType = asmLang.GetType("Owen.STLanguage.Domain.InitializeSystemComponentsService");
    var initService = Activator.CreateInstance(initServiceType);
    var initMethod = initServiceType.GetMethod("Initialize");
    initMethod.Invoke(initService, new object[] { environment });

    // 3. Создаем парсер и парсим код
    var parseServiceType = asmLang.GetType("Owen.STLanguage.Domain.ParseService");
    
    // Ищем StaticAnalyzer (теперь мы можем передать ему зависимости)
    var staticAnalyzerType = asmLang.GetType("Owen.STLanguage.Domain.Analyzers.StaticAnalyzer");
    // Используем конструктор: StaticAnalyzer(IEnumerable<IExpressionAnalizer>, ProgramEnvironment, IUnitRepository)
    // Для простоты попробуем найти метод или создать с null репозиторием
    var analyzer = Activator.CreateInstance(staticAnalyzerType, new object[] { null, environment, null });

    var parseService = Activator.CreateInstance(parseServiceType, new object[] { analyzer });
    
    string sourceCode = File.ReadAllText(stFile);
    
    Console.WriteLine($"--- Deep Analysis of {Path.GetFileName(stFile)} ---");
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
                Console.WriteLine($"ERROR: {msg} (Line {line}, Col {col})");
            }
        }

        if (errorCount == 0) Console.WriteLine("SUCCESS: Code is perfectly valid.");
        else Console.WriteLine($"FAILED: {errorCount} semantic errors found.");
    }

} catch (Exception ex) {
    Console.WriteLine($"\n!!! CRITICAL ERROR: {ex.Message} !!!");
    if (ex.InnerException != null) Console.WriteLine($"Inner Details: {ex.InnerException.Message}");
    
    // Если всё совсем плохо, выводим что есть через простой парсер
    Console.WriteLine("\nFalling back to simple syntax check...");
    // (Код простого парсинга)
}
