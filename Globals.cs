using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GKH
{
    // Used to store global state, including matrix data and file data (for convenience)
    public static class Globals
    {
        public static readonly string CurrentDirectoryPath =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        public static string FileName { get; internal set; }
        public static string SelectedWorksheet { get; internal set; }
        public static int MatrixSize { get; internal set; }
        public static int MatrixX { get; internal set; }
        public static int MatrixY { get; internal set; }
        public static int Iterations { get; internal set; }
        public static int SelectedMethod { get; internal set; }

        public static int[][] Distances { get; internal set; }

        public static void SaveState()
        {
            var state = new GlobalsState
            {
                MatrixSize = MatrixSize,
                MatrixX = MatrixX,
                MatrixY = MatrixY,
                Iterations = Iterations,
                FileName = FileName,
                SelectedWorksheet = SelectedWorksheet,
                SelectedMethod = SelectedMethod
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            File.WriteAllText(Path.Combine(CurrentDirectoryPath, "state.json"), JsonSerializer.Serialize(state, options), Encoding.UTF8);
        }

        public static void LoadState()
        {
            var json = File.ReadAllText(Path.Combine(CurrentDirectoryPath, "state.json"), Encoding.UTF8);
            var state = JsonSerializer.Deserialize<GlobalsState>(json)!;

            MatrixSize = state.MatrixSize;
            MatrixX = state.MatrixX;
            MatrixY = state.MatrixY;
            Iterations = state.Iterations;
            FileName = state.FileName;
            SelectedWorksheet = state.SelectedWorksheet;
            SelectedMethod = state.SelectedMethod;
        }

        internal class GlobalsState
        {
            public int MatrixSize { get; set; }
            public int MatrixX { get; set; }
            public int MatrixY { get; set; }
            public int Iterations { get; set; }
            public string FileName { get; set; }
            public string SelectedWorksheet { get; set; }
            public int SelectedMethod { get; set; }
        }
    }
}