
using MSDiskManagerData.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MSDiskManager.ViewModels
{
    public class TypeModel : INotifyPropertyChanged
    {

        private bool isChecked;

        public MSItemType Type { get; }
        public TypeModel(MSItemType t, bool isChecked = true)
        {
            this.Type = t;
            this.isChecked = isChecked;
        }
        public bool IsChecked { get => isChecked; set { isChecked = value; NotifyPropertyChanged("IsChecked"); } }
        public string TypeName
        {
            get
            {
                switch (Type)
                {
                    case MSItemType.All:
                        return "All";
                    case MSItemType.Directory:
                        return "Folder";
                    case MSItemType.ImageFile:
                        return "Image File";
                    case MSItemType.MusicFile:
                        return "Music File";
                    case MSItemType.TextFile:
                        return "Text File";
                    case MSItemType.UnknownFile:
                        return "Unknown File";
                    case MSItemType.VideoFile:
                        return "Video File";
                    case MSItemType.CompressedFile:
                        return "Compressed File";
                    case MSItemType.DocumentFile:
                        return "Document File";
                }
                return "All";
            }
        }
        public FileType FileType
        {
            get
            {
                switch (Type)
                {
                    case MSItemType.ImageFile:
                        return FileType.Image;
                    case MSItemType.MusicFile:
                        return FileType.Music;
                    case MSItemType.TextFile:
                        return FileType.Text;
                    case MSItemType.UnknownFile:
                        return FileType.Unknown;
                    case MSItemType.VideoFile:
                        return FileType.Video;
                    case MSItemType.CompressedFile:
                        return FileType.Compressed;
                    case MSItemType.DocumentFile:
                        return FileType.Document;
                }
                throw new ArgumentException("No Valid file type was given");
            }
        }
        public static List<MSItemType> AllTypes
        {
            get
            {
                var result = new List<MSItemType>();
                result.Add(MSItemType.All);
                result.Add(MSItemType.Directory);
                result.Add(MSItemType.ImageFile);
                result.Add(MSItemType.MusicFile);
                result.Add(MSItemType.TextFile);
                result.Add(MSItemType.UnknownFile);
                result.Add(MSItemType.VideoFile);
                result.Add(MSItemType.CompressedFile);
                result.Add(MSItemType.DocumentFile);
                return result;
            }
        }
        public static List<TypeModel> AllFilterTypes { get => AllTypes.Select(ft => new TypeModel(ft)).ToList(); }
        public static List<TypeModel> OnlyFileFilterTypes { get => AllTypes.Select(ft => new TypeModel(ft)).Where(f => f.Type != MSItemType.All && f.Type != MSItemType.Directory).ToList(); }

        public static IDictionary<MSItemType, TypeModel> AllFilterTypesDictionary
        {
            get
            {
                var result = new Dictionary<MSItemType, TypeModel>();
                var all = AllFilterTypes;
                foreach (var t in all)
                {
                    result.Add(t.Type, t);
                }
                return result;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
