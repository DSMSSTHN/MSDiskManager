#nullable enable
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class DirectoryViewModel : BaseEntityViewModel
    {
        private List<DirectoryTag> directoryTags = new List<DirectoryTag>();
        private List<FileViewModel> files = new List<FileViewModel>();
        private List<DirectoryViewModel> children = new List<DirectoryViewModel>();
        private int itemsCount = 0;

        public virtual List<DirectoryTag> DirectoryTags { get => directoryTags; set { directoryTags = value; NotifyPropertyChanged("DirectoryTags"); } }
        public virtual List<FileViewModel> Files { get => files; set { files = value; NotifyPropertyChanged("Files"); } }
        public virtual List<DirectoryViewModel> Children { get => children; set { children = value; NotifyPropertyChanged("Children"); } }
        public override IconType IconType => IconType.Directory;
        public int ItemsCount { get => itemsCount; set { itemsCount = value; NotifyPropertyChanged("ItemsCount"); } }
    }
}
