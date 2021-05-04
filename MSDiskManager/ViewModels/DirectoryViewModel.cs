#nullable enable
using MSDiskManager.Dialogs;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Data.Repositories;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
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
    public class DirectoryViewModel : BaseEntityViewModel
    {

        private List<FileViewModel> files = new List<FileViewModel>();
        private List<DirectoryViewModel> children = new List<DirectoryViewModel>();
        private int itemsCount = 0;

        public virtual List<FileViewModel> Files { get => files; set { files = value; NotifyPropertyChanged("Files"); } }
        public virtual List<DirectoryViewModel> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public override IconType IconType => IconType.Directory;
        public int ItemsCount { get => itemsCount; set { itemsCount = value; NotifyPropertyChanged("ItemsCount"); } }


        public bool StartRemoving()
        {
            Files = Files.Where(f => !f.ShouldRemove).ToList();
            Children = Children.Where(c => !c.StartRemoving()).ToList();
            ShouldRemove = Files.Count == 0 && children.Count == 0 && ShouldRemove;
            return ShouldRemove;
        }
        public void ResetShouldRemove()
        {
            this.ShouldRemove = false;
            children.ForEach(c => c.ResetShouldRemove());
            Files.ForEach(f => f.ShouldRemove = false);
        }


        public async Task<bool> AddToDb(bool move,
            Action<BaseEntityViewModel?, CopyMoveEventType> callback,
            PauseTokenSource pauses, CancellationTokenSource cancels, bool bypass = false, bool dbOnly = false)
        {
            if (!bypass)
            {
                await pauses.Token.WaitWhilePausedAsync();
                if (cancels.IsCancellationRequested) return false;
            }
            DirectoryRepository dRep = new DirectoryRepository();
            this.Path = Parent != null ? Parent.Path + '\\' + OnDeskName : OnDeskName;
            var dir = DirectoryEntity;
            try
            {
                if (Id == null)
                {
                    var created = await dRep.CreateDirectory(dir, dontAddToDB: IgnoreAdd);
                    this.Path = created.Path;
                    this.Id = created.Id;
                }
                else
                {
                    var updated = await dRep.Update(dir);
                    this.Path = updated.Path;
                }
                var result = true;
                foreach (var file in files)
                {
                    result &= await file.AddToDb(move, callback, pauses, cancels);
                }
                foreach (var child in children)
                {
                    result &= await child.AddToDb(move, callback, pauses, cancels);
                }
                if (result && move)
                {
                    Directory.Delete(this.OriginalPath);
                }
                if (callback != null) callback(this, CopyMoveEventType.Success);
                if (bypass) pauses.IsPaused = false;
                ShouldRemove = true;
                return result;
            }
            catch (IOException ioe)
            {
                pauses.IsPaused = true;
                bool result = false;
                var msg = $"Directory :[{OriginalPath}]\n{ioe.Message}";
                var diag = new CopyMoveErrorDialog(msg , pauses, cancels, async () => result = await AddToDb(move, callback, pauses, cancels, true), (et) => callback(null, et));
                diag.ShowDialog();
                return result;
            }
            catch (Npgsql.NpgsqlException npe)
            {
                pauses.IsPaused = true;
                bool result = false;
                var msg = $"Directory :[{OriginalPath}]\n{npe.Message}";
                var diag = new CopyMoveErrorDialog(msg, pauses, cancels, async () => result = await AddToDb(move, callback, pauses, cancels, true, true), (et) => callback(null, et));
                diag.ShowDialog();
                return result;
            }
            catch (Exception e)
            {
                cancels.Cancel();
                var msg = $"Directory :[{OriginalPath}]\n{e.Message}";
                MessageBox.Show(msg);
                ShouldRemove = false;
                pauses.IsPaused = false;
                return false;
            }
        }
        public DirectoryEntity DirectoryEntity => new DirectoryEntity
        {
            Name = Name,
            Description = Description,
            OnDeskName = OnDeskName,
            DirectoryTags = Tags.Select(t => new DirectoryTag { TagId = t.Id ?? -1 }).ToList(),
            Path = Path,
            Parent = Parent?.DirectoryEntity,
            IsHidden = IsHidden,
            ParentId = Parent?.Id,
        };
        public int AllItemsCountRecursive
        {
            get
            {
                var count = files.Count + children.Count;
                foreach (var d in children)
                {
                    count += d.AllItemsCountRecursive;
                }
                return count;
            }
        }
        public int FileCountRecursive
        {
            get
            {
                var count = files.Count;
                foreach (var d in children)
                {
                    count += d.FileCountRecursive;
                }
                return count;
            }
        }

        public int DirectoryCountRecursive
        {
            get
            {
                var count = children.Count;
                foreach (var d in children)
                {
                    count += d.DirectoryCountRecursive;
                }
                return count;
            }
        }
        public List<string> OriginalPathesRecursive
        {
            get
            {
                var result = new List<string>();
                result.Add(OriginalPath);
                result.AddRange(Files.Select(f => f.OriginalPath));
                result.AddRange(Children.SelectMany(f => f.OriginalPathesRecursive));
                return result;
            }
        }
        public List<FileViewModel> FilesRecursive
        {
            get
            {
                var result = files.ToList();
                foreach (var c in children)
                {
                    result.AddRange(c.FilesRecursive);
                }
                return result;
            }
        }

        public override bool IsHidden { get => base.IsHidden; set { files.ForEach(f => f.IsHidden = value); children.ForEach(c => c.IsHidden = value); base.IsHidden = value; } }

        public override bool IgnoreAdd { get => base.IgnoreAdd; set { files.ForEach(f => f.IgnoreAdd = value); children.ForEach(c => c.IgnoreAdd = value); base.IgnoreAdd = value; } }
    }
}
