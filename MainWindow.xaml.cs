using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Python.Runtime;

namespace GKH;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private static readonly object Lock = new();
    private int _selectedSolverIndex = 0;

    public MainWindow()
    {
        InitializeComponent();

        ParseGlobals();

        try
        {
            SelectWorksheet();

            Globals.Distances = ExcelParser.TryParse();
        }
        catch (InvalidDataException)
        {
            Log("Проблемы с чтением файла. Убедитесь, что файл существует в формате .xslx");
        }
        catch (InvalidOperationException)
        {
            Log("Проблема с чтением данных. Проверьте массив заказов");
        }
        catch (Exception e)
        {
            Log(e.Message);
            Log(e.GetType().ToString());
        }

        SolverComboBox.ItemsSource = new List<string> { "LKH", "2opt" };
        SolverComboBox.SelectedIndex = 1;

        Log("Данные успешно загружены\n");

        return;

        void ParseGlobals()
        {
            Globals.LoadState();
            FileNameLabel.Content = Globals.FileName;
            WorksheetComboBox.SelectedItem = Globals.SelectedWorksheet;
            XTextBox.Text = Globals.MatrixX.ToString();
            YTextBox.Text = Globals.MatrixY.ToString();
            MatrixSizeTextBox.Text = Globals.MatrixSize.ToString();
            IterationsTextBox.Text = Globals.Iterations.ToString();
        }
    }

    private void SelectWorksheet()
    {
        WorksheetComboBox.ItemsSource = ExcelParser.ParseWorksheets();
        var worksheetIndex = WorksheetComboBox.Items.IndexOf(Globals.SelectedWorksheet);
        if (worksheetIndex != -1)
        {
            WorksheetComboBox.SelectedIndex = worksheetIndex;
        }
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() => { LogTextBox.Text += $"{message}"; });
    }

    private void ChooseFileButton_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new();
        if (openFileDialog.ShowDialog() == true)
        {
            Globals.FileName = openFileDialog.FileName;

            FileNameLabel.Content = Globals.FileName;

            try
            {
                SelectWorksheet();
            }
            catch (Exception exception)
            {
                Log("Проблемы с чтением файла. Убедитесь, что файл существует в формате .xslx");
            }
        }
    }

    private void MainWindow_OnClosed(object? sender, EventArgs e)
    {
        Globals.SaveState();
        PythonEngine.Shutdown();
    }

    private void WorksheetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        Globals.SelectedWorksheet = WorksheetComboBox.SelectedItem.ToString();
    }

    private void XTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        int.TryParse(XTextBox.Text, out int x);
        Globals.MatrixX = x;
    }

    private void YTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        int.TryParse(YTextBox.Text, out int y);
        Globals.MatrixY = y;
    }

    private void MatrixSizeTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        int.TryParse(MatrixSizeTextBox.Text, out int size);
        Globals.MatrixSize = size;
    }

    private void IterationsTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
    {
        int.TryParse(IterationsTextBox.Text, out int iterations);
        Globals.Iterations = iterations;
    }

    private void SearchButton_OnClick(object sender, RoutedEventArgs e)
    {
        var searchThread = new Thread((startButton) =>
        {
            Dispatcher.Invoke(() =>
            {
                (startButton as Button)!.IsEnabled = false;
                LogTitleTextBlock.Text = "Идет решение: ожидайте, программа отобразит его автоматически";
            });

            Log("Решение...\n");
            Globals.Distances = ExcelParser.TryParse();
            var solutions = new ConcurrentBag<int[]>();
            var totalStopwatch = Stopwatch.StartNew();

            switch ((Algorithms)_selectedSolverIndex)
            {
                case Algorithms.TwoOpt:
                {
                    var semaphoreSlim = new SemaphoreSlim(Environment.ProcessorCount - 1);
                    var threads = new ConcurrentDictionary<Thread, int>();


                    for (var iteration = 0; iteration < Globals.Iterations; iteration++)
                    {
                        semaphoreSlim.Wait();

                        var thread = new Thread(values =>
                        {
                            var (solutionsBag, semaphore, i, threadList, currentThread) =
                                values as (ConcurrentBag<int[]>, SemaphoreSlim, int, ConcurrentDictionary<Thread, int>,
                                    Thread)? ?? (null, null, 0, null, null)!;

                            var solver = new TwoOptSolver();

                            var stopwatch = Stopwatch.StartNew();

                            solver.Solve();
                            solutionsBag.Add(solver.Solution);

                            stopwatch.Stop();
                            // Log($"{i + 1}: {stopwatch.ElapsedMilliseconds} мс\n");
                            // Console.WriteLine(i);
                            threadList.TryRemove(currentThread, out _);
                            semaphore.Release();
                        }) { IsBackground = true, Priority = ThreadPriority.Highest };

                        threads.TryAdd(thread, 0);
                        thread.Start(
                            new ValueTuple<ConcurrentBag<int[]>, SemaphoreSlim, int, ConcurrentDictionary<Thread, int>,
                                Thread>(solutions, semaphoreSlim, iteration, threads, thread));
                    }


                    while (!threads.IsEmpty)
                    {
                        Thread.Sleep(1000);
                    }

                    break;
                }
                case Algorithms.Lkh:
                {
                    var solver = new LkhSolver();

                    solver.Solve();
                    solutions.Add(solver.Solution);

                    PythonEngine.Shutdown();
                    break;
                }
            }


            var bestSolution = solutions.MinBy(ISolver.GetSum)!;
            totalStopwatch.Stop();

            Log($"Решение: {ISolver.PrintSolution(bestSolution)}\n");
            Log($"Суммы переходов: {ISolver.PrintCosts(bestSolution, ISolver.GetCosts(bestSolution))}\n");
            Log($"Полная сумма: {ISolver.GetSum(bestSolution)}\n");
            Log($"Всего затрачено: {totalStopwatch.ElapsedMilliseconds} мс\n");

            Dispatcher.Invoke(() =>
            {
                (startButton as Button)!.IsEnabled = true;
                LogTitleTextBlock.Text = "Лог:";
                LogTextBox.ScrollToEnd();
            });
        }) { Priority = ThreadPriority.Highest };

        searchThread.Start(SearchButton);
    }

    private void SolverComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedSolverIndex = SolverComboBox.SelectedIndex;
    }
}