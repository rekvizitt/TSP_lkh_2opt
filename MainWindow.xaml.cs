using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Text.Json;

namespace GKH
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool _isRunning;
        private volatile bool _shouldStopCalculation;
        private bool _dataLoaded;

        public MainWindow()
        {
            InitializeComponent();

            ParseGlobals();

            try
            {
                SelectWorksheet();
                Globals.Distances = ExcelParser.TryParse();
                _dataLoaded = true;
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

            if (!_dataLoaded)
            {
                Log("Невозможно продолжить выполнение без данных. Пожалуйста, проверьте файл и попробуйте снова.");
                return;
            }

            SolverComboBox.ItemsSource = new List<string> { "LKH", "2opt" };
            SolverComboBox.SelectedIndex = Globals.SelectedMethod;
        }
        private void ParseGlobals()
        {
            Globals.LoadState();
            FileNameLabel.Content = Globals.FileName;
            WorksheetComboBox.SelectedItem = Globals.SelectedWorksheet;
            XTextBox.Text = Globals.MatrixX.ToString();
            YTextBox.Text = Globals.MatrixY.ToString();
            MatrixSizeTextBox.Text = Globals.MatrixSize.ToString();
            IterationsTextBox.Text = Globals.Iterations.ToString();
            RepeatsTextBox.Text = Globals.Repeats.ToString();
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

        private void ClearLog()
        {
            Dispatcher.Invoke(() => { LogTextBox.Clear(); });
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
                    ParseGlobals();
                    _dataLoaded = true;
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
        private void RepeatsTextBox_OnSelectionChanged(object sender, RoutedEventArgs e)
        {
            int.TryParse(RepeatsTextBox.Text, out var repeats);
            Globals.Repeats = repeats;
        }

        private void SearchButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_dataLoaded)
            {
                Log("Данные не были загружены. Пожалуйста, проверьте файл и попробуйте снова.");
                return;
            }
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

                for (var repeat = 0; repeat < Globals.Repeats; repeat++)
                {
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
                                    })
                                    { IsBackground = true, Priority = ThreadPriority.Highest };

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
                    var solutionData = new SolutionData
                    {
                        Algorithm = ((Algorithms)Globals.SelectedMethod).ToString(),
                        Solution = bestSolution,
                        Costs = ISolver.GetCosts(bestSolution),
                        TotalSum = ISolver.GetSum(bestSolution),
                        ElapsedMilliseconds = totalStopwatch.ElapsedMilliseconds
                    };
                    Log($"Решение: {ISolver.PrintSolution(bestSolution)}");
                    Log($"Суммы переходов: {ISolver.PrintCosts(bestSolution, ISolver.GetCosts(bestSolution))}");
                    Log($"Полная сумма: {ISolver.GetSum(bestSolution)}");
                    Log($"Всего затрачено: {totalStopwatch.ElapsedMilliseconds} мс");
                    Dispatcher.Invoke(() =>
                    {
                        if (SaveToJsonCheckBox.IsChecked == true)
                        {
                            Log("Сохранение в JSON...");
                            DateTime currentTime = DateTime.Now;
                            string timestamp = currentTime.ToString("yyyy-MM-dd_HH-mm-ss");
                            string jsonFileName = $"solution_{timestamp}.json";
                            string jsonFilePath = Path.Combine(Globals.CurrentDirectoryPath, jsonFileName);
                            SaveToJson(solutionData, jsonFilePath);
                            Log($"Данные сохранены в файл: {jsonFilePath}");
                        }
                    });
                }

                

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
        private void ClearLogButton_OnClick(object sender, RoutedEventArgs e)
        {
            ClearLog();
        }
        private void SaveToJson(SolutionData solutionData, string filePath)
        {
            string jsonString = JsonSerializer.Serialize(solutionData, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonString);
        }
    }
}