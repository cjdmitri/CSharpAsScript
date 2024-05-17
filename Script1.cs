using System;
using System.Threading;
using static CSharpAsScript.Program;


public class Script1
{
     public void Run(string message)
     {
          Log($"Script running!! {message}");
          GetSum();

          Log("Иммитация затяжного процесса");
          for (int i = 0; i < 10; i++)
          {
               Log($"Выполнение процесса... {i}");
               Thread.Sleep(200);
          }
          Log("Процесс завершен.");
          Log("Я метод главной программы, вызванный из скрипта!");
     }

     private void GetSum()
     {
          int a = 5;
          int b = 5;
          int sum = a + b;
          Log($"Sum  = {sum}");
     }
}
