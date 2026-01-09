using Libs.Yaml;
using System.Linq;
using System.Windows;
using yt_dlp_gui.Models;

namespace yt_dlp_gui {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        public static string CurrentVersion = "2023.03.28";
        public static Lang Lang { get; set; } = new();
        private async void Application_Startup(object sender, StartupEventArgs e) {
            var args = e.Args.ToList();
            LoadPath();

            // Initializing dependencies
            await Libs.DependencyManager.EnsureDependencies();

            var langPath = App.Path(App.Folders.root, App.AppName + ".lang");
            Lang = Yaml.Open<Lang>(langPath);
            new Views.Main().Show();
        }
    }
}
