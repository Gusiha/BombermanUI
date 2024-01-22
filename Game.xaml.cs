using ClientBomberman;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class Game : Page
    {
        public Client client { get; set; }

        private const int FieldWidth = 13;
        private const int FieldHeight = 11;
        private const int CellSize = 50;
        private int p1OldCoordX = -1;
        private int p1OldCoordY = -1;
        private int p2OldCoordX = -1;
        private int p2OldCoordY = -1;
        private int p1OldCanvasPlace = -1;
        private int p2OldCanvasPlace = -1;
        private int[,] OldGameState { get; set; }
        private Canvas ClonedCanvas;
        private string ServerAddress;

        public Game(string address)
        {
            Loaded += Loaded_Page;
            ServerAddress = address;

            client = new(IPAddress.Parse(ServerAddress), 65535);

            //TODO: Add try catch in a correct way, so that wrong IP address is handleled correctly.
            client.StartMessageLoop();
            client.SendTo(Encoding.UTF8.GetBytes("connect"));


            InitializeComponent();
            CreateGrid();

            ClonedCanvas = CloneCanvasChildren(GameCanvas, FieldWidth, FieldHeight);

            UpdateWithTickrate();
            OldGameState = new int[FieldWidth, FieldHeight];
        }

        private void Loaded_Page(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Deep copy of canvas children
        /// </summary>
        /// <param name="toClone">A canvas to deep copy children from.</param>
        /// <returns>A new canvas with deep cloned children.</returns>
        private static Canvas CloneCanvasChildren(Canvas toClone, int xS, int yS)
        {
            Canvas newCanvas = new();

            if (toClone == null || toClone.Children.Count == 0)
            {
                return newCanvas;
            }

            for (int i = 0; i < xS; i++)
            {
                for (int j = 0; j < yS; j++)
                {
                    int n = j;
                    if (n == 0) n++;
                    Rectangle rect = (Rectangle)toClone.Children[n * i];
                    Rectangle clonedRect = new()
                    {
                        Fill = rect.Fill.Clone(),
                        Stroke = rect.Stroke.Clone(),
                        StrokeThickness = rect.StrokeThickness,
                        Width = rect.Width,
                        Height = rect.Height
                    };
                    DrawCell(newCanvas, i * 50, j * 50, clonedRect);
                }
            }

            return newCanvas;
        }

        private static void CloneCanvasChildren(Canvas newCanvas, Rectangle[,] toClone)
        {
            if (toClone == null || toClone.GetLength(0) == 0)
            {
                return;
            }

            newCanvas.Children.Clear();

            for (int i = 0; i < toClone.GetLength(0); i++)
            {
                for (int j = 0; j < toClone.GetLength(1); j++)
                {
                    Rectangle clonedRect = new()
                    {
                        Fill = toClone[i, j].Fill.Clone(),
                        Stroke = toClone[i, j].Stroke.Clone(),
                        StrokeThickness = toClone[i, j].StrokeThickness,
                        Width = toClone[i, j].Width,
                        Height = toClone[i, j].Height
                    };

                    DrawCell(newCanvas, i * 50, j * 50, clonedRect);
                }
            }
        }

        /// <summary>
        /// Children color comparer method.
        /// </summary>
        /// <param name="one">First canvas</param>
        /// <param name="two">Second canvas</param>
        /// <returns>True if elements are of the same color, false if otherwise.</returns>
        private static bool CompareCanvasChildren(Canvas one, Canvas two)
        {
            if (one == null || two == null) return false;
            if (one.Children.Count != two.Children.Count) return false;

            for (int i = 0; i < one.Children.Count; i++)
            {
                Rectangle oneRect = (Rectangle)one.Children[i];
                Rectangle twoRect = (Rectangle)two.Children[i];

                if (oneRect.Fill.ToString() != twoRect.Fill.ToString())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool CompareCanvasChildren(Canvas one, Rectangle[,] two)
        {
            if (one == null || two == null) return false;
            if (one.Children.Count != two.GetLength(0) * two.GetLength(1)) return false;

            for (int i = 0; i < two.GetLength(0); i++)
            {
                for (int j = 0; j < two.GetLength(1); j++)
                {
                    int n = i;
                    int g = j;
                    Rectangle oneRect = (Rectangle)one.Children[n * g];

                    if (n == 0 && g != 0) n++;
                    if (g == 0 && n != 0) g++;

                    if (two[i, j].Fill.ToString() != oneRect.Fill.ToString())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private void OnUpdate(object? sender, EventArgs e)
        {

        }

        public void UpdateWithTickrate()
        {
            double tickrate = 1000 / 30;

            Task.Run(() =>
            {
                while (true)
                {
                    Task task = new(() =>
                    {
                        Update();
                    });
                    Stopwatch timer = Stopwatch.StartNew();

                    timer.Restart();
                    task.Start();
                    task.Wait();
                    timer.Stop();

                    if (timer.ElapsedMilliseconds <= tickrate)
                    {
                        Thread.Sleep((int)(tickrate - timer.ElapsedMilliseconds));
                    }

                }
            });
        }

        private void ProcessPlayer(ref int oldPlayerCoordsX, ref int oldPlayerCoordsY, bool canvasCleared, ref int oldCanvasPlace, int playerNum)
        {
            int[] array = new int[2];
            SolidColorBrush color = Brushes.Blue;

            switch (playerNum)
            {
                case 1:
                    array = client.Player1Coorditantes;
                    color = Brushes.Blue;
                    break;

                case 2:
                    array = client.Player2Coorditantes;
                    color = Brushes.Red;
                    break;

                default:
                    break;
            }


            if (array[0] != oldPlayerCoordsX || array[1] != oldPlayerCoordsY)
            {
                int temp = oldCanvasPlace;
                oldPlayerCoordsX = array[0]; oldPlayerCoordsY = array[1];

                Dispatcher.Invoke(() =>
                {
                    if (temp >= 0 && !canvasCleared && GameCanvas.Children.Count > temp)
                    {
                        GameCanvas.Children.RemoveAt(temp);
                    }

                    DrawPlayer(array[0] * 50, array[1] * 50, color);
                    temp = GameCanvas.Children.Count - 1;
                });
                oldCanvasPlace = temp;
            }
        }

        public Task Update()
        {
            bool canvasCleared = false;

            bool stateChanged = false;
            for (int i = 0; i < client.GameState.GetLength(0); i++)
            {
                for (int j = 0; j < client.GameState.GetLength(1); j++)
                {
                    if (OldGameState[i, j] != client.GameState[i, j])
                    {
                        stateChanged = true;
                    }
                }
            }

            //TODO: refactor canvas re-write logic.
            if (stateChanged)
            {
                canvasCleared = true;
                Rectangle[,] assembledCanvas = new Rectangle[FieldWidth, FieldHeight];
                for (int i = 0; i < FieldWidth; i++)
                {
                    for (int j = 0; j < FieldHeight; j++)
                    {
                        OldGameState[i, j] = client.GameState[i, j];
                        switch (client.GameState[i, j])
                        {
                            //emptiness
                            case 0:
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        assembledCanvas[i, j] =
                                            new()
                                            {
                                                Fill = Brushes.Beige,
                                                Stroke = Brushes.Gray,
                                                StrokeThickness = 0.05,
                                                Width = CellSize,
                                                Height = CellSize
                                            };
                                    });

                                    //DrawCell(assembledCanvas, i * 50, j * 50, Brushes.Beige, Brushes.Black);
                                    break;
                                }

                            //wall
                            case 1:
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        assembledCanvas[i, j] =
                                        new()
                                        {
                                            Fill = Brushes.Gray,
                                            Stroke = Brushes.Black,
                                            StrokeThickness = 0.5,
                                            Width = CellSize,
                                            Height = CellSize
                                        };
                                    });
                                    break;
                                }

                            //player
                            case 2:
                                {
                                    //is likely not needed at all.
                                    break;
                                }

                            //bomb
                            case 3:
                                {
                                    //halfway through implementation
                                    break;
                                }

                            //buff
                            case 4:
                                {
                                    //not implemented
                                    break;
                                }

                            //destroyable block
                            case 5:
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        assembledCanvas[i, j] =
                                            new()
                                            {
                                                Fill = Brushes.SandyBrown,
                                                Stroke = Brushes.Gray,
                                                StrokeThickness = 0.05,
                                                Width = CellSize,
                                                Height = CellSize
                                            };

                                    });
                                    break;
                                }
                            default:
                                break;
                        }

                    }

                }
                Dispatcher.Invoke(() =>
                {
                    if (!CompareCanvasChildren(ClonedCanvas, assembledCanvas))
                    {
                        CloneCanvasChildren(ClonedCanvas, assembledCanvas);
                        CloneCanvasChildren(GameCanvas, assembledCanvas);
                    }
                });
            }

            ProcessPlayer(ref p1OldCoordX, ref p1OldCoordY, canvasCleared, ref p1OldCanvasPlace, 1);
            ProcessPlayer(ref p2OldCoordX, ref p2OldCoordY, canvasCleared, ref p2OldCanvasPlace, 2);

            return Task.CompletedTask;
        }

        private void CreateGrid()
        {
            for (int y = 0; y < FieldHeight; y++)
            {
                for (int x = 0; x < FieldWidth; x++)
                {
                    DrawCell(GameCanvas, x * CellSize, y * CellSize, Brushes.Gray, Brushes.Black);
                }
            }
        }

        private static void DrawCell(Canvas canvas, double x, double y, SolidColorBrush fillColor, SolidColorBrush strokeColor)
        {
            var cellRect = new Rectangle
            {
                Fill = fillColor,
                Stroke = strokeColor,
                StrokeThickness = 0.5,
                Width = CellSize,
                Height = CellSize
            };

            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            canvas.Children.Add(cellRect);
        }

        private static void DrawCell(Canvas canvas, double x, double y, Rectangle cellRect)
        {
            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            canvas.Children.Add(cellRect);
        }


        private Rectangle DrawPlayer(double x, double y, SolidColorBrush color)
        {
            var playerRect = new Rectangle
            {
                Fill = color,
                Width = CellSize,
                Height = CellSize
            };

            Canvas.SetLeft(playerRect, x);
            Canvas.SetTop(playerRect, y);

            GameCanvas.Children.Add(playerRect);

            return playerRect;
        }

        private Point GetCellCoordinates(double x, double y)
        {
            int cellX = (int)(x / CellSize);
            int cellY = (int)(y / CellSize);

            return new Point(cellX, cellY);
        }

        //TODO Move command to the server
        private async void OnKeyDown(object sender, KeyEventArgs e)
        {
            await SendMoveCommand(e);
        }

        private async Task SendMoveCommand(KeyEventArgs key)
        {
            await client.SendCommand(key.Key);
        }
    }
}
