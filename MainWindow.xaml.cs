using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace GKH
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isRunning;
        private volatile bool _shouldStopCalculation;

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
            catch (FileNotFoundException)
            {
                Log("Файл не найден");
            }
            catch (Exception e)
            {
                Log(e.Message);
                Log(e.GetType().ToString());
            }

            SolverComboBox.ItemsSource = new List<string> { "LKH", "2opt" };
            SolverComboBox.SelectedIndex = Globals.SelectedMethod;

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
                Log("Данные успешно загружены");
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() => { LogTextBox.Text += $"{message}\n"; });
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
                    Log("Данные успешно загружены");
                }
                catch (Exception)
                {
                    Log("Проблемы с чтением файла. Убедитесь, что файл существует в формате .xslx");
                }
            }
        }

        private void MainWindow_OnClosed(object? sender, EventArgs e)
        {
            Globals.SaveState();
        }

        private void WorksheetComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.SelectedWorksheet = WorksheetComboBox.SelectedItem.ToString()!;
        }

        private void XTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            int.TryParse(XTextBox.Text, out var x);
            Globals.MatrixX = x;
        }

        private void YTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            int.TryParse(YTextBox.Text, out var y);
            Globals.MatrixY = y;
        }

        private void MatrixSizeTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            int.TryParse(MatrixSizeTextBox.Text, out var size);
            Globals.MatrixSize = size;
        }

        private void IterationsTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            int.TryParse(IterationsTextBox.Text, out var iterations);
            Globals.Iterations = iterations;
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (_isRunning)
            {
                _shouldStopCalculation = true;
                return;
            }

            var iterationsPassed = 0;
            var searchThread = new Thread(startButton =>
            {
                _shouldStopCalculation = false;
                _isRunning = true;

                Dispatcher.Invoke(() =>
                {
                    (startButton as Button)!.Content = "Остановить";
                    LogTitleTextBlock.Text = "Идет решение: ожидайте, программа отобразит его автоматически";
                });

                Log("Решение...");
                Globals.Distances = ExcelParser.TryParse();
                var solutions = new ConcurrentBag<int[]>();
                var totalStopwatch = Stopwatch.StartNew();

                switch ((Algorithms)Globals.SelectedMethod)
                {
                    case Algorithms.TwoOpt:
                    {
                        var semaphoreSlim = new SemaphoreSlim(Environment.ProcessorCount - 1);
                        var threads = new ConcurrentDictionary<Thread, int>();

                        for (var iteration = 0; iteration < Globals.Iterations; iteration++)
                        {
                            if (_shouldStopCalculation)
                            {
                                Log("Остановка");
                                iterationsPassed = iteration;
                                break;
                            }

                            semaphoreSlim.Wait();

                            var thread = new Thread(values =>
                            {
                                var (solutionsBag, semaphore, i, threadList, currentThread) =
                                    values as (ConcurrentBag<int[]>, SemaphoreSlim, int,
                                        ConcurrentDictionary<Thread, int>,
                                        Thread)? ?? (null, null, 0, null, null)!;

                                var solver = new TwoOptSolver();

                                var stopwatch = Stopwatch.StartNew();

                                solver.Solve();
                                solutionsBag.Add(solver.Solution);

                                stopwatch.Stop();
                                threadList.TryRemove(currentThread, out _);
                                semaphore.Release();
                            }) { IsBackground = true, Priority = ThreadPriority.Highest };

                            threads.TryAdd(thread, 0);
                            thread.Start(
                                new ValueTuple<ConcurrentBag<int[]>, SemaphoreSlim, int,
                                    ConcurrentDictionary<Thread, int>,
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
                        Dispatcher.Invoke(() => { SearchButton.IsEnabled = false; });

                        var solver = new LkhSolver();

                        solver.Solve();
                        solutions.Add(solver.Solution);

                        Dispatcher.Invoke(() => { SearchButton.IsEnabled = true; });

                        break;
                    }
                }


                var bestSolution = solutions.MinBy(ISolver.GetSum)!;
                totalStopwatch.Stop();

                Log($"Решение: {ISolver.PrintSolution(bestSolution)}");
                Log($"Суммы переходов: {ISolver.PrintCosts(bestSolution, ISolver.GetCosts(bestSolution))}");
                Log($"Полная сумма: {ISolver.GetSum(bestSolution)}");
                Log($"Всего затрачено: {totalStopwatch.ElapsedMilliseconds} мс");

                if (iterationsPassed != 0)
                {
                    Log($"Итераций: {iterationsPassed}");
                }

                Dispatcher.Invoke(() =>
                {
                    (startButton as Button)!.Content = "Искать";
                    LogTitleTextBlock.Text = "Лог:";
                    LogTextBox.ScrollToEnd();
                });

                _isRunning = false;
            }) { Priority = ThreadPriority.Highest };

            searchThread.Start(SearchButton);
        }

        private void SolverComboBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Globals.SelectedMethod = SolverComboBox.SelectedIndex;
        }
    }
}