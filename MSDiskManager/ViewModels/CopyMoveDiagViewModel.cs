using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace MSDiskManager.ViewModels
{
    public class CopyMoveDiagViewModel : INotifyPropertyChanged
    {
        private bool canGoBack = false;
        private string filter = "";
        private DirectoryEntity parent;

        public CopyMoveDiagViewModel()
        {
            _ = loadData();
        }

        public bool CanGoBack { get => canGoBack; set { canGoBack = value; NotifyPropertyChanged("CanGoBack"); } }
        public string Filter { get => filter; set { filter = value; _ = loadData(); NotifyPropertyChanged("Filter"); } }
        public DirectoryEntity Parent { get => parent; set { parent = value;
                CanGoBack = value != null;
                Filter = "";
                NotifyPropertyChanged("Parent"); } }


        public int DirectoryCount { get; set; }
        public int FileCount { get; set; }
        public string HeaderContent { get; set; }
        public string MCString { get; set; }


        private ConcurrentStack<CancellationTokenSource> cancellations = new ConcurrentStack<CancellationTokenSource>();
        private async Task loadData(string filter = null)
        {
            CancellationTokenSource source;
            cancellations.TryPop(out source);
            source?.Cancel();
            source = new CancellationTokenSource();
            cancellations.Push(source);
            var token = source.Token;
            if (token.IsCancellationRequested) return;

            Directories.Clear();
            if (Globals.IsNullOrEmpty(filter))
            {
                var dirs = await new DirectoryRepository().FilterDirectories(new DirectoryFilter { ParentId = Parent?.Id ?? -1 });
                if (token.IsCancellationRequested) return;
                Directories.AddRange(dirs, token);
                Console.WriteLine(Directories.Count);
            }
            else
            {
                var dirs = await new DirectoryRepository().FilterDirectories(new DirectoryFilter { Name = Filter, AncestorIds = Parent == null ? null : new List<long>(new long[(long)Parent.Id]) });
                if (token.IsCancellationRequested) return;
                Directories.AddRange(dirs, token);
            }
            if (!token.IsCancellationRequested)
            {
                cancellations.TryPop(out source);
            }
        }

        public ObservableCollection<DirectoryEntity> Directories { get; set; } = new ObservableCollection<DirectoryEntity>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
