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
        static Globals()
        {
            socket = IO.Socket("https://seasatwar.herokuapp.com");
            socket.Connect();
            DrawReady = false;
            GameID = -1;
            player = new Player();
        }
    }

    public class Player
    {
        public Tile[][] homeGrid = new Tile[9][];
        public Tile[][] targetGrid = new Tile[9][];
        public Ship[] fleet;
        public bool hasTurn { get; set; }

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
            hasTurn = false;
        }


        public void loadGrid(string grid, Point topLeftCorner, double tileSize)
        {
            double x = topLeftCorner.x;
            double y = topLeftCorner.y;
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


        public void clearGrids()
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
        public Point gridPoint { get; private set; }
        public Point screenPoint { get; private set; }
        public bool hasShip { get; set; }
        public int shipHit { get; set; }
        public int shipIndex { get; set; }
        public bool partialVision { get; set; }
        public bool scrambled { get; set; }
        public bool detected { get; set; }
        public int scanCount { get; set; }

        public Tile(Point pixelPoint, Point gameboardPoint)
        {
            screenPoint = pixelPoint;         //top left corner pixel coordinates
            gridPoint = gameboardPoint;	//x and y coordinate in relation to grid
            hasShip = false;       //whether or not a ship occupies this tile
            shipHit = 2;   //1 = 'hit', 0 = 'miss', 2 = 'not shot at'
            shipIndex = -1;        //contains index of ship in player fleet
            partialVision = false; //Whether Tile is under the influence of Scanner's special attack.
            scrambled = false;
            detected = false;
            scanCount = -1;
        }


        public bool shipPresent()
        {
            return hasShip;
        }


        public bool isShotAt()
        {
            if (shipHit > 1)
                return false;
            return true; 
        }


        public void updateTile()
        {
            if (!isShotAt())
            {
                if (shipPresent())
                {
                    shipHit = 1;
                    Globals.player.fleet[shipIndex].shotCounter++;
                }
                else
                {
                    shipHit = 0;
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
        public double x { get; set; }
        public double y { get; set; }

        public Point(double xPos, double yPos)
        {
            x = xPos;
            y = yPos;
        }

        public void move(double newX, double newY)
        {
            x = newX;
            y = newY;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;
            Point p = (Point)obj;
            return (x == p.x) && (y == p.y);
        }

        public override int GetHashCode()
        {
            return (int)(x*y);
        }
    }

    public class Ship
    {
        public string shipName { get; set; }
        public Point mainPoint;
        public bool vertical { get; set; }
        public int length { get; set; }
        public bool alive { get; set; }
        public Point[] positionArray;
        public int shotCounter { get; set; }
        public int specialAttacksLeft { get; set; }
        public bool firstHit { get; set; }

        public Ship(string name, int shipSize, Point frontPoint)
        {
            shipName = name;
            mainPoint = frontPoint;
            vertical = true;
            length = shipSize;
            alive = true;
            positionArray = updatePosArray();
            updateHomeGrid(true, length - 2);
            shotCounter = 0;  //if counter reaches ship's length, it sinks
            specialAttacksLeft = 1;
            firstHit = false;
        }

        public Tuple<Point[], bool> getRotationPosition()
        {
            if ((!vertical && ((mainPoint.y + length - 1) > 8)) || (vertical && ((mainPoint.x + length - 1) > 8)))
                return new Tuple<Point[], bool>(new Point[0], false);   //outside of game board
            Point[] pos = new Point[length];
            pos[0] = mainPoint;
            bool success = true;
            for (int i = 1; i < length; i++)
            {
                if (vertical)
                {
                    if (Globals.player.homeGrid[(int)(mainPoint.x + i)][(int)(mainPoint.y)].hasShip && Globals.player.homeGrid[(int)(mainPoint.x + i)][(int)(mainPoint.y)].shipIndex != (length - 2))
                        success = false;
                    pos[i] = new Point(mainPoint.x + i, mainPoint.y);
                }
                else
                {
                    if (Globals.player.homeGrid[(int)(mainPoint.x)][(int)(mainPoint.y + i)].hasShip && Globals.player.homeGrid[(int)(mainPoint.x)][(int)(mainPoint.y + i)].shipIndex != (length - 2))
                        success = false;
                    pos[i] = new Point(mainPoint.x, mainPoint.y + i);
                }
            }
            return new Tuple<Point[], bool>(pos, success);
        }

        public Tuple<Point[], bool> getMovePosition(Point newMainPoint)
        {
            if (newMainPoint.x < 0 || newMainPoint.y < 0 || (vertical && ((newMainPoint.y + length - 1) > 8)) || (!vertical && ((newMainPoint.x + length - 1) > 8)))
                return new Tuple<Point[], bool>(new Point[0], false);   //outside of game board
            bool success = true;
            Point[] pos = new Point[length];
            for (int i = 0; i < length; i++)
            {
                if (vertical)
                {
                    if (Globals.player.homeGrid[(int)(newMainPoint.x)][(int)(newMainPoint.y + i)].hasShip && Globals.player.homeGrid[(int)(newMainPoint.x)][(int)(newMainPoint.y + i)].shipIndex != (length - 2))
                        success = false;  //another ship is in the spot
                    pos[i] = new Point(newMainPoint.x, newMainPoint.y + i);
                }
                else
                {
                    if (Globals.player.homeGrid[(int)(newMainPoint.x + i)][(int)(newMainPoint.y)].hasShip && Globals.player.homeGrid[(int)(newMainPoint.x + i)][(int)(newMainPoint.y)].shipIndex != (length - 2))
                        success = false;  //another ship is in the spot
                    pos[i] = new Point(newMainPoint.x + i, newMainPoint.y);
                }
            }
            return new Tuple<Point[], bool>(pos, success);
        }

        public void updateHomeGrid(bool updateValue, int shipIndex)
        {
            for (int i = 0; i < positionArray.Length; i++)
            {
                Globals.player.homeGrid[(int)positionArray[i].x][(int)positionArray[i].y].hasShip = updateValue;
                Globals.player.homeGrid[(int)positionArray[i].x][(int)positionArray[i].y].shipIndex = shipIndex;
            }
        }

        public bool updateAlive()
        {
            if (alive)
            {
                if (shotCounter == length)
                {
                    alive = false;
                    return true;
                }
            }
            return false;
        }

        public void updateSpecialAttacksLeft()
        {
            if (shipName.Equals("Scanner") || shipName.Equals("Defender"))
                specialAttacksLeft = 2;
            else if (shipName.Equals("Submarine") || shipName.Equals("Cruiser"))
            {
                specialAttacksLeft = 0;
                firstHit = true;
            }
        }

        public Point[] updatePosArray()
        {
            Point[] pos = new Point[length];
            pos[0] = mainPoint;
            for (int i = 1; i < length; i++)
            {
                if (vertical)
                    pos[i] = new Point(mainPoint.x, mainPoint.y + i);
                else
                    pos[i] = new Point(mainPoint.x + i, mainPoint.y);
            }
            return pos;
        }

        public bool containsPoint(Point inputPoint)
        {
            for (int i = 0; i < length; i++)
            {
                if (positionArray[i].Equals(inputPoint))
                    return true;
            }
            return false;
        }

        public int rotate()
        {
            Tuple<Point[], bool> result = getRotationPosition();
            Point[] rotationArray = result.Item1;
            bool rotateSuccess = result.Item2;
            if (rotationArray.Length > 0)
            {
                if (!rotateSuccess)
                    return 1;

                vertical = !vertical;
                updateHomeGrid(false, -1);
                positionArray = rotationArray;
                updateHomeGrid(true, length - 2);
                return 0;
            }
            return -1;
        }

        public int move(Point newMainPoint)
        {
            Tuple<Point[], bool> result = getMovePosition(newMainPoint);
            Point[] moveArray = result.Item1;
            bool moveSuccess = result.Item2;
            if (moveArray.Length > 0)
            {
                if (!moveSuccess)
                    return 1; //fail, but in game board

                updateHomeGrid(false, -1);
                mainPoint = newMainPoint;
                positionArray = moveArray;
                updateHomeGrid(true, length - 2);
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
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                SolidColorBrush scb = new SolidColorBrush();
                scb.Color = Windows.UI.Colors.Black;
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
                    rootFrame.Navigate(typeof(MainMenu), e.Arguments);
                    //rootFrame.Navigate(typeof(ShipSelect), e.Arguments);
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
            Globals.socket.Disconnect();
            deferral.Complete();
        }
    }
}
