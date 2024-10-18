using System.IO;
using FastExcel;

namespace GKH
{
    public static class ExcelParser
    {
        // singletone
        private static FastExcel.FastExcel? _excelManager;

        private static FastExcel.FastExcel ExcelManager =>
            _excelManager ??= new FastExcel.FastExcel(new FileInfo(Globals.FileName));

        public static IEnumerable<string> ParseWorksheets()
        {
            return ExcelManager.Worksheets.Select(wsh => wsh.Name);
        }

        public static int[][] TryParse()
        {
            var worksheet = ExcelManager.Read(Globals.SelectedWorksheet);

            var cells = worksheet.GetCellsInRange(
                new CellRange(
                    ConvertColumnNumberToLetter(worksheet, Globals.MatrixY),
                    ConvertColumnNumberToLetter(worksheet, Globals.MatrixY + Globals.MatrixSize - 1),
                    Globals.MatrixX,
                    Globals.MatrixX + Globals.MatrixSize - 1)).ToArray();

            if (cells.Length < Globals.MatrixSize * Globals.MatrixSize)
            {
                throw new InvalidOperationException("Not enough cells to fill the distance matrix.");
            }

            var distances = new int[Globals.MatrixSize][];

            for (var i = 0; i < Globals.MatrixSize; i++)
            {
                distances[i] = new int[Globals.MatrixSize];
                for (var j = 0; j < Globals.MatrixSize; j++)
                {
                    var cellValue = cells[i * Globals.MatrixSize + j].Value;
                    if (cellValue == null)
                    {
                        throw new InvalidOperationException($"Cell value at [{i}, {j}] is null.");
                    }

                    if (!int.TryParse(cellValue.ToString(), out distances[i][j]))
                    {
                        throw new FormatException($"Cell value at [{i}, {j}] is not a valid integer.");
                    }
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