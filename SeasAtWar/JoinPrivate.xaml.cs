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
    public sealed partial class JoinPrivate : Page
    {
        private CoreDispatcher dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
        public JoinPrivate()
        {
            InitializeComponent();
            Globals.socket.On("join error", async (data) =>
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                 {
                     userInput.Text = "";
                     errorText.Text = "Session ID Not Found";
                     joinGame.IsEnabled = true;
                 });
            });
            Globals.socket.On("join full", async (data) =>
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    userInput.Text = "";
                    errorText.Text = "Game Full";
                    joinGame.IsEnabled = true;
                });
            });
            Globals.socket.On("join success", async (data) =>
            {
                Globals.GameID = (long)data;
                Globals.socket.Off("join error");
                Globals.socket.Off("join full");
                Globals.socket.Off("join success");
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(ShipSelect));
                });
            });

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off("join error");
            Globals.socket.Off("join full");
            Globals.socket.Off("join success");
            Frame.Navigate(typeof(MainMenu));
        }

        private void JoinGame_Click(object sender, RoutedEventArgs e)
        {
            if (userInput.Text.Length > 0)
            {
                if (int.TryParse(userInput.Text, out int input))
                {
                    errorText.Text = "Waiting for server...";
                    Globals.socket.Emit("join private game", string.Format("{{\"playerID\": {0}, \"gameID\": {1}}}", Globals.PlayerID, input));
                    joinGame.IsEnabled = false;
                }
                else
                {
                    errorText.Text = "Not a Number";
                }
            }
            else
                errorText.Text = "No Input";
            userInput.Text = "";
        }
    }
}
