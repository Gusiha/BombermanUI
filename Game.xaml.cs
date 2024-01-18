using ClientBomberman;
using System;
using System.Collections.Generic;
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
        private readonly HashSet<Point> player1OccupiedCells = new HashSet<Point>();
        private readonly HashSet<Point> player2OccupiedCells = new HashSet<Point>();
        private Rectangle player1;
        private Rectangle player2;
        private Canvas ClonedCanvas;
        private string ServerAddress;

        public Game(string address)
        {
            ServerAddress = address;

            //"192.168.0.102"
            client = new(IPAddress.Parse(ServerAddress), 65535);

            client.StartMessageLoop();
            client.SendTo(Encoding.UTF8.GetBytes("connect"));


            InitializeComponent();
            CreateGrid();
            CreatePlayers();

            ClonedCanvas = CloneCanvasChildren(GameCanvas);


            KeyDown += OnKeyDown;
            UpdateWithTickrate();
        }

        /// <summary>
        /// Deep copy of canvas children
        /// </summary>
        /// <param name="toClone">A canvas to deep copy children from.</param>
        /// <returns>A new canvas with deep cloned children.</returns>
        private Canvas CloneCanvasChildren(Canvas toClone)
        {
            Canvas newCanvas = new Canvas();

            if (toClone == null || toClone.Children.Count == 0)
            {
                return newCanvas;
            }

            for (int i = 0; i < FieldWidth * FieldHeight; i++)
            {


                Rectangle rect = (Rectangle)toClone.Children[i];
                Rectangle clonedRect = new()
                {
                    Fill = rect.Fill.Clone(),
                    Stroke = rect.Stroke.Clone(),
                    StrokeThickness = rect.StrokeThickness,
                    Width = rect.Width,
                    Height = rect.Height
                };
                newCanvas.Children.Add(clonedRect);
            }

            return newCanvas;
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

                    //while (Sessions.Count > 0)

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



        public Task Update()
        {

            MovePlayer(player1, client.Player1Coorditantes[0] * 50, client.Player1Coorditantes[1] * 50, player1OccupiedCells);
            MovePlayer(player2, client.Player2Coorditantes[0] * 50, client.Player2Coorditantes[1] * 50, player2OccupiedCells);

            client.GameState[client.Player1Coorditantes[0], client.Player1Coorditantes[1]] = 2;
            client.GameState[client.Player2Coorditantes[0], client.Player2Coorditantes[1]] = 2;

            Dispatcher.BeginInvoke(() =>
            {
                Canvas assembledCanvas = new();
                for (int i = 0; i < FieldWidth; i++)
                {
                    for (int j = 0; j < FieldHeight; j++)
                    {
                        int index1 = i, index2 = j;

                        if (i == 0)
                        {
                            index1++;
                        }

                        if (j == 0)
                        {
                            index2++;
                        }
                        int toDelete = index1 * index2;
                        switch (client.GameState[i, j])
                        {
                            //emptiness
                            case 0:
                                {
                                    DrawCell(assembledCanvas, i * 50, j * 50, Brushes.Beige, Brushes.Black);
                                    break;
                                }

                            //wall
                            case 1:
                                {
                                    break;
                                }
                            //player
                            case 2:
                                {
                                    DrawCell(assembledCanvas, i * 50, j * 50, Brushes.Aqua, Brushes.Black);
                                    break;
                                }

                            //bomb
                            case 3:
                                {

                                    break;
                                }

                            //buff
                            case 4:
                                {

                                    break;
                                }

                            //destroyable block
                            case 5:
                                {

                                    break;
                                }

                            default:
                                break;
                        }

                    }

                }

                if (!CompareCanvasChildren(ClonedCanvas, assembledCanvas))
                {
                    ClonedCanvas = CloneCanvasChildren(assembledCanvas);
                    Canvas tempVar = CloneCanvasChildren(ClonedCanvas);
                    Dispatcher.Invoke(() =>
                    {
                        GameCanvas = tempVar;
                    });
                }
            });


            return Task.CompletedTask;
        }

        private void CreateGrid()
        {
            for (int y = 0; y < FieldHeight; y++)
            {
                for (int x = 0; x < FieldWidth; x++)
                {
                    int index1 = y, index2 = x;
                    if (y == 0)
                    {
                        index1++;
                    }
                    if (x == 0)
                    {
                        index2++;
                    }
                    DrawCell(GameCanvas, x * CellSize, y * CellSize, Brushes.Gray, Brushes.Black);
                }
            }
        }

        private void DrawCell(Canvas canvas, double x, double y, SolidColorBrush fillColor, SolidColorBrush strokeColor)
        {
            var cellRect = new Rectangle
            {
                Fill = fillColor,
                Stroke = strokeColor,
                StrokeThickness = 1,
                Width = CellSize,
                Height = CellSize
            };

            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            canvas.Children.Add(cellRect);
        }

        private void CreatePlayers()
        {
            player1 = DrawPlayer(0, 0, Brushes.Blue, player1OccupiedCells);
            player2 = DrawPlayer((FieldWidth - 1) * CellSize, (FieldHeight - 1) * CellSize, Brushes.Red, player2OccupiedCells);
        }

        private Rectangle DrawPlayer(double x, double y, SolidColorBrush color, HashSet<Point> occupiedCells)
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

            OccupyCell(occupiedCells, GetCellCoordinates(x, y));

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

        private void MovePlayer(Rectangle player, double newX, double newY, HashSet<Point> occupiedCells)
        {
            this.Dispatcher.Invoke(() =>
            {
                Canvas.SetLeft(player, newX);
                Canvas.SetTop(player, newY);

                OccupyCell(occupiedCells, GetCellCoordinates(newX, newY));
            });

        }

        private bool IsCellInsideField(Point cell)
        {
            return cell.X >= 0 && cell.X < FieldWidth && cell.Y >= 0 && cell.Y < FieldHeight;
        }

        private bool IsCellOccupied(Point cell, HashSet<Point> occupiedCells)
        {
            return occupiedCells.Contains(cell);
        }

        private void OccupyCell(HashSet<Point> occupiedCells, Point cell)
        {
            occupiedCells.Clear();
            occupiedCells.Add(cell);
        }
    }
}
