using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class JoinPublic : Page
    {
        private CoreDispatcher dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
        public JoinPublic()
        {
            InitializeComponent();
            Globals.socket.On(Globals.PlayerID + " join success", async (data) =>
            {
                Globals.GameID = (long)data;
                Globals.socket.Off(Globals.PlayerID + " join success");
                Globals.socket.Emit("update gameID", Globals.GameID);
                await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                 {
                     Frame.Navigate(typeof(ShipSelect));
                 });
            });
            Globals.socket.Emit("join public game", Globals.PlayerID);
            publicText.Text = "Searching for other player...";
        }

        private void CancelSearch_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off(Globals.PlayerID + " join success");
            Globals.socket.Emit("remove from public queue", Globals.PlayerID);
            Frame.Navigate(typeof(MainMenu));
        }
    }
}
