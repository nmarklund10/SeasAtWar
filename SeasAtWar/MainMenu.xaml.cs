using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            Globals.Scale = Window.Current.Bounds.Width / 1920;
        }
        private void HostGame_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HostPrivate)); 
        }
        private void JoinPrivate_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(JoinPrivate));
        }
        private void JoinPublic_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(JoinPublic));
        }
        private void HowToPlay_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(HowToPlay));
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Globals.StopPing();
            Globals.socket.Disconnect();
            CoreApplication.Exit();
        }
    }
}
