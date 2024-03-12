using Python.Runtime;

namespace GKH
{
    public class LkhSolver : ISolver
    {
        public int[] Solution { get; private set; } = new int[Globals.MatrixSize];

        public void Solve()
        {
            Solution = new int[Globals.MatrixSize];
            
            // yes, this is python in c#
            // necessary to start
            if (!PythonEngine.IsInitialized)
            {
                Runtime.PythonDLL = "python311.dll";
                PythonEngine.Initialize();
            }

            using var _ = Py.GIL();
            // importing
            dynamic elkai = Py.Import("elkai");

            dynamic distances = Globals.Distances;

            dynamic edges = elkai.DistanceMatrix(distances);

            dynamic solution = edges.solve_tsp(Globals.Iterations);

            // solution is loop by default so pop
            solution.pop();

            // to parse from python to c# 
            string solutionInString = solution.ToString();
            solutionInString = solutionInString[1..^1];
            Solution = solutionInString.Split(',').Select(int.Parse).ToArray();
            
        }
    }
}