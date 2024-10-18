using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace GKH
{
    public class LkhSolver : ISolver
    {
        public int[] Solution { get; private set; } = new int[Globals.MatrixSize];

        public void Solve()
        {
            // Формируем аргументы для запуска Python скрипта
            var distancesString = string.Join(";", Globals.Distances.Select(row => string.Join(",", row)));
            var iterations = Globals.Iterations.ToString();


            // Запускаем Python скрипт с аргументами
            var startInfo = new ProcessStartInfo
            {
                FileName = "./solve.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                Arguments = $"{iterations} \"{distancesString}\""
            };

            using (var solver = Process.Start(startInfo))
            {
                // Ждем завершения процесса
                solver.WaitForExit();

                // Читаем решение из стандартного вывода Python скрипта
                using var reader = solver.StandardOutput;
                var solutionFromFile = reader.ReadLine();
                Solution = solutionFromFile![1..^1].Split(',').Select(int.Parse).ToArray();
            }
        }
    }
}
