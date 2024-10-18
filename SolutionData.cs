namespace GKH
{
    public class SolutionData
    {
        public string Algorithm { get; set; }
        public int[] Solution { get; set; }
        public int[] Costs { get; set; }
        public int TotalSum { get; set; }
        public int Iterations { get; set; }
        public long ElapsedMilliseconds { get; set; }
    }
}
