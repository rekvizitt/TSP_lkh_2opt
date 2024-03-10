namespace GKH
{
    public interface ISolver
    {
        public int[] Solution
        {
            get;
        }
        
        public static int[] GetCosts(int[] solution)
        {
            var costs = new int[solution.Length - 1];
        
            for (var i = 0; i < solution.Length - 1; i++)
            {
                costs[i] = Globals.Distances[solution[i]][solution[i + 1]];
            }

            return costs;
        }

        public void Solve();
        
        public static int GetSum(IReadOnlyList<int> solution)
        {
            var sum = 0;

            for (var i = 0; i < solution.Count - 1; i++)
            {
                sum += Globals.Distances[solution[i]][solution[i + 1]];
            }

            return sum;
        }

        public static string PrintSolution(int[] solution)
        {
            var result = "";
            for (int i = 0; i < solution.Length - 1; i++)
            {
                result += $"{solution[i] + 1} -> ";
            }

            result += $"{solution.Last() + 1}";

            return result;
        }

        public static string PrintCosts(int[] solution, int[] costs)
        {
            var result = "";
            for (int i = 0; i < costs.Length - 1; i++)
            {
                result += $"{costs[i]} -> ";
            }

            result += $"{costs.Last()}";

            return result;
        }
    }
}