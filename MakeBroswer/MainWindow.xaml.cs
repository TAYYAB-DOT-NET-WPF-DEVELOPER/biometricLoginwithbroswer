using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace biometric_Login
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Browser.Navigate(new Uri("http://www.google.com")); 
        }
        private void GoButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Uri uri = new Uri(UrlTextBox.Text.StartsWith("http://") || UrlTextBox.Text.StartsWith("https://")
                    ? UrlTextBox.Text
                    : "http://" + UrlTextBox.Text, UriKind.Absolute);
                Browser.Navigate(uri);
            }
            catch (UriFormatException)
            {
                MessageBox.Show("Please enter a valid URL.", "Invalid URL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
