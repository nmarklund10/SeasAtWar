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
        private double scale = Window.Current.Bounds.Width / 1920;
        private object clickedLock = new object();
        private object hoverLock = new object();
        private object tileLock = new object();
        private Ship clickedShip;
        private Ship hoveredShip;
        private Tile hoveredTile;

        public GameScreen()
        {
            InitializeComponent();
            Globals.player.loadGrid("target", new Point(adjust(710), adjust(30)), adjust(70));
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
            Tuple<Point, string> result = getGridPosition(pointer);
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
            Tuple<Point, string> result = getGridPosition(pointer);
            Point pointerPosition = result.Item1;
            string pointerGrid = result.Item2;
            lock (hoverLock)
            {
                getHoveredShip(pointerPosition, pointerGrid);
            }
            lock(tileLock)
            {
                getHoveredTile(pointerPosition, pointerGrid);
            }
        }

        private void getHoveredShip(Point pointerPosition, string pointerGrid)
        {
            if (pointerPosition.x > -1 && pointerGrid.Equals("home"))
            {
                bool pointerOverShip = false;
                for (int i = 0; i < Globals.player.fleet.Length; i++)
                {
                    if (Globals.player.fleet[i].containsPoint(pointerPosition))
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

        private void getHoveredTile(Point pointerPosition, string pointerGrid)
        {
            if (clickedShip != null)
            {
                if (pointerPosition.x > -1 && pointerGrid.Equals("target"))
                    hoveredTile = Globals.player.targetGrid[(int)pointerPosition.x][(int)pointerPosition.y];
                else
                    hoveredTile = null;
            }
            else
            {
                hoveredTile = null;
                //TODO:  add error message
            }
        }

        private Tuple<Point, string> getGridPosition(Windows.UI.Input.PointerPoint pointerPosition)
        {
            double x = pointerPosition.Position.X;
            double y = pointerPosition.Position.Y;
            string grid = "";
            if (y >= adjust(30) && y <= adjust(660))
            {
                if (x >= adjust(40) && x <= adjust(670))  //home grid
                {
                    x = x - adjust(40);
                    grid = "home";
                }
                else if (x >= adjust(710) && x <= adjust(1340))  //target grid
                {
                    x = x - adjust(710);
                    grid = "target";
                }
                else
                    return new Tuple<Point, string>(new Point(-1, -1), grid);
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
                return new Tuple<Point, string>(new Point(x, y), grid);
            }
            return new Tuple<Point, string>(new Point(-1, -1), grid);
        }

        private double adjust(double arg)
        {
            return arg * scale;
        }

        private void drawSelectRectangle(Ship s, CanvasAnimatedDrawEventArgs args)
        {
            float drawX = (float)Globals.player.homeGrid[(int)(s.mainPoint.x)][(int)(s.mainPoint.y)].screenPoint.x;
            float drawY = (float)Globals.player.homeGrid[(int)(s.mainPoint.x)][(int)(s.mainPoint.y)].screenPoint.y;
            float imageWidth = (float)shipImages[s.length - 2].GetBounds(screenCanvas).Width;
            float imageHeight = (float)shipImages[s.length - 2].GetBounds(screenCanvas).Height;
            args.DrawingSession.DrawRectangle(drawX, drawY, imageWidth, imageHeight, Windows.UI.Colors.Red, 3);
        }

        private void screenCanvas_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
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
                            if (!Globals.player.hasTurn)
                                args.DrawingSession.DrawImage(transparentShips[i], xPos, yPos);
                            else
                            {
                                args.DrawingSession.DrawImage(shipShadows[i], xPos, yPos);
                                args.DrawingSession.DrawImage(shipImages[i], xPos, yPos);
                            }
                        }
                    }
                }
                if (Globals.player.hasTurn)
                {
                    lock (clickedLock)
                    {
                        if (clickedShip != null)
                            drawSelectRectangle(clickedShip, args);
                    }
                    lock (hoverLock)
                    {
                        if (hoveredShip != null && hoveredShip != clickedShip)
                            drawSelectRectangle(hoveredShip, args);
                    }
                    lock (tileLock)
                    {
                        if (hoveredTile != null)
                            args.DrawingSession.DrawRectangle((float)hoveredTile.screenPoint.x, (float)hoveredTile.screenPoint.y, (float)adjust(70), (float)adjust(70), Windows.UI.Colors.Red, 3);
                    }
                }
            }
        }

        private void screenCanvas_CreateResources(CanvasAnimatedControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(CreateResourcesAsync(sender).AsAsyncAction());
        }

        async Task CreateResourcesAsync(CanvasAnimatedControl sender)
        {
            for (int i = 0; i < shipImages.Length; i++)
            {
                string fileName;
                if (Globals.player.fleet[i].vertical)
                    fileName = "images/Ships/ship" + (i + 2) + Globals.player.fleet[i].shipName + ".png";
                else
                    fileName = "images/Ships/ship" + (i + 2) + Globals.player.fleet[i].shipName + "Hor.png";
                var originalImage = await CanvasBitmap.LoadAsync(sender, fileName);
                shipImages[i] = scaleImage(originalImage);
                shipShadows[i] = getShipShadow(i);
                var transShip = new OpacityEffect();
                transShip.Source = shipImages[i];
                transShip.Opacity = (float) 0.8;
                transparentShips[i] = transShip;
                var transShadow = new OpacityEffect();
                transShadow.Source = shipShadows[i];
                transShadow.Opacity = (float)0.8;
                transparentShadows[i] = transShadow;
                if (Globals.player.fleet[i].shipName.Equals("Submarine"))
                {
                    if (Globals.player.fleet[i].vertical)
                        originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship3SubmarineHor.png");
                    else
                        originalImage = await CanvasBitmap.LoadAsync(sender, "images/Ships/ship3Submarine.png");
                    alternateSubmarine = scaleImage(originalImage);
                }
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

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            screenCanvas.RemoveFromVisualTree();
            screenCanvas = null;
        }
    }
}
