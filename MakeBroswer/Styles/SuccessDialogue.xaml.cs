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

namespace Back_Office.Styles
{
    /// <summary>
    /// Interaction logic for SuccessDialogue.xaml
    /// </summary>
    public partial class SuccessDialogue : Window
    {
        public SuccessDialogue()
        {
            InitializeComponent();
        }
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            Close(); // Close the error dialog window.
        }
    }
}
