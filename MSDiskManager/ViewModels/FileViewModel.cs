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
using System.Windows.Media.Imaging;

namespace MSDiskManager.ViewModels
{

    public class FileViewModel : BaseEntityViewModel
    {

        private FileType fileType = FileType.Unknown;
        private string extension = "txt";
        private static MediaElement? media;
        private TextBlock? txt;
        private Image? img;
        public static CancellationTokenSource? imgCancel;
        public void StopPlaying() => media?.Stop();
        public void MouseWheelDown()
        {
            switch (FileType)
            {
                case FileType.Unknown:
                    break;
                case FileType.Text:
                    break;
                case FileType.Image:
                    if (img == null) return;
                    if (img.MaxWidth > 100)
                    {
                        img.MaxWidth -= 50;
                    }
                    break;
                case FileType.Music:
                case FileType.Video:
                    if ((media?.Volume ?? 0) != 0) media!.Volume -= 0.1;
                    break;
                case FileType.Compressed:
                    break;
                case FileType.Document:
                    break;
            }
        }
        public void MouseWheelUp()
        {
            switch (FileType)
            {

                case FileType.Unknown:
                    break;
                case FileType.Text:
                    break;
                case FileType.Image:
                    if (img == null) return;
                    if (img.MaxWidth < System.Windows.SystemParameters.FullPrimaryScreenWidth / 2)
                    {
                        img.MaxWidth += 50;
                    }
                    break;
                case FileType.Music:
                case FileType.Video:
                    if ((media?.Volume ?? 1) != 1) media!.Volume += 0.1;
                    break;
                case FileType.Compressed:
                    break;
                case FileType.Document:
                    break;
            }
        }

        public override long Size
        {
            get
            {
                if (size == 0)
                {
                    try { size = new FileInfo(File.Exists(FullPath) ? FullPath : OriginalPath).Length; } catch { return 0; };
                }
                return size;
            }
        }

        public FileType FileType { get => fileType; set { fileType = value; NotifyPropertyChanged("FileType"); } }
        public String Extension { get => extension; set { extension = value; NotifyPropertyChanged("Extension"); } }


        public FileViewModel()
        {
            if (imgCancel == null) imgCancel = new CancellationTokenSource();
        }
        public void FreeResources()
        {
            Image?.Freeze();
            Image = null;
            if (!imgCancel!.IsCancellationRequested) imgCancel?.Cancel();
            if (img != null) { img?.Source?.Freeze(); img!.Source = null; }
            if (media != null) { media.Source = null; media.Stop(); media.Close(); }
            img = null;
            txt = null;
            media = null;
        }
        public void ResetResources()
        {
            imgCancel?.Dispose();
            imgCancel = new CancellationTokenSource();
        }

        public MSFile FileEntity
        {
            get
            {
                Path = (Parent != null ? Parent.Path + '\\' + OnDeskName : OnDeskName) + "." + extension;
                return new MSFile
                {
                    Name = Name,
                    Description = Description,
                    OnDeskName = OnDeskName,
                    Extension = extension,
                    FileType = FileType,
                    FileTags = Tags.Select(t => new FileTag { TagId = t.Id ?? -1 }).ToList(),
                    Path = Path,
                    Parent = null,//(Parent?.Id ?? null) != null ? Parent?.DirectoryEntity : null,
                    IsHidden = IsHidden,
                    ParentId = Parent?.Id,
                    OldPath = OriginalPath,
                };
            }
        }

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
        public override ImageSource? Image
        {
            get
            {

                return base.Image;

            }
            set
            {
                base.Image = value;
            }
        }
        private bool isBusy = false;
        public async Task LoadImage(int tries = 0)
        {
            if (imgCancel.IsCancellationRequested) return;
            isBusy = true;
            var img = await Task.Run(async () =>
          {
              try
              {
                  if (imgCancel.IsCancellationRequested) return null;
                  ImageSource? image = null;
                  switch (fileType)
                  {
                      case FileType.Unknown:
                          image = "/images/unknownFile.png".AsUri();
                          break;
                      case FileType.Text:
                          image = "/images/textFile.png".AsUri();
                          break;
                      case FileType.Image:
                          image = Globals.IsNotNullNorEmpty(OriginalPath) ? await OriginalPath.asBitmapAsync(imgCancel) : await FullPath.asBitmapAsync(imgCancel);
                          break;
                      case FileType.Music:
                          image = "/images/musicFile.png".AsUri();
                          break;
                      case FileType.Video:
                          image = "/images/videoFile.png".AsUri();
                          break;
                      case FileType.Compressed:
                          image = "/images/compressedFile.png".AsUri();
                          break;
                      case FileType.Document:
                          image = "/images/documentFile.png".AsUri();
                          break;
                  }
                  return image;
              }

              catch (Exception)
              {
                  return null;

              }
          });
            if (img != null) Application.Current.Dispatcher.Invoke(() => Image = img);
            else if (tries < 5 && !(imgCancel?.IsCancellationRequested ?? false)) await LoadImage(tries + 1);
            isBusy = false;
        }
        public Stretch Stretch => FileType == FileType.Image ? Stretch.UniformToFill : Stretch.Uniform;

        public override double ImageWidth => FileType == FileType.Image ? (System.Windows.SystemParameters.FullPrimaryScreenWidth / 2) : 50;

        public override object? TooltipContent
        {
            get
            {
                if (content != null) return content;
                switch (FileType)
                {
                    case FileType.Unknown:
                        content = Image;
                        break;
                    case FileType.Text:
                        content = TextContent;
                        break;
                    case FileType.Image:
                        content = ImageContent;
                        break;
                    case FileType.Music:
                        content = AudioContent;
                        break;
                    case FileType.Video:
                        content = VideoContent;
                        break;
                    case FileType.Compressed:
                        content = Image;
                        break;
                    case FileType.Document:
                        content = Image;
                        break;
                }
                content = Image;
                return content;
            }
        }
        public Image? ImageContent
        {
            get
            {
                if (isBusy) return null;
                if (img != null) return img;
                if (img == null)
                {
                    img = new Image();
                    img.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth / 2;
                }

                img.Source = Globals.IsNotNullNorEmpty(OriginalPath) ? OriginalPath.AsUri(true) : FullPath.AsUri(true);
                NotifyPropertyChanged("ImageContent");
                return img;
            }
        }
        public TextBlock TextContent
        {
            get
            {
                if (txt != null) return txt;
                txt = new TextBlock();
                
                txt.TextWrapping = TextWrapping.Wrap;
                var text = File.ReadAllText(OriginalPath);
                if (text.Length > 1000)
                {
                    text = text.Substring(0, 1000) + "......";
                }
                txt.Text = text;
                txt.Background = Application.Current.Resources["Primary"] as SolidColorBrush;
                txt.Foreground = Application.Current.Resources["BrightText"] as SolidColorBrush;
                //txt.Foreground = Brushes.White;
                txt.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth / 2;
                return txt;
            }
        }

        private int wheelEvent;
        private long size;

        public override string OnDeskSize { get { if (File.Exists(FullPath) && base.OnDeskSize.Length == 0) base.OnDeskSize = new FileInfo(FullPath).Length.ByteSizeToSizeString(); return base.OnDeskSize; } set => base.OnDeskSize = value; }

        public MediaElement VideoContent
        {
            get
            {
                if (media == null)
                {
                    media = new MediaElement();

                    media.LoadedBehavior = MediaState.Manual;
                    media.UnloadedBehavior = MediaState.Manual;
                    media.Volume = 0.5;
                    media.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth / 2;
                }
                media.Source = new Uri(OriginalPath);

                media.MediaOpened += (a, b) =>
                {
                    MediaElement player = (MediaElement)a;
                    try
                    {

                        var duration = (int)player.NaturalDuration.TimeSpan.TotalMinutes;
                        this.AppInvoke(() => { player.Position = new TimeSpan(0, new Random().Next(duration), 0); });
                    }
                    catch (Exception)
                    {
                        
                        player.Position = new TimeSpan(0);
                    }
                };

                return media;
            }
        }
        public MediaElement AudioContent
        {
            get
            {
                if (media == null)
                {
                    media = new MediaElement();
                    media.LoadedBehavior = MediaState.Manual;
                    media.UnloadedBehavior = MediaState.Manual;
                    media.Volume = 0.5;
                    media.MaxWidth = System.Windows.SystemParameters.FullPrimaryScreenWidth / 2;
                }
                media.Source = new Uri(OriginalPath);


                try
                {
                    media.Position = new TimeSpan(0, 0, new Random().Next(100));
                }
                catch (Exception)
                {
                    media.Position = new TimeSpan(0);
                }



                return media;
            }
        }
    }
}

