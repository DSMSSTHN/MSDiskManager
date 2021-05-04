using Nito.AsyncEx;
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
using System.Windows.Shapes;

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for CopyMoveErrorDialog.xaml
    /// </summary>
    public partial class CopyMoveErrorDialog : Window
    {
        public string Message { get; set; }
        private PauseTokenSource pauses;
        private CancellationTokenSource cancels;
        Action retryFunc;
        Action<CopyMoveEventType> otherFunc;
        public CopyMoveErrorDialog(string message, PauseTokenSource pauses, CancellationTokenSource cancels,Action retryFunc,Action< CopyMoveEventType> otherFunc)
        {
            this.pauses = pauses;
            this.cancels = cancels;
            this.Message = message;
            this.retryFunc = retryFunc;
            this.otherFunc = otherFunc;
            InitializeComponent();
        }


        private void retry()
        {
            
            retryFunc();
            
        }
        private void skip()
        {
           
            this.otherFunc(CopyMoveEventType.Skip);
            pauses.IsPaused = false;
        }
        private void cancel()
        {
            
            cancels.Cancel();
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.otherFunc(CopyMoveEventType.Cancel);
            cancel();
            this.Close();
        }

        private void RetryClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
            retry();
        }

        private void SkipClicked(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            skip();
            this.Close();
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { DragMove(); };
        }
    }
}
