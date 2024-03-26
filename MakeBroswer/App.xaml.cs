
using biometric_Login;
using biometric_Login.ViewModels;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MakeBroswer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider _serviceprovider;

        public App()
        {
            ConfigureServices();
           // LoggingConfiguration.ConfigureLogging();
        }

        private void ConfigureServices()
        {
            IServiceCollection services = new ServiceCollection();
            services.AddSingleton<LoginVM>();
            services.AddSingleton(s => new LoginWindow
            {
                DataContext = s.GetRequiredService<LoginVM>()
            });services.AddSingleton<MainWindowVM>();
            services.AddSingleton(s => new MainWindow
            {
                DataContext = s.GetRequiredService<MainWindowVM>()
            });

            services.AddSingleton<UserRegistrationVM>();
            services.AddSingleton(s => new SaveFinger
            {
                DataContext = s.GetRequiredService<UserRegistrationVM>()
            });
            

            _serviceprovider = services.BuildServiceProvider();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow = _serviceprovider.GetRequiredService<LoginWindow>();
            MainWindow.Show();
        }
    }
}
