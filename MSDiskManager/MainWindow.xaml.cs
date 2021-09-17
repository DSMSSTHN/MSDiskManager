using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManager.Pages.AddItems;
using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace MSDiskManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private bool dialogIsShown = false;
        public AppSettings Settings { get; set; } = null;
        public MainWindow()
        {
            InitializeComponent();
            ToolTipService.ShowDurationProperty.OverrideMetadata(
    typeof(DependencyObject), new FrameworkPropertyMetadata(Int32.MaxValue));
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
        }

        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.PreviewMouseDown += (a, b) =>
            {
                switch (b.ChangedButton)
                {
                    case MouseButton.XButton1:
                    case MouseButton.XButton2:
                        b.Handled = true;
                        break;
                }
            };
            
        }



        private void NewConnectionClicked(object sender, RoutedEventArgs e)
        {
            var diag = new StartInfo(AppSettings.StoredAppSettings);
            diag.ShowDialog();

        }


    }
}
