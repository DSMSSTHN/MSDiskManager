using MSDiskManager.ViewModels;
using MSDiskManagerData.Data.Entities;
using MSDiskManagerData.Data.Entities.Relations;
using MSDiskManagerData.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MSDiskManager.Helpers
{
 
    public class BooleanOppositConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
    }
    public class PathExistsVisibility_CollapseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var str = value?.ToString();
            var bol = File.Exists(str) || Directory.Exists(str);
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return bol ? Visibility.Visible : Visibility.Collapsed;
            return bol ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DirectoryOnlyVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is DirectoryEntity || value is DirectoryViewModel) ? Visibility.Visible : Visibility.Collapsed;

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BaseEntityNumberOfItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DirectoryEntity) return $"{(value as DirectoryEntity).NumberOfItemsRec} items";
            if (value is DirectoryViewModel) return $"{(value as DirectoryViewModel).NumberOfItems} items";
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class BooleanScrollVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((bool)value) ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Visible;
            return ((bool)value) ? ScrollBarVisibility.Visible : ScrollBarVisibility.Disabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((ScrollBarVisibility)value) != ScrollBarVisibility.Visible;
            return ((ScrollBarVisibility)value) == ScrollBarVisibility.Visible;
        }
    }
    public class BooleanVisibility_HiddenConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((bool)value) ? Visibility.Hidden : Visibility.Visible;
            return ((bool)value) ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((Visibility)value) != Visibility.Visible;
            return ((Visibility)value) == Visibility.Visible;
        }
    }
    public class BooleanVisibility_CollapseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((bool)value) ? Visibility.Collapsed : Visibility.Visible;
            return ((bool)value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var inverse = parameter != null && bool.Parse(parameter.ToString());
            if (inverse) return ((Visibility)value) != Visibility.Visible;
            return ((Visibility)value) == Visibility.Visible;
        }
    }
    public class BooleanOpposetConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return !((bool)value);
        }
    }
    public class EntityListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";
            var str = "";
            var seperator = parameter?.ToString() ?? ",";

            if (value is ICollection<BaseEntity>) foreach (var item in (value as ICollection<BaseEntity>)) str += item.Name + seperator;
            else if (value is ICollection<BaseEntityViewModel>) foreach (var item in (value as ICollection<BaseEntityViewModel>)) str += item.Name + seperator;
            else if (value is ICollection<Tag>) foreach (var item in (value as ICollection<Tag>)) str += item.Name + seperator;
            else if (value is ICollection<DirectoryTag>) foreach (var item in (value as ICollection<DirectoryTag>)) str += item.Tag.Name + seperator;
            return Globals.IsNullOrEmpty(str) ? "" : str.Substring(0, str.Length - 1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class DoubleDividerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter == null ? value : (((double)value) / (double.Parse(parameter.ToString())));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return parameter == null ? value : (((double)value) * (double.Parse(parameter.ToString())));
        }
    }
    public class TypeToIconSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            IconType iconType = (IconType)value;

            switch (iconType)
            {
                case IconType.Unknown:
                    return "/images/unknownFile.png".AsUri();
                case IconType.Text:
                    return "/images/textFile.png".AsUri();
                case IconType.Image:
                    return "/images/imageFile.png".AsUri();
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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
            //var str = ((BitmapImage)value).UriSource.LocalPath;
            //switch (str)
            //{
            //    case "/images/unknownFile.png":
            //        return IconType.Unknown;
            //        case "/images/textFile.png":
            //    return  IconType.Text;
            //        case "/images/imageFile.png":
            //    return  IconType.Image;
            //        case "/images/musicFile.png":
            //    return  IconType.Music;
            //        case "/images/videoFile.png":
            //    return  IconType.Video;
            //        case "/images/compressedFile.png":
            //    return  IconType.Compressed;
            //        case "/images/documentFile.png":
            //    return  IconType.Document;
            //        case "/images/directory.png":
            //    return  IconType.Directory;
            //}
            //return IconType.Unknown;
        }
    }
    public static class STRExtensions
    {
        public async static Task<ImageSource> asBitmapAsync(this string str, CancellationTokenSource imgToken = null)
        {
            if (imgToken.IsCancellationRequested) return null;
            BitmapImage BitmapSourceToBitmap(BitmapSource source)
            {
                var encoder = new PngBitmapEncoder();
                var memoryStream = new MemoryStream();
                var image = new BitmapImage();
                if (imgToken.IsCancellationRequested) return null;
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(memoryStream);
                if (imgToken.IsCancellationRequested) return null;
                image.BeginInit();
                image.StreamSource = new MemoryStream(memoryStream.ToArray());
                image.EndInit();
                memoryStream.Close();
                if (imgToken.IsCancellationRequested) return null;
                return image;
            }
            BitmapImage ScaleImage(BitmapImage original, double scale)
            {
                if (imgToken.IsCancellationRequested) return null;
                var scaledBitmapSource = new TransformedBitmap();
                scaledBitmapSource.BeginInit();
                scaledBitmapSource.Source = original;
                scaledBitmapSource.Transform = new ScaleTransform(scale, scale);
                scaledBitmapSource.EndInit();
                if (imgToken.IsCancellationRequested) return null;
                return BitmapSourceToBitmap(scaledBitmapSource);
            }
            BitmapImage CropImage(BitmapImage original, int width, int height)
            {
                if (imgToken.IsCancellationRequested) return null;
                var deltaWidth = original.PixelWidth - width;
                var deltaHeight = original.PixelHeight - height;
                var marginX = deltaWidth / 2;
                var marginY = deltaHeight / 2;
                var rectangle = new Int32Rect(marginX, marginY, width, height);
                var croppedBitmap = new CroppedBitmap(original, rectangle);
                if (imgToken.IsCancellationRequested) return null;
                return BitmapSourceToBitmap(croppedBitmap);
            }
            if (imgToken.IsCancellationRequested) return null;
            return await Task.Run(() =>
            {
                try
                {
                if (imgToken.IsCancellationRequested) return null;
                    var bi = new BitmapImage(new Uri(str));
                    var width = bi.PixelWidth;
                    var height = bi.PixelHeight;
                    var expectedHeightAtCurrentWidth = width * 4.0 / 3.0;
                    var expectedWidthAtCurrentHeight = height * 3.0 / 4.0;
                    var newWidth = Math.Min(expectedWidthAtCurrentHeight, width);
                    var newHeight = Math.Min(expectedHeightAtCurrentWidth, height);
                    var croppedImage = CropImage(bi, (int)newWidth, (int)newHeight);
                    var ratio = 100.0 / newWidth;
                    var scaledImage = ScaleImage(croppedImage, ratio);
                    scaledImage?.Freeze();
                    if (imgToken.IsCancellationRequested) return null;
                    return scaledImage;
                }
                catch(Exception)
                {
                    return null;
                }
            });
        }

        public static ImageSource AsUri(this string str, bool raw = false, bool thumbnail = false)
        {
            if (thumbnail)
            {
                BitmapImage BitmapSourceToBitmap(BitmapSource source)
                {
                    var encoder = new PngBitmapEncoder();
                    var memoryStream = new MemoryStream();
                    var image = new BitmapImage();

                    encoder.Frames.Add(BitmapFrame.Create(source));
                    encoder.Save(memoryStream);

                    image.BeginInit();
                    image.StreamSource = new MemoryStream(memoryStream.ToArray());
                    image.EndInit();
                    memoryStream.Close();
                    return image;
                }
                BitmapImage ScaleImage(BitmapImage original, double scale)
                {
                    var scaledBitmapSource = new TransformedBitmap();
                    scaledBitmapSource.BeginInit();
                    scaledBitmapSource.Source = original;
                    scaledBitmapSource.Transform = new ScaleTransform(scale, scale);
                    scaledBitmapSource.EndInit();
                    return BitmapSourceToBitmap(scaledBitmapSource);
                }
                BitmapImage CropImage(BitmapImage original, int width, int height)
                {
                    var deltaWidth = original.PixelWidth - width;
                    var deltaHeight = original.PixelHeight - height;
                    var marginX = deltaWidth / 2;
                    var marginY = deltaHeight / 2;
                    var rectangle = new Int32Rect(marginX, marginY, width, height);
                    var croppedBitmap = new CroppedBitmap(original, rectangle);
                    return BitmapSourceToBitmap(croppedBitmap);
                }
                var bi = new BitmapImage(new Uri((raw ? "" : "pack://application:,,,/MSDiskManager;component") + str));
                var width = bi.PixelWidth;
                var height = bi.PixelHeight;
                var expectedHeightAtCurrentWidth = width * 4.0 / 3.0;
                var expectedWidthAtCurrentHeight = height * 3.0 / 4.0;
                var newWidth = Math.Min(expectedWidthAtCurrentHeight, width);
                var newHeight = Math.Min(expectedHeightAtCurrentWidth, height);
                var croppedImage = CropImage(bi, (int)newWidth, (int)newHeight);
                var ratio = 100.0 / newWidth;
                var scaledImage = ScaleImage(croppedImage, ratio);
                scaledImage.Freeze();
                return scaledImage;

            }
            var imageSource = new ImageSourceConverter().ConvertFromString((raw ? "" : "pack://application:,,,/MSDiskManager;component") + str);//.ConvertFromInvariantString(str);
            return imageSource as ImageSource;
            //return new Uri("pack://application:,,," + str);
        }
    }
    public class IntToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string str = new IntToColorConverter().Convert(value, targetType, parameter, culture).ToString();
            return new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(str));

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value as System.Windows.Media.Color?)?.ToString();
        }
    }
    public class IntToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var i = (int)value;
            switch (i)
            {
                case 1:
                    return "#B71C1C";
                case 2:
                    return "#880E4F";
                case 3:
                    return "#900";
                case 4:
                    return "#900";
                case 5:
                    return "#263238";
                case 6:
                    return "#0D47A1";
                case 7:
                    return "#006064";
                case 8:
                    return "#004D40";
                case 9:
                    return "#1B5E20";
                default:
                    return "#212121";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var i = value.ToString();
            switch (i)
            {
                case "#B71C1C":
                    return 1;
                case "#880E4F":
                    return 2;
                case "#900":
                    return 3;
                //return 4;
                case "#263238":
                    return 5;
                case "#0D47A1":
                    return 6;
                case "#006064":
                    return 7;
                case "#004D40":
                    return 8;
                case "#1B5E20":
                    return 9;
                default:
                    return 0;
            }
        }
    }
}
