using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
using MSDM_IO;
using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MSDiskManager.ViewModels
{
    public class CopyMoveProcessVM : INotifyPropertyChanged
    {

        private long finishedSize = 0;
        private long skippedSize = 0;
        private long doneSize = 0;
        private long pendingSize = 0;
        private long fullSize = 0;
        private long finishedCount = 0;
        private long skippedCount = 0;
        private long doneCount = 0;
        private long pendingCount = 0;
        private long fullCount = 0;
        //private int fileCount = 0;
        private long fileErrorStrategy = 0;
        private List<DirectoryViewModel> dirs;
        private List<FileViewModel> files;
        private readonly bool move;
        private static PauseTokenSource pauses;
        private static CancellationTokenSource cancels;
        private ExistsStrategy fileStrategy = ExistsStrategy.None;
        private ExistsStrategy dirStrategy = ExistsStrategy.None;


        public long FinishedSize
        {
            get => finishedSize; set
            {
                finishedSize = value;
                var skipped = Interlocked.Read(ref skippedSize);
                var full = Interlocked.Read(ref fullSize);
                Interlocked.Exchange(ref doneSize, value + skipped);
                Interlocked.Exchange(ref pendingSize, full - (value + skipped));
                NotifyPropertyChanged("FinishedSize");
                NotifyPropertyChanged("DoneSize");
                NotifyPropertyChanged("PendingSize");
            }
        }
        public long SkippedSize
        {
            get => skippedSize; set
            {
                skippedSize = value;
                var full = Interlocked.Read(ref fullSize);
                var finshedd = Interlocked.Read(ref finishedSize);
                Interlocked.Exchange(ref doneSize, value + finishedSize);
                Interlocked.Exchange(ref pendingSize, (fullSize - (value + finishedSize)));
                NotifyPropertyChanged("FinishedSize");
                NotifyPropertyChanged("DoneSize");
                NotifyPropertyChanged("PendingSize");
            }
        }
        public long DoneSize
        {
            get => doneSize; set
            {
                if (doneSize == value) return; doneSize = value; NotifyPropertyChanged("DoneSize");

                if (value == fullSize)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                }
            }
        }
        public long PendingSize { get => pendingSize; set { pendingSize = value; NotifyPropertyChanged("PendingSize"); } }
        public long FullSize { get => fullSize; set { fullSize = value; NotifyPropertyChanged("FullSize"); } }
        public long FinishedCount
        {
            get => finishedCount; set
            {
                finishedCount = value;
                DoneCount = value + skippedCount;
                PendingCount = fullCount - (value + skippedCount);
                NotifyPropertyChanged("FinishedCount");
            }
        }
        public long SkippedCount
        {
            get => skippedCount; set
            {
                skippedCount = value;
                DoneCount = value + finishedCount;
                PendingCount = fullCount - (value + finishedCount);
                NotifyPropertyChanged("SkippedCount");
            }
        }
        public long DoneCount
        {
            get => doneCount; set
            {
                if (doneCount == value) return; doneCount = value; NotifyPropertyChanged("DoneCount");
                if (value == fullCount)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                }
            }
        }
        public long PendingCount { get => pendingCount; set { pendingCount = value; NotifyPropertyChanged("PendingCount"); } }
        public long FullCount { get => fullCount; set { fullCount = value; NotifyPropertyChanged("FullCount"); } }


        private ConcurrentBag<BaseEntityViewModel> failedItems = new ConcurrentBag<BaseEntityViewModel>();
        private ConcurrentBag<BaseEntityViewModel> succeedItems = new ConcurrentBag<BaseEntityViewModel>();
        public ObservableCollection<BaseEntityViewModel> CurrentFails = new ObservableCollection<BaseEntityViewModel>();


        public string PRSC { get => percentage; set { percentage = value; NotifyPropertyChanged("Percentage"); } }




        private void close()
        {
            Clear();
            if (finishedSize == fullSize) CloseSuccess();
            CloseFailure();
        }


        private Action CloseFailure;
        private Action CloseSuccess;




        private async Task addDirsRec(List<DirectoryViewModel> dirs)
        {
            var res = await addDirs(dirs);
            if (cancels?.IsCancellationRequested ?? true) return;
            await pauses.Token.WaitWhilePausedAsync();

            if (res != null)
            {
                foreach (var s in res)
                {
                    await addDirsRec(s.Children);
                    await addFiles(s.Files);
                }
            }
            if (move)
            {
                foreach (var dir in dirs)
                {
                    if (Directory.GetFiles(dir.OriginalPath).ToList().Count == 0 && Directory.GetDirectories(dir.OriginalPath).ToList().Count == 0)
                    {
                        try
                        {
                            Directory.Delete(dir.OriginalPath);
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show($"Couldn't remove directory:[{dir.OriginalPath}].\nError:[{e.Message}]");
                        }
                    }
                }
            }
        }
        private async Task<List<DirectoryViewModel>> addDirs(List<DirectoryViewModel> dirs)
        {
            if (Globals.IsNullOrEmpty(dirs)) return null;
            if (cancels?.IsCancellationRequested ?? true) return null;
            var ds = new List<MSDirecotry>();
            var dRip = new DirectoryRepository();
            var ignoreAdd = new List<DirectoryViewModel>();
            foreach (var d in dirs)
            {
                var de = d.DirectoryEntity;
                var valid = false;
                var canReplace = true;
                if (cancels?.IsCancellationRequested ?? true) break;
                if (de.OldPath.ToLower().Replace("/", "\\").Replace(";", "").Replace("\\\\", "\\") == de.FullPath.ToLower().Replace("/", "\\").Replace(";", "").Replace("\\\\", "\\"))
                {
                    if (!d.IgnoreAdd)
                    {
                    ds.Add(de);

                    }

                    continue;
                }
                do
                {
                    if (cancels?.IsCancellationRequested ?? true) break;
                    if (Directory.Exists(de.FullPath))
                    {
                        var strategy = dirStrategy;
                        if (strategy == ExistsStrategy.None)
                        {
                            var diag = new ItemExistsDialog(de.FullPath, canReplace);
                            diag.ShowDialog();
                            if (diag.Cancel)
                            {
                                Cancel();
                                break;
                            }
                            strategy = diag.ExistsStrategy;
                            if (diag.ForAllFiles)
                            {
                                dirStrategy = strategy;
                                fileStrategy = strategy;
                            }
                        }
                        if (strategy == ExistsStrategy.Skip)
                        {
                            Skip(d);
                            break;
                        }
                        var result = MSDMIO.CreateDirectory(de.FullPath, strategy);
                        valid = result.success;
                        if (strategy == ExistsStrategy.Replace)
                        {
                            if (!valid) canReplace = false;
                            else await new DirectoryRepository().DeletePerPath(de.Path);
                        }
                        if (valid && result.additional != null && result.additional.Length > 0)
                        {
                            d.OnDeskName += result.additional;
                            d.Path += result.additional;
                        }
                        if (d.IgnoreAdd) ignoreAdd.Add(d);
                        else ds.Add(de);
                    }
                    else
                    {
                        var result = MSDMIO.CreateDirectory(@d.FullPath, dirStrategy);
                        if (d.IgnoreAdd) ignoreAdd.Add(d);
                        else ds.Add(de);
                        break;
                    }
                } while (!valid && Directory.Exists(@d.FullPath));

            }
            if (ds.Count == 0)
            {

                if (doneCount == fullCount)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                    close();
                }
                return new List<DirectoryViewModel>();
            }
            try
            {
                var dResult = await dRip.CreateDirectories(ds, cancels: cancels?.Token);

                if (Globals.IsNotNullNorEmpty(dResult))
                {
                    succeedItems.AddRange(dirs.Where(d => dResult.Any(s =>
                    {
                        if (s.OldPath == d.OriginalPath)
                        {
                            d.Id = s.Id;
                            d.Path = s.Path;
                            d.OnDeskName = s.OnDeskName;
                            d.ShouldRemove = true;
                            d.AncestorIds = s.AncestorIds;
                            d.Description = s.Description;
                            d.IsHidden = s.IsHidden;
                            return true;
                        }
                        return false;
                    }
                    )));

                    return succeedItems.Where(f => dResult.Any(s => s.OldPath == f.OriginalPath)).Cast<DirectoryViewModel>().ToList().With(ignoreAdd);
                }
                if (doneCount == fullCount)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                    close();
                }
                return new List<DirectoryViewModel>();
            }
            catch (Exception e)
            {
                MessageBox.Show(e?.InnerException?.Message ?? e.Message);

                return new List<DirectoryViewModel>();
            }
        }
        private async Task<bool> addFiles(List<FileViewModel> files)
        {
            var p = pauses;
            var fs = new List<MSFile>();
            var ignoreAdd = new List<FileViewModel>();
            var token = p.Token;
            if (Globals.IsNullOrEmpty(files))
            {
                return true;
            }
            if (cancels?.IsCancellationRequested ?? true) return false;
            foreach (var f in files)
            {
                var fe = f.FileEntity;
                var valid = false;
                var canReplace = true;
                if (cancels?.IsCancellationRequested ?? true) break;
                if (fe.OldPath.ToLower().Replace("/", "\\").Replace(";", "").Replace("\\\\", "\\") == fe.FullPath.ToLower().Replace("/", "\\").Replace(";", "").Replace("\\\\", "\\"))
                {
                    if (!f.IgnoreAdd)
                    {
                    fs.Add(fe);
                    }
                    var s = Interlocked.Read(ref finishedSize);
                    Interlocked.Exchange(ref finishedSize, s + f.Size);
                    FinishedSize = finishedSize;
                    FinishedCount = finishedCount + 1;
                    continue;
                }
                do
                {
                    if (cancels?.IsCancellationRequested ?? true) break;
                    if (File.Exists(fe.FullPath))
                    {
                        var strategy = fileStrategy;
                        if (strategy == ExistsStrategy.None)
                        {
                            var diag = new ItemExistsDialog(fe.FullPath, canReplace);
                            diag.ShowDialog();
                            if (diag.Cancel)
                            {
                                Cancel();
                                break;
                            }
                            strategy = diag.ExistsStrategy;
                            if (diag.ForAllFiles)
                            {
                                dirStrategy = strategy;
                                fileStrategy = strategy;
                            }
                        }
                        if (strategy == ExistsStrategy.Skip)
                        {
                            Skip(f);
                            break;
                        }
                        var iores = await MSDMIO.AddFile(f.OriginalPath, f.FullPath,
                            (res) =>
                            {
                                var s = Interlocked.Read(ref finishedSize);
                                Interlocked.Exchange(ref finishedSize, s + res);
                                FinishedSize = finishedSize;
                                PRSC = fullSize > 0 ? ((finishedSize + skippedSize) / fullSize).ToString() : "0";
                            }, strategy, cancels.Token);
                        valid = iores.success;
                        if (strategy == ExistsStrategy.Replace)
                        {
                            if (!valid) canReplace = false;
                            else await new FileRepository().DeletePerPath(fe.Path);
                        }
                        valid = iores.success;
                        if (valid && iores.additional != null && iores.additional.Length > 0)
                        {
                            f.OnDeskName += iores.additional;
                            fe = f.FileEntity;

                        }
                        if (f.IgnoreAdd) ignoreAdd.Add(f);
                        else fs.Add(fe);
                        FinishedCount = finishedCount + 1;
                    }
                    else
                    {
                        var iores = await MSDMIO.AddFile(f.OriginalPath, f.FullPath,
                            (res) =>
                            {
                                var s = Interlocked.Read(ref finishedSize);
                                Interlocked.Exchange(ref finishedSize, s + res);
                                FinishedSize = finishedSize;
                                PRSC = fullSize > 0 ? ((finishedSize + skippedSize) / fullSize).ToString() : "0";
                            }, fileStrategy, cancels.Token);
                        valid = iores.success;
                        if(valid && iores.additional != null && iores.additional.Length > 0)
                        {
                            f.OnDeskName += iores.additional;
                            fe = f.FileEntity;

                        }
                        if (f.IgnoreAdd) ignoreAdd.Add(f);
                        else fs.Add(fe);
                        FinishedCount = finishedCount + 1;
                        break;
                    }
                } while (!valid && File.Exists(f.FullPath));
            }
            var fRip = new FileRepository();
            if (fs.Count == 0)

            {
                if (doneCount == fullCount)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                    close();
                }
                return false;

            }
            try
            {
                var result = await fRip.AddFiles(fs, cancels: cancels?.Token);
                if (result != null && result.Count > 0)
                {
                    succeedItems.AddRange(dirs.Where(d => result.Any(s =>
                    {
                        if (s.OldPath == d.OriginalPath)
                        {
                            d.Id = s.Id;
                            d.Path = s.Path;
                            d.OnDeskName = s.OnDeskName;
                            d.ShouldRemove = true;
                            d.AncestorIds = s.AncestorIds;
                            d.Description = s.Description;
                            d.IsHidden = s.IsHidden;
                            return true;
                        }
                        return false;
                    }
                   )));
                    if (move)
                    {
                        foreach (var f in result)
                        {
                            var deleted = false;
                            while (!deleted)
                            {
                                try
                                {
                                    File.Delete(f.OldPath);
                                    deleted = true;
                                }
                                catch (Exception e)
                                {

                                    var mbr = MessageBox.Show($"Couldn't delete file:[{f.OldPath}].",$"Error:[{e.Message}].\nDo you want to retry?",MessageBoxButton.YesNo);
                                    if (mbr == MessageBoxResult.No) break;
                                }
                            }
                        }
                    }
                }


                if (doneCount == fullCount)
                {
                    foreach (var i in succeedItems) i.ShouldRemove = true;
                    foreach (var i in failedItems) i.ShouldRemove = false;
                    close();
                }
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e?.InnerException?.Message ?? e.Message);
                return false;
            }
        }
        public async Task Start()
        {

            long size = 0;
            long count = 0;
            foreach (var f in files) { size += f.Size; count++; }
            foreach (var d in dirs) { size += d.Size; count += d.FileCountRecursive; }
            FullSize = size;
            FullCount = count;
            if (fullCount == 0 || fullCount == doneCount)
            {
                CloseSuccess();
                return;
            }
            if (Globals.IsNotNullNorEmpty(dirs)) await addDirsRec(dirs);
            await addFiles(files);
        }
        public void Cancel()
        {
            Interlocked.Exchange(ref fileErrorStrategy, 2);
            cancels?.Cancel();
            this.close();

        }
        private bool disposed = false;
        private string percentage = "";

        public void Clear()
        {
            if (disposed) return;
            disposed = true;
            cancels?.Cancel(); if (pauses != null) pauses.IsPaused = false;
        }
        public async Task Retry(BaseEntityViewModel? item)
        {
            var selected = CurrentFails.RemoveWhere(f => f.IsSelected);
            if (item != null && !selected.Contains(item)) { selected.Add(item); try { var i = CurrentFails.First(f => f.OriginalPath == item.OriginalPath); CurrentFails.Remove(i); } catch { } }
            if (item is DirectoryViewModel) await addDirsRec(selected.Cast<DirectoryViewModel>().ToList());
            else
            {
                Interlocked.Exchange(ref fileErrorStrategy, 0);
                pauses.IsPaused = false;
            }
        }
        public async void Skip(BaseEntityViewModel item)
        {
            var selected = CurrentFails.RemoveWhere(f => f.IsSelected);
            if (item != null && !selected.Contains(item)) { selected.Add(item); try { var i = CurrentFails.First(f => f.OriginalPath == item.OriginalPath); } catch { } }
            var skipped = selected;
            SkippedSize += skipped.Sum(s => s.Size);
            SkippedCount += skipped.Count;
            foreach (var d in skipped) if (d is DirectoryViewModel) SkippedCount += ((d as DirectoryViewModel).FileCountRecursive) - 1;
            percentage = fullSize > 0 ? ((finishedSize + skippedSize) / fullSize).ToString() : "0";
            PRSC = percentage;
            if (Globals.IsNotNullNorEmpty(skipped)) failedItems.AddRange(skipped.Select(s => s));
            if (item is FileViewModel)
            {
                //var s = item.Size;
                //var fs = Interlocked.Read(ref fullSize);
                //Interlocked.Exchange(ref fullSize, fs - s);
                //FullSize = fullSize;
                Interlocked.Exchange(ref fileErrorStrategy, 1);
            }
        }

        public CopyMoveProcessVM(List<DirectoryViewModel> dirs, List<FileViewModel> files, bool move, Action closeFailure, Action closeSuccess)
        {
            this.move = move;
            CloseFailure = closeFailure;
            CloseSuccess = closeSuccess;
            cancels?.Dispose();
            pauses = new PauseTokenSource();
            cancels = new CancellationTokenSource();
            var alldirs = dirs.ToList();
            var allfiles = files.ToList();
            //allfiles.AddRange(alldirs.SelectMany(d => d.FilesRecursive));
            this.files = allfiles;
            this.dirs = alldirs;
            long size = 0;
            long count = 0;
            foreach (var f in files) { size += f.Size; count++; }
            foreach (var d in dirs) { size += d.Size; count += d.FileCountRecursive; }
            FullSize = size;
            FullCount = count;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
