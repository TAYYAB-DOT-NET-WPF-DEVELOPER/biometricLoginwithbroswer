using Back_Office.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows;
using System.Diagnostics;
using System.Windows.Forms;
using System.Windows.Controls;

namespace biometric_Login.ViewModels
{
    public class MainWindowVM: ViewModelBase
    {
        private string _urlText;

        public string UrlText
        {
            get { return _urlText; }
            set
            {
                if (_urlText != value)
                {
                    _urlText = value;
                    OnPropertyChanged(nameof(UrlText));
                }
            }
        }

        public ICommand NavigateCommand { get; private set; }

        public MainWindowVM()
        {
            NavigateCommand = new RelayCommand(Navigate);
            UrlText = "http://www.google.com";
        }

        private void Navigate(object parameter)
        {
            try
            {
                Uri uri = new Uri(UrlText.StartsWith("http://") || UrlText.StartsWith("https://") ? UrlText : "http://" + UrlText, UriKind.Absolute);

                // Use ProcessStartInfo to specify the browser and URL as arguments
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = uri.ToString(),
                    UseShellExecute = true // Ensure the URL is opened using the default browser
                };

                // Start the process
                System.Diagnostics.Process.Start(startInfo);

                // Hide the current window
                Application.Current.MainWindow.Hide();

                // Get the primary screen dimensions using SystemParameters
                double screenWidth = SystemParameters.PrimaryScreenWidth;
                double screenHeight = SystemParameters.PrimaryScreenHeight;

                // Create a new window for the browser
                Window browserWindow = new Window
                {
                   // WindowStyle = WindowStyle.None, // No window border
                    WindowState = WindowState.Maximized, // Maximize the window
                    Topmost = true, // Always on top
                    Left = 0,
                    Top = 0,
                    Width = screenWidth,
                    Height = screenHeight,
                    ShowInTaskbar = false, // Hide from taskbar
                    Content = new WebBrowser { Source = uri } // Display web content
                };

                // Show the browser window
                browserWindow.Show();
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
