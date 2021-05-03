#nullable enable
using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MSDiskManager.ViewModels
{

    public class FileViewModel : BaseEntityViewModel
    {
        private FileType fileType = FileType.Unknown;
        private List<FileTag> fileTags = new List<FileTag>();
        private string extension = "txt";



        public FileType FileType { get => fileType; set { fileType = value; NotifyPropertyChanged("FileType"); } }
        public virtual List<FileTag> FileTags { get => fileTags; set { fileTags = value; NotifyPropertyChanged("FileTags"); } }
        public String Extension { get => extension; set { extension = value; NotifyPropertyChanged("Extension"); } }


        public override IconType IconType
        {
            get
            {
                switch (FileType)
                {
                    case FileType.Unknown:
                        return IconType.Unknown;
                    case FileType.Text:
                        return IconType.Text;
                    case FileType.Image:
                        return IconType.Image;
                    case FileType.Music:
                        return IconType.Music;
                    case FileType.Video:
                        return IconType.Video;
                    case FileType.Compressed:
                        return IconType.Compressed;
                    case FileType.Document:
                        return IconType.Document;
                }
                return IconType.Unknown;
            }
        }
        public ImageSource Image
        {
            get
            {
                switch (IconType)
                {
                    case IconType.Unknown:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Text:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Image:
                        return OriginalPath?.AsUri(true) ?? FullPath.AsUri(true);
                    case IconType.Music:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Video:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Compressed:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Document:
                        return "/images/blackImg.jpg".AsUri();
                    case IconType.Directory:
                        return "/images/directory.png".AsUri();
                }

                return "/images/unknownFile.png".AsUri();
            }
        }
    }
}
