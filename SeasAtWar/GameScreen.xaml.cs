using Windows.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.Graphics.Canvas.UI;
using System.Threading.Tasks;
using System;
using Microsoft.Graphics.Canvas;
using Windows.UI.Xaml;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SeasAtWar
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GameScreen : Page
    {
        private ICanvasImage[] shipImages = new ICanvasImage[4];
        private ICanvasImage[] shipShadows = new ICanvasImage[4];
        private ICanvasImage[] transparentShips = new ICanvasImage[4];
        private ICanvasImage[] transparentShadows = new ICanvasImage[4];
        private ICanvasImage alternateSubmarine;
        private object clickedLock = new object();
        private object hoverLock = new object();
        private object tileLock = new object();
        private Ship clickedShip;
        private Ship hoveredShip;
        private Tile hoveredTile;

        public GameScreen()
        {
            InitializeComponent();
            Globals.player.LoadGrid("target", new Point(Globals.Adjust(710), Globals.Adjust(30)), Globals.Adjust(70));
            gameBoard.PointerMoved += Grid_PointerMoved;
            gameBoard.PointerPressed += Grid_PointerPressed;
            gameBoard.PointerReleased += Grid_PointerReleased;
        }

        private void Grid_PointerReleased(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            
        }

        private void Grid_PointerPressed(object sender, Windows.UI.Xaml.Input.PointerRoutedEventArgs e)
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
                        }
                    }
                }
            }
            else if (pointerGrid.Equals("target"))
            {
                //TODO
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
            if (pointerPosition.X > -1 && pointerGrid.Equals("home"))
            {
                bool pointerOverShip = false;
                for (int i = 0; i < Globals.player.fleet.Length; i++)
                {
                    if (Globals.player.fleet[i].ContainsPoint(pointerPosition))
                    {
                        hoveredShip = Globals.player.fleet[i];
                        pointerOverShip = true;
                        //showDescriptionText();
                        break;
                    }
                }
                if (!pointerOverShip)
                {
                    //showInstructionsText();
                    hoveredShip = null;
                }
            }
            else
                hoveredShip = null;
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
                //TODO:  add error message
            }
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            screenCanvas.RemoveFromVisualTree();
            screenCanvas = null;
        }
    }
}
