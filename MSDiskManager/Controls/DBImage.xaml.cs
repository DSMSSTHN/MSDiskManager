using MSDiskManager.Helpers;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Repositories;
using Nito.AsyncEx;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using MSDiskManager.ViewModels;
using System.Drawing;

namespace MSDiskManager.Controls
{
    /// <summary>
    /// Interaction logic for DBImage.xaml
    /// </summary>
    public partial class DBImage : UserControl
    {
        public static readonly DependencyProperty ImgWidthProperty = DependencyProperty.Register("ImgWidth", typeof(double),
            typeof(DBImage), new PropertyMetadata(default(double)));
        public static readonly DependencyProperty ImgWidthImgFileProperty = DependencyProperty.Register("ImgWidthImageFile", typeof(double),
            typeof(DBImage), new PropertyMetadata(default(double)));


        public double ImgWidth { get => (double)GetValue(ImgWidthProperty); set => SetValue(ImgWidthProperty, value); }
        public double ImgWidthImgFile { get => (double)GetValue(ImgWidthImgFileProperty); set => SetValue(ImgWidthImgFileProperty, value); }

        private static ConcurrentDictionary<long, byte[]> images = new ConcurrentDictionary<long, byte[]>();
        private static ConcurrentDictionary<long, ImageSource?> sources = new ConcurrentDictionary<long, ImageSource?>();
        private static ConcurrentBag<long?> loadedDirectories = new ConcurrentBag<long?>();
        private FileViewModel file;
        private CancellationTokenSource cancels;
        private static PauseTokenSource pauses = new PauseTokenSource();
        private static int processing = 0;
        public DBImage()
        {
            InitializeComponent();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (ImgWidth > 0) ImgBorder.Width = ImgWidth;
            var dc = DataContext as BaseEntityViewModel;
            if (dc == null) return;
            if (dc is FileViewModel && ImgWidthImgFile > 0 && (dc as FileViewModel).FileType == FileType.Image)
            {
                ImgBorder.Width = ImgWidthImgFile;
            }
            byte[] thumb;
            ImageSource source = null;
            if (dc.Id != null && dc is FileViewModel && (dc as FileViewModel).FileType == FileType.Image)
            {
                if (sources.TryGetValue(dc.Id ?? 0, out source))
                {
                    IMG.Source = source;
                    return;
                }
                file = dc as FileViewModel;
                byte[] s = null;
                if (images.TryGetValue(file.Id ?? 0, out s))
                {
                    source = bytesToIS(s);
                    sources.TryAdd((long)file.Id, source);
                    IMG.Source = source;
                    return;
                }
                cancels = new CancellationTokenSource();
                await tryLoadImage();
                return;
            }
            else if (dc.Id == null && dc is FileViewModel && (dc as FileViewModel).FileType == FileType.Image)
            {
                file = dc as FileViewModel;
                source = fileToIS(dc.OriginalPath);
                IMG.Source = source;
                return;
            }
            

            ImageSource image = null;
            if (dc is FileViewModel)
            {
                var f = dc as FileViewModel;
                var fileType = f.FileType;

                switch (fileType)
                {
                    case FileType.Unknown:
                        image = "/images/unknownFile.png".AsUri();
                        break;
                    case FileType.Text:
                        image = "/images/textFile.png".AsUri();
                        break;
                    case FileType.Image:
                        image = "/images/imageFile.png".AsUri();
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
            }
            else
            {
                image = "/images/directory.png".AsUri();
            }
            image?.Freeze();
            IMG.Source = image;

        }
        private async Task tryLoadImage()
        {
            if (pauses.IsPaused)
            {
                if (cancels?.IsCancellationRequested ?? true) return;
                await Task.Delay(500);
                if (cancels?.IsCancellationRequested ?? true) return;
                await tryLoadImage();
                return;
            }
            if (cancels?.IsCancellationRequested ?? true) return;
            try
            {
                if (Interlocked.Increment(ref processing) > 5)
                {
                    pauses.IsPaused = true;
                }
                byte[] bts = null;
                if ((bts = await new FileRepository().GetThumbnail((long)file.Id)) != null)
                {
                    if (cancels?.IsCancellationRequested ?? true)
                    {
                        images.TryAdd((long)file.Id, bts);
                        return;
                    }
                    ImageSource source = null;
                    source = bytesToIS(bts);
                    sources.TryAdd((long)file.Id, source);
                    if (cancels?.IsCancellationRequested ?? true) return;
                    IMG.Source = source;
                    Interlocked.Decrement(ref processing);
                    pauses.IsPaused = false;
                    return;
                }
                Interlocked.Decrement(ref processing);
                pauses.IsPaused = false;
                await Task.Delay(500);
                if (cancels?.IsCancellationRequested ?? true) return;
                await tryLoadImage();
            }
            catch (Exception)
            {
                Interlocked.Decrement(ref processing);
                pauses.IsPaused = false;
                if (cancels?.IsCancellationRequested ?? true) return;
                await Task.Delay(500);
                if (cancels?.IsCancellationRequested ?? true) return;
                await tryLoadImage();
            }
        }
        public static async Task LoadDirectory(long? id)
        {
            //if (loadedDirectories.Contains(id))
            //{
            //    return;
            //}
            //if (images.Count > 1000)
            //{
            //    images.Clear();
            //    loadedDirectories.Clear();
            //    sources.Clear();
            //}
            //var ts = await new DirectoryRepository().LoadThumbnails(id);
            //ts?.ForEach(async t =>  images.TryAdd(t.FileId, t.Thumbnail));
            //loadedDirectories.Add(id);
        }
        private ImageSource fileToIS(string url)
        {
            using (var bitmap = new Bitmap(url))
            {
                var size = 400;
                var width = bitmap.Width;
                var height = bitmap.Height;
                //handle = bitmap.GetHbitmap();
                if (bitmap.Width > bitmap.Height)
                {
                    if (bitmap.Width > size)
                    {
                        width = size;
                        var h = height * (width / (double)bitmap.Width);
                        height = (int)Math.Round(h);
                    }
                }
                else
                {
                    if (bitmap.Height > size)
                    {
                        height = size;
                        var w = width * (height / (double)bitmap.Height);
                        width = (int)Math.Round(w);
                    }
                }
                using (var thumnailImage = bitmap.GetThumbnailImage(width, height, delegate () { return false; }, IntPtr.Zero))
                {
                    thumnailImage.Save("D:\\2.jpg");
                    ImageConverter imageConverter = new ImageConverter();
                    byte[] bytes = imageConverter.ConvertTo(thumnailImage, typeof(byte[])) as byte[];
                    return bytesToIS(bytes);
                }
            }
        }
        private ImageSource bytesToIS(byte[] bts)
        {
            if (bts == null) return null;
            using (var stream = new MemoryStream(bts))
            {
                BitmapImage image = new BitmapImage();
                stream.Position = 0;
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = stream;
                image.EndInit();
                image.Freeze();
                if (file != null && file.Id != null)
                {
                    sources.TryAdd((long)file.Id, image);
                    var count = sources.Count;

                    if (count > 400)
                    {
                        foreach (var kv in sources.Take(count - 400))
                        {
                            sources.GetValueOrDefault(kv.Key);
                        }
                    }
                }

                return image;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            cancels?.Cancel();
            cancels?.Dispose();
            cancels = null;
        }
    }
}

