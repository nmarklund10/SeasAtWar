using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ShipSelect : Page
    {
        private string[] shipNames = new string[8] { "Scrambler", "Scanner", "Submarine", "Defender", "Cruiser", "Carrier", "Executioner", "Artillery" };
        private string[] shipSpecialNames = new string[8] { "Misleading Intelligence", "Tracer Rounds", "Dive! Dive!", "Sabotage!", "Counter Attack", "UAV", "Killing Blow", "Barrage" };
        private string[] shipDescriptions = new string[8];
        private double scale = Window.Current.Bounds.Width/1920;
        private ICanvasImage[] shipImages = new ICanvasImage[4];
        private ICanvasImage[] shipShadows = new ICanvasImage[4];
        private ICanvasImage[] verticalShips = new ICanvasImage[8];
        private ICanvasImage[] horizontalShips = new ICanvasImage[8];
        private Button[] shipSelectButtons;
        private Ship clickedShip;
        private Ship hoveredShip;
        private int clickedShipTile = -1;
        private CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        private object hoverLock = new object();
        private object clickedLock = new object();
        private Tuple<Point, int> moveError;
        private string instructionsText;
        private string errorText = "Must select ship type before moving it!";
        private int timerValue = 90;
        private Timer timer;

        public ShipSelect()
        {
            InitializeComponent();
            Globals.DrawReady = true;
            shipSelectButtons = new Button[8] { Scrambler, Scanner, Submarine, Defender, Cruiser, Carrier, Executioner, Artillery };
            Globals.socket.On(Globals.GameID + " player disconnect", async (data) =>
            {
                Globals.DrawReady = false;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var dialog = new ContentDialog()
                    {
                        Title = "Opponent Disconnect",
                        MaxWidth = ActualWidth, // Required for Mobile!
                        Content = "The other player has disconnected from the game session.  Press OK to return to Main Menu."
                    };

                    dialog.PrimaryButtonText = "OK";
                    dialog.IsSecondaryButtonEnabled = false;

                    var result = await dialog.ShowAsync();
                    Frame.Navigate(typeof(MainMenu));
                });
                Globals.socket.Off(Globals.GameID + " player disconnect");
                Globals.player.clearGrids();
                Globals.player.fleet = new Ship[4];
                Globals.GameID = -1;
            });
            gameBoard.PointerMoved += GameBoard_PointerMoved;
            gameBoard.PointerPressed += GameBoard_PointerPressed;
            gameBoard.PointerReleased += GameBoard_PointerReleased;
            gameBoard.DoubleTapped += GameBoard_DoubleTapped;
            InitializeShipDescriptions();
            Globals.player.loadGrid("home", new Point(adjust(40), adjust(30)), adjust(70));
            Globals.player.fleet = new Ship[4] 
            {
                new Ship("temp2", 2, new Point(5, 3)),
                new Ship("temp3", 3, new Point(4, 3)),
                new Ship("temp4", 4, new Point(3, 3)),
                new Ship("temp5", 5, new Point(2, 3))
            };
            instructionsText = shipDescriptionDetails.Text;
            timer = new Timer(timerCallback, null, 0, 1000);
        }

        private async void timerCallback(object state)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (timerValue == 20)
                    timerText.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
                timerText.Text = "Timer:  " + timerValue;
                if (timerValue == 0)
                {
                    FinishButton_Click(null, new RoutedEventArgs());
                    return;
                }

            });
            timerValue--;
        }

        private void InitializeShipDescriptions()
        {
            shipDescriptions[0] = "• For the next 3 turns, the enemy will not know whether they got a hit or miss.\n• Disables Enemy Scanner, Carrier and Cruiser special attacks.\n• Can only be used once per game.";
            shipDescriptions[1] = "• Fires a normal shot to a target location. The area around it will be highlighted and a message will tell you how many ships are in that highlighted area.\n• Does not work when radar is jammed.\n• Can only be used once per game.";
            shipDescriptions[2] = "• The first time the submarine is hit, it will be moved to a new location.\n• On reposition, the submarine regains full health.\n• PASSIVE EFFECT.";
            shipDescriptions[3] = "• The next shot the enemy makes, normal or special, will be forced to miss.\n• Fires a normal attack to target location.\n• Two uses per game.";
            shipDescriptions[4] = "• The first time this ship is hit, the enemy ship that shot it takes a hit of damage and is revealed.\n• Does not work when radar is jammed.\n• PASSIVE EFFECT";
            shipDescriptions[5] = "• Reveals the position of an enemy ship tile.\n• Does not work when radar is jammed.\n• One use per game.";
            shipDescriptions[6] = "• Can be fired to a target location and if it hits a ship, that ship is instantly killed.\n• If fired on the highlighted area after the Scanner’s ability, it will kill the smallest ship in that area.\n• One use per game.";
            shipDescriptions[7] = "• Fires an attack in the shape of a 3 x 3 cross.\n• One use per game.";
        }

        private void GameBoard_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            lock (clickedLock)
            {
                if (clickedShip != null)
                {
                    if (clickedShip.shipName.Contains("temp"))
                        return;
                    var rotateResult = clickedShip.rotate();
                    if (rotateResult == 0)
                    {
                        if (!clickedShip.vertical)
                            shipImages[clickedShip.length - 2] = horizontalShips[Array.IndexOf(shipNames, clickedShip.shipName)];
                        else
                            shipImages[clickedShip.length - 2] = verticalShips[Array.IndexOf(shipNames, clickedShip.shipName)];
                        shipShadows[clickedShip.length - 2] = getShipShadow(clickedShip.length - 2);
                    }
                }
            }
        }

        private void GameBoard_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameBoard);
            Point pointerPosition = getGridPosition(pointer);
            lock (clickedLock)
            {
                moveError = null;
                clickedShip = null;
                clickedShipTile = -1;
            }
            lock (hoverLock)
            {
                getHoveredShip(pointerPosition);
                if (hoveredShip != null)
                    showDescriptionText();
            }
        }

        private void GameBoard_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
                    return;
            }
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameBoard);
            Point pointerPosition = getGridPosition(pointer);
            if (pointerPosition.x > -1)
            {
                lock(clickedLock)
                {
                    for (int i = 0; i < Globals.player.fleet.Length; i++)
                    {
                        if (Globals.player.fleet[i].containsPoint(pointerPosition))
                        {
                            if (!Globals.player.fleet[i].shipName.Contains("temp"))
                            {
                                clickedShip = Globals.player.fleet[i];
                                clickedShipTile = Array.IndexOf(clickedShip.positionArray, pointerPosition);
                                lock (hoverLock)
                                {
                                    hoveredShip = null;
                                }
                                break;
                            }
                            else
                                showErrorText();
                        }
                    }
                    if (clickedShip == null)
                        return;
                }
            }
        }

        private void showErrorText()
        {
            shipDescriptionTitle.Text = "";
            shipDescriptionDetails.TextAlignment = TextAlignment.Center;
            shipDescriptionDetails.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            shipDescriptionDetails.Text = errorText;
        }

        private void showInstructionsText()
        {
            shipDescriptionTitle.Text = "Controls";
            shipDescriptionDetails.TextAlignment = TextAlignment.Left;
            shipDescriptionDetails.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            shipDescriptionDetails.Text = instructionsText;
        }

        private void showDescriptionText()
        {
            shipDescriptionDetails.TextAlignment = TextAlignment.Left;
            shipDescriptionDetails.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            int index = Array.IndexOf(shipNames, hoveredShip.shipName);
            shipDescriptionTitle.Text = shipSpecialNames[index];
            shipDescriptionDetails.Text = shipDescriptions[index];
        }

        private double adjust(double arg)
        {
            return arg * scale;
        }

        private void GameBoard_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameBoard);
            Point pointerPosition = getGridPosition(pointer);
            lock (hoverLock)
            {
                getHoveredShip(pointerPosition);
            }
            lock (clickedLock)
            {
                if (clickedShip != null && pointerPosition.x > -1)
                {
                    Point newMainPoint;
                    if (clickedShip.vertical)
                        newMainPoint = new Point(pointerPosition.x, pointerPosition.y - clickedShipTile);
                    else
                        newMainPoint = new Point(pointerPosition.x - clickedShipTile, pointerPosition.y);
                    int result = clickedShip.move(newMainPoint);
                    if (result == 1)
                        moveError = new Tuple<Point, int>(newMainPoint, 5);
                    else
                        moveError = null;
                }
            }
        }

        private void getHoveredShip(Point pointerPosition)
        { 
            if (pointerPosition.x > -1)
            {
                lock (clickedLock)
                {
                    if (clickedShip == null)
                    {
                        bool pointerOverShip = false;
                        for (int i = 0; i < Globals.player.fleet.Length; i++)
                        {
                            if (Globals.player.fleet[i].containsPoint(pointerPosition))
                            {
                                if (Globals.player.fleet[i].shipName.Contains("temp"))
                                {
                                    hoveredShip = null;
                                    showErrorText();
                                    return;
                                }
                                hoveredShip = Globals.player.fleet[i];
                                pointerOverShip = true;
                                showDescriptionText();
                                break;
                            }
                        }
                        if (!pointerOverShip)
                        {
                            showInstructionsText();
                            hoveredShip = null;
                        }
                    }
                }
            }
        }

        private Point getGridPosition(Windows.UI.Input.PointerPoint pointerPosition)
        {
            double x = pointerPosition.Position.X;
            double y = pointerPosition.Position.Y;
            if (x >= adjust(40) && x <= adjust(670) && y >= adjust(30) && y <= adjust(660))
            {
                x = x - adjust(40);
                y = y - adjust(30);
                bool xSet = false;
                bool ySet = false;
                for (int i = 0; i < 9; i++)
                {
                    if (x <= adjust(70 * (i + 1)) && !xSet)
                    {
                        x = i;
                        xSet = true;
                    }
                    if (y <= adjust(70 * (i + 1)) && !ySet)
                    {
                        y = i;
                        ySet = true;
                    }
                    if (xSet && ySet)
                        break;
                }
                return new Point(x, y);
            }
            return new Point(-1, -1);
        }

        private void quit_Click(object sender, RoutedEventArgs e)
        {
            Globals.socket.Off(Globals.GameID + " player disconnect");
            Globals.socket.Emit("quit game", Globals.GameID);
            Frame.Navigate(typeof(MainMenu));
            Globals.player.clearGrids();
            Globals.player.fleet = new Ship[4];
            Globals.GameID = -1;
        }

        private void canvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if (Globals.DrawReady)
            {
                for (int i = 0; i < shipImages.Length; i++)
                {
                    lock (clickedLock)
                    {
                        if (Globals.player.fleet[i] != null)
                        {
                            double x = Globals.player.fleet[i].mainPoint.x;
                            double y = Globals.player.fleet[i].mainPoint.y;
                            float xPos = (float)Globals.player.homeGrid[(int)x][(int)y].screenPoint.x;
                            float yPos = (float)Globals.player.homeGrid[(int)x][(int)y].screenPoint.y;

                            args.DrawingSession.DrawImage(shipShadows[i], xPos, yPos);
                            args.DrawingSession.DrawImage(shipImages[i], xPos, yPos);
                        }
                    }
                }
                lock (hoverLock)
                {
                    if (hoveredShip != null)
                    {
                        float drawX = (float)Globals.player.homeGrid[(int)(hoveredShip.mainPoint.x)][(int)(hoveredShip.mainPoint.y)].screenPoint.x;
                        float drawY = (float)Globals.player.homeGrid[(int)(hoveredShip.mainPoint.x)][(int)(hoveredShip.mainPoint.y)].screenPoint.y;
                        float imageWidth = (float)shipImages[hoveredShip.length - 2].GetBounds(screenCanvas).Width;
                        float imageHeight = (float)shipImages[hoveredShip.length - 2].GetBounds(screenCanvas).Height;
                        args.DrawingSession.DrawRectangle(drawX, drawY, imageWidth, imageHeight, Windows.UI.Colors.Red, 3);
                        var rotateResult = hoveredShip.getRotationPosition();
                        int imageIndex = Array.IndexOf(shipNames, hoveredShip.shipName);
                        var transparentShip = new OpacityEffect();
                        if (hoveredShip.vertical)
                            transparentShip.Source = horizontalShips[imageIndex];
                        else
                            transparentShip.Source = verticalShips[imageIndex];
                        transparentShip.Opacity = (float)0.6;
                        if (rotateResult.Item1.Length > 0 && rotateResult.Item2)
                            args.DrawingSession.DrawImage(transparentShip, drawX, drawY);
                        else if (rotateResult.Item1.Length > 0)
                        {
                            var tintedShip = new TintEffect();
                            tintedShip.Source = transparentShip;
                            tintedShip.Color = Windows.UI.Colors.Red;
                            args.DrawingSession.DrawImage(tintedShip, drawX, drawY);
                        }
                    }
                }
                lock (clickedLock)
                {
                    if (moveError != null)
                    {
                        var tintedShip = new TintEffect();
                        tintedShip.Source = shipImages[clickedShip.length - 2];
                        tintedShip.Color = Windows.UI.Colors.Red;
                        var transparentShip = new OpacityEffect();
                        transparentShip.Source = tintedShip;
                        transparentShip.Opacity = (float)0.75;
                        float xPos = (float)Globals.player.homeGrid[(int)moveError.Item1.x][(int)moveError.Item1.y].screenPoint.x;
                        float yPos = (float)Globals.player.homeGrid[(int)moveError.Item1.x][(int)moveError.Item1.y].screenPoint.y;
                        args.DrawingSession.DrawImage(transparentShip, xPos, yPos);
                    }
                }
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            screenCanvas.RemoveFromVisualTree();
            screenCanvas = null;
        }

        private void canvas_CreateResources(CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            for (int i = 0; i < shipImages.Length; i++)
            {
                var originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship" + (i + 2) + "temp.png");
                shipImages[i] = scaleImage(originalImage);
                shipShadows[i] = getShipShadow(i);
            }
            for (int i = 0; i < 8; i++)
            {
                var originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship" + ((i + 4) / 2) + shipSelectButtons[i].Name + ".png");
                verticalShips[i] = scaleImage(originalImage);
                originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship" + ((i + 4) / 2) + shipSelectButtons[i].Name + "Hor.png");
                horizontalShips[i] = scaleImage(originalImage);
            }
        }

        private ICanvasImage scaleImage(ICanvasImage image)
        {
            var scaleEffect = new ScaleEffect();
            scaleEffect.Source = image;
            scaleEffect.Scale = new Vector2((float)scale, (float)scale);
            return scaleEffect;
        }

        private ICanvasImage getShipShadow(int index)
        {
            var shadowEffect = new ShadowEffect();
            shadowEffect.Source = shipImages[index];
            var finalShadow = new Transform2DEffect
            {
                Source = shadowEffect,
                TransformMatrix = Matrix3x2.CreateTranslation(1, 1)
            };
            return finalShadow;
        }

        private void shipSelect_Click(object sender, RoutedEventArgs e)
        {
            int shipIndex = (int.Parse((sender as Button).Content.ToString()) - 1) / 2;
            int imageIndex = Array.IndexOf(shipSelectButtons, sender);
            Globals.player.fleet[shipIndex].shipName = shipSelectButtons[imageIndex].Name;
            (sender as Button).IsEnabled = false;
            if (imageIndex % 2 == 0)
                shipSelectButtons[imageIndex + 1].IsEnabled = true;
            else
                shipSelectButtons[imageIndex - 1].IsEnabled = true;
            if (Globals.player.fleet[shipIndex].vertical)
                shipImages[shipIndex] = verticalShips[imageIndex];
            else
                shipImages[shipIndex] = horizontalShips[imageIndex];
            shipShadows[shipIndex] = getShipShadow(shipIndex);
            shipDescriptionDetails.TextAlignment = TextAlignment.Left;
            shipDescriptionDetails.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            shipDescriptionTitle.Text = shipSpecialNames[imageIndex];
            shipDescriptionDetails.Text = shipDescriptions[imageIndex];            
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            gameBoard.PointerMoved -= GameBoard_PointerMoved;
            gameBoard.PointerPressed -= GameBoard_PointerPressed;
            gameBoard.PointerReleased -= GameBoard_PointerReleased;
            gameBoard.DoubleTapped -= GameBoard_DoubleTapped;
            for (int i = 0; i < Globals.player.fleet.Length; i++)
            {
                if (Globals.player.fleet[i].shipName.Contains("temp"))
                {
                    if (Globals.player.fleet[i].length == 2)
                    {
                        Globals.player.fleet[i].shipName = "Scrambler";
                        shipImages[0] = verticalShips[0];
                        shipShadows[0] = getShipShadow(0);
                    }
                    else if (Globals.player.fleet[i].length == 3)
                    {
                        Globals.player.fleet[i].shipName = "Submarine";
                        shipImages[1] = verticalShips[2];
                        shipShadows[1] = getShipShadow(1);
                    }
                    else if (Globals.player.fleet[i].length == 4)
                    {
                        Globals.player.fleet[i].shipName = "Cruiser";
                        shipImages[2] = verticalShips[4];
                        shipShadows[2] = getShipShadow(2);

                    }
                    else if (Globals.player.fleet[i].length == 5)
                    {
                        Globals.player.fleet[i].shipName = "Executioner";
                        shipImages[3] = verticalShips[6];
                        shipShadows[3] = getShipShadow(3);
                    }
                }
                var transparent = new OpacityEffect
                {
                    Source = shipImages[i],
                    Opacity = (float)0.7
                };
                shipImages[i] = transparent;
                var transparentShadow = new OpacityEffect
                {
                    Source = shipShadows[i],
                    Opacity = (float)0.7
                };
                shipShadows[i] = transparentShadow;
            }
            foreach (Button b in shipSelectButtons)
                b.IsEnabled = false;
            hoveredShip = null;
            clickedShip = null;
            moveError = null;
            finishButton.IsEnabled = false;
            shipDescriptionTitle.Text = "";
            shipDescriptionDetails.Foreground = new SolidColorBrush(Windows.UI.Colors.White);
            shipDescriptionDetails.TextAlignment = TextAlignment.Center;
            shipDescriptionDetails.FontFamily = new FontFamily("Stencil");
            shipDescriptionDetails.FontSize = 28;
            shipDescriptionDetails.Text = "\nWaiting for other player...";
            timer.Dispose();
            timerText.Text = "";
            Globals.socket.On(Globals.GameID + " ready", async (data) =>
            {
                Globals.socket.Off(Globals.GameID + " ready");
                if ((long)data == Globals.PlayerID)
                    Globals.player.hasTurn = true;
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    Frame.Navigate(typeof(GameScreen));
                });
            });
            Globals.socket.Emit("fleet finished", string.Format("{{\"playerID\": {0}, \"gameID\": {1}}}", Globals.PlayerID, Globals.GameID));
        }
    }
}
