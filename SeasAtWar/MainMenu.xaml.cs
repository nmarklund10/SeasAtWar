using System;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainMenu : Page
    {
        private Random random = new Random();
        public MainMenu()
        {
            InitializeComponent();
        }
        private void hostGame_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HostPrivate)); 
        }
        private void joinPrivate_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(JoinPrivate));
        }
        private void joinPublic_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(JoinPublic));
        }
        private void howToPlay_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HowToPlay));
        }
        private void exit_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Disconnect();
            CoreApplication.Exit();
        }
    }
}
