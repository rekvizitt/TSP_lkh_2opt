using System.IO;
using System.Reflection;
using System.Text.Json;

namespace GKH
{
    // Used to store global state, including matrix data and file data (for convenience)
public static class Globals
{
    public static string FileName { get; internal set; }
    public static string SelectedWorksheet { get; internal set; }
    public static int MatrixSize { get; internal set; }
    public static int MatrixX { get; internal set; }
    public static int MatrixY { get; internal set; }
    public static int Iterations { get; internal set; }

    public static int[][] Distances { get; internal set; }

    public static void SaveState()
    {
        var state = new GlobalsState()
        {
            MatrixSize = Globals.MatrixSize,
            MatrixX = Globals.MatrixX,
            MatrixY = Globals.MatrixY,
            Iterations = Globals.Iterations,
            FileName = Globals.FileName,
            SelectedWorksheet = Globals.SelectedWorksheet
        };
        
        File.WriteAllText(Path.Combine(CurrentDirectoryPath, "state.json"), JsonSerializer.Serialize(state));
    }

    public static void LoadState()
    {
        string json = File.ReadAllText(Path.Combine(CurrentDirectoryPath, "state.json"));
        GlobalsState state = JsonSerializer.Deserialize<GlobalsState>(json)!;

        Globals.MatrixSize = state.MatrixSize;
        Globals.MatrixX = state.MatrixX;
        Globals.MatrixY = state.MatrixY;
        Globals.Iterations = state.Iterations;
        Globals.FileName = state.FileName;
        Globals.SelectedWorksheet = state.SelectedWorksheet;
    }
    
    internal class GlobalsState
    {
        public int MatrixSize { get; set; }
        public int MatrixX { get; set; }
        public int MatrixY { get; set; }
        public int Iterations { get; set; }
        public string FileName { get; set; }
        public string SelectedWorksheet { get; set; }
    }
    
    public static readonly string CurrentDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
}
}