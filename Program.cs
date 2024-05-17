using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System.Reflection;
using System.Runtime.Loader;

namespace CSharpAsScript
{
     public class Program
     {

          /// <summary>
          /// Сколько было выделено памяти на процесс 
          /// </summary>
          private static long consumedInMegabytes = 0;

          public static void Main(string[] args)
          {

               long before = GC.GetTotalMemory(false);

               Log("Чтение файла скрипта");
               string codeToCompile = System.IO.File.ReadAllText("Script1.cs");

               Log("Парсинг исходника файла...");
               SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeToCompile);

               string assemblyName = Path.GetRandomFileName();
               Log($"Имя нового файла: {assemblyName}");
               Log("Добавляем зависимости");
               var refPaths = new[] {
                    typeof(System.Object).GetTypeInfo().Assembly.Location,
                    typeof(Console).GetTypeInfo().Assembly.Location,
                    //Path.Combine(Path.GetDirectoryName(typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.Location), "System.Runtime.dll"),
                    typeof(Program).GetTypeInfo().Assembly.Location
               };
               MetadataReference[] references = refPaths.Select(r => MetadataReference.CreateFromFile(r)).ToArray();

               foreach (var r in refPaths)
                    Log(r);

               Log("Компиляция...");
               CSharpCompilation compilation = CSharpCompilation.Create(
                   assemblyName,
                   syntaxTrees: new[] { syntaxTree },
                   references: references,
                   options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

               using (var ms = new MemoryStream())
               {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                         Log("Ошибка компиляции");
                         IEnumerable<Diagnostic> failures = result.Diagnostics.Where(d =>
                             d.IsWarningAsError ||
                             d.Severity == DiagnosticSeverity.Error);

                         foreach (Diagnostic diagnostic in failures)
                         {
                              Console.Error.WriteLine("\t{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                         }
                    }
                    else
                    {
                         Log("Компиляция завершена успешно!");
                         Log("Создание экземпляра и выполнение кода");
                         ms.Seek(0, SeekOrigin.Begin);

                         //Создаём контекст для сборки
                         //Для возможности дальнейшей выгрузки, после отработки скрипта
                         var context = new AssemblyLoadContext(name: Guid.NewGuid().ToString(), isCollectible: true);

                         Assembly assembly = context.LoadFromStream(ms);
                         var type = assembly.GetType("Script1");
                         var instance = assembly.CreateInstance("Script1");
                         var method = type.GetMember("Run").First() as MethodInfo;

                         //Выполняем метод с параметрами
                         method.Invoke(instance, new[] { "Параметр 1" });

                         //Сколько памяти было выделено на процесс
                         long after = GC.GetTotalMemory(false);
                         consumedInMegabytes = (after - before) / 1024;
                         Log($"Скрипт выполнен! Выделено памяти: {consumedInMegabytes} Kb");

                         //Выгрузка контекста после выполнения метода и очистка памяти
                         context.Unloading += Context_Unloading;
                         context.Unload();
                    }
               }
               GC.Collect();
               GC.WaitForPendingFinalizers();
               Log("Выполнена сборка мусора.");

               //Console.Write("Нажмите любую клавишу для выхода");
               //Console.ReadLine();
          }

          private static void Context_Unloading(AssemblyLoadContext obj)
          {
               Log("Контекст выгружен.");
          }

          public static void Log(string message)
          {
               long tMemory = GC.GetTotalMemory(false) / 1024;
               Console.WriteLine($"{DateTime.Now.ToLongTimeString()}:{DateTime.Now.Millisecond} \t{tMemory.ToString()} Kb \t{message}");
          }
     }
}
