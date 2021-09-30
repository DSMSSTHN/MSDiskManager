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

namespace MSDiskManager.Dialogs
{
    /// <summary>
    /// Interaction logic for MSMessageBox.xaml
    /// </summary>
    public partial class MSMessageBox : Window
    {
        public string Message { get; set; }
        public MSMessageBox(string message)
        {
            this.Message = message;
            InitializeComponent();
        }


        public static void Show(string message)
        {
            var diag = new MSMessageBox(message);
            diag.ShowDialog();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
