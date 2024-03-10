namespace GKH;

public class TwoOptSolver : ISolver
{
    public int[] Solution { get; private set; } = new int[Globals.MatrixSize];

    public void Solve()
    {
        Solution = new int[Globals.MatrixSize];

        var random = new Random();
        var swappedSolution = new int[Solution.Length];
        var possibleSwaps = new Dictionary<(int, int), int>();

        #region initial

        var firstStop = random.Next(0, Globals.MatrixSize);
        Solution[0] = firstStop;

        for (var i = 1; i < Solution.Length; i++)
        {
            Solution[i] = -2;
        }

        #endregion

        #region nearestNeighbors

        // form set using NearestNeighbours algorithm (for each stop connect it by minimum distance)
        for (var i = 0; i < Solution.Length - 1; i++)
        {
            var minCost = int.MaxValue;

            // nextStop is expected to always be in range 0..MatrixSize
            // if it won't be found then -1 in Solution will produce exception indicating something is wrong with data 
            var possibleNextStops = new List<int>();

            for (var j = 0; j < Solution.Length; j++)
            {
                // skip self reference and already added stops
                if (Solution[i] == j || Solution.Contains(j)) continue;

                if (Globals.Distances[Solution[i]][j] < minCost)
                {
                    minCost = Globals.Distances[Solution[i]][j];
                    possibleNextStops.Clear();
                    possibleNextStops.Add(j);
                }

                if (Globals.Distances[Solution[i]][j] == minCost)
                {
                    
                    possibleNextStops.Add(j);
                }
            }

            Solution[i + 1] = possibleNextStops[random.Next(0, possibleNextStops.Count)];
        }

        #endregion

        #region 2opt

        // for every node check its possible swaps, if swapped is better then make it current solution
        // until last run wasnt better than previous one
        bool wasImproved;
        do
        {
            wasImproved = false;
            var currentSum = ISolver.GetSum(Solution);
            for (var i = 0; i < Solution.Length; i++)
            {
                for (var j = i + 1; j < Solution.Length; j++)
                {
                    // possibly can be improved by moving sum calculation to cycles in swap function 
                    Swap(swappedSolution, Solution[i], Solution[j]);

                    var diff = ISolver.GetSum(swappedSolution) - currentSum;
                    if (diff < 0)
                    {
                        possibleSwaps.Add((Solution[i], Solution[j]), diff);
                    }
                }
            }

            if (possibleSwaps.Values.Any(diff => diff < 0))
            {
                var bestSwap = possibleSwaps.MinBy(swap => swap.Value);
                Swap(swappedSolution, bestSwap.Key.Item1, bestSwap.Key.Item2);
                Array.Copy(swappedSolution, Solution, Solution.Length);

                wasImproved = true;
                possibleSwaps.Clear();
            }
        } while (wasImproved);

        #endregion

        return;

        void Swap(IList<int> newSolution, int firstStopToSwap, int secondStopToSwap)
        {
            var indexOfFirstStopToSwap = Array.IndexOf(Solution, firstStopToSwap);
            var indexOfSecondStopToSwap = Array.IndexOf(Solution, secondStopToSwap);

            for (var i = 0; i < indexOfFirstStopToSwap; i++)
            {
                newSolution[i] = Solution[i];
            }

            newSolution[indexOfFirstStopToSwap] = secondStopToSwap;

            for (int i = indexOfFirstStopToSwap + 1, j = indexOfSecondStopToSwap - 1;
                 i < indexOfSecondStopToSwap;
                 i++, j--)
            {
                newSolution[i] = Solution[j];
            }

            newSolution[indexOfSecondStopToSwap] = firstStopToSwap;

            for (var i = indexOfSecondStopToSwap + 1; i < Solution.Length; i++)
            {
                newSolution[i] = Solution[i];
            }
        }
    }
}