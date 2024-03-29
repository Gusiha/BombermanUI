﻿using BomberGameUI;
using ClientBomberman;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace WpfApp1
{
    public partial class Game : Page
    {
        public Client ThisClient { get; set; }

        private const int FieldWidth = 13;
        private const int FieldHeight = 11;
        private const int CellSize = 50;
        private readonly HashSet<Point> player1OccupiedCells = new HashSet<Point>();
        private readonly HashSet<Point> player2OccupiedCells = new HashSet<Point>();
        private Rectangle player1;
        private Rectangle player2;
        private Canvas ClonedCanvas;
        private string ServerAddress;
        private int Port;
        private Image player1Image;
        private Image player2Image;


        public Game(string address, string clientPort)
        {
            Loaded += Loaded_Page;
            ServerAddress = address;
            Port = int.Parse(clientPort);

            //"192.168.0.102"
            ThisClient = new(IPAddress.Parse(ServerAddress), 65535, Port);
            ThisClient.LastPingTime = DateTime.Now;

            //TODO: Add try catch in a correct way, so that wrong IP address is handleled correctly.
            ThisClient.StartMessageLoop();
            ThisClient.SendTo(Encoding.UTF8.GetBytes("connect"));

            InitializeComponent();
            CreateGrid();
            CreatePlayers();

            ClonedCanvas = CloneCanvasChildren(GameCanvas, FieldWidth, FieldHeight);


            //KeyDown += OnKeyDown;
            UpdateWithTickrate();
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
        private Canvas CloneCanvasChildren(Canvas toClone, int xS, int yS)
        {
            Canvas newCanvas = new Canvas();

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

        private void CloneCanvasChildren(Canvas newCanvas, Rectangle[,] toClone)
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
                while (ThisClient.Status == "Ongoing")
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
                Debug.Indent();
                Debug.WriteLine("End");
                Debug.Unindent();
                Dispatcher.Invoke(() =>
                {
                    string status = ThisClient.Status;
                    GameEnd end = new(status);
                    this.NavigationService.Navigate(end);
                });
            });
            Task.WaitAll();
        }


        public Task Update()
        {
            MovePlayer(player1Image, ThisClient.Player1Coorditantes[0] * 50, ThisClient.Player1Coorditantes[1] * 50, player1OccupiedCells);
            MovePlayer(player2Image, ThisClient.Player2Coorditantes[0] * 50, ThisClient.Player2Coorditantes[1] * 50, player2OccupiedCells);

            /*client.GameState[client.Player1Coorditantes[0], client.Player1Coorditantes[1]] = 2;
            client.GameState[client.Player2Coorditantes[0], client.Player2Coorditantes[1]] = 2;*/

            Rectangle[,] assembledCanvas = new Rectangle[FieldWidth, FieldHeight];
            for (int i = 0; i < FieldWidth; i++)
            {
                for (int j = 0; j < FieldHeight; j++)
                {
                    switch (ThisClient.GameState[i, j])
                    {
                        //emptiness
                        case 0:
                            {
                                //rectIndex[i, j] = j;
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
                                //rectIndex[i, j] = j;
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
                                /*rectIndex[i, j] = j;
                                Dispatcher.Invoke(() =>
                                {
                                    assembledCanvas.Add(
                                    new()
                                    {
                                        Fill = Brushes.Aqua,
                                        Stroke = Brushes.Black,
                                        StrokeThickness = 0.5,
                                        Width = CellSize,
                                        Height = CellSize
                                    });
                                });*/
                                break;
                            }

                        //bomb
                        case 3:
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    assembledCanvas[i, j] =
                                    new()
                                    {
                                        Fill = Brushes.Turquoise,
                                        Stroke = Brushes.Yellow,
                                        StrokeThickness = 0.5,
                                        Width = CellSize,
                                        Height = CellSize
                                    };
                                });
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
                                //rectIndex[i, j] = j;
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

                DrawPlayer(ThisClient.Player1Coorditantes[0] * 50, ThisClient.Player1Coorditantes[1] * 50, 1, player1OccupiedCells);
                DrawPlayer(ThisClient.Player2Coorditantes[0] * 50, ThisClient.Player2Coorditantes[1] * 50, 2, player2OccupiedCells);

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
                StrokeThickness = 0.5,
                Width = CellSize,
                Height = CellSize
            };

            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            canvas.Children.Add(cellRect);
        }

        private void DrawCell(Canvas canvas, double x, double y, Rectangle cellRect)
        {
            Canvas.SetLeft(cellRect, x);
            Canvas.SetTop(cellRect, y);
            canvas.Children.Add(cellRect);
        }

        private void CreatePlayers()
        {
            player1Image = DrawPlayer(0, 0, 1, player1OccupiedCells);
            player2Image = DrawPlayer((FieldWidth - 1) * CellSize, (FieldHeight - 1) * CellSize, 2, player2OccupiedCells);

            //player1 = DrawPlayer(0, 0, Brushes.Blue, player1OccupiedCells);
            //player2 = DrawPlayer((FieldWidth - 1) * CellSize, (FieldHeight - 1) * CellSize, Brushes.Red, player2OccupiedCells);
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

        private Image DrawPlayer(double x, double y, int number, HashSet<Point> occupiedCells)
        {
            string BodyName = "Body" + number;
            string fuck = System.IO.Path.GetPathRoot(Directory.GetCurrentDirectory());
            Image BodyImage = new Image
            {
                Width = 50,
                Height = 50,
                Name = BodyName,
                Source = new BitmapImage(new Uri(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Assets", $"Body{number}.png")))
            };

            Canvas.SetLeft(BodyImage, x);
            Canvas.SetTop(BodyImage, y);

            GameCanvas.Children.Add(BodyImage);

            OccupyCell(occupiedCells, GetCellCoordinates(x, y));

            return BodyImage;
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
            await ThisClient.SendCommand(key.Key);
        }

        private void MovePlayer(Image player, double newX, double newY, HashSet<Point> occupiedCells)
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
