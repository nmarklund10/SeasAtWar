using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using System.Threading.Tasks;
using System;
using Microsoft.Graphics.Canvas;
using Windows.UI.Xaml;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using Windows.UI.Xaml.Media;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using System.Collections.Generic;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    /// 

    public class TurnData {
        public long GameID = Globals.GameID;
        public long PlayerID = Globals.PlayerID;
        public int AttackCode { get; set; }
        public List<Tile> AttackTiles = new List<Tile>();
        public string ShipName { get; set; }
    }

    public sealed partial class GameScreen : Page
    {
        private ICanvasImage[] shipImages = new ICanvasImage[4];
        private ICanvasImage[] shipShadows = new ICanvasImage[4];
        private ICanvasImage[] transparentShips = new ICanvasImage[4];
        private ICanvasImage[] transparentShadows = new ICanvasImage[4];
        private ICanvasImage alternateSubmarine;
        private CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
        private object clickedLock = new object();
        private object hoverLock = new object();
        private object tileLock = new object();
        private Ship clickedShip;
        private Ship hoveredShip;
        private Tile hoveredTile;
        private string instructions = "1.Select desired attacking ship on left grid\n2. Select what kind of attack you want\n3. Select desired attack location on right grid\n4. Sink all of your enemy's ships before they sink yours!";
        private Dictionary<bool, string[]> turnStrings = new Dictionary<bool, string[]>()
        {
            {true, new string[] {"Your Turn",  "Select ship and tile to attack!"} },
            {false, new string[] {"Enemy Turn",  "Waiting for other player..."} },
        };
        private Dictionary<string, string> shipDescriptions = new Dictionary<string, string>()
        {
            {"Scrambler", "• For the next 3 turns, the enemy will not know whether they got a hit or miss.\n• Disables Enemy Scanner, Carrier and Cruiser special attacks.\n• Can only be used once per game." },
            {"Scanner", "• Fires a normal shot to a target location. The area around it will be highlighted and a message will tell you how many ships are in that highlighted area.\n• Does not work when radar is jammed.\n• Can only be used once per game." },
            {"Submarine", "• The first time the submarine is hit, it will be moved to a new location.\n• On reposition, the submarine regains full health.\n• PASSIVE EFFECT."},
            {"Defender", "• The next shot the enemy makes, normal or special, will be forced to miss.\n• Fires a normal attack to target location.\n• Two uses per game."},
            {"Cruiser", "• The first time this ship is hit, the enemy ship that shot it takes a hit of damage and is revealed.\n• Does not work when radar is jammed.\n• PASSIVE EFFECT"},
            {"Carrier", "• Reveals the position of an enemy ship tile.\n• Does not work when radar is jammed.\n• One use per game."},
            {"Executioner", "• Can be fired to a target location and if it hits a ship, that ship is instantly killed.\n• If fired on the highlighted area after the Scanner’s ability, it will kill the smallest ship in that area.\n• One use per game."},
            {"Artillery", "• Fires an attack in the shape of a 3 x 3 cross.\n• One use per game."},
        };

        public GameScreen()
        {
            InitializeComponent();
            Globals.player.LoadGrid("target", new Point(Globals.Adjust(710), Globals.Adjust(30)), Globals.Adjust(70));
            gameBoard.PointerMoved += Grid_PointerMoved;
            gameBoard.PointerReleased += Grid_PointerReleased;
            ShipDescriptionText.Text = instructions;
            Globals.player.HasTurn = true;
            Globals.socket.On(Globals.GameID + " player disconnect", async (data) =>
            {
                Globals.DrawReady = false;
                Globals.socket.Off(Globals.GameID + " player disconnect");
                await dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
                {
                    CleanUp();
                    var dialog = new ContentDialog
                    {
                        Title = "Opponent Disconnect",
                        Content = "The other player has disconnected from the game session.  Press OK to return to Main Menu.",
                        CloseButtonText = "OK",
                    };
                    var result = await dialog.ShowAsync();
                    Frame.Navigate(typeof(MainMenu));
                });
                Globals.player.ClearGrids();
                Globals.player.fleet = new Ship[4];
                Globals.GameID = -1;
            });
            DisplayTurn();
        }

        private void DisplayTurn()
        {
            Turn.Text = turnStrings[Globals.player.HasTurn][0];
            TurnText.Text = turnStrings[Globals.player.HasTurn][1];
            SpecialAttackButton.IsEnabled = Globals.player.HasTurn;
            if (Globals.player.HasTurn)
            {
                NormalAttackRect.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            }
        }

        private void Grid_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameBoard);
            Tuple<Point, string> result = GetGridPosition(pointer);
            Point pointerPosition = result.Item1;
            string pointerGrid = result.Item2;
            if (pointerGrid.Equals("home"))
            {
                lock (hoverLock)
                {
                    if (hoveredShip != null)
                    {
                        lock (clickedLock)
                        {
                            clickedShip = hoveredShip;
                            DisplayError("");
                        }
                    }
                }
            }
            else if (pointerGrid.Equals("target"))
            {
                //TODO
                if (clickedShip == null && Globals.player.HasTurn)
                {
                    DisplayError("Must Select Ship First!");
                }
                else
                {
                    DisplayError("");
                }
            }
        }

        private void Grid_PointerMoved(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            Windows.UI.Input.PointerPoint pointer = e.GetCurrentPoint(gameBoard);
            Tuple<Point, string> result = GetGridPosition(pointer);
            Point pointerPosition = result.Item1;
            string pointerGrid = result.Item2;
            lock (hoverLock)
            {
                GetHoveredShip(pointerPosition, pointerGrid);
            }
            lock(tileLock)
            {
                GetHoveredTile(pointerPosition, pointerGrid);
            }
        }

        private void GetHoveredShip(Point pointerPosition, string pointerGrid)
        {
            bool pointerOverShip = false;
            if (pointerPosition.X > -1 && pointerGrid.Equals("home") && Globals.player.HasTurn)
            {
                for (int i = 0; i < Globals.player.fleet.Length; i++)
                {
                    if (Globals.player.fleet[i].ContainsPoint(pointerPosition))
                    {
                        hoveredShip = Globals.player.fleet[i];
                        pointerOverShip = true;
                        ShipDescriptionTitle.Text = hoveredShip.ShipName + "'s Special Ability";
                        ShipDescriptionText.Text = shipDescriptions[hoveredShip.ShipName];
                        break;
                    }
                }
                
            }
            if (!pointerOverShip)
            {
                ShipDescriptionTitle.Text = "Instructions";
                ShipDescriptionText.Text = instructions;
                hoveredShip = null;
            }
        }

        private void GetHoveredTile(Point pointerPosition, string pointerGrid)
        {
            if (clickedShip != null)
            {
                if (pointerPosition.X > -1 && pointerGrid.Equals("target"))
                    hoveredTile = Globals.player.targetGrid[(int)pointerPosition.X][(int)pointerPosition.Y];
                else
                    hoveredTile = null;
            }
            else
            {
                hoveredTile = null;
            }
        }

        private void DisplayError(String err)
        {
            errorMessage.TextAlignment = TextAlignment.Center;
            errorMessage.Foreground = new SolidColorBrush(Windows.UI.Colors.Red);
            errorMessage.Text = err;
        }

        private Tuple<Point, string> GetGridPosition(Windows.UI.Input.PointerPoint pointerPosition)
        {
            double x = pointerPosition.Position.X;
            double y = pointerPosition.Position.Y;
            string grid = "";
            if (y >= Globals.Adjust(30) && y <= Globals.Adjust(660))
            {
                if (x >= Globals.Adjust(40) && x <= Globals.Adjust(670))  //home grid
                {
                    x = x - Globals.Adjust(40);
                    grid = "home";
                }
                else if (x >= Globals.Adjust(710) && x <= Globals.Adjust(1340))  //target grid
                {
                    x = x - Globals.Adjust(710);
                    grid = "target";
                }
                else
                    return new Tuple<Point, string>(new Point(-1, -1), grid);
                y = y - Globals.Adjust(30);
                bool xSet = false;
                bool ySet = false;
                for (int i = 0; i < 9; i++)
                {
                    if (x <= Globals.Adjust(70 * (i + 1)) && !xSet)
                    {
                        x = i;
                        xSet = true;
                    }
                    if (y <= Globals.Adjust(70 * (i + 1)) && !ySet)
                    {
                        y = i;
                        ySet = true;
                    }
                    if (xSet && ySet)
                        break;
                }
                return new Tuple<Point, string>(new Point(x, y), grid);
            }
            return new Tuple<Point, string>(new Point(-1, -1), grid);
        }

        private void DrawSelectRectangle(Ship s, CanvasAnimatedDrawEventArgs args)
        {
            float drawX = (float)Globals.player.homeGrid[(int)(s.mainPoint.X)][(int)(s.mainPoint.Y)].ScreenPoint.X;
            float drawY = (float)Globals.player.homeGrid[(int)(s.mainPoint.X)][(int)(s.mainPoint.Y)].ScreenPoint.Y;
            float imageWidth = (float)shipImages[s.Length - 2].GetBounds(screenCanvas).Width;
            float imageHeight = (float)shipImages[s.Length - 2].GetBounds(screenCanvas).Height;
            args.DrawingSession.DrawRectangle(drawX, drawY, imageWidth, imageHeight, Windows.UI.Colors.Red, 3);
        }

        private void ScreenCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            if (Globals.DrawReady)
            {
                for (int i = 0; i < shipImages.Length; i++)
                {
                    lock (clickedLock)
                    {
                        if (Globals.player.fleet[i] != null)
                        {
                            double x = Globals.player.fleet[i].mainPoint.X;
                            double y = Globals.player.fleet[i].mainPoint.Y;
                            float xPos = (float)Globals.player.homeGrid[(int)x][(int)y].ScreenPoint.X;
                            float yPos = (float)Globals.player.homeGrid[(int)x][(int)y].ScreenPoint.Y;
                            if (!Globals.player.HasTurn)
                                args.DrawingSession.DrawImage(transparentShips[i], xPos, yPos);
                            else
                            {
                                args.DrawingSession.DrawImage(shipShadows[i], xPos, yPos);
                                args.DrawingSession.DrawImage(shipImages[i], xPos, yPos);
                            }
                        }
                    }
                }
                if (Globals.player.HasTurn)
                {
                    lock (clickedLock)
                    {
                        if (clickedShip != null)
                            DrawSelectRectangle(clickedShip, args);
                    }
                    lock (hoverLock)
                    {
                        if (hoveredShip != null && hoveredShip != clickedShip)
                            DrawSelectRectangle(hoveredShip, args);
                    }
                    lock (tileLock)
                    {
                        if (hoveredTile != null)
                            args.DrawingSession.DrawRectangle((float)hoveredTile.ScreenPoint.X, (float)hoveredTile.ScreenPoint.Y, (float)Globals.Adjust(70), (float)Globals.Adjust(70), Windows.UI.Colors.Red, 3);
                    }
                }
            }
        }

        private void ScreenCanvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            for (int i = 0; i < shipImages.Length; i++)
            {
                string fileName;
                if (Globals.player.fleet[i].Vertical)
                    fileName = "images/Ships/ship" + (i + 2) + Globals.player.fleet[i].ShipName + ".png";
                else
                    fileName = "images/Ships/ship" + (i + 2) + Globals.player.fleet[i].ShipName + "Hor.png";
                var originalImage = await CanvasBitmap.LoadAsync(sender, fileName);
                shipImages[i] = Globals.ScaleImage(originalImage);
                shipShadows[i] = GetShipShadow(i);
                var transShip = new OpacityEffect
                {
                    Source = shipImages[i],
                    Opacity = (float)0.8
                };
                transparentShips[i] = transShip;
                var transShadow = new OpacityEffect
                {
                    Source = shipShadows[i],
                    Opacity = (float)0.8
                };
                transparentShadows[i] = transShadow;
                if (Globals.player.fleet[i].ShipName.Equals("Submarine"))
                {
                    if (Globals.player.fleet[i].Vertical)
                        originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship3SubmarineHor.png");
                    else
                        originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship3Submarine.png");
                    alternateSubmarine = Globals.ScaleImage(originalImage);
                }
            }
        }

        private ICanvasImage GetShipShadow(int index)
        {
            var shadowEffect = new ShadowEffect
            {
                Source = shipImages[index]
            };
            var finalShadow = new Transform2DEffect
            {
                Source = shadowEffect,
                TransformMatrix = Matrix3x2.CreateTranslation(1, 1)
            };
            return finalShadow;
        }

        private void CleanUp()
        {
            gameBoard.PointerMoved -= Grid_PointerMoved;
            gameBoard.PointerReleased -= Grid_PointerReleased;
            Globals.socket.Off(Globals.GameID + " player disconnect");
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            screenCanvas.RemoveFromVisualTree();
            screenCanvas = null;
        }

        private void NormalAttack_Click(object sender, RoutedEventArgs e)
        {
            SpecialAttackButton.IsEnabled = true;
            NormalAttackButton.IsEnabled = false;
            NormalAttackRect.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            SpecialAttackRect.Stroke = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }

        private void SpecialAttack_Click(object sender, RoutedEventArgs e)
        {
            SpecialAttackButton.IsEnabled = false;
            NormalAttackButton.IsEnabled = true;
            SpecialAttackRect.Stroke = new SolidColorBrush(Windows.UI.Colors.Red);
            NormalAttackRect.Stroke = new SolidColorBrush(Windows.UI.Colors.Transparent);
        }
    }
}
