using MSDiskManager.ViewModels;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MSDiskManager.Pages.AddItems
{
    /// <summary>
    /// Interaction logic for AddItemsPage.xaml
    /// </summary>
    public partial class AddItemsPage : Page
    {
        public AddItemsViewModel AddItemsViewModel { get; } = new AddItemsViewModel();
        public AddItemsPage()
        {
            InitializeComponent();
        }
    }
}
