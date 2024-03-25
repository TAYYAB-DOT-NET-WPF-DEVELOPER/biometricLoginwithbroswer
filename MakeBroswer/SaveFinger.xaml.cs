using biometric_Login.ViewModels;
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
    /// Interaction logic for SaveFinger.xaml
    /// </summary>
    public partial class SaveFinger : Window
    {
        //public SaveFinger()
        //{
        //    InitializeComponent();
        //}
        private UserRegistrationVM viewModel;

        public SaveFinger()
        {
            InitializeComponent();
            viewModel = new UserRegistrationVM();
            Loaded += UserRegistrationView_Loaded;
        }

        private void UserRegistrationView_Loaded(object sender, RoutedEventArgs e)
        {
            viewModel.frmDBEnrollment_Load(sender, e);
        }
    }
}
