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
    public sealed partial class HostPrivate : Page
    {
        private CoreDispatcher dispatcher = Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher;
        public HostPrivate()
        {
            InitializeComponent();
            Globals.socket.On(Globals.playerID + " join success", async (data) =>
            {
                Globals.socket.Off(Globals.playerID + " gameID created");
                Globals.socket.Off(Globals.playerID + " join success");
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(ShipSelect));
                });
            });
            Globals.socket.On("private game created", async (data) =>
            {
                Globals.gameID = (long)data;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    sessionTextBlock.Text = "Your Private Session ID is " + Globals.gameID + "\nWaiting for other player ...";
                });
            });
            Globals.socket.Emit("new private game", Globals.playerID);
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off(Globals.playerID + " join success");
            Globals.socket.Off("private game created");
            if (Globals.gameID > -1)
                Globals.socket.Emit("delete game", Globals.gameID);
            Globals.gameID = -1;
            Frame.Navigate(typeof(MainMenu));
        }
    }
}
