﻿using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            Globals.socket.On(Globals.PlayerID + " join success", async (data) =>
            {
                Globals.socket.Off(Globals.PlayerID + " gameID created");
                Globals.socket.Off(Globals.PlayerID + " join success");
                await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    Frame.Navigate(typeof(ShipSelect));
                });
            });
            Globals.socket.On("private game created", async (data) =>
            {
                Globals.GameID = (long)data;
                await dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    sessionTextBlock.Text = "Your Private Session ID is " + Globals.GameID + "\nWaiting for other player ...";
                });
            });
            Globals.socket.Emit("new private game", Globals.PlayerID);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off(Globals.PlayerID + " join success");
            Globals.socket.Off("private game created");
            if (Globals.GameID > -1)
                Globals.socket.Emit("delete game", Globals.GameID);
            Globals.GameID = -1;
            Frame.Navigate(typeof(MainMenu));
        }
    }
}
