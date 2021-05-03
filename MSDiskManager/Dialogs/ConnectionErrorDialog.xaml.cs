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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for ConnectionErrorDialog.xaml
    /// </summary>
    public partial class ConnectionErrorDialog : Window
    {
        private bool busy = false;
        private Color bg = ((SolidColorBrush)Application.Current.Resources["primary"]).Color;
        public ConnectionErrorDialog()
        {
            InitializeComponent();
        }

        private void RetryConnection(object sender, RoutedEventArgs e)
        {
            if (busy) return;
            busy = true;
            RetryText.Text = " trying to connect";
            checkConnection();
        }
        private void checkConnection()
        {
            
            animateButtonColor(bg, Colors.Black, 100, false);
            Task.Factory.StartNew(() =>
            {
                var state = MSDM_DBContext.ConnectionState;
                if (state == System.Data.ConnectionState.Open)
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        this.Close();
                    });
                }
                else
                {

                    
                    Application.Current.Dispatcher.Invoke(() => { animateButtonColor(Colors.Black, Colors.Red, 300, false); RetryText.Text = "Failed To Connect"; });
                    Task.Delay(500).ContinueWith(t =>
                    {
                        Application.Current.Dispatcher.Invoke(() => { animateButtonColor(Colors.Red, bg, 300, false); RetryText.Text = "Retry"; busy = false;  });
                        
                    });


                }
            });
        }
        private void animateButtonColor(Color from, Color to, int msDuration, bool reverse = false)
        {
            ColorAnimation ca = new ColorAnimation(from, to, new Duration(TimeSpan.FromMilliseconds(msDuration)));
            ca.AutoReverse = reverse;
            Storyboard.SetTarget(ca, RetryButton);
            Storyboard.SetTargetProperty(ca, new PropertyPath("Background.Color"));

            Storyboard stb = new Storyboard();
            stb.Children.Add(ca);
            stb.Begin();
        }

        private void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { DragMove(); };
        }
    }
}
