using MSDM_IO;
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
    /// Interaction logic for ItemExistsDialog.xaml
    /// </summary>
    public partial class ItemExistsDialog : Window
    {
        public bool ForAllFiles { get; set; } = false;
        public bool Cancel { get; set; } = false;
        public ExistsStrategy ExistsStrategy { get; set; } = ExistsStrategy.None;
        private string filePath = "";
        private bool canReplace = true;
        public ItemExistsDialog(string filePath, bool canReplace = true)
        {
            this.canReplace = canReplace;
            this.filePath = filePath;
            this.DataContext = this;
            InitializeComponent();

        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseLeftButtonDown += delegate { DragMove(); };
            this.ContentTxtBx.Text = filePath;
            if (!this.canReplace) ReplaceButton.IsEnabled = false;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var cb = sender as CheckBox;
            ForAllFiles = cb.IsChecked ?? false;
        }

        private void CancelClicked(object sender, RoutedEventArgs e)
        {
            ExistsStrategy = ExistsStrategy.None;
            Cancel = true;
            if (this.DialogResult == null)
            {
                this.DialogResult = false;
                return;
            }
        }

        private void SkipClicked(object sender, RoutedEventArgs e)
        {
            ExistsStrategy = ExistsStrategy.Skip;
            Cancel = false;
            if (this.DialogResult == null)
            {
                this.DialogResult = true;
                return;
            }
        }

    

        private void RenameClicked(object sender, RoutedEventArgs e)
        {
            ExistsStrategy = ExistsStrategy.Rename;
            Cancel = false;
            if (this.DialogResult == null)
            {
                this.DialogResult = true;
                return;
            }
        }

        private void RelaceClicked(object sender, RoutedEventArgs e)
        {
            if (!canReplace) return;
            ExistsStrategy = ExistsStrategy.Replace;
            Cancel = false;
            if (this.DialogResult == null)
            {
                this.DialogResult = true;
                return;
            }
        }
    }
}
