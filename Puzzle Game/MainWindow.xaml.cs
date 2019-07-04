using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Puzzle_Game
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool _isDragging = false;
        private Point _lastPos;

        // Empty pos
        private int _emptyI;
        private int _emptyJ;

        // Last pos
        private int _lastI;
        private int _lastJ;

        // Count number of moves
        int number_of_moves = 0;

        // Var get image cropped and shuffle
        readonly Image[,] _images = new Image[3, 3];

        // Save images origin after cropped
        private readonly List<Image> _samples = new List<Image>();

        // Var path image
        private string pathImg;

        // Create new playing time
        private static readonly DateTime now = DateTime.Now.Date;
        private DateTime time = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);

        // Call func interval = 1s
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            playingTime.Text = time.ToString("mm:ss");
            time = time.AddSeconds(1);
        }

        readonly DispatcherTimer dispatcherTimer = new DispatcherTimer();

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);

            string path = AppDomain.CurrentDomain.BaseDirectory + "/state.txt";

            // Check open file
            bool flag = false;

            if (File.Exists(path))
            {
                MessageBoxResult result = MessageBox.Show("Found old plays, do you want to continue?", "", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    flag = true;
                }
            }

            if (flag)
            {
                // Read state from file
                using (StreamReader sr = File.OpenText(path))
                {
                    String s;

                    int i = 0, j = 0;

                    while ((s = sr.ReadLine()) != null)
                    {
                        if (s.Contains("path"))
                        {
                            string[] str = s.Split('|');
                            pathImg = str[1];
                            if (!CropImage(pathImg)) { return; };
                        }
                        else if (s.Contains("time"))
                        {
                            string[] str = s.Split('|');
                            time = DateTime.Parse(str[1]);
                        }
                        else if (s.Contains("number of moves"))
                        {
                            string[] str = s.Split('|');
                            number_of_moves = int.Parse(str[1]);
                        }
                        else
                        {
                            string[] str = s.Split(',');

                            int x = int.Parse(str[0]);
                            int y = int.Parse(str[1]);

                            int pos = 0;

                            if (x == 2 && y == 2) { _emptyI = i; _emptyJ = j; }

                            if (y == 0) { pos = x; }
                            else if (y == 1) { pos = 3 + x; }
                            else if (y == 2) { pos = 6 + x; }

                            _images[i, j] = _samples[pos];

                            content.Children.Add(_images[i, j]);

                            Canvas.SetLeft(_images[i, j], i * 160);
                            Canvas.SetTop(_images[i, j], j * 160);

                            i++; if (i == 3) { i = 0; j++; if (j == 3) j = 0; }
                        }
                    }

                    _samples.Clear();
                }
            }
            else
            {
                var dialog = new OpenFileDialog { Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png" };

                if (dialog.ShowDialog() == true) { pathImg = dialog.FileName; }
                else return;

                if (!CropImage(pathImg)) { return; };

                // Random image
                Random rng = new Random();

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        int pos = rng.Next(_samples.Count);

                        // Get pos of empty image after random
                        var tag = _samples[pos].Tag as Tuple<int, int>;

                        if (tag.Item1 == 2 && tag.Item2 == 2)
                        {
                            _emptyI = i;
                            _emptyJ = j;
                        }

                        _images[i, j] = _samples[pos];

                        content.Children.Add(_images[i, j]);

                        Canvas.SetLeft(_images[i, j], i * 160);
                        Canvas.SetTop(_images[i, j], j * 160);

                        _samples.RemoveAt(pos);
                    }
                }
            }

            numberOfMoves.Text = number_of_moves.ToString();

            dispatcherTimer.Start();
        }

        private bool CropImage(string pathImg)
        {
            try
            {
                var image = new BitmapImage(new Uri(pathImg));
                finalImage.Source = image;

                var width = image.PixelWidth / 3;
                var height = image.PixelHeight / 3;

                // Loop to crop image
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if ((i == 2) && (j == 2))
                        {
                            _samples.Add(new Image()
                            {
                                Width = 160,
                                Height = 160,
                                Tag = new Tuple<int, int>(i, j)
                            });
                        }
                        else
                        {
                            var part = new CroppedBitmap(image, new Int32Rect(i * width, j * height, width, height));

                            // Save image after cropped
                            _samples.Add(new Image()
                            {
                                Source = part,
                                Width = 160,
                                Height = 160,
                                Tag = new Tuple<int, int>(i, j)
                            });
                        }
                    }
                }

                return true;
            }
            catch (Exception e) { MessageBox.Show(e.Message); return false; }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is Image)
            {
                content.CaptureMouse();

                _lastPos = e.GetPosition(content);

                int i = ((int)_lastPos.X - 25) / 160;
                int j = ((int)_lastPos.Y - 25) / 160;

                _lastI = i;
                _lastJ = j;
                _isDragging = true;
            }
        }

        private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;

            content.ReleaseMouseCapture();

            var curPos = e.GetPosition(this);

            var i = ((int)curPos.X - 25) / 160;
            var j = ((int)curPos.Y - 25) / 160;

            if ((_lastI == _emptyI + 1) || (_lastI == _emptyI - 1) || (_lastJ == _emptyJ + 1) || (_lastJ == _emptyJ - 1))
            {
                _emptyI = _lastI;
                _emptyJ = _lastJ;

                Canvas.SetLeft(_images[_lastI, _lastJ], i * 160);
                Canvas.SetTop(_images[_lastI, _lastJ], j * 160);

                Canvas.SetLeft(_images[i, j], _lastI * 160);
                Canvas.SetTop(_images[i, j], _lastJ * 160);

                // Swap _images
                var temp = _images[_lastI, _lastJ];
                _images[_lastI, _lastJ] = _images[i, j];
                _images[i, j] = temp;

                if (i != _lastI || j != _lastJ) { numberOfMoves.Text = (++number_of_moves).ToString(); }

                CheckWin();
            }
            else
            {
                Canvas.SetLeft(_images[_lastI, _lastJ], _lastI * 160);
                Canvas.SetTop(_images[_lastI, _lastJ], _lastJ * 160);
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var curPos = e.GetPosition(content);

                var dx = curPos.X - _lastPos.X;
                var dy = curPos.Y - _lastPos.Y;

                var newLeft = Canvas.GetLeft(_images[_lastI, _lastJ]) + dx;
                var newTop = Canvas.GetTop(_images[_lastI, _lastJ]) + dy;

                if (curPos.X < 0)
                    newLeft = 0;
                else if (newLeft + _images[_lastI, _lastJ].ActualWidth > 480)
                    newLeft = 480 - _images[_lastI, _lastJ].ActualWidth;

                if (curPos.Y < 0)
                    newTop = 0;
                else if (newTop + _images[_lastI, _lastJ].ActualHeight > 480)
                    newTop = 480 - _images[_lastI, _lastJ].ActualHeight;

                if (newLeft > 0 && newTop > 0)
                {
                    Canvas.SetLeft(_images[_lastI, _lastJ], newLeft);
                    Canvas.SetTop(_images[_lastI, _lastJ], newTop);
                }

                _lastPos = curPos;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();

            string path = AppDomain.CurrentDomain.BaseDirectory + "/state.txt";

            using (StreamWriter sw = File.CreateText(path))
            {
                sw.WriteLine("path | " + pathImg);
                sw.WriteLine("time | " + time);
                sw.WriteLine("number of moves | " + number_of_moves);

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        var tag = _images[i, j].Tag as Tuple<int, int>;
                        sw.WriteLine(tag.Item1 + "," + tag.Item2);
                    }
                }
            }

            MessageBoxResult result = MessageBox.Show("Do you want to exit?", "Exit notification", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                MessageBoxResult result1 = MessageBox.Show("Are you sure?", "Exit notification", MessageBoxButton.YesNo);
                if (result1 == MessageBoxResult.Yes)
                    this.Close();
            }

            dispatcherTimer.Start();
        }

        private void UpPadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_emptyJ > 0)
            {
                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ - 1], _emptyJ * 160);

                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ + 1 * 160);

                // Swap _images
                var temp = _images[_emptyI, _emptyJ];
                _images[_emptyI, _emptyJ] = _images[_emptyI, _emptyJ - 1];
                _images[_emptyI, _emptyJ - 1] = temp;

                _emptyJ--;

                numberOfMoves.Text = (++number_of_moves).ToString();

                CheckWin();
            }
        }

        private void DownPadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_emptyJ < 2)
            {
                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ + 1], _emptyJ * 160);

                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ - 1 * 160);

                // Swap _images
                var temp = _images[_emptyI, _emptyJ];
                _images[_emptyI, _emptyJ] = _images[_emptyI, _emptyJ + 1];
                _images[_emptyI, _emptyJ + 1] = temp;

                _emptyJ++;

                numberOfMoves.Text = (++number_of_moves).ToString();

                CheckWin();
            }
        }

        private void LeftPadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_emptyI > 0)
            {
                Canvas.SetLeft(_images[_emptyI - 1, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ * 160);

                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI + 1 * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ * 160);

                // Swap _images
                var temp = _images[_emptyI, _emptyJ];
                _images[_emptyI, _emptyJ] = _images[_emptyI - 1, _emptyJ];
                _images[_emptyI - 1, _emptyJ] = temp;

                _emptyI--;

                numberOfMoves.Text = (++number_of_moves).ToString();

                CheckWin();
            }
        }

        private void RightPadButton_Click(object sender, RoutedEventArgs e)
        {
            if (_emptyI < 2)
            {
                Canvas.SetLeft(_images[_emptyI + 1, _emptyJ], _emptyI * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ * 160);

                Canvas.SetLeft(_images[_emptyI, _emptyJ], _emptyI + 1 * 160);
                Canvas.SetTop(_images[_emptyI, _emptyJ], _emptyJ * 160);

                // Swap _images
                var temp = _images[_emptyI, _emptyJ];
                _images[_emptyI, _emptyJ] = _images[_emptyI + 1, _emptyJ];
                _images[_emptyI + 1, _emptyJ] = temp;

                _emptyI++;

                numberOfMoves.Text = (++number_of_moves).ToString();

                CheckWin();
            }
        }

        private void CheckWin()
        {
            bool flag = true;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    var tag = _images[i, j].Tag as Tuple<int, int>;

                    if (tag.Item1 != i || tag.Item2 != j)
                    {
                        flag = false;
                        break;
                    }
                }
            }

            if (flag) { MessageBox.Show("You win!"); }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up: UpPadButton_Click(this, new RoutedEventArgs()); break;
                case Key.Down: DownPadButton_Click(this, new RoutedEventArgs()); break;
                case Key.Left: LeftPadButton_Click(this, new RoutedEventArgs()); break;
                case Key.Right: RightPadButton_Click(this, new RoutedEventArgs()); break;
                default: break;
            }
        }
    }
}
