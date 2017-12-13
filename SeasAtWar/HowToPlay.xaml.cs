using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HowToPlay : Page
    {
        private int index = 0;
        private BitmapImage[] intstructions = new BitmapImage[7];
        public HowToPlay()
        {
            this.InitializeComponent();
            prevInstruction.Visibility = Visibility.Collapsed;
            for (int i = 0; i < intstructions.Length; i++)
            {
                intstructions[i] = new BitmapImage(new Uri("ms-appx:/images/Instructions/instruct" + (i + 1) + ".jpg", UriKind.Absolute));
            }
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(MainMenu));
        }

        private void nextInstruction_Click(object sender, RoutedEventArgs e)
        {
            index++;
            if (index == 1)
                prevInstruction.Visibility = Visibility.Visible;
            else if (index == 6)
                nextInstruction.Visibility = Visibility.Collapsed;
            currentInstruction.Source = intstructions[index];
        }

        private void prevInstruction_Click(object sender, RoutedEventArgs e)
        {
            index--;
            if (index == 0)
                prevInstruction.Visibility = Visibility.Collapsed;
            else if (index == 5)
                nextInstruction.Visibility = Visibility.Visible;
            currentInstruction.Source = intstructions[index];
        }
    }
}
