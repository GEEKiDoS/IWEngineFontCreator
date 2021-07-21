using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace IWEngineFontCreator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] pixels;
        BitmapSource bitmap;
        IWEngine.Font font;
        private byte[] ttf;

        ObservableCollection<CharacterRange> ranges = new ObservableCollection<CharacterRange> 
        {
            CharacterRange.BasicLatin,
        };
        
        public MainWindow()
        {
            InitializeComponent();

            lRange.ItemsSource = ranges;
        }

        private void GenerateFont(object sender, RoutedEventArgs e)
        {
            if(ttf == null)
                SelectFont(sender, e);

            if (ttf == null)
                return;

            int width = int.Parse(iWidth.Text);
            int height = int.Parse(iHeight.Text);

            byte xos = byte.Parse(iXos.Text);
            byte yos = byte.Parse(iYos.Text);

            int size = int.Parse(iSize.Text);

            var baker = new FontBaker();
            baker.Begin(width, height, 0, xos, yos);
            baker.Add(ttf, size, ranges);

            byte[] alpha;
            (alpha, font) = baker.End(iName.Text, Encoding.GetEncoding(iEncoding.Text));

            pixels = new byte[alpha.Length * 4];

            for(int i = 0; i < alpha.Length; i++)
            {
                pixels[4 * i] = pixels[4 * i + 1] = pixels[4 * i + 2] = 255;
                pixels[4 * i + 3] = alpha[i];
            }

            var image = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
            image.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);

            bitmap = image;

            imDisplay.Source = image;

            RefreshTextPreview(null, null);
        }

        private void RefreshTextPreview(object sender, TextChangedEventArgs e)
        {
            if (font == null)
                return;

            gPreview.Children.Clear();

            int curX = 5;
            var yOff = gPreview.ActualHeight / 2 - font.PixelHeight / 2;

            var gbkEncoding = Encoding.GetEncoding(iEncoding.Text);

            foreach (var ch in iMessage.Text)
            {
                int letter = ch;

                var bytes = gbkEncoding.GetBytes(ch.ToString());

                ushort code = bytes.Length == 2 ? BitConverter.ToUInt16(bytes) : ch;

                var g = font.Glyphs.Find(gt => gt.Letter == code);

                if(g == null)
                    g = font.Glyphs.Find(gt => gt.Letter == '.');

                var pixelW = (g.S1 - g.S0) * bitmap.Width;
                var pixelH = (g.T1 - g.T0) * bitmap.Height;

                var ele = new Border
                {
                    Background = new ImageBrush
                    {
                        ImageSource = bitmap,
                        Viewbox = new Rect(g.S0, g.T0, g.S1 - g.S0, g.T1 - g.T0),
                        Stretch = Stretch.None,
                    },
                    Width = pixelW,
                    Height = pixelH,
                    Margin = new Thickness(curX + g.X0, yOff + g.Y0, 0, 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                    RenderTransform = new ScaleTransform
                    {
                        ScaleX = g.PixelWidth / pixelW,
                        ScaleY = g.PixelHeight / pixelH,
                    },
                };

                curX += g.Dx;

                gPreview.Children.Add(ele);
            }
        }

        private void SaveFont(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Portable Network Graphic (*.png)|*.png",
                Title = "Save font texture",
                FileName = iName.Text + ".png"
            };

            if (!sfd.ShowDialog().Value)
                return;

            using (var fileStream = File.OpenWrite(sfd.FileName))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((WriteableBitmap)bitmap));
                encoder.Save(fileStream);
            }

            sfd.Filter = "IW Engine font file|*";
            sfd.Title = "Save font";
            sfd.FileName = iName.Text;

            if (!sfd.ShowDialog().Value)
                return;

            using (var fileStream = File.OpenWrite(sfd.FileName))
            {
                font.Write(new BinaryWriter(fileStream, Encoding.ASCII, true));
            }
        }

        private void SelectFont(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                CheckFileExists = true,
                Filter = "Truetype Font (*.ttf)|*.ttf",
                Title = "Select font",
                Multiselect = false
            };

            if (!ofd.ShowDialog().Value)
                return;

            ttf = File.ReadAllBytes(ofd.FileName);
            bFile.Content = System.IO.Path.GetFileName(ofd.FileName);
        }

        private void TogglePreviewMode(object sender, MouseButtonEventArgs e)
        {
            imDisplay.Stretch = imDisplay.Stretch == Stretch.None ? Stretch.Uniform : Stretch.None;
        }

        NewRangeWindow newRangeWindow = new NewRangeWindow();

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            newRangeWindow.RealClose();
        }

        private async void AddRange(object sender, RoutedEventArgs e)
        {
            var result = await newRangeWindow.NewRange();

            if (result == null)
                return;

            if (ranges.Where(r => r.Size == result.Start && r.End == result.End).Count() > 0)
                return;

            ranges.Add(result);
        }

        private void RemoveRange(object sender, RoutedEventArgs e)
        {
            if(lRange.SelectedItem != null)
            {
                ranges.Remove((CharacterRange)lRange.SelectedItem);
            }
        }
    }
}
