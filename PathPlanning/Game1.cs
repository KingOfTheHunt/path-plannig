using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PathPlanning
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private SpriteFont _font;
        private int _mapWidth, _mapHeight, _cellSize, _boderSize;
        private bool[] _obstacleMap;
        private Color _color = Color.Blue;
        private MouseState _previousMouseState, _currentMouseState;
        private KeyboardState _previousKeyboardState, _currentKeyboardState;
        private int _startX, _startY, _endX, _endY;
        private int[] _distances;
        private List<KeyValuePair<int, int>> _path;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here
            _boderSize = 4;
            _cellSize = 32;
            _mapWidth = _graphics.PreferredBackBufferWidth / _cellSize;
            _mapHeight = _graphics.PreferredBackBufferHeight / _cellSize;
            _obstacleMap = new bool[_mapWidth * _mapHeight];
            _distances = new int[_mapWidth * _mapHeight];
            _path = new();

            _startX = 1;
            _startY = 2;
            _endX = 12;
            _endY = 8;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _font = Content.Load<SpriteFont>("font");
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here
            _previousMouseState = _currentMouseState;
            _currentMouseState = Mouse.GetState();
            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            if (_previousMouseState.LeftButton == ButtonState.Pressed &&
                    _currentMouseState.LeftButton == ButtonState.Released)
            {
                int mousePositionX = Math.Clamp(_currentMouseState.Position.X / _cellSize, 0, 
                    _mapWidth);
                int mousePositionY = Math.Clamp(_currentMouseState.Position.Y / _cellSize, 0,
                    _mapHeight);
                _obstacleMap[ToOneDimension(mousePositionX, mousePositionY)] =
                       !_obstacleMap[ToOneDimension(mousePositionX, mousePositionY)];
            }

            if (_previousKeyboardState.IsKeyDown(Keys.LeftShift) &&
                    _previousMouseState.RightButton == ButtonState.Pressed &&
                    _currentMouseState.RightButton == ButtonState.Released)
            {
                _endX = _currentMouseState.Position.X / _cellSize;
                _endY = _currentMouseState.Position.Y / _cellSize;
            }
            else if (_previousMouseState.RightButton == ButtonState.Pressed &&
                    _currentMouseState.RightButton == ButtonState.Released)
            {
                _startX = _currentMouseState.Position.X / _cellSize;
                _startY = _currentMouseState.Position.Y / _cellSize;
            }

            UpdateMap();
            WavePropagation();
            Pathfinding();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();
            for (int x = 0; x < _mapWidth; x++)
                for (int y = 0; y < _mapHeight; y++)
                {
                    if (_obstacleMap[ToOneDimension(x, y)] == true)
                        _color = Color.Gray;
                    else
                        _color = Color.Blue;

                    if (x == _startX && y == _startY)
                        _color = Color.Green;

                    if (x == _endX && y == _endY)
                        _color = Color.Red;

                    _spriteBatch.FillRectangle(x * _cellSize, y * _cellSize, _cellSize - _boderSize, _cellSize - _boderSize, _color);
                    _spriteBatch.DrawString(_font, _distances[ToOneDimension(x, y)].ToString(),
                        new Vector2(x * _cellSize, y * _cellSize), Color.White);
                }

            bool isFirstPoint = true;
            int originalX = 0, originalY = 0;
            foreach (var path in _path)
            {
                if (isFirstPoint)
                {
                    originalX = path.Key;
                    originalY = path.Value;
                    isFirstPoint = false;
                }
                else
                {
                    _spriteBatch.DrawLine(originalX * _cellSize + (_cellSize - _boderSize) / 2,
                        originalY * _cellSize + (_cellSize - _boderSize) / 2,
                        path.Key * _cellSize + (_cellSize - _boderSize) / 2,
                        path.Value * _cellSize + (_cellSize - _boderSize) / 2,
                        Color.Yellow);

                    originalX = path.Key;
                    originalY = path.Value;
                }

            }
            _spriteBatch.End();
            base.Draw(gameTime);
        }

        // Converte a posição do mouse para um indíce de um array.
        private int ToOneDimension(Point mouseLocation) =>
            ToOneDimension(mouseLocation.X, mouseLocation.Y);

        private int ToOneDimension(int x, int y) =>
            y * _mapWidth + x;

        // Este método vai calcular a distancia do ponto de chegada ao ponto de partida.
        // Ele vai pegar um node do mapa e vai verificar os vizinhos válidos.
        private void WavePropagation()
        {
            // Os elementos da tupla representam as coordenadas (x, y) e a distância.
            var nodes = new List<Tuple<int, int, int>>
            {
                new (_endX, _endY, 0)
            };

            while (nodes.Any())
            {
                // A cada iteração novos nodes serão descobertos, então uma novas lista
                // será criada para manter o controle dos novos nodes.
                var newNodes = new List<Tuple<int, int, int>>();

                foreach (var node in nodes)
                {
                    var x = node.Item1;
                    var y = node.Item2;
                    var distance = node.Item3;

                    _distances[ToOneDimension(x, y)] = distance + 1;

                    // Checando se os nodes vizinhos são vizinhos válidos.
                    // Leste
                    if ((x + 1) < _mapWidth && _distances[ToOneDimension(x + 1, y)] == 0)
                    {
                        newNodes.Add(new(x + 1, y, distance + 1));
                    }
                    // Oeste
                    if ((x - 1) >= 0 && _distances[ToOneDimension(x - 1, y)] == 0)
                    {
                        newNodes.Add(new(x - 1, y, distance + 1));
                    }
                    // Norte
                    if ((y - 1) >= 0 && _distances[ToOneDimension(x, y - 1)] == 0)
                    {
                        newNodes.Add(new(x, y - 1, distance + 1));
                    }
                    // Sul
                    if ((y + 1) < _mapHeight && _distances[ToOneDimension(x, y + 1)] == 0)
                    {
                        newNodes.Add(new(x, y + 1, distance + 1));
                    }
                }

                newNodes.Sort();
                newNodes = newNodes.Distinct().ToList();

                nodes.Clear();
                nodes.AddRange(newNodes);
            }
        }

        private void UpdateMap()
        {
            for (int x = 0; x < _mapWidth; x++)
                for (int y = 0; y < _mapHeight; y++)
                {
                    if (x == 0 || y == 0 || x == (_mapWidth - 1) || y == (_mapHeight - 1) ||
                        _obstacleMap[ToOneDimension(x, y)])
                    {
                        _distances[ToOneDimension(x, y)] = -1;
                        _obstacleMap[ToOneDimension(x, y)] = true;
                    }
                    else
                    {
                        _distances[ToOneDimension(x, y)] = 0;
                    }
                }
        }

        // Com as distâncias calculada, podemos agora descobrir o menor caminho entre a
        // saída e chegada.
        private void Pathfinding()
        {
            _path.Clear();
            _path.Add(new(_startX, _startY));
            int currentX = _startX;
            int currentY = _startY;
            bool noPath = false;

            while ((currentX == _endX && currentY == _endY) == false && noPath == false)
            {
                var neighboursNodes = new List<Tuple<int, int, int>>();

                // Leste
                if ((currentX + 1) < _mapWidth && _distances[ToOneDimension(currentX + 1, currentY)] > 0)
                {
                    neighboursNodes.Add(new(currentX + 1, currentY,
                        _distances[ToOneDimension(currentX + 1, currentY)]));
                }

                // Oeste 
                if ((currentX - 1) > 0 && _distances[ToOneDimension(currentX - 1, currentY)] > 0)
                {
                    neighboursNodes.Add(new(currentX - 1, currentY,
                        _distances[ToOneDimension(currentX - 1, currentY)]));
                }

                // Norte 
                if ((currentY - 1) > 0 && _distances[ToOneDimension(currentX, currentY - 1)] > 0)
                {
                    neighboursNodes.Add(new(currentX, currentY - 1,
                        _distances[ToOneDimension(currentX, currentY - 1)]));
                }

                // Sul 
                if ((currentY + 1) < _mapHeight && _distances[ToOneDimension(currentX, currentY + 1)] > 0)
                {
                    neighboursNodes.Add(new(currentX, currentY + 1,
                        _distances[ToOneDimension(currentX, currentY + 1)]));
                }

                // Ordenando a lista pela distância.
                neighboursNodes = neighboursNodes.OrderBy(x => x.Item3).ToList();

                if (neighboursNodes.Any() == false)
                {
                    noPath = true;
                }
                else
                {
                    currentX = neighboursNodes[0].Item1;
                    currentY = neighboursNodes[0].Item2;
                    _path.Add(new(currentX, currentY));
                }
            }
        }
    }
}