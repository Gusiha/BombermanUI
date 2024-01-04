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

namespace YourNamespace
{
    public partial class MainWindow : Window
    {
        public Client client { get; set; }

        private const int FieldWidth = 13;
        private const int FieldHeight = 11;
        private const int CellSize = 50;
        private readonly HashSet<Point> player1OccupiedCells = new HashSet<Point>();
        private readonly HashSet<Point> player2OccupiedCells = new HashSet<Point>();
        private Rectangle player1;
        private Rectangle player2;

        public MainWindow()
        {
            client = new(IPAddress.Parse("192.168.0.102"), 65535);
            client.StartMessageLoop();
            client.SendTo(Encoding.UTF8.GetBytes("connect"));

            InitializeComponent();

            CreateGrid();
            CreatePlayers();

            KeyDown += OnKeyDown;
            //UpdateWithTickrate();
        }

        private void OnUpdate(object? sender, EventArgs e)
        {
            
        }

        public void UpdateWithTickrate()
        {
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

                    if (timer.ElapsedMilliseconds <= 1000)
                    {
                        Thread.Sleep((int)(1000 - timer.ElapsedMilliseconds));
                    }

                }
            });


        }


        public Task Update()
        {
            Task.Run(() =>
            {

                MovePlayer(player1, client.Player1Coorditantes[0], client.Player1Coorditantes[1], player1OccupiedCells);
                MovePlayer(player2, client.Player1Coorditantes[0], client.Player2Coorditantes[1], player2OccupiedCells);

                client.GameState[client.Player1Coorditantes[0], client.Player1Coorditantes[1]] = 2;
                client.GameState[client.Player2Coorditantes[0], client.Player2Coorditantes[1]] = 2;


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
                                    DrawCell(i * 50, j * 50, Brushes.Beige, Brushes.Black, toDelete);
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
                                    DrawCell(i * 50, j * 50, Brushes.Aqua, Brushes.Black, i * j);
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
                    DrawCell(x * CellSize, y * CellSize, Brushes.Gray, Brushes.Black, index1 * index2);
                }
            }
        }

        private void DrawCell(double x, double y, SolidColorBrush fillColor, SolidColorBrush strokeColor, int toDelete)
        {

            Dispatcher.Invoke(() =>
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
                try
                {
                    if (GameCanvas.Children[toDelete] != cellRect)
                    {
                        GameCanvas.Children[toDelete] = cellRect;
                    }
                }
                catch
                {
                    GameCanvas.Children.Add(cellRect);
                }
            });

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
