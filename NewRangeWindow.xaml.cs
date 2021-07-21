using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace IWEngineFontCreator
{
    /// <summary>
    /// NewRangeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class NewRangeWindow : Window
    {
        readonly CharacterRange[] Ranges =
        {
            CharacterRange.BasicLatin,
            CharacterRange.Latin1Supplement,
            CharacterRange.LatinExtendedA,
            CharacterRange.LatinExtendedB,
            CharacterRange.Cyrillic,
            CharacterRange.CyrillicSupplement,
            CharacterRange.Hiragana ,
            CharacterRange.Katakana,
            CharacterRange.Greek ,
            CharacterRange.CjkSymbolsAndPunctuation,
            CharacterRange.CjkUnifiedIdeographs,
            CharacterRange.HangulCompatibilityJamo,
            CharacterRange.HangulSyllables,
            CharacterRange.FullWidth ,
        };

        public NewRangeWindow()
        {
            InitializeComponent();
        }

        private void OnChangePresent(object sender, SelectionChangedEventArgs e)
        {
            var r = Ranges[cPresent.SelectedIndex];

            iStart.Text = r.Start.ToString("x");
            iEnd.Text = r.End.ToString("x");
        }

        bool clickedOk = false;
        bool isShowed = false;
        public async Task<CharacterRange> NewRange()
        {
            if (!isShowed)
            {
                Show();
                isShowed = true;
            }

            Visibility = Visibility.Visible;
            clickedOk = false;

            await Task.Run(() =>
            {
                while (!clickedOk || Visibility == Visibility.Hidden)
                    Thread.Sleep(10);
            });

            if (Visibility == Visibility.Hidden)
                return null;

            Visibility = Visibility.Hidden;

            return new CharacterRange(int.Parse(iStart.Text, NumberStyles.HexNumber), int.Parse(iEnd.Text, NumberStyles.HexNumber));
        }

        private void OnOk(object sender, RoutedEventArgs e)
        {
            clickedOk = true;
        }

        bool needClose = false;

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(!needClose)
            {
                Visibility = Visibility.Hidden;
                e.Cancel = true;
            }
        }

        public void RealClose()
        {
            needClose = true;
            Close();
        }
    }
}
