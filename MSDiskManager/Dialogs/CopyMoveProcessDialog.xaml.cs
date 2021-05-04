using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Repositories;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    /// Interaction logic for CopyMoveProcessDialog.xaml
    /// </summary>
    public partial class CopyMoveProcessDialog : Window
    {
        public Prop<int> Finished { get; } = new Prop<int>(0);
        private int allFinished = 0;
        public int FileCount { get; set; }
        private int allCount { get; set; }
        private List<DirectoryViewModel> dirs;
        private List<FileViewModel> files;
        private readonly bool move;
        private object lk = new object();
        private PauseTokenSource pauses;
        private CancellationTokenSource cancels;
        private bool result = true;
        public CopyMoveProcessDialog(List<DirectoryViewModel> allDirs, List<FileViewModel> allFiles, bool move)
        {
            this.files = allFiles;
            this.dirs = allDirs;
            var count = dirs?.Count ?? 0;
            if (dirs != null) foreach (var d in dirs) count += d.AllItemsCountRecursive;
            var filecount = files?.Count ?? 0;
            if (dirs != null) foreach (var d in dirs) filecount += d.FileCountRecursive;
            this.move = move;
            this.FileCount = filecount;
            this.allCount = (files?.Count ?? 0) + count;
            InitializeComponent();
        }



        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            this.MouseDown += delegate { DragMove(); };
            pauses = new PauseTokenSource();
            cancels = new CancellationTokenSource();
            //worker.RunWorkerAsync();
            void scp(BaseEntityViewModel entity, CopyMoveEventType eventType)
            {
                if (eventType == CopyMoveEventType.Cancel)
                {
                    this.DialogResult = false;
                    this.Close();
                    return;
                }

                lock (lk)
                {

                    if (entity is FileViewModel || eventType == CopyMoveEventType.Skip)
                    {
                        Finished.Value += 1;
                    }
                    if (eventType == CopyMoveEventType.Skip) this.result = false;
                    //if (eventType == CopyMoveEventType.Success) successCallback(entity);
                    allFinished += 1;
                    if (allFinished == allCount)
                    {
                        this.DialogResult = this.result;
                        this.Close();
                    }
                }
            };
            files.ForEach(async f => await f.AddToDb(move, scp, pauses, cancels));
            dirs.ForEach(async d => await d.AddToDb(move, scp, pauses, cancels));
        }
    }
    public enum CopyMoveEventType
    {
        Success,
        Skip,
        Cancel
    }
}
