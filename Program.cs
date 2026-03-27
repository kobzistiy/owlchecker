using System;
using System.Reflection;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

class Program {
    static void Main(string[] args) {
        if (args.Length < 2) {
            Console.WriteLine("Usage: OwenStChecker.exe <OwenLogicPath> <StFilePath>");
            return;
        }

        string owenPath = args[0];
        string stFile = args[1];
        
        // Пути к библиотекам
        string stLanguageDll = Path.Combine(owenPath, "ST", "STLanguage.dll");
        string stParserDll = Path.Combine(owenPath, "ST", "StParser.dll");

        if (!File.Exists(stLanguageDll)) {
            Console.WriteLine($"Error: {stLanguageDll} not found.");
            return;
        }

        try {
            // 1. Загружаем сборки
            var asmLanguage = Assembly.LoadFrom(stLanguageDll);
            var asmParser = Assembly.LoadFrom(stParserDll);

            // 2. Ищем необходимые типы (имена взяты из анализа DLL)
            var compilerType = asmLanguage.GetType("Owen.STLanguage.Domain.Compilation.Compiler");
            var errorType = asmParser.GetType("StParser.ProgramError");

            if (compilerType == null) {
                Console.WriteLine("Error: Could not find Compiler type.");
                return;
            }

            // 3. Читаем код ST
            string sourceCode = File.ReadAllText(stFile);

            // 4. Вызываем компиляцию (упрощенная модель вызова через рефлексию)
            Console.WriteLine($"--- Analysing: {Path.GetFileName(stFile)} ---");
            
            // В Owen Logic компиляция часто идет через статические методы или синглтоны.
            // На основе строк в DLL, попробуем найти метод Verify или Analize.
            var verifyMethod = compilerType.GetMethod("Verify", BindingFlags.Public | BindingFlags.Static);
            
            if (verifyMethod == null) {
                // Если метода Verify нет, попробуем вывести список всех методов для отладки
                Console.WriteLine("Method 'Verify' not found. Available methods in Compiler:");
                foreach (var m in compilerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)) {
                    Console.WriteLine($" - {m.Name}");
                }
                return;
            }

            // Вызываем метод (предполагаем, что он принимает строку кода)
            // Примечание: точная сигнатура может потребовать подстройки после первого запуска
            var result = verifyMethod.Invoke(null, new object[] { sourceCode });

            if (result is IEnumerable errors) {
                int count = 0;
                foreach (var err in errors) {
                    count++;
                    // Выводим детали ошибки (Line, Message)
                    var lineProp = err.GetType().GetProperty("Line");
                    var msgProp = err.GetType().GetProperty("Message");
                    Console.WriteLine($"Error at line {lineProp?.GetValue(err)}: {msgProp?.GetValue(err)}");
                }
                if (count == 0) Console.WriteLine("No errors found.");
            } else {
                Console.WriteLine("Compilation finished with no errors (or unknown result type).");
            }

        } catch (Exception ex) {
            Console.WriteLine($"CRITICAL ERROR: {ex.Message}");
            if (ex.InnerException != null) Console.WriteLine($"Inner: {ex.InnerException.Message}");
        }
    }
}
