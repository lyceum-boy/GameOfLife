using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GameOfLife
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private const int minFieldSize = 10; // Минимальный размер сетки игрового поля.
        private const int maxFieldSize = 50; // Максимальный размер сетки игрового поля.
        private const double spacing = 1.0; // Расстояние между клетками на игровом поле.

        private readonly Brush ON = Brushes.LimeGreen; // Цвет живой клетки.
        private readonly Brush OFF = Brushes.Black; // Цвет мёртвой клетки.

        private readonly bool STOP = false; // Псевдоним для параметра остановки игры.

        private int fieldWidth = 30; // Ширина сетки игрового поля по умолчанию.
        private int fieldHeight = 30; // Высота сетки игрового поля по умолчанию.

        private Rectangle[,] matrix; // Представление состояния поля.
        private Rectangle[,] previousMatrix; // Предыдущее состояние поля.

        private readonly DispatcherTimer timer = new DispatcherTimer(); // Игровой таймер.

        private bool isStateSaved = false; // Флаг сохранения поля.
        private Brush[,] savedState = null; // Сохранённое состояние поля.

        private int currentGeneration = 0; // Счётчик текущего поколения.
        private const int maxGenerationsCount = 1000; // Лимит поколений.
        private int unchangedGenerationCount = 0; // Счётчик неизменных поколений.
        private const int maxUnchangedGenerations = 1; // Лимит статичных поколений.

        private int previousPopulation = -1; // Предыдущая популяция (-1 означает, что она ещё не установлена).
        private int unchangedPopulationCount = 0; // Счётчик неизменной популяции.
        private const int maxUnchangedPopulationGenerations = 100; // Лимит поколений без изменения популяции.

        private bool IsGameRunning()
        {
            return toggleButton.Content.ToString() == "Остановить";
        }

        private int CalculatePopulation()
        {
            int population = 0;

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    if (matrix[y, x].Fill == ON)
                    {
                        population++;
                    }
                }
            }

            return population;
        }

        private void UpdateSizeLabel()
        {
            sizeLabel.Content = $"{fieldWidth} x {fieldHeight}";
        }

        private void UpdateSizeButtonsState()
        {
            increaseSizeRepeatButton.IsEnabled = (fieldWidth < maxFieldSize) && !IsGameRunning();
            decreaseSizeRepeatButton.IsEnabled = (fieldWidth > minFieldSize) && !IsGameRunning();
        }

        private void UpdateGeneration()
        {
            if (currentGeneration == 0)
            {
                SaveState();
            }
            generationTextBox.Text = currentGeneration.ToString();
        }

        private void UpdatePopulation(int count)
        {
            populationTextBox.Text = count.ToString();
            gameFieldCanvas.IsEnabled = true;

            if (count == 0)
            {
                toggleButton.IsEnabled = false;
                nextRepeatButton.IsEnabled = false;
            }
            else
            {
                if (currentGeneration == maxGenerationsCount)
                {
                    gameFieldCanvas.IsEnabled = false;
                }
                else
                {
                    toggleButton.IsEnabled = true;
                    nextRepeatButton.IsEnabled = !IsGameRunning();
                }
            }
        }

        private void UpdateUI()
        {
            UpdateSizeLabel();
            UpdateGeneration();
            UpdateSizeButtonsState();
            UpdatePopulation(CalculatePopulation());
        }

        private bool IsFieldUnchanged()
        {
            if (previousMatrix == null)
                return false; // Нет данных для сравнения

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    if (previousMatrix[y, x].Fill != matrix[y, x].Fill)
                        return false; // Поле изменилось
                }
            }
            return true; // Поле осталось статичным
        }

        private void SaveFieldState()
        {
            previousMatrix = new Rectangle[fieldHeight, fieldWidth];

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    previousMatrix[y, x] = new Rectangle { Fill = matrix[y, x].Fill };
                }
            }
        }

        private void SaveState()
        {
            savedState = new Brush[fieldHeight, fieldWidth];

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    savedState[y, x] = matrix[y, x].Fill;
                }
            }
        }

        private void RestoreState()
        {
            if (savedState == null) return; // Если состояние не сохранено, выход.

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    matrix[y, x].Fill = savedState[y, x];
                }
            }

            UpdateUI();
            SaveFieldState();
        }

        private void ClearSavedState()
        {
            savedState = null;
        }

        private void BuildGameField()
        {
            // Очистка существующих клеток.
            gameFieldCanvas.Children.Clear();

            matrix = new Rectangle[fieldHeight, fieldWidth];
            previousMatrix = null;

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    Rectangle r = new Rectangle
                    {
                        Width = gameFieldCanvas.ActualWidth / fieldWidth - spacing,
                        Height = gameFieldCanvas.ActualHeight / fieldHeight - spacing,
                        Fill = OFF
                    };
                    gameFieldCanvas.Children.Add(r);

                    Canvas.SetLeft(r, x * gameFieldCanvas.ActualWidth / fieldWidth);
                    Canvas.SetTop(r, y * gameFieldCanvas.ActualHeight / fieldHeight);

                    matrix[y, x] = r;

                    r.MouseDown += cell_MouseClick;
                    r.MouseEnter += cell_MouseEnter;
                }
            }

            currentGeneration = 0;
            unchangedGenerationCount = 0;

            previousPopulation = -1;
            unchangedPopulationCount = 0;

            ClearSavedState(); // Очистка сохранённого состояния.
            UpdateUI();
        }

        private void ToggleStartStopGame(bool changeGameState = true)
        {
            // Изменение надписи на кнопке.
            bool isGameRunning = IsGameRunning();

            if (!changeGameState && !isGameRunning) return;

            toggleButton.Content = !isGameRunning ? "Остановить" : "Запустить";

            UpdateUI();

            // Обновление включенности/выключенности кнопок.

            nextRepeatButton.IsEnabled = isGameRunning;
            clearButton.IsEnabled = isGameRunning;

            restartButton.IsEnabled = isGameRunning;
            randomFieldButton.IsEnabled = isGameRunning;

            // Обновление статуса таймера.
            if (!timer.IsEnabled)
                timer.Start();
            else
                timer.Stop();
        }

        private void StopGameWithMessage(string message)
        {
            ToggleStartStopGame(STOP);
            timer.Stop();

            UpdateUI();
            toggleButton.IsEnabled = false; // Отключение кнопки "Следующее поколение".
            nextRepeatButton.IsEnabled = false; // Отключение кнопки "Очистить поле".
            MessageBox.Show(message, "Игра остановлена", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private int[,] GetNeighbours()
        {
            int[,] neighbors = new int[fieldHeight, fieldWidth];

            // Офсеты для всех 8 направлений (по окружности).
            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    int count = 0;

                    // Проверка всех 8 направлений.
                    for (int dir = 0; dir < 8; dir++)
                    {
                        int nx = x + dx[dir];
                        int ny = y + dy[dir];

                        // Проверка, что соседняя клетка находится в пределах поля.
                        if (nx >= 0 && nx < fieldWidth && ny >= 0 && ny < fieldHeight)
                        {
                            if (matrix[ny, nx].Fill == ON)
                            {
                                count++;
                            }
                        }
                    }

                    // Запись количества соседей для текущей клетки.
                    neighbors[y, x] = count;
                }
            }

            return neighbors;
        }

        private void NextGeneration()
        {
            // Сохранение начального статуса.
            if (!isStateSaved)
            {
                SaveState();
                isStateSaved = true;
            }

            currentGeneration++;

            int[,] neighbors = GetNeighbours();
            int liveCount = 0;

            for (int y = 0; y < fieldHeight; y++)
            {
                for (int x = 0; x < fieldWidth; x++)
                {
                    bool isAlive = matrix[y, x].Fill == ON;
                    int countONPixels = neighbors[y, x];

                    if (isAlive && countONPixels <= 1)
                    {
                        // Правило 1: Смерть от одиночества.
                        matrix[y, x].Fill = OFF;
                    }
                    else if (isAlive && countONPixels >= 4)
                    {
                        // Правило 2: Смерть от перенаселения.
                        matrix[y, x].Fill = OFF;
                    }
                    else if (!isAlive && countONPixels == 3)
                    {
                        // Правило 4: Клетка с тремя соседями становится заселённой.
                        matrix[y, x].Fill = ON;
                    }

                    // Подсчёт живых клеток.
                    if (matrix[y, x].Fill == ON)
                        liveCount++;
                }
            }

            UpdateUI();

            // Проверка на отсутствие клеток на игровом поле.
            if ((previousPopulation == -1 || previousPopulation == 0) && CalculatePopulation() == 0)
            {
                StopGameWithMessage("Жизнь на игровом поле завершилась — все клетки погибли.");
                return;
            }

            // Проверка на достижение лимита поколения.
            if (currentGeneration >= maxGenerationsCount)
            {
                StopGameWithMessage("Достигнуто максимальное количество поколений.");
                return;
            }

            // Проверка на неизменность популяции.
            if (CalculatePopulation() == previousPopulation)
            {
                unchangedPopulationCount++;
                if (unchangedPopulationCount >= maxUnchangedPopulationGenerations)
                {
                    previousPopulation = -1; // Предыдущая популяция (-1 означает, что она ещё не установлена).
                    unchangedPopulationCount = 0; // Счётчик неизменной популяции.
                    StopGameWithMessage("Популяция живых клеток не изменялась в течение 100 поколений.");
                    return;
                }
            }
            else
            {
                unchangedPopulationCount = 0; // Популяция изменилась, сброс счётчика.
            }

            previousPopulation = CalculatePopulation();


            // Проверка на статичность поля.
            if (IsFieldUnchanged())
            {
                unchangedGenerationCount++;
                if (unchangedGenerationCount >= maxUnchangedGenerations)
                {
                    unchangedGenerationCount = 0; // Поле изменилось, сброс счётчика.
                    SaveFieldState();

                    StopGameWithMessage("Комбинация живых клеток стала стабильной\nи больше не изменяется!");
                    return;
                }
            }
            else
            {
                unchangedGenerationCount = 0; // Поле изменилось, сброс счётчика.
            }

            SaveFieldState(); // Сохранение текущего состояния поля
        }

        private void RandomFillField()
        {
            BuildGameField();

            // Общее количество клеток.
            int totalCells = fieldHeight * fieldWidth;

            // Генератор случайных чисел.
            Random random = new Random();

            // Определение диапазона клеток для активации (от 15% до 45%).
            int minActiveCells = (int)Math.Ceiling(totalCells * 0.15);
            int maxActiveCells = (int)Math.Floor(totalCells * 0.45);

            // Случайное число активных клеток в заданном диапазоне.
            int targetActiveCells = random.Next(minActiveCells, maxActiveCells + 1);

            // Подсчёт уже активных клеток.
            int activeCells = 0;

            // Список всех клеток для случайной выборки.
            var allCells = Enumerable.Range(0, fieldHeight)
                                      .SelectMany(y => Enumerable.Range(0, fieldWidth).Select(x => (y, x)))
                                      .ToList();

            // Перемешивание списка клеток.
            allCells = allCells.OrderBy(_ => random.Next()).ToList();

            // Активация клеток до достижения целевого количества.
            foreach (var (y, x) in allCells)
            {
                if (!(matrix[y, x].Fill == ON))
                {
                    matrix[y, x].Fill = ON;
                    activeCells++;

                    if (activeCells >= targetActiveCells)
                        break;
                }
            }

            UpdateUI();
            SaveState();
        }

        private void LoadGameField(string filePath, bool showMessages)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("Файл не найден!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string[] lines = File.ReadAllLines(filePath);

                int newHeight = lines.Length;
                int newWidth = lines[0].Length;

                bool gameFieldSizeChanged = false;

                // Проверка на квадратность поля.
                if (newHeight != newWidth)
                {
                    MessageBox.Show(
                        "Файл повреждён: поле должно быть квадратным.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                // Предварительная проверка: все строки должны быть одинаковой длины и содержать только символы '.' и '*'.
                foreach (var line in lines)
                {
                    if (line.Length != newWidth || !line.All(c => c == '.' || c == '*'))
                    {
                        MessageBox.Show(
                            "Файл повреждён: строки имеют разную длину или содержат недопустимые символы.", 
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }
                }

                if (newHeight < minFieldSize || newHeight > maxFieldSize || newWidth < minFieldSize || newWidth > maxFieldSize)
                {
                    MessageBox.Show(
                        "Размер поля в файле превосходит минимальный или максимальный допустимый размер!",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return;
                }

                if (showMessages && (fieldHeight != newHeight || fieldWidth != newWidth)) gameFieldSizeChanged = true;

                fieldHeight = newHeight;
                fieldWidth = newWidth;
                BuildGameField();

                for (int y = 0; y < fieldHeight; y++)
                {
                    for (int x = 0; x < fieldWidth; x++)
                    {
                        matrix[y, x].Fill = lines[y][x] == '*' ? ON : OFF;
                    }
                }

                UpdateUI();
                SaveState();

                if (showMessages)
                {
                    if (gameFieldSizeChanged)
                    {
                        MessageBox.Show(
                            "Размер поля изменён.\nПоле загружено успешно!",
                            "Загрузка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                    else
                    {
                        MessageBox.Show(
                            "Поле загружено успешно!",
                            "Загрузка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке файла: {ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void SaveGameField(string filePath)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    for (int y = 0; y < fieldHeight; y++)
                    {
                        for (int x = 0; x < fieldWidth; x++)
                        {
                            writer.Write(matrix[y, x].Fill == ON ? "*" : ".");
                        }
                        writer.WriteLine(); // Переход на новую строку.
                    }
                }

                MessageBox.Show(
                    "Игровое поле сохранено успешно!",
                    "Сохранение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ConfirmExit()
        {
            ToggleStartStopGame(STOP);

            MessageBoxResult result = MessageBox.Show(
                "Вы уверены, что хотите выйти?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question,
                MessageBoxResult.No // Указание, что "No" является значением по умолчанию.
            );

            // Если пользователь выбрал "Yes", возвращает true; иначе — false.
            return result == MessageBoxResult.Yes;
        }

        // Обработчики событий.

        private void cell_MouseEnter(object sender, MouseEventArgs e)
        {
            bool leftButton = e.LeftButton == MouseButtonState.Pressed;
            if (!leftButton) return;

            Rectangle pixel = (Rectangle)sender;
            pixel.Fill = pixel.Fill == ON ? OFF : ON;

            UpdateUI();
            SaveState();
            SaveFieldState();
        }

        private void cell_MouseClick(object sender, MouseEventArgs e)
        {
            ToggleStartStopGame(STOP);
            cell_MouseEnter(sender, e);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.S:
                        saveFileMenuItem_Click(null, null); // Вызов метода сохранения.
                        e.Handled = true; // Предотвращение дальнейшей обработки.
                        break;
                    case Key.O:
                        openFileMenuItem_Click(null, null); // Вызов метода загрузки.
                        e.Handled = true;
                        break;
                    case Key.Q:
                        Close();
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        Close();
                        e.Handled = true;
                        break;
                    case Key.Subtract: // Уменьшение размера поля (кнопка "-").
                    case Key.OemMinus: // Поддержка альтернативного кода клавиши "-".
                        if (decreaseSizeRepeatButton.IsEnabled)
                        {
                            decreaseSizeRepeatButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.Add: // Увеличение размера поля (кнопка "+").
                    case Key.OemPlus: // Поддержка альтернативного кода клавиши "+".
                        if (increaseSizeRepeatButton.IsEnabled)
                        {
                            increaseSizeRepeatButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.Enter: // Запуск/остановка (пробел).
                        if (toggleButton.IsEnabled)
                        {
                            toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.Space: // Запуск/остановка (пробел).
                        if (toggleButton.IsEnabled)
                        {
                            toggleButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.Left: // Уменьшение скорости (стрелка влево).
                        speedSlider.Value = Math.Max(speedSlider.Minimum, speedSlider.Value - 1);
                        e.Handled = true;
                        break;
                    case Key.Right: // Увеличение скорости (стрелка вправо).
                        speedSlider.Value = Math.Min(speedSlider.Maximum, speedSlider.Value + 1);
                        e.Handled = true;
                        break;
                    case Key.Down:
                        speedSlider.Value = Math.Max(speedSlider.Minimum, speedSlider.Value - 5);
                        break;
                    case Key.Up:
                        speedSlider.Value = Math.Min(speedSlider.Maximum, speedSlider.Value + 5);
                        break;
                    case Key.N: // Следующее поколение.
                        if (nextRepeatButton.IsEnabled)
                        {
                            nextRepeatButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.F: // Случайное заполнение поля.
                        if (randomFieldButton.IsEnabled)
                        {
                            randomFieldButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.R: // Перезапуск.
                        if (restartButton.IsEnabled)
                        {
                            restartButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                    case Key.C: // Очистка поля.
                        if (clearButton.IsEnabled)
                        {
                            clearButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                            e.Handled = true;
                        }
                        break;
                }
                
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ConfirmExit()) // Если пользователь отказался
            {
                e.Cancel = true; // Отменяем закрытие окна
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => ToggleStartStopGame(STOP);

        private void Window_StateChanged(object sender, EventArgs e) => ToggleStartStopGame(STOP);

        private void gameTimerTick(object sender, EventArgs e) => NextGeneration();

        private void DockPanel_Loaded(object sender, RoutedEventArgs e)
        {
            timer.Interval = TimeSpan.FromMilliseconds(1000 - Math.Ceiling(0.0 * 80));
            timer.Tick += gameTimerTick;
        }

        private void openFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartStopGame(STOP);

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolderPath = System.IO.Path.Combine(documentsPath, "Game of Life");

            // Проверка существования целевой папки.
            if (!Directory.Exists(defaultFolderPath))
            {
                Directory.CreateDirectory(defaultFolderPath);
            }

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Game of Life Field (*.golf)|*.golf",
                Title = "Открыть игровое поле",
                InitialDirectory = defaultFolderPath // Установка папки по умолчанию.
            };

            if (openFileDialog.ShowDialog() == true)
            {
                LoadGameField(openFileDialog.FileName, true);
            }
        }

        private void saveFileMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartStopGame(STOP);

            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            string defaultFolderPath = System.IO.Path.Combine(documentsPath, "Game of Life");

            // Проверка существования целевой папки.
            if (!Directory.Exists(defaultFolderPath))
            {
                Directory.CreateDirectory(defaultFolderPath);
            }

            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Game of Life Field (*.golf)|*.golf",
                Title = "Сохранить игровое поле",
                InitialDirectory = defaultFolderPath, // Установка папки по умолчанию.
                FileName = "game-field.golf" // Имя файла по умолчанию.
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                SaveGameField(saveFileDialog.FileName);
            }
        }

        private void closeMenuItem_Click(object sender, RoutedEventArgs e) => Close();

        private void rulesMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartStopGame(STOP);

            RulesWindow rulesWindow = new RulesWindow();

            if (rulesWindow == null || !rulesWindow.IsLoaded)
            {
                rulesWindow = new RulesWindow
                {
                    Owner = this
                };

                // Деактивация пункта меню.
                rulesMenuItem.IsEnabled = false;

                // Подписка на событие закрытия окна.
                rulesWindow.Closed += (s, args) =>
                {
                    // Повторная активация пункта меню.
                    rulesMenuItem.IsEnabled = true;
                    rulesWindow = null;
                };

                rulesWindow.Show();
            }
            else
            {
                rulesWindow.Focus(); // Установка активного окна, если окно уже открыто.
            }
        }

        private void exampleMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartStopGame(STOP);

            try
            {
                // Проверка, что sender — это MenuItem.
                if (sender is MenuItem menuItem)
                {
                    // Получение имени файла из Header пункта меню.
                    string fileName = menuItem.Header.ToString();

                    // Путь к папке /static/golf.
                    string baseDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "static", "golf");

                    // Путь к файлу.
                    string filePath = System.IO.Path.Combine(baseDirectory, $"{fileName}.golf");

                    // Проверка существования файла.
                    if (!File.Exists(filePath))
                    {
                        MessageBox.Show(
                            $"Файл {fileName}.golf не найден в директории {baseDirectory}!", 
                            "Ошибка",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        return;
                    }

                    // Загрузка игрового поля.
                    LoadGameField(filePath, false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке примера: {ex.Message}", 
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void aboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ToggleStartStopGame(STOP);

            AboutWindow aboutWindow = new AboutWindow();

            if (aboutWindow == null || !aboutWindow.IsLoaded)
            {
                aboutWindow = new AboutWindow
                {
                    Owner = this
                };

                // Деактивация пункта меню.
                aboutMenuItem.IsEnabled = false;

                // Подписка на событие закрытия окна.
                aboutWindow.Closed += (s, args) =>
                {
                    // Повторная активация пункта меню.
                    aboutMenuItem.IsEnabled = true;
                    aboutWindow = null;
                };

                aboutWindow.ShowDialog();
            }
            else
            {
                aboutWindow.Focus(); // Установка активного окна, если окно уже открыто.
            }
        }

        private void gameFieldCanvas_Loaded(object sender, RoutedEventArgs e) => BuildGameField();

        private void gameFieldCanvas_DragEnter(object sender, DragEventArgs e)
        {
            ToggleStartStopGame(STOP);

            // Проверка, что перетаскиваемые данные — это файлы.
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                // Проверка, что перетаскивается ровно один файл.
                if (files.Length == 1 && System.IO.Path.GetExtension(files[0]).Equals(".golf", StringComparison.OrdinalIgnoreCase))
                {
                    e.Effects = DragDropEffects.Copy; // Разрешение сбросить файл.
                }
                else
                {
                    e.Effects = DragDropEffects.None; // Запрет сброса, если файлов больше одного или неправильный формат.
                }
            }
            else
            {
                e.Effects = DragDropEffects.None; // Если это не файлы, то сброс не разрешён.
            }
        }

        private void gameFieldCanvas_Drop(object sender, DragEventArgs e)
        {
            ToggleStartStopGame(STOP);

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string filePath = files[0];

                // Загрузка, если файл существует и имеет расширение .golf.
                if (File.Exists(filePath) && System.IO.Path.GetExtension(filePath).Equals(".golf", StringComparison.OrdinalIgnoreCase))
                {
                    LoadGameField(filePath, true); // Загрузка игрового поля из файла.
                }
                else
                {
                    MessageBox.Show(
                        "Неверный формат файла.\nПожалуйста, выберите файл с расширение *.golf.", 
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private void toggleButton_Click(object sender, RoutedEventArgs e) => ToggleStartStopGame();

        private void nextRepeatButton_Click(object sender, RoutedEventArgs e) => NextGeneration();

        private void restartButton_Click(object sender, RoutedEventArgs e)
        {
            if (savedState != null)
            {
                RestoreState();
                currentGeneration = 0;
                UpdateUI();
            }
            else
            {
                MessageBox.Show(
                    "Сохранённое состояние отсутствует.", 
                    "Перезапуск невозможен",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        }

        private void randomFieldButton_Click(object sender, RoutedEventArgs e) => RandomFillField();

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            // Очистить сохранённые статусы.
            savedState = null;
            isStateSaved = false;

            // Перестроить игровое поле.
            BuildGameField();
        }

        private bool IsClickOnThumb(Slider slider, MouseButtonEventArgs e)
        {
            // Получение Track из Slider.
            if (slider.Template.FindName("PART_Track", slider) is Track track)
            {
                // Проверка был ли клик сделан на Thumb.
                Point clickPosition = e.GetPosition(track.Thumb);
                return clickPosition.X >= 0 && clickPosition.X <= track.Thumb.ActualWidth
                       && clickPosition.Y >= 0 && clickPosition.Y <= track.Thumb.ActualHeight;
            }

            return false; // Если Track не найден, считается, что клик не на Thumb.
        }

        private void speedSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            Slider slider = (Slider)sender;

            // Проверка, куда сделан клик.
            if (IsClickOnThumb(slider, e)) return; // Если клик на Thumb, выход.

            // Получение позиции мыши относительно Slider.
            Point clickPosition = e.GetPosition(slider);
            double relativePosition = clickPosition.X / slider.ActualWidth;

            // Рассчёт нового значение для Slider.
            double newValue = slider.Minimum + (slider.Maximum - slider.Minimum) * relativePosition;

            // Установка значения.
            slider.Value = newValue;
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var slider = sender as Slider;
            double value = slider.Value;

            timer.Interval = TimeSpan.FromMilliseconds(1000 - Math.Ceiling(value * 10));
        }

        private void decreaseSizeRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (fieldWidth > minFieldSize && fieldHeight > minFieldSize)
            {
                fieldWidth--;
                fieldHeight--;
                BuildGameField();
                UpdateUI();
            }
        }

        private void increaseSizeRepeatButton_Click(object sender, RoutedEventArgs e)
        {
            if (fieldWidth < maxFieldSize && fieldHeight < maxFieldSize)
            {
                fieldWidth++;
                fieldHeight++;
                BuildGameField();
                UpdateUI();
            }
        }
    }
}
