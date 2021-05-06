using MSDiskManager.Dialogs;
using MSDiskManager.Pages.AddItems;
using MSDiskManagerData.Data;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            checkConnection(null, null);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += checkConnection;
            _timer.Start();
        }
       
        private void checkConnection(object state, EventArgs args)
        {
            if (dialogIsShown) return;
            var connectionState = MSDM_DBContext.ConnectionState;
            if (connectionState != System.Data.ConnectionState.Open)
            {
                var diag = new ConnectionErrorDialog();
                dialogIsShown = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    diag.ShowDialog();
                    dialogIsShown = false;
                });
            }
        }
    }
}
