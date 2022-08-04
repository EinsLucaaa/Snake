using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CsvHelper;

namespace PraktikantLuca
{
    public partial class MainWindow : Window
    {
        public enum SnakeDirection
        {
            Left,
            Right,
            Up,
            Down
        }

        private const int SnakeSquareSize = 20;

        private const int SnakeStartLength = 3;
        private const int SnakeStartSpeed = 450;
        private const int SnakeSpeedThreshold = 100;


        private readonly ContextMenu contextMenu;
        private readonly SolidColorBrush foodBrush = Brushes.Red;

        private readonly DispatcherTimer gameTickTimer = new DispatcherTimer();


        private readonly Random random = new Random();
        private readonly SolidColorBrush snakeHeadBrush;
        private readonly List<SnakePart> snakeParts = new List<SnakePart>();
        private readonly IDictionary<string, int> users = new Dictionary<string, int>();

        private int currentScore;
        private SolidColorBrush defaultBlack = Brushes.Black;
        private SolidColorBrush defaultWhite = Brushes.White;
        private SolidColorBrush snakeBodyBrush = Brushes.LightBlue;
        private SnakeDirection snakeDirection = SnakeDirection.Right;

        private UIElement snakeFood;
        private int snakeLength;

        public SolidColorBrush troxBlue = (SolidColorBrush)new BrushConverter().ConvertFrom("#0550a0");

        private string userName;


        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
            snakeHeadBrush = troxBlue;

            contextMenu = FindResource("btnClose") as ContextMenu;
            LoadData();
        }

        public ObservableCollection<User> HighscoreList { get; set; } = new ObservableCollection<User>();


        private void AddUser()
        {
            if (!users.ContainsKey(userName)) users.Add(userName, currentScore);
        }

        private void UpdateUser()
        {
            if (currentScore > users[userName]) users[userName] = currentScore;
        }

        private void LoadData()
        {
            using var streamReader = File.OpenText("C:/Users/Luca Arians/Documents/Projekt/users.csv");
            using var csvReader = new CsvReader(streamReader, CultureInfo.CurrentCulture);

            var usersData = csvReader.GetRecords<User>();

            foreach (var user in usersData) users.Add(user.UserName, user.Highscore);
        }

        private void LoadHighScores()
        {
            var mySortedList = users.OrderBy(d => d.Value).ToList();

            listBox1.Items.Clear();
            foreach (var item in mySortedList.OrderByDescending(x => x.Value))
                if (listBox1.Items.Count < 5)
                    listBox1.Items.Add(item.Key + "       " + item.Value);
        }

        private void UpdatePlayerData()
        {
            using var mem = new MemoryStream();
            using var writer = new StreamWriter("C:/Users/Luca Arians/Documents/Projekt/users.csv");
            using var csvWriter = new CsvWriter(writer, CultureInfo.CurrentCulture);

            csvWriter.WriteField("UserName");
            csvWriter.WriteField("Highscore");
            csvWriter.NextRecord();

            foreach (var user in users)
            {
                csvWriter.WriteField(user.Key);
                csvWriter.WriteField(user.Value);
                csvWriter.NextRecord();
            }

            writer.Flush();
            var res = Encoding.UTF8.GetString(mem.ToArray());
        }

        public void Window_ContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            bdrDied.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;
            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
        }

        private void btnColorSelector_Blue(object sender, RoutedEventArgs eventArgs)
        {
            defaultWhite = Brushes.Blue;
            defaultBlack = Brushes.DarkBlue;
            DrawGameArea();

            SnakeCanva.Focus();
            contextMenu.ReleaseMouseCapture();
        }


        private void btnColorSelector_Default(object sender, RoutedEventArgs eventArgs)
        {
            defaultWhite = Brushes.White;
            defaultBlack = Brushes.Black;
            DrawGameArea();

            SnakeCanva.Focus();
            contextMenu.ReleaseMouseCapture();
        }

        private void btnColorSelector_Yellow(object sender, RoutedEventArgs eventArgs)
        {
            defaultWhite = Brushes.LightYellow;
            defaultBlack = Brushes.Yellow;
            DrawGameArea();

            SnakeCanva.Focus();
            contextMenu.ReleaseMouseCapture();
        }

        private void btnLoginSucces_Click(object sender, RoutedEventArgs e)
        {
            userName = login.Text;
            bdrLogi.Visibility = Visibility.Collapsed;
            bdrWelcomeMessage.Visibility = Visibility.Visible;

            AddUser();
        }

        private void btnColorSelector_Green(object sender, RoutedEventArgs eventArgs)
        {
            defaultWhite = Brushes.LightGreen;
            defaultBlack = Brushes.DarkGreen;
            DrawGameArea();

            SnakeCanva.Focus();
            contextMenu.ReleaseMouseCapture();
        }

        private void btnGameStart_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame();
        }

        // Is triggered when an input is made on the keyboard
        public void Window_KeyUp(object sender, KeyEventArgs e)
        {
            var cm = FindResource("btnClose") as ContextMenu;
            var originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right;
                    break;
                case Key.Space:
                    cm.IsOpen = false;
                    Keyboard.Focus(SnakeCanva);
                    if (!gameTickTimer.IsEnabled)

                        StartNewGame();
                    break;
            }

            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }

        // Draws the surface of the playing field
        private void DrawGameArea()
        {
            var doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            var rowCounter = 0;
            var nextIsOdd = false;


            snakeBodyBrush = Brushes.LightBlue;

            var troxBlue = new SolidColorBrush();
            troxBlue = (SolidColorBrush)new BrushConverter().ConvertFrom("#0550a0");

            while (doneDrawingBackground == false)
            {
                var rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Stroke = Brushes.Black,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? defaultBlack : defaultWhite
                };

                var snakeCanva = (Canvas)FindName("SnakeCanva");


                snakeCanva.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if (nextX >= snakeCanva.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = rowCounter % 2 != 0;
                }

                if (nextY >= SnakeCanva.ActualHeight)
                    doneDrawingBackground = true;
            }
        }


        private void DrawSnake()
        {
            foreach (var snakePart in snakeParts)
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush
                    };
                    SnakeCanva.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
        }

        private void MoveSnake()
        {
            if (gameTickTimer.IsEnabled)
                while (snakeParts.Count >= snakeLength)
                {
                    SnakeCanva.Children.Remove(snakeParts[0].UiElement);
                    snakeParts.RemoveAt(0);
                }
            else
                return;


            foreach (var snakePart in snakeParts)
            {
                (snakePart.UiElement as Rectangle).Fill = snakeBodyBrush;
                snakePart.IsHead = false;
            }


            var snakeHead = snakeParts[snakeParts.Count - 1];
            var nextX = snakeHead.Position.X;
            var nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }


            snakeParts.Add(new SnakePart
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });

            DrawSnake();

            DoCollisionCheck();
        }

        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();

            if (currentScore >= 10) snakeBodyBrush = Brushes.BlueViolet;

            if (currentScore >= 20) snakeBodyBrush = Brushes.Gold;
        }

        private void StartNewGame()
        {
            snakeBodyBrush = Brushes.LightBlue;

            bdrWelcomeMessage.Visibility = Visibility.Collapsed;
            bdrDied.Visibility = Visibility.Collapsed;
            bdrHighscoreList.Visibility = Visibility.Collapsed;

            // Remove potential dead snake parts and leftover food
            foreach (var snakeBodyPart in snakeParts)
                if (snakeBodyPart.UiElement != null)
                    SnakeCanva.Children.Remove(snakeBodyPart.UiElement);
            snakeParts.Clear();
            if (snakeFood != null)
                SnakeCanva.Children.Remove(snakeFood);

            // Reset stuff
            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);

            // Draw the snake again and some new food
            DrawSnake();
            DrawSnakeFood();

            // Update status
            UpdateGameStatus();

            // Go        
            gameTickTimer.IsEnabled = true;
        }

        private Point GetNextFoodPosition()
        {
            var maxX = (int)(SnakeCanva.ActualWidth / SnakeSquareSize);
            var maxY = (int)(SnakeCanva.ActualHeight / SnakeSquareSize);
            var foodX = random.Next(0, maxX) * SnakeSquareSize;
            var foodY = random.Next(0, maxY) * SnakeSquareSize;

            foreach (var snakePart in snakeParts)
                if (snakePart.Position.X == foodX && snakePart.Position.Y == foodY)
                    return GetNextFoodPosition();

            return new Point(foodX, foodY);
        }

        private void DrawSnakeFood()
        {
            var foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse
            {
                Width = SnakeSquareSize,
                Stroke = Brushes.Black,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };
            SnakeCanva.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }

        private void DoCollisionCheck()
        {
            var snakeHead = snakeParts[snakeParts.Count - 1];

            if (snakeHead.Position.X == Canvas.GetLeft(snakeFood) && snakeHead.Position.Y == Canvas.GetTop(snakeFood))
            {
                EatSnakeFood();
                return;
            }

            if (snakeHead.Position.Y < 0 || snakeHead.Position.Y >= SnakeCanva.ActualHeight ||
                snakeHead.Position.X < 0 || snakeHead.Position.X >= SnakeCanva.ActualWidth)
                EndGame();

            foreach (var snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
                if (snakeHead.Position.X == snakeBodyPart.Position.X &&
                    snakeHead.Position.Y == snakeBodyPart.Position.Y)
                    EndGame();
        }

        private void EatSnakeFood()
        {
            snakeLength++;
            currentScore++;
            var timerInterval = Math.Max(SnakeSpeedThreshold,
                (int)gameTickTimer.Interval.TotalMilliseconds - currentScore * 2);
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            SnakeCanva.Children.Remove(snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
            UpdateUser();
        }

        private void UpdateGameStatus()
        {
            tbStatusScore.Text = currentScore.ToString();
            tbStatusSpeed.Text = gameTickTimer.Interval.TotalMilliseconds.ToString();
        }

        private void EndGame()
        {
            UpdatePlayerData();
            SystemSounds.Beep.Play();
            gameTickTimer.IsEnabled = false;
            DrawGameArea();

            tbFinalescore.Text = currentScore + " | " + users[userName];


            bdrDied.Visibility = Visibility.Visible;
            UpdatePlayerData();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void BootScreen_Click(object sender, KeyEventArgs eventArgs)
        {
            if (eventArgs.Equals(Key.Space)) StartNewGame();
        }

        private void DeathScreen_Click(object sender, KeyEventArgs eventArgs)
        {
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var cm = FindResource("btnClose") as ContextMenu;
            //cm.PlacementTarget = sender as Button;
            cm.IsOpen = true;
        }

        private void BtnScores_Click(object sender, RoutedEventArgs e)
        {
            LoadHighScores();
            bdrHighscoreList.Visibility = Visibility.Visible;
        }

        private void login_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public class User
        {
            public string UserName { get; set; }
            public int Highscore { get; set; }
        }
    }
}