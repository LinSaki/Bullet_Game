using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
// By: Kaitlin Saqui (991723734) 
namespace ShapeAnimator
{
    public partial class MainWindow : Window
    {
        private string ActiveTool = "Circle";
        private Point initialClickPosition;
        private bool isFirstClick = true;
        private Brush currentColor = Brushes.PeachPuff;
        private readonly DispatcherTimer animationTimer;
        private readonly DispatcherTimer messageTimer;
        private readonly Random random = new Random();
        private readonly Dictionary<UIElement, Vector> shapeVelocities = new Dictionary<UIElement, Vector>();
        private readonly List<UIElement> bullets = new List<UIElement>();

        private bool isMovingLeft = false;
        private bool isMovingRight = false;
        private int killCount = 0;
        private int bulletCount = 1; // Initial number of bullets shot
        private bool isGameOver = false; // Flag to track if the game is over

        public MainWindow()
        {
            InitializeComponent();
            animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(30)
            };
            animationTimer.Tick += AnimationTimer_Tick;

            messageTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            messageTimer.Tick += MessageTimer_Tick;

            // Event handlers for keyboard input
            KeyDown += MainWindow_KeyDown;
            KeyUp += MainWindow_KeyUp;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                isMovingLeft = true;
            }
            else if (e.Key == Key.D)
            {
                isMovingRight = true;
            }
            else if (e.Key == Key.W)
            {
                ShootBullet();
            }
        }

        private void MainWindow_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                isMovingLeft = false;
            }
            else if (e.Key == Key.D)
            {
                isMovingRight = false;
            }
        }

        private void ShootBullet()
        {
            // Calculate bullet positions based on player position
            double playerCenterX = Canvas.GetLeft(PlayerImage) + (PlayerImage.ActualWidth / 2);
            double playerTop = Canvas.GetTop(PlayerImage) - 20; // Adjust the starting top position

            // Determine the number of bullets to shoot based on the kill count
            int numberOfBullets = (killCount / 5) + 1; // Add one bullet for every 5 kills, starting with one bullet

            // Calculate the spacing between bullets
            double bulletSpacing = 20; // Adjust as needed for spacing between bullets
            double totalWidth = (numberOfBullets - 1) * bulletSpacing;
            double startingLeft = playerCenterX - (totalWidth / 2);

            for (int i = 0; i < numberOfBullets; i++)
            {
                Image bullet = new Image
                {
                    Source = new BitmapImage(new Uri("C:/Sheridan/2024Winter/SEMESTER 4/PROG32356 - .NET Technologies using C#/Week6/ShapeAnimator/ShapeAnimator/Images/BulletWait.png", UriKind.Absolute)),
                    Width = 40,
                    Height = 20,
                    Tag = "Bullet"
                };

                double bulletLeft = startingLeft + (i * bulletSpacing) - (bullet.Width / 2);
                Canvas.SetLeft(bullet, bulletLeft);
                Canvas.SetTop(bullet, playerTop);

                ShapeCanvas.Children.Add(bullet);
                bullets.Add(bullet);
            }
        }



        private void MoveBullets()
        {
            List<UIElement> bulletsToRemove = new List<UIElement>();

            foreach (var bullet in new List<UIElement>(bullets))  // Iterate over a copy of bullets list
            {
                double top = Canvas.GetTop(bullet);
                top -= 10; // Bullet speed

                Canvas.SetTop(bullet, top);

                if (top < 0)
                {
                    bulletsToRemove.Add(bullet);
                }
                else
                {
                    CheckBulletCollisions(bullet);
                }
            }

            foreach (var bullet in bulletsToRemove)
            {
                ShapeCanvas.Children.Remove(bullet);
                bullets.Remove(bullet);
            }
        }

        private void CheckBulletCollisions(UIElement bullet)
        {
            List<UIElement> shapes = new List<UIElement>(ShapeCanvas.Children.OfType<UIElement>());

            List<UIElement> collidedBullets = new List<UIElement>();

            foreach (var shape in shapes)
            {
                if (shape is Ellipse || shape is Rectangle)
                {
                    if (IsBulletColliding(bullet, shape))
                    {
                        // Collect the collided bullet
                        collidedBullets.Add(bullet);

                        // Remove the shape only once
                        ShapeCanvas.Children.Remove(shape);
                        shapeVelocities.Remove(shape);
                        killCount++;
                    }
                }
            }

            // Update kill count display
            KillCountTextBlock.Text = $"Kill Count: {killCount}";

            // Remove all collided bullets
            foreach (var collidedBullet in collidedBullets)
            {
                ShapeCanvas.Children.Remove(collidedBullet);
                bullets.Remove(collidedBullet);
            }

            // Adjust bullet count if needed
            if (killCount > 3)
            {
                bulletCount = 2; // Increase bullet count
            }
        }

        private bool IsBulletColliding(UIElement bullet, UIElement shape)
        {
            if (!(bullet is Image bulletImage) || !(shape is FrameworkElement shapeElement))
            {
                return false;
            }

            Rect bulletRect = new Rect(Canvas.GetLeft(bulletImage), Canvas.GetTop(bulletImage), bulletImage.Width, bulletImage.Height);
            Rect shapeRect = new Rect(Canvas.GetLeft(shapeElement), Canvas.GetTop(shapeElement), shapeElement.ActualWidth, shapeElement.ActualHeight);

            return bulletRect.IntersectsWith(shapeRect);
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (isGameOver) return; // Stop updating if the game is over

            MovePlayer();

            List<UIElement> shapes = new List<UIElement>(ShapeCanvas.Children.OfType<UIElement>());

            foreach (var shape in shapes)
            {
                if (shape is Ellipse || shape is Rectangle)
                {
                    if (!shapeVelocities.TryGetValue(shape, out Vector velocity))
                    {
                        continue;
                    }

                    double left = Canvas.GetLeft(shape);
                    double top = Canvas.GetTop(shape);

                    left += velocity.X;
                    top += velocity.Y;

                    if (left <= 0 || left + ((FrameworkElement)shape).Width >= ShapeCanvas.ActualWidth)
                    {
                        velocity.X = -velocity.X;
                    }

                    if (top <= 0 || top + ((FrameworkElement)shape).Height >= ShapeCanvas.ActualHeight)
                    {
                        velocity.Y = -velocity.Y;
                    }

                    Canvas.SetLeft(shape, left);
                    Canvas.SetTop(shape, top);
                    shapeVelocities[shape] = velocity;

                    CheckCollisions(shape);

                    // Check for collision with the player
                    if (IsPlayerColliding(shape))
                    {
                        GameOver();
                        return;
                    }
                }
            }

            MoveBullets();
        }

        private void MovePlayer()
        {
            double playerSpeed = 10; // Adjust as needed

            if (isMovingLeft)
            {
                MovePlayerHorizontally(-playerSpeed);
            }
            else if (isMovingRight)
            {
                MovePlayerHorizontally(playerSpeed);
            }
        }

        private void MovePlayerHorizontally(double deltaX)
        {
            double left = Canvas.GetLeft(PlayerImage);
            left += deltaX;

            // Limit player movement within canvas bounds
            if (left < 0)
                left = 0;
            else if (left + PlayerImage.ActualWidth > ShapeCanvas.ActualWidth)
                left = ShapeCanvas.ActualWidth - PlayerImage.ActualWidth;

            Canvas.SetLeft(PlayerImage, left);
        }

        private void CheckCollisions(UIElement currentShape)
        {
            foreach (UIElement otherShape in ShapeCanvas.Children)
            {
                if (currentShape == otherShape) continue;

                if (currentShape is Ellipse && otherShape is Ellipse)
                {
                    if (IsColliding((Ellipse)currentShape, (Ellipse)otherShape))
                    {
                        BounceShapes(currentShape, otherShape);
                    }
                }
                else if (currentShape is Rectangle && otherShape is Rectangle)
                {
                    if (IsColliding((Rectangle)currentShape, (Rectangle)otherShape))
                    {
                        BounceShapes(currentShape, otherShape);
                    }
                }
                else if (currentShape is Ellipse && otherShape is Rectangle)
                {
                    if (IsColliding((Ellipse)currentShape, (Rectangle)otherShape))
                    {
                        BounceShapes(currentShape, otherShape);
                    }
                }
                else if (currentShape is Rectangle && otherShape is Ellipse)
                {
                    if (IsColliding((Rectangle)currentShape, (Ellipse)otherShape))
                    {
                        BounceShapes(currentShape, otherShape);
                    }
                }
            }
        }

        private bool IsColliding(Ellipse e1, Ellipse e2)
        {
            double left1 = Canvas.GetLeft(e1);
            double top1 = Canvas.GetTop(e1);
            double left2 = Canvas.GetLeft(e2);
            double top2 = Canvas.GetTop(e2);

            double centerX1 = left1 + e1.Width / 2;
            double centerY1 = top1 + e1.Height / 2;
            double centerX2 = left2 + e2.Width / 2;
            double centerY2 = top2 + e2.Height / 2;

            double dx = centerX2 - centerX1;
            double dy = centerY2 - centerY1;
            double distance = Math.Sqrt(dx * dx + dy * dy);
            double minDistance = (e1.Width + e2.Width) / 2;

            return distance < minDistance;
        }

        private bool IsColliding(Rectangle r1, Rectangle r2)
        {
            Rect rect1 = new Rect(Canvas.GetLeft(r1), Canvas.GetTop(r1), r1.Width, r1.Height);
            Rect rect2 = new Rect(Canvas.GetLeft(r2), Canvas.GetTop(r2), r2.Width, r2.Height);
            return rect1.IntersectsWith(rect2);
        }

        private bool IsColliding(Ellipse e, Rectangle r)
        {
            Rect rect = new Rect(Canvas.GetLeft(r), Canvas.GetTop(r), r.Width, r.Height);
            double centerX = Canvas.GetLeft(e) + e.Width / 2;
            double centerY = Canvas.GetTop(e) + e.Height / 2;
            double radius = e.Width / 2;

            double nearestX = Math.Max(rect.Left, Math.Min(centerX, rect.Right));
            double nearestY = Math.Max(rect.Top, Math.Min(centerY, rect.Bottom));

            double dx = centerX - nearestX;
            double dy = centerY - nearestY;

            return (dx * dx + dy * dy) < (radius * radius);
        }

        private bool IsColliding(Rectangle r, Ellipse e)
        {
            return IsColliding(e, r);
        }

        private void BounceShapes(UIElement shape1, UIElement shape2)
        {
            if (!shapeVelocities.ContainsKey(shape1))
            {
                AddShapeVelocity(shape1);
            }
            if (!shapeVelocities.ContainsKey(shape2))
            {
                AddShapeVelocity(shape2);
            }

            Vector velocity1 = shapeVelocities[shape1];
            Vector velocity2 = shapeVelocities[shape2];

            shapeVelocities[shape1] = velocity2;
            shapeVelocities[shape2] = velocity1;
        }

        private bool IsPlayerColliding(UIElement shape)
        {
            if (!(shape is FrameworkElement shapeElement))
                return false;

            Rect playerRect = new Rect(Canvas.GetLeft(PlayerImage), Canvas.GetTop(PlayerImage), PlayerImage.ActualWidth, PlayerImage.ActualHeight);
            Rect shapeRect = new Rect(Canvas.GetLeft(shapeElement), Canvas.GetTop(shapeElement), shapeElement.ActualWidth, shapeElement.ActualHeight);

            return playerRect.IntersectsWith(shapeRect);
        }

        private void GameOver()
        {
            isGameOver = true;
            animationTimer.Stop();

            // Change the player image to a game over image
            PlayerImage.Source = new BitmapImage(new Uri("C:/Sheridan/2024Winter/SEMESTER 4/PROG32356 - .NET Technologies using C#/Week6/ShapeAnimator/ShapeAnimator/Images/BulletGameOver.png", UriKind.Absolute));

            MessageBoxResult result = MessageBox.Show("Game Over! Would you like to play again?", "Game Over", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                RestartGame();
            }
            else
            {
                Close();
            }
        }

        private void RestartGame()
        {
            // Reset game state
            isGameOver = false;
            killCount = 0;
            bulletCount = 1;
            KillCountTextBlock.Text = $"Kill Count: {killCount}"; //update
            KillCountTextBlock.Visibility = Visibility.Hidden; // hide

            // Clear velocities and bullets list
            shapeVelocities.Clear();
            bullets.Clear();

            // Remove all shapes and bullets from the canvas except the player, KillCountTextBlock, and StartText
            List<UIElement> elementsToKeep = new List<UIElement> { PlayerImage, KillCountTextBlock, StartText };
            List<UIElement> elementsToRemove = new List<UIElement>();

            foreach (UIElement element in ShapeCanvas.Children)
            {
                if (!elementsToKeep.Contains(element))
                {
                    elementsToRemove.Add(element);
                }
            }

            foreach (UIElement element in elementsToRemove)
            {
                ShapeCanvas.Children.Remove(element);
            }

            // Reset the player image
            PlayerImage.Source = new BitmapImage(new Uri("C:/Sheridan/2024Winter/SEMESTER 4/PROG32356 - .NET Technologies using C#/Week6/ShapeAnimator/ShapeAnimator/Images/BulletStart.png", UriKind.Absolute));
            Canvas.SetLeft(PlayerImage, ShapeCanvas.ActualWidth / 2 - PlayerImage.ActualWidth / 2);
            Canvas.SetTop(PlayerImage, ShapeCanvas.ActualHeight - PlayerImage.ActualHeight - 15);

            // Ensure StartText is visible
            StartText.Visibility = Visibility.Collapsed;
            KillCountTextBlock.Visibility = Visibility.Visible;

            // Start the message timer
            messageTimer.Stop();

            // Restart the animation timer
            animationTimer.Start();
        }

        private void Circle_Click(object sender, RoutedEventArgs e)
        {
            ActiveTool = "Circle";
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            ActiveTool = "Rectangle";
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                currentColor = (Brush)new BrushConverter().ConvertFromString(clickedButton.Tag.ToString());
            }
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (isGameOver)
            {
                RestartGame();
            }
            else
            {
                MoveShapes();
                // Display the start message
                StartText.Visibility = Visibility.Visible;

                // Start the message timer
                messageTimer.Start();

                // Start the animation timer
                animationTimer.Start();
            }
        }

        private void MessageTimer_Tick(object sender, EventArgs e)
        {
            // Hide the start message
            StartText.Visibility = Visibility.Collapsed;

            // Stop the message timer
            messageTimer.Stop();
        }

        private void Pause_Click(object sender, RoutedEventArgs e)
        {
            animationTimer.Stop();
        }

        private void Step_Click(object sender, RoutedEventArgs e)
        {
            MoveShapes();
            animationTimer.Start();
        }

        private void MoveShapes()
        {
            foreach (UIElement shape in ShapeCanvas.Children)
            {
                if (shape is Ellipse || shape is Rectangle)
                {
                    AddShapeVelocity(shape);
                }
            }
        }

        private void ShapeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ActiveTool == "Circle" || ActiveTool == "Rectangle")
            {
                initialClickPosition = e.GetPosition(ShapeCanvas);
                isFirstClick = false;
            }
        }

        private void ShapeCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!isFirstClick)
            {
                Point finalClickPosition = e.GetPosition(ShapeCanvas);

                if (ActiveTool == "Circle")
                {
                    DrawCircle(initialClickPosition, finalClickPosition);
                }
                else if (ActiveTool == "Rectangle")
                {
                    DrawRectangle(initialClickPosition, finalClickPosition);
                }

                isFirstClick = true;
            }
        }

        private void DrawCircle(Point start, Point end)
        {
            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);

            Ellipse ellipse = new Ellipse
            {
                Width = width,
                Height = height,
                Stroke = currentColor
            };

            // Set the position to the minimum x and y coordinates
            double left = Math.Min(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);

            Canvas.SetLeft(ellipse, left);
            Canvas.SetTop(ellipse, top);

            ShapeCanvas.Children.Add(ellipse);
        }

        private void DrawRectangle(Point start, Point end)
        {
            double width = Math.Abs(end.X - start.X);
            double height = Math.Abs(end.Y - start.Y);

            Rectangle rectangle = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = currentColor
            };

            // Set the position to the initial click position, adjusting for width and height
            double left = Math.Min(start.X, end.X);
            double top = Math.Min(start.Y, end.Y);

            Canvas.SetLeft(rectangle, left);
            Canvas.SetTop(rectangle, top);

            ShapeCanvas.Children.Add(rectangle);
        }

        private void AddShapeVelocity(UIElement shape)
        {
            if (!shapeVelocities.ContainsKey(shape))
            {
                double angle = random.NextDouble() * 2 * Math.PI;
                Vector velocity = new Vector(Math.Cos(angle), Math.Sin(angle)) * 5; // Speed factor
                shapeVelocities[shape] = velocity;
            }
        }

        private void Shoot_Click(object sender, RoutedEventArgs e)
        {
            ShootBullet();
        }
    }
}