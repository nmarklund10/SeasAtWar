using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Quobject.SocketIoClientDotNet.Client;
using Windows.UI.ViewManagement;
using Windows.Foundation;
using Windows.ApplicationModel.Core;
using System;
using Windows.UI.Core;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using System.Numerics;
using System.Threading;

namespace SeasAtWar
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>

    public static class Globals
    {
        public static long PlayerID { get; set; }
        public static long GameID { get; set; }
        public static Socket socket;
        public static Player player;
        public static bool DrawReady { get; set; }
        public static double Scale { get; set; }
        private static Timer ping_timer;
        private static bool pinging = false;
        static Globals()
        {
            //socket = IO.Socket("https://seasatwar.herokuapp.com");
            socket = IO.Socket("http://127.0.0.1:3000");
            socket.Connect();
            DrawReady = false;
            GameID = -1;
            player = new Player();
        }

        public static void StartPing()
        {
            if (!pinging)
            {
                ping_timer = new Timer(Ping, null, 0, 25000);
                pinging = true;
            }
        }

        public static void StopPing()
        {
            if (pinging)
            {
                ping_timer.Dispose();
                pinging = false;
            }
        }

        private static void Ping(object state)
        {
            socket.Emit("ping", "");
        }

        public static double Adjust(double arg)
        {
            return arg * Scale;
        }
        
        public static ICanvasImage ScaleImage(ICanvasImage image)
        {
            var scaleEffect = new ScaleEffect
            {
                Source = image,
                Scale = new Vector2((float)Scale, (float)Scale)
            };
            return scaleEffect;
        }
    }

    public class Player
    {
        public Tile[][] homeGrid = new Tile[9][];
        public Tile[][] targetGrid = new Tile[9][];
        public Ship[] fleet;
        public bool HasTurn { get; set; }

        public Player()
        {
            for (int i = 0; i < 9; i++)
            {
                homeGrid[i] = new Tile[9];
            }
            for (var i = 0; i < 9; i++)
            {
                targetGrid[i] = new Tile[9];
            }
            HasTurn = false;
        }


        public void LoadGrid(string grid, Point topLeftCorner, double tileSize)
        {
            double x = topLeftCorner.X;
            double y = topLeftCorner.Y;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (grid.Equals("home"))
                        homeGrid[i][j] = new Tile(new Point(i * tileSize + x, j * tileSize + y), new Point(i, j));
                    else if (grid.Equals("target"))
                        targetGrid[i][j] = new Tile(new Point(i * tileSize + x, j * tileSize + y), new Point(i, j));
                }
            }
        }


        public void ClearGrids()
        {
            for (int i = 0; i < 9; i++)
            {
                homeGrid[i] = new Tile[9];   
                targetGrid[i] = new Tile[9];
            }
        }
    }

    /*
    Tile class
    ---------------------
    Stores the information for a single Tile in a Grid

    */
    public class Tile
    {
        public Point GridPoint { get; private set; }
        public Point ScreenPoint { get; private set; }
        public bool HasShip { get; set; }
        public int ShipHit { get; set; }
        public int ShipIndex { get; set; }
        public bool PartialVision { get; set; }
        public bool Scrambled { get; set; }
        public bool Detected { get; set; }
        public int ScanCount { get; set; }

        public Tile(Point pixelPoint, Point gameboardPoint)
        {
            ScreenPoint = pixelPoint;         //top left corner pixel coordinates
            GridPoint = gameboardPoint;	//x and y coordinate in relation to grid
            HasShip = false;       //whether or not a ship occupies this tile
            ShipHit = 2;   //1 = 'hit', 0 = 'miss', 2 = 'not shot at'
            ShipIndex = -1;        //contains index of ship in player fleet
            PartialVision = false; //Whether Tile is under the influence of Scanner's special attack.
            Scrambled = false;
            Detected = false;
            ScanCount = -1;
        }


        public bool ShipPresent()
        {
            return HasShip;
        }


        public bool IsShotAt()
        {
            if (ShipHit > 1)
                return false;
            return true; 
        }


        public void UpdateTile()
        {
            if (!IsShotAt())
            {
                if (ShipPresent())
                {
                    ShipHit = 1;
                    Globals.player.fleet[ShipIndex].ShotCounter++;
                }
                else
                {
                    ShipHit = 0;
                }
            }
        }
    }

    /*
    Point class
    ---------------------
    Representation of a basic (x, y) coordinate pair

    */
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point(double xPos, double yPos)
        {
            X = xPos;
            Y = yPos;
        }

        public void Move(double newX, double newY)
        {
            X = newX;
            Y = newY;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            Point p = (Point)obj;
            return (X == p.X) && (Y == p.Y);
        }

        public override int GetHashCode()
        {
            return (int)(X*Y);
        }
    }

    public class Ship
    {
        public string ShipName { get; set; }
        public Point mainPoint;
        public bool Vertical { get; set; }
        public int Length { get; set; }
        public bool Alive { get; set; }
        public Point[] positionArray;
        public int ShotCounter { get; set; }
        public int SpecialAttacksLeft { get; set; }
        public bool FirstHit { get; set; }

        public Ship(string name, int shipSize, Point frontPoint)
        {
            ShipName = name;
            mainPoint = frontPoint;
            Vertical = true;
            Length = shipSize;
            Alive = true;
            positionArray = UpdatePosArray();
            UpdateHomeGrid(true, Length - 2);
            ShotCounter = 0;  //if counter reaches ship's length, it sinks
            SpecialAttacksLeft = 1;
            FirstHit = false;
        }

        public Tuple<Point[], bool> GetRotationPosition()
        {
            if ((!Vertical && ((mainPoint.Y + Length - 1) > 8)) || (Vertical && ((mainPoint.X + Length - 1) > 8)))
                return new Tuple<Point[], bool>(new Point[0], false);   //outside of game board
            Point[] pos = new Point[Length];
            pos[0] = mainPoint;
            bool success = true;
            for (int i = 1; i < Length; i++)
            {
                if (Vertical)
                {
                    if (Globals.player.homeGrid[(int)(mainPoint.X + i)][(int)(mainPoint.Y)].HasShip && Globals.player.homeGrid[(int)(mainPoint.X + i)][(int)(mainPoint.Y)].ShipIndex != (Length - 2))
                        success = false;
                    pos[i] = new Point(mainPoint.X + i, mainPoint.Y);
                }
                else
                {
                    if (Globals.player.homeGrid[(int)(mainPoint.X)][(int)(mainPoint.Y + i)].HasShip && Globals.player.homeGrid[(int)(mainPoint.X)][(int)(mainPoint.Y + i)].ShipIndex != (Length - 2))
                        success = false;
                    pos[i] = new Point(mainPoint.X, mainPoint.Y + i);
                }
            }
            return new Tuple<Point[], bool>(pos, success);
        }

        public Tuple<Point[], bool> GetMovePosition(Point newMainPoint)
        {
            if (newMainPoint.X < 0 || newMainPoint.Y < 0 || (Vertical && ((newMainPoint.Y + Length - 1) > 8)) || (!Vertical && ((newMainPoint.X + Length - 1) > 8)))
                return new Tuple<Point[], bool>(new Point[0], false);   //outside of game board
            bool success = true;
            Point[] pos = new Point[Length];
            for (int i = 0; i < Length; i++)
            {
                if (Vertical)
                {
                    if (Globals.player.homeGrid[(int)(newMainPoint.X)][(int)(newMainPoint.Y + i)].HasShip && Globals.player.homeGrid[(int)(newMainPoint.X)][(int)(newMainPoint.Y + i)].ShipIndex != (Length - 2))
                        success = false;  //another ship is in the spot
                    pos[i] = new Point(newMainPoint.X, newMainPoint.Y + i);
                }
                else
                {
                    if (Globals.player.homeGrid[(int)(newMainPoint.X + i)][(int)(newMainPoint.Y)].HasShip && Globals.player.homeGrid[(int)(newMainPoint.X + i)][(int)(newMainPoint.Y)].ShipIndex != (Length - 2))
                        success = false;  //another ship is in the spot
                    pos[i] = new Point(newMainPoint.X + i, newMainPoint.Y);
                }
            }
            return new Tuple<Point[], bool>(pos, success);
        }

        public void UpdateHomeGrid(bool updateValue, int shipIndex)
        {
            for (int i = 0; i < positionArray.Length; i++)
            {
                Globals.player.homeGrid[(int)positionArray[i].X][(int)positionArray[i].Y].HasShip = updateValue;
                Globals.player.homeGrid[(int)positionArray[i].X][(int)positionArray[i].Y].ShipIndex = shipIndex;
            }
        }

        public bool UpdateAlive()
        {
            if (Alive)
            {
                if (ShotCounter == Length)
                {
                    Alive = false;
                    return true;
                }
            }
            return false;
        }

        public void UpdateSpecialAttacksLeft()
        {
            if (ShipName.Equals("Scanner") || ShipName.Equals("Defender"))
                SpecialAttacksLeft = 2;
            else if (ShipName.Equals("Submarine") || ShipName.Equals("Cruiser"))
            {
                SpecialAttacksLeft = 0;
                FirstHit = true;
            }
        }

        public Point[] UpdatePosArray()
        {
            Point[] pos = new Point[Length];
            pos[0] = mainPoint;
            for (int i = 1; i < Length; i++)
            {
                if (Vertical)
                    pos[i] = new Point(mainPoint.X, mainPoint.Y + i);
                else
                    pos[i] = new Point(mainPoint.X + i, mainPoint.Y);
            }
            return pos;
        }

        public bool ContainsPoint(Point inputPoint)
        {
            for (int i = 0; i < Length; i++)
            {
                if (positionArray[i].Equals(inputPoint))
                    return true;
            }
            return false;
        }

        public int Rotate()
        {
            Tuple<Point[], bool> result = GetRotationPosition();
            Point[] rotationArray = result.Item1;
            bool rotateSuccess = result.Item2;
            if (rotationArray.Length > 0)
            {
                if (!rotateSuccess)
                    return 1;

                Vertical = !Vertical;
                UpdateHomeGrid(false, -1);
                positionArray = rotationArray;
                UpdateHomeGrid(true, Length - 2);
                return 0;
            }
            return -1;
        }

        public int Move(Point newMainPoint)
        {
            Tuple<Point[], bool> result = GetMovePosition(newMainPoint);
            Point[] moveArray = result.Item1;
            bool moveSuccess = result.Item2;
            if (moveArray.Length > 0)
            {
                if (!moveSuccess)
                    return 1; //fail, but in game board

                UpdateHomeGrid(false, -1);
                mainPoint = newMainPoint;
                positionArray = moveArray;
                UpdateHomeGrid(true, Length - 2);
                return 0; //success
            }
            return -1; //outside of game board
        }
    }

    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        private bool connected = false;
        private object connect_lock = new object();
        private bool dialog_open = false;

        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;
            Globals.socket.On("welcome", (data) =>
            {
                Globals.PlayerID = (long)data;
                lock (connect_lock) 
                {
                    connected = true;
                }
                Globals.StartPing();
            });
            Globals.socket.On("disconnect", async () =>
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    if (!dialog_open)
                    {
                        dialog_open = true;
                        var dialog = new ContentDialog
                        {
                            Title = "Server Disconnect",
                            Content = "You have lost connection to the server.  Press OK to close the app.",
                            PrimaryButtonText = "OK",
                            IsSecondaryButtonEnabled = false
                        };
                        var result = await dialog.ShowAsync();
                        Globals.StopPing();
                        Globals.socket.Disconnect();
                        CoreApplication.Exit();
                    }
                });
            });
            Globals.socket.Emit("new player", "");
        }

        public double ActualWidth { get; private set; }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                DebugSettings.EnableFrameRateCounter = true;
            }

#endif
            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                SolidColorBrush scb = new SolidColorBrush
                {
                    Color = Windows.UI.Colors.Black
                };
                rootFrame.Background = scb;

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    ApplicationView.PreferredLaunchViewSize = new Size(1280, 720);
                    ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                    while (true)
                    {
                        lock (connect_lock)
                        {
                            if (connected)
                            {
                                break;
                            }
                        }
                    }
                    //rootFrame.Navigate(typeof(MainMenu), e.Arguments);
                    rootFrame.Navigate(typeof(GameScreen), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
