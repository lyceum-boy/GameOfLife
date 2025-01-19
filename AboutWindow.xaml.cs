using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameOfLife
{
    /// <summary>
    /// Логика взаимодействия для AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();

            string productName = GetAppProductName(); // Имя продукта.
            string version = GetAppVersion(); // Версия программы.
            string buildDate = GetBuildDate(); // Дата сборки.
            string copyright = GetCopyright(); // Копирайт.

            // Установка текст в элементы интерфейса.
            productLabel.Content = $"{productName}";
            versionLabel.Content = $"{version}";
            builtOnLabel.Content += $"{buildDate}";
            copyrightLabel.Content = $"{copyright}";
        }

        private string GetAppProductName()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attributes = assembly.GetCustomAttribute<AssemblyProductAttribute>();
            return attributes?.Product ?? "Неизвестное имя продукта";
        }

        private string GetAppVersion()
        {
            // Получение сборки текущего приложения.
            var assembly = Assembly.GetExecutingAssembly();
            // Извлечение информацию о версии.
            var version = assembly.GetName().Version;
            // Преобразование версии в строку.
            return version?.ToString() ?? "Неизвестная версия";
        }

        private string GetBuildDate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var filePath = assembly.Location;
            var fileInfo = new FileInfo(filePath);
            // Указание формата "MMMM dd, yyyy, H:mm" с английской культурой.
            return fileInfo.LastWriteTime.ToString("MMMM dd yyyy, H:mm", System.Globalization.CultureInfo.InvariantCulture);
        }

        private string GetCopyright()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var attributes = assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            return attributes?.Copyright ?? "Нет данных о копирайте";
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                switch (e.Key)
                {
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
                    case Key.Enter:
                        closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                        e.Handled = true;
                        break;
                    case Key.Escape:
                        closeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // Имитация нажатия кнопки.
                        e.Handled = true;
                        break;
                }
            }
        }
    }
}
