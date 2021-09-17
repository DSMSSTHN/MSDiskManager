#nullable enable
using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Data.Repositories;
using MSDiskManagerData.Helpers;
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
using System.Windows.Controls;
using System.Windows.Media;

namespace MSDiskManager.ViewModels
{
    public class DirectoryViewModel : BaseEntityViewModel
    {

        private List<FileViewModel> files = new List<FileViewModel>();
        private List<DirectoryViewModel> children = new List<DirectoryViewModel>();
        private int itemsCount = 0;
        private bool hasRemovedItem = false;


        public virtual List<FileViewModel> Files { get => files; set { files = value; Changed("Files"); } }
        public virtual List<DirectoryViewModel> Children { get => children; set { children = value; Changed("Children"); } }
        public override IconType IconType => IconType.Directory;
        public int ItemsCount { get => itemsCount; set { itemsCount = value; Changed("ItemsCount"); } }

           public DirectoryViewModel()
        {
            IconPath = "/images/directory.png";
        }
        public void RemoveChild(BaseEntityViewModel entity)
        {

            if (entity is FileViewModel) files.Remove((FileViewModel)entity);
            else children.Remove((DirectoryViewModel)entity);
            hasRemovedItem = true;
        }

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

        public void FreeResources()
        {
            foreach (var f in files) f.FreeResources();
            foreach (var c in children) c.FreeResources();
        }
        public void ResetResources()
        {
            Files.ForEach(f => f.ResetResources());
            children.ForEach(c => c.ResetResources());
        }

        public MSDirecotry DirectoryEntity
        {
            get
            {
                Path = Parent != null ? (Parent.Path + '\\' + OnDeskName) : OnDeskName;
                var result = new MSDirecotry
                {
                    Name = Name,
                    Description = Description,
                    OnDeskName = OnDeskName,
                    DirectoryTags = Tags.Select(t => new DirectoryTag { TagId = t.Id ?? -1 }).ToList(),
                    Path = Path,
                    Parent = null,//(Parent?.Id ?? null) != null ? Parent.DirectoryEntity : null,
                    IsHidden = IsHidden,
                    ParentId = Parent?.Id,
                    OldPath = OriginalPath,
                };
                return result;
            }
        }
        public void AddTagRecursive(Tag tag)
        {
            if (!this.Tags.Contains(tag)) this.Tags.Add(tag);
            files.ForEach(f => { if (!f.Tags.Contains(tag))f.Tags.Add(tag); });
            children.ForEach(d => d.AddTagRecursive(tag));
        }
        public void AddTagsRecursive(ICollection<Tag> tags)
        {
            foreach (var t in tags) AddTagRecursive(t);
        }
        public void RemoveTagRecursive(Tag tag)
        {
            Tags.RemoveWhere(t => t.Id == tag.Id);
            files.ForEach(f => f.Tags.RemoveWhere(t => t.Id == tag.Id));
            children.ForEach(d => d.RemoveTagRecursive(tag));
        }
        public void RemoveTagsRecursive(ICollection<Tag> tags)
        {
            foreach (var t in tags) RemoveTagRecursive(t);
        }
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
        public List<DirectoryViewModel> ChildrenRecursive
        {
            get
            {
                var result = children.ToList();
                foreach (var c in children)
                {
                    result.AddRange(c.ChildrenRecursive);
                }
                return result;
            }
        }
        public void LoadOnDeskSize()
        {
            Console.WriteLine("Gettings size");
            if (!Directory.Exists(FullPath)) return;
            long sz = 0;
            new DirectoryInfo(FullPath).GetDirSize((s) => { sz += s; base.OnDeskSize = sz.ByteSizeToSizeString(); });


        }


        public override bool IsHidden { get => base.IsHidden; set { files.ForEach(f => f.IsHidden = value); children.ForEach(c => c.IsHidden = value); base.IsHidden = value; } }

        public override bool IgnoreAdd { get => base.IgnoreAdd; set { files.ForEach(f => f.IgnoreAdd = value); children.ForEach(c => c.IgnoreAdd = value); base.IgnoreAdd = value; } }

        public override ImageSource Image => "/images/directory.png".AsUri();

        public override double ImageWidth => 50;

        public override object TooltipContent
        {
            get
            {
                if (content != null) return content;
                var img = new Image();
                img.Source = Image;
                img.MaxWidth = 50;
                content = img;
                return img;
            }
        }

        public override long Size
        {
            get
            {
                long size = 0;
                foreach (var f in files) size += f.Size;
                foreach (var d in children) size += d.Size;
                return size;
            }
        }



        internal async Task LoadNumberOfItems()
        {
            NumberOfItems = await new DirectoryRepository().GetItemsCount(Id);
            Changed("NumberOfItems");
        }






        
    }
}
