using System.IO;
using FastExcel;

namespace GKH
{
    public static class ExcelParser
    {
        // singletone
        private static FastExcel.FastExcel? _excelManager;

        private static FastExcel.FastExcel ExcelManager
        {
            get { return _excelManager ??= new FastExcel.FastExcel(new FileInfo(Globals.FileName)); }
        }

        public static IEnumerable<string> ParseWorksheets()
        {
            return ExcelManager.Worksheets.Select(wsh => wsh.Name);
        }

        public static int[][] TryParse()
        {
            var worksheet = ExcelManager.Read(Globals.SelectedWorksheet);

            // get all cells in a range from matrix start to matrix end = matrix start + matrix size
            // library uses column names instead of numbers so we use conversion method
            // if you wanna use column names you'll still need conversion to determine matrix end so why bother
            var cells = worksheet.GetCellsInRange(
                new CellRange(
                    ConvertColumnNumberToLetter(worksheet, Globals.MatrixY),
                    ConvertColumnNumberToLetter(worksheet, Globals.MatrixY + Globals.MatrixSize - 1),
                    Globals.MatrixX,
                    Globals.MatrixX + Globals.MatrixSize - 1)).ToArray();

            int[][] distances = new int[Globals.MatrixSize][];

            for (var i = 0; i < Globals.MatrixSize; i++)
            {
                distances[i] = new int[Globals.MatrixSize];
                for (var j = 0; j < Globals.MatrixSize; j++)
                {
                    distances[i][j] = int.Parse(cells[i * Globals.MatrixSize + j].Value.ToString()!);
                }
            }

            return distances;
        }

        // get cells in first row, get cell with given column number, get column name
        private static string ConvertColumnNumberToLetter(Worksheet worksheet, int columnNumber)
        {
            return worksheet.Rows.First().Cells.First(c => c.ColumnNumber == columnNumber).ColumnName;
        }
    }
}