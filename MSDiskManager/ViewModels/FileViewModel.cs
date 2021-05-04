#nullable enable
using MSDiskManager.Dialogs;
using MSDiskManager.Helpers;
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
using System.Windows.Media;

namespace MSDiskManager.ViewModels
{

    public class FileViewModel : BaseEntityViewModel
    {
        private FileType fileType = FileType.Unknown;
        private string extension = "txt";



        public FileType FileType { get => fileType; set { fileType = value; NotifyPropertyChanged("FileType"); } }
        public String Extension { get => extension; set { extension = value; NotifyPropertyChanged("Extension"); } }



        public async Task<bool> AddToDb(bool move, Action<BaseEntityViewModel?, CopyMoveEventType> callback, PauseTokenSource pauses, CancellationTokenSource cancels, bool bypass = false, bool dbOnly = false)
        {
           
            if (!bypass)
            {
                await pauses.Token.WaitWhilePausedAsync();
                if (cancels.IsCancellationRequested) return false;
            }
            FileRepository rep = new FileRepository();
            this.Path = (Parent != null ? Parent.Path + '\\' + OnDeskName : OnDeskName) + "." + extension;
            var file = FileEntity;
            try
            {
                var created = dbOnly ? await rep.AddFileToDBOnly(file) :
                    await rep.AddFile(file, OriginalPath, move: move, dontAddToDB: IgnoreAdd);
                this.Path = created.Path;
                this.Id = created.Id;
                if (callback != null) callback(this,CopyMoveEventType.Success);
                if (bypass) pauses.IsPaused = false;
                ShouldRemove = true;
                return true;
            }
            catch(IOException ioe)
            {
                pauses.IsPaused = true;
                var result = false;
                var msg = $"Directory :[{OriginalPath}]\n{ioe.Message}";
                var diag = new CopyMoveErrorDialog(msg, pauses, cancels, async ()  => result = await AddToDb(move,callback,pauses,cancels,true),(et)=>callback(this,et));
                diag.ShowDialog();
                return result;
            }
            catch (Npgsql.NpgsqlException npe)
            {
                pauses.IsPaused = true;
                var result = false;
                var msg = $"Directory :[{OriginalPath}]\n{npe.Message}";
                var diag = new CopyMoveErrorDialog(msg, pauses, cancels, async () => result = await AddToDb(move, callback, pauses, cancels, true,true), (et) => callback(this,et));
                diag.ShowDialog();
                return result;
            }
            catch (Exception e)
            {
                cancels.Cancel();
                var msg = $"Directory :[{OriginalPath}]\n{e.Message}";
                MessageBox.Show(msg);
                pauses.IsPaused = false;
                ShouldRemove = false;
                return false;
            }
        }
        public FileEntity FileEntity => new FileEntity
        {
            Name = Name,
            Description = Description,
            OnDeskName = OnDeskName,
            Extension = extension,
            FileType = FileType,
            FileTags = Tags.Select(t => new FileTag { TagId = t.Id ?? -1 }).ToList(),
            Path = Path,
            Parent = Parent?.DirectoryEntity,
            IsHidden = IsHidden,
            ParentId = Parent?.Id,
            };


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
                        return "/images/unknownFile.png".AsUri();
                    case IconType.Text:
                        return "/images/textFile.png".AsUri();
                    case IconType.Image:
                        return OriginalPath?.AsUri(true) ?? FullPath.AsUri(true);
                    case IconType.Music:
                        return "/images/musicFile.png".AsUri();
                    case IconType.Video:
                        return "/images/videoFile.png".AsUri();
                    case IconType.Compressed:
                        return "/images/compressedFile.png".AsUri();
                    case IconType.Document:
                        return "/images/documentFile.png".AsUri();
                    case IconType.Directory:
                        return "/images/directory.png".AsUri();
                }

                return "/images/unknownFile.png".AsUri();
            }
        }
        public Stretch Stretch => FileType == FileType.Image ? Stretch.UniformToFill : Stretch.Uniform;
    }
}

