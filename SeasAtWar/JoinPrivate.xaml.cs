using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off("join error");
            Globals.socket.Off("join full");
            Globals.socket.Off("join success");
            Frame.Navigate(typeof(MainMenu));
        }

        private void joinGame_Click(object sender, RoutedEventArgs e)
        {
            if (userInput.Text.Length > 0)
            {
                int input;
                if (int.TryParse(userInput.Text, out input))
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
