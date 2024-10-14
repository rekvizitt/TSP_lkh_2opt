using System.Diagnostics;
using System.IO;
using System.Text;

namespace GKH
{
    public class LkhSolver : ISolver
    {
        public int[] Solution { get; private set; } = new int[Globals.MatrixSize];

        public void Solve()
        {
            // to solve using lkh we use python script, to transfer data between script and this app we use temp.txt
            using (var streamWriter = new StreamWriter("./temp.txt", Encoding.UTF8, new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create
            }))
            {
                streamWriter.WriteLine($"{Globals.Iterations}");

                for (var i = 0; i < Globals.MatrixSize; i++)
                {
                    for (var j = 0; j < Globals.MatrixSize - 1; j++)
                    {
                        streamWriter.Write($"{Globals.Distances[i][j]}, ");
                    }

                    streamWriter.Write($"{Globals.Distances[i][Globals.MatrixSize - 1]}");
                    streamWriter.WriteLine();
                }
            }

            // run the script, wait for it to finish
            Solution = new int[Globals.MatrixSize];

            var startInfo = new ProcessStartInfo
            {
                FileName = "./solve.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            using (var solver = Process.Start(startInfo))
            {
                solver.WaitForExit();

                // script will produce solution in the same file, we read it
                using var streamReader = new StreamReader("./temp.txt", Encoding.UTF8);
                var solutionFromFile = streamReader.ReadLine();
                Solution = solutionFromFile![1..^1].Split(',').Select(int.Parse).ToArray();
            }
        }
    }
}