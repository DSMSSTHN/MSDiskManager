using MSDiskManager.Pages.Main;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Collections.Concurrent;
using System.Threading;
using MSDiskManager.ViewModels;
using System.Diagnostics;

namespace MSDiskManager.Controls
{
    /// <summary>
    /// Interaction logic for FilesFoldersList.xaml
    /// </summary>
    public partial class FilesFoldersList : UserControl
    {
        private FilterTopViewModel filterTopViewModel;
        public ObservableCollection<BaseEntity> Items { get; set; } = new ObservableCollection<BaseEntity>();
        //private int lastFolderIndex = -1;
        private ConcurrentStack<CancellationTokenSource> _cancellationTokens = new ConcurrentStack<CancellationTokenSource>();
        //private List<FilterModel> history { get; set; } = new List<FilterModel>();
        private Stack<DirectoryEntity> history { get; set; } = new Stack<DirectoryEntity>();
        public Prop<Boolean> CanGoBack { get; set; } = new Prop<bool>(false);
        public FilesFoldersList()
        {

            this.filterTopViewModel = Application.Current.Resources["TopViewModel"] as FilterTopViewModel;
            this.filterTopViewModel.FilterModel.PropertyChanged -= applyFilter;
            this.filterTopViewModel.FilterModel.PropertyChanged += applyFilter;
            //this.history.Add(this.filterTopViewModel.FilterModel.Clone);
            filterTopViewModel.PropertyChanged += (model, args) => filterChanged((model as FilterTopViewModel).FilterModel);
            this.DataContext = this;
            InitializeComponent();
        }
        private void filterChanged(FilterModel filter)
        {

            filter.PropertyChanged -= applyFilter;
            filter.PropertyChanged += applyFilter;
            _ = filterData(filter);
        }
        void applyFilter(object f, PropertyChangedEventArgs args)
        {
            var propName = (args as PropertyChangedEventArgs).PropertyName;
            var filter = (f as FilterModel);
            if (propName == "Parent" && filter.Parent != null)
            {
                if (history.Count ==  0 || history.Peek().Id != filter.Parent.Id)
                {
                    history.Push(filter.Parent);
                }
            }
            checkCanGoBack(filter);

            _ = filterData(f as FilterModel);
        }

        private async Task filterData(FilterModel filter)
        {
            CancellationTokenSource old;
            var last = _cancellationTokens.TryPop(out old);
            if (last && old != null)
            {
                old.Cancel();
            }
            var source = new CancellationTokenSource();
            var token = source.Token;

            _cancellationTokens.Push(source);
            if (token.IsCancellationRequested) return;
            Items.Clear();
            if (filter.IncludeFolders)
            {

                var df = filter.DirectoryFilter;
                var directories = await new DirectoryRepository().FilterDirectories(df);
                if (token.IsCancellationRequested) return;
                foreach (var dir in directories)
                {
                    if (token.IsCancellationRequested) return;
                    Items.Add(dir);
                }
                if (filter.OnlyFolders)
                {
                    return;
                }
            }
            if (token.IsCancellationRequested) return;
            var ff = filter.FileFilter;
            var files = await new FileRepository().FilterFiles(ff);
            if (token.IsCancellationRequested) return;
            foreach (var file in files)
            {
                if (token.IsCancellationRequested) return;
                Items.Add(file);
            }
            _cancellationTokens.TryPop(out old);

        }



        private void EntityClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entity = button.CommandParameter as BaseEntity;
            if (entity is FileEntity)
            {
                handelFileClicked(entity as FileEntity);
            }
            else if (entity is DirectoryEntity)
            {
                handleDirectoryClicked(entity as DirectoryEntity);

            }
        }
        private void handelFileClicked(FileEntity file)
        {
            try
            {
                var pi = new ProcessStartInfo { UseShellExecute=true,FileName = file.FullPath,Verb="Open"};
                System.Diagnostics.Process.Start(pi);
            }
            catch (Exception e)
            {
                MessageBox.Show($"Error while tying to open file [{file.FullPath}].\n{e.Message}" );
            }
        }
        private void handleDirectoryClicked(DirectoryEntity directory)
        {
            filterTopViewModel.FilterModel.Parent = directory;
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            DirectoryEntity dir;
            var poped = history.TryPop(out dir);
            if (!poped) return;
            var peek = history.TryPeek(out dir);
            if (!peek) CanGoBack.Value = false;
            filterTopViewModel.FilterModel.Parent = peek? dir : null;
        }

        private void GoHome(object sender, RoutedEventArgs e)
        {
            history.Clear();
            filterTopViewModel.FilterModel.Parent = null;

        }
        private void checkCanGoBack(FilterModel filter)
        {
            if (CanGoBack.Value && (filter.Parent == null || !filter.CurrentFolderOnly)) CanGoBack.Value = false;
           else  if (!CanGoBack.Value && (filter.Parent != null && filter.CurrentFolderOnly)) CanGoBack.Value = true;
        }
        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            _ = filterData(filterTopViewModel.FilterModel);
            //filterTopViewModel.FilterModel.Name = "";
            //filterTopViewModel.FilterModel.Name = "";
        }
    }


}
