using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SquareGame
{
    class Game
    {
        public readonly int GRID_SIZE = 600;
        public readonly int MIN_CONNECTED = 2;
        public readonly int TIMEOUT = 5000;

        private Random _random;
        private List<List<Square>> _squares;
        private Control.ControlCollection _controls;
        private List<List<Square>> _initialSquares;
        private List<Tuple<int, int>> _solution;

        public Game(int seed, Control.ControlCollection controls)
        {
            _random = new Random(seed);
            _squares = new List<List<Square>>();
            _initialSquares = new List<List<Square>>();
            _controls = controls;
            _solution = new List<Tuple<int, int>>();
        }

        public async Task<bool> Generate(int squareCount, int colors)
        {
            _squares.ForEach(l => l.ForEach(s => s.Hide()));
            _initialSquares.ForEach(l => l.ForEach(s => s.Hide()));

            _squares = new List<List<Square>>();
            _initialSquares = new List<List<Square>>();

            int squareWidth = GRID_SIZE / squareCount;

            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            while (true)
            {
                if (stopwatch.ElapsedMilliseconds > TIMEOUT)
                {
                    stopwatch.Stop();
                    return false;
                }
                List<List<Square>> squares = new List<List<Square>>();

                HashSet<int> realColors = new HashSet<int>();

                for (int y = 0; y < squareCount; y++)
                {
                    List<Square> row = new List<Square>();

                    for (int x = 0; x < squareCount; x++)
                    {
                        int color = _random.Next(0, colors);
                        realColors.Add(color);

                        row.Add(new Square(x, y, color, squareWidth, _controls, squares, this));
                        row[x].Freeze();
                        row[x].Show();
                    }
                    squares.Add(row);
                }

                if (colors != realColors.Count) continue;

                List<List<Square>> squaresCP = DeepCopy(squares);

                List<Tuple<int, int>> solution = new List<Tuple<int, int>>();

                bool gameWon = false;
                int clicks = 0;
                int tries = 0;
                int squared = squareCount * squareCount;
                while (clicks < squared / MIN_CONNECTED && tries < squared)
                {
                    int id = _random.Next(0, squared);
                    int x = id % squareCount;
                    int y = id / squareCount;

                    Square target = squares[y][x];
                    if (!target.visible) continue;

                    bool moved = await target.Click();

                    if (!moved)
                    {
                        tries += 1;
                        continue;
                    }
                    tries = 0;
                    clicks += 1;

                    solution.Add(new Tuple<int, int>(x, y));

                    if (GameWon(squares))
                    {
                        gameWon = true;
                        break;
                    }
                }

                if (gameWon)
                {
                    _squares = squaresCP;
                    _solution = solution;
                    break;
                }
            }

            _initialSquares = DeepCopy(_squares);
            _squares.ForEach(l => l.ForEach(s => s.Show()));

            stopwatch.Stop();
            return true;
        }
        public List<List<Square>> DeepCopy(List<List<Square>> squares)
        {
            List<List<Square>> squaresCP = new List<List<Square>>();

            int squareCount = squares.Count;

            for (int y = 0; y < squareCount; y++)
            {
                List<Square> rowCP = new List<Square>();

                for (int x = 0; x < squareCount; x++)
                {
                    rowCP.Add(new Square(x, y, squares[y][x].color, squares[y][x].width, _controls, squaresCP, this));
                }
                squaresCP.Add(rowCP);
            }
            return squaresCP;
        }
        public void Restore()
        {
            if (_initialSquares.Count == 0) return;

            _squares.ForEach(r => r.ForEach(s => s.Hide()));
            _squares = DeepCopy(_initialSquares);
            _squares.ForEach(l => l.ForEach(s => s.Show()));
        }
        public async Task ShowSolution()
        {
            Restore();
            if (_solution.Count == 0) return;
            _squares.ForEach(r => r.ForEach(s => s.Disable()));
            
            foreach (var t in _solution)
            {
                await Task.Delay(1000);
                await _squares[t.Item2][t.Item1].Click(true);
            }
            Restore();
        }
        public void TryMoveDown(List<List<Square>> squares)
        {
            int squareCount = squares.Count;

            for (int x = 0; x < squareCount; x++)
            {
                int count = 0;
                for (int y = squareCount - 1; y >= 0; y--)
                {
                    Square thatSquare = squares[y][x];
                    if (!thatSquare.visible) count += 1;
                    else if (count > 0)
                    {
                        Square targetSquare = squares[y + count][x];

                        thatSquare.Hide();
                        targetSquare.ChangeColor(thatSquare.color);
                        targetSquare.Show();
                    }
                }
            }
        }
        public void TryMoveRight(List<List<Square>> squares)
        {
            int squareCount = squares.Count;

            for (int x = 0; x < squareCount; x++)
            {
                bool empty = true;
                for (int y = 0; y < squareCount && empty; y++)
                {
                    if (squares[y][x].visible) empty = false;
                }
                if (empty)
                {
                    for (int yy = 0; yy < squareCount; yy++)
                    {
                        for (int xx = x; xx > 0; xx--)
                        {
                            if (squares[yy][xx - 1].visible)
                            {
                                squares[yy][xx - 1].Hide();
                                squares[yy][xx].ChangeColor(squares[yy][xx - 1].color);
                                squares[yy][xx].Show();
                            }
                        }
                    }
                }
            }

            // for (int y = 0; y < squareCount; y++)
            // {
            //     int count = 0;
            //     for (int x = squareCount - 1; x >= 0; x--)
            //     {
            //         Square thatSquare = squares[y][x];
            //         if (!thatSquare.visible) count += 1;
            //         else if (count > 0)
            //         {
            //             Square targetSquare = squares[y][x + count];
            // 
            //             thatSquare.Hide();
            //             targetSquare.ChangeColor(thatSquare.color);
            //             targetSquare.Show();
            //         }
            //     }
            // }
        }
        public bool GameWon(List<List<Square>> squares)
        {
            return squares.All(r => r.All(s => !s.visible));
        }
        public bool GameLost(List<List<Square>> squares)
        {
            int squareCount = squares.Count;
            bool[,] visited = new bool[squareCount, squareCount];

            int recurse(int x, int y, int color)
            {
                if (x < 0 || y < 0 || x >= squareCount || y >= squareCount || visited[y, x]) return 0;

                Square target = squares[y][x];
                if (target.color != color || !target.visible) return 0;
                visited[y, x] = true;

                return 1 + recurse(x + 1, y, color) + recurse(x - 1, y, color) + recurse(x, y + 1, color) + recurse(x, y - 1, color);
            }

            for (int y = 0; y < squareCount; y++)
            {
                for (int x = 0; x < squareCount; x++)
                {
                    if (recurse(x, y, squares[y][x].color) >= MIN_CONNECTED) return false;
                }
            }
            return true;
        }
        public void TryGameFinish(List<List<Square>> squares)
        {
            if (GameWon(squares))
            {
                GameWonScreen gameWonScreen = new GameWonScreen();
                gameWonScreen.ShowDialog();
            }
            else if (GameLost(squares))
            {
                GameLostScreen gameLostScreen = new GameLostScreen();
                gameLostScreen.ShowDialog();
                Restore();
            }
        }
        public class Square
        {
            private readonly int OFFSET_X = 230;
            private readonly int OFFSET_Y = 30;

            public int x;
            public int y;
            public int width;
            public int color;
            public bool visible;
            private bool _disabled;
            public bool frozen;
            public Button _button;
            private Control.ControlCollection _controls;
            private List<List<Square>> _squares;
            private Game _game;
            public Square(int x, int y, int color, int width, Control.ControlCollection controls, List<List<Square>> squares, Game game)
            {
                this.x = x;
                this.y = y;
                this.width = width;
                this.color = color;

                _squares = squares;
                _controls = controls;
                _game = game;

                visible = false;
                frozen = false;
                _disabled = false;
            }
            private async void OnClick(object sender, EventArgs e)
            {
                await Click();
            }
            public async Task<bool> Click(bool slow = false)
            {
                int size = _squares.Count;
                bool[,] visited = new bool[size, size];
                List<Square> group = new List<Square>();

                void recurse(int x, int y)
                {
                    if (x < 0 || y < 0 || x >= size || y >= size || visited[y, x]) return;
                    visited[y, x] = true;

                    Square thatSquare = _squares[y][x];
                    if (thatSquare.color != color || !thatSquare.visible) return;
                    group.Add(thatSquare);
                    recurse(x + 1, y);
                    recurse(x - 1, y);
                    recurse(x, y + 1);
                    recurse(x, y - 1);
                }
                recurse(x, y);

                if (group.Count < _game.MIN_CONNECTED) return false;

                if (slow && !frozen)
                {
                    foreach (var s in group)
                    {
                        s._button.BackColor = System.Drawing.Color.Red;
                    }
                    await Task.Delay(1000);
                }
                group.ForEach(s => s.Hide());

                _game.TryMoveDown(_squares);
                _game.TryMoveRight(_squares);
                // _game.TryMoveDown(_squares);
                if (!frozen) _game.TryGameFinish(_squares);

                return true;
            }
            public void ChangeColor(int color)
            {
                this.color = color;
            }
            public void Show()
            {
                if (visible) return;
                visible = true;

                if (frozen) return;

                Array colorsArray = Enum.GetValues(typeof(System.Drawing.KnownColor));
                System.Drawing.KnownColor[] allColors = new System.Drawing.KnownColor[colorsArray.Length]; // 174 colors
                Array.Copy(colorsArray, allColors, colorsArray.Length);
                System.Drawing.Color backColor = System.Drawing.Color.FromKnownColor(allColors[color + 51]);

                _button = new Button
                {
                    Visible = true,
                    Text = "" + (char)(65 + color),
                    Size = new System.Drawing.Size(width, width),
                    Location = new System.Drawing.Point(OFFSET_X + x * width, OFFSET_Y + y * width),
                    BackColor = backColor,
                    Enabled = !_disabled
                };
                _button.Click += new EventHandler(OnClick);
                _controls.Add(_button);

            }
            public void Hide()
            {
                if (!visible) return;
                visible = false;

                if (frozen) return;

                _controls.Remove(_button);
            }
            public void Freeze()
            {
                frozen = true;
            }
            public void Unfreeze()
            {
                frozen = false;
            }
            public void Disable()
            {
                if (frozen) return;
                _disabled = true;
                if (visible) _button.Enabled = false;
            }
            public void Enable()
            {
                if (frozen) return;
                _disabled = false;
                if (visible) _button.Enabled = true;
            }
        }

    }
}
