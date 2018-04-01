using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using MahApps.Metro.IconPacks;
using Tile = MahApps.Metro.Controls.Tile;

namespace BettingBot.Common.UtilityClasses
{
    public class TilesMenu
    {
        private static readonly object _lock = new object();
        private double[] _mainMenuTilesOpacities;
        private Tile _tlToMove;
        private Tile _tlMoving;
        private Point? _matcMainDraggingStartPoint;
        private Canvas _cvMovingTile;
        private double? _emptyPosY;
        private readonly List<Tile> _switchingTIles = new List<Tile>();
        private StackPanel _spMenu;

        private bool _menuMouseDown_Finished;
        private bool _menuMouseMove_BeforeAnimationFinished;
        private bool _menuMouseMove_Finished;
        private bool _menuMouseUp_BeforeAnimationFinished;
        private bool _menuMouseUp_Finished;
        private Tile _selectedTile;

        public Tile[] MenuTiles { get; private set; }
        public List<string> TilesOrder { get; private set; }
        public Tile ResizeTile { get; private set; }
        public bool IsFullSize { get; private set; }
        public int DefaultTileWidth { get; private set; }
        public int ResizeValue { get; private set; }

        public Color MouseOverColor { get; set; }
        public Color MouseOutColor { get; set; }
        public Color ResizeMouseOverColor { get; set; }
        public Color ResizeMouseOutColor { get; set; }

        public Tile SelectedTile
        {
            get => _selectedTile;
            set
            {
                _selectedTile = value;
                if (value != null)
                    _selectedTile.Highlight(MouseOverColor);
            }
        }

        public TilesMenu(Panel spMenu, bool isFullSize, int resizeValue, Color mouseOverColor, Color mouseOutColor, Color resizeMouseOverColor, Color resizeMouseOutColor)
        {
            var allTiles = spMenu.Children.OfType<Tile>().ToArray();
            var resizeTile = allTiles.Single(tl => tl.Name.StartsWith("tlResize"));
            var menuTiles = allTiles.Except(resizeTile).ToArray();
            InitializeTilesMenu(menuTiles, resizeTile, isFullSize, resizeValue, mouseOverColor, mouseOutColor, resizeMouseOverColor, resizeMouseOutColor);
        }

        public TilesMenu(Tile[] menuTiles, Tile resizeTile, bool isFullSize, int resizeValue, Color mouseOverColor, Color mouseOutColor, Color resizeMouseOverColor, Color resizeMouseOutColor)
        {
            InitializeTilesMenu(menuTiles, resizeTile, isFullSize, resizeValue, mouseOverColor, mouseOutColor, resizeMouseOverColor, resizeMouseOutColor);
        }

        private void InitializeTilesMenu(Tile[] menuTIles, Tile resizeTile, bool isFullSize, int resizeValue, Color mouseOverColor, Color mouseOutColor, Color resizeMouseOverColor, Color resizeMouseOutColor)
        {
            MenuTiles = menuTIles;
            ResizeTile = resizeTile;
            MouseOverColor = mouseOverColor;
            MouseOutColor = mouseOutColor;
            ResizeMouseOverColor = resizeMouseOverColor;
            ResizeMouseOutColor = resizeMouseOutColor;
            IsFullSize = isFullSize;

            _mainMenuTilesOpacities = new double[MenuTiles.Length];
            DefaultTileWidth = MenuTiles[0].ActualWidth.ToInt();
            ResizeValue = resizeValue;
            if (IsFullSize)
                DefaultTileWidth -= ResizeValue;
            _spMenu = menuTIles.First().FindLogicalAncestor<StackPanel>();
            foreach (var tl in MenuTiles)
            {
                tl.Background = new SolidColorBrush(mouseOutColor);
                tl.Click += tlTab_Click;
                tl.MouseEnter += tlTab_MouseEnter;
                tl.MouseLeave += tlTab_MouseLeave;
                tl.PreviewMouseLeftButtonDown += tlTab_PreviewMouseLeftButtonDown;
                tl.PreviewMouseLeftButtonUp += tlTab_PreviewMouseLeftButtonUp;
                tl.PreviewMouseMove += tlTab_PreviewMouseMove;
                tl.Width = IsFullSize ? DefaultTileWidth + ResizeValue : DefaultTileWidth;
            }

            ResizeTile.Background = new SolidColorBrush(resizeMouseOutColor);
            ResizeTile.Click += tlResizeMainMenu_Click;
            ResizeTile.MouseEnter += tlResizeMainMenu_MouseEnter;
            ResizeTile.MouseLeave += tlResizeMainMenu_MouseLeave;

            SelectedTile = null;
            TilesOrder = MenuTiles.Select(tl => tl.Name).ToList();
        }

        private void tlTab_Click(object sender, RoutedEventArgs e)
        {
            if (!new[] { false, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
                return;

            OnMenuTIleClicking(new MenuTileClickedEventArgs((Tile) sender));
        }

        private void tlTab_MouseEnter(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            if (Equals(tile, SelectedTile)) return;
            tile.Highlight(MouseOverColor);
        }

        private void tlTab_MouseLeave(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            if (Equals(tile, SelectedTile)) return;
            tile.Highlight(MouseOutColor);
        }
        
        private void tlTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (new[] { false, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    _tlToMove = (Tile)sender;
                    var parentGrid = _tlToMove.FindLogicalAncestor<Grid>();
                    _matcMainDraggingStartPoint = e.GetPosition(parentGrid);

                    _menuMouseDown_Finished = true;
                }
            }
        }

        private async void tlTab_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Tile matchingDummyTIle = null;
            DoubleAnimation moveAni = null;
            double? emptyPosYLocal = null;

            if (new[] { true, _menuMouseDown_Finished }.AllEqual() &&
                new[] { false, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    if (e.LeftButton != MouseButtonState.Pressed || _matcMainDraggingStartPoint == null)
                        return;

                    var tile = (Tile)sender;
                    var parentGrid = tile.FindLogicalAncestor<Grid>();
                    var mousePos = e.GetPosition(parentGrid);
                    var draggingStartPoint = (Point)_matcMainDraggingStartPoint;
                    var diff = draggingStartPoint - mousePos;
                    if (!(Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance) && !(Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                        return;

                    if (_cvMovingTile == null)
                    {
                        _cvMovingTile = new Canvas { Name = "cvMovingTileContainer" };
                        parentGrid.Children.Add(_cvMovingTile);
                    }

                    if (_tlMoving == null)
                    {
                        var i = 0;
                        foreach (var tl in TilesOrder.Select(o => MenuTiles.Single(tl => tl.Name == o)))
                        {
                            var clonedTile = tl.CloneControl($"{tl.Name}_Clone");
                            _cvMovingTile.Children.Add(clonedTile);
                            clonedTile.PositionY(i++ * (clonedTile.Height + clonedTile.Margin.Top + clonedTile.Margin.Bottom));
                            if (Equals(tl, tile))
                            {
                                clonedTile.ZIndex(MenuTiles.Select(x => x.ZIndex()).Max() + 1);
                                _tlMoving = clonedTile;
                                _emptyPosY = _tlMoving.PositionY();
                            }
                        }

                        for (var j = 0; j < MenuTiles.Length; j++)
                        {
                            _mainMenuTilesOpacities[j] = MenuTiles[j].Opacity;
                            MenuTiles[j].Opacity = 0;
                        }
                    }
                    if (_tlMoving == null || _emptyPosY == null) throw new NullReferenceException($"{nameof(_tlMoving)} or {nameof(_emptyPosY)} is still null after the loop");

                    var initPosRelToTile = parentGrid.TranslatePoint(draggingStartPoint, _tlToMove);
                    var lastTilePos = (_cvMovingTile.FindLogicalDescendants<Tile>().Count() - 1) * (tile.Height + tile.Margin.Top + tile.Margin.Bottom);
                    var rawMovingTilePos = mousePos.Y - initPosRelToTile.Y;
                    var movingTilePos = Math.Min(lastTilePos, Math.Max(0, rawMovingTilePos));
                    
                    _tlMoving.PositionY(movingTilePos);

                    var dummyTiles = _cvMovingTile.Children.OfType<Tile>().Except(_tlMoving).ToArray();
                    var tileHalfH = _tlMoving.Height / 2;

                    if (_tlMoving == null) return;
                    var tilesMovingUp = dummyTiles.Where(tl => (_tlMoving.PositionY() + _tlMoving.Height - (tl.PositionY() + tileHalfH)).Between(0, tileHalfH)).ToArray();
                    var tilesMovingDown = dummyTiles.Where(tl => (tl.PositionY() + tileHalfH - _tlMoving.PositionY()).Between(0, tileHalfH)).ToArray();
                    var matchingDummyTIles = tilesMovingUp.Concat(tilesMovingDown).Except(_switchingTIles).ToArray();
                    if (matchingDummyTIles.Length > 1) throw new ArgumentException($"{nameof(matchingDummyTIles)}.Length is greater than 1");
                    if (matchingDummyTIles.Length == 1)
                    {
                        emptyPosYLocal = _emptyPosY;
                        matchingDummyTIle = matchingDummyTIles.Single();
                        moveAni = new DoubleAnimation(emptyPosYLocal.ToDouble(), new Duration(TimeSpan.FromMilliseconds(200)));
                        var newEmptyPosY = matchingDummyTIle.PositionY();
                        _switchingTIles.Add(matchingDummyTIle);
                        _emptyPosY = newEmptyPosY;
                    }

                    _menuMouseMove_BeforeAnimationFinished = true;
                }

                if (matchingDummyTIle != null)
                    await matchingDummyTIle.AnimateAsync(Canvas.TopProperty, moveAni);
            }

            if (new[] { true, _menuMouseDown_Finished, _menuMouseMove_BeforeAnimationFinished }.AllEqual() &&
                new[] { false, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    if (matchingDummyTIle != null)
                    {
                        _switchingTIles.Remove(matchingDummyTIle);
                        matchingDummyTIle.PositionY(emptyPosYLocal.ToDouble());
                    }
                    
                    _menuMouseMove_Finished = true;
                }
            }
        }

        private async void tlTab_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (new[] { true, _menuMouseDown_Finished, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished }.AllEqual() &&
                new[] { false, _menuMouseUp_Finished }.AllEqual())
            {
                DoubleAnimation moveAni;
                lock (_lock)
                {
                    if (_emptyPosY == null)
                        return;

                    moveAni = new DoubleAnimation((double)_emptyPosY, new Duration(TimeSpan.FromMilliseconds(200)));

                    _menuMouseUp_BeforeAnimationFinished = true;
                }

                await _tlMoving.AnimateAsync(Canvas.TopProperty, moveAni);
            }

            if (new[] { true, _menuMouseDown_Finished, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished, _menuMouseUp_BeforeAnimationFinished }.AllEqual())
            {
                lock (_lock)
                {
                    if (_emptyPosY == null)
                        return;

                    _tlMoving.PositionY((double)_emptyPosY);

                    var orderedDummyTiles = _cvMovingTile.FindLogicalDescendants<Tile>().OrderBy(tl => tl.PositionY());
                    var orderedTiles = orderedDummyTiles.Select(dtl => MenuTiles.Single(tl => dtl.Name.Contains(tl.Name))).ToList();
                    var utilTiles = _spMenu.FindLogicalDescendants<Tile>().Except(orderedTiles).ToList();
                    _spMenu.Children.ReplaceAll(orderedTiles);
                    _spMenu.Children.AddRange(utilTiles);
                    TilesOrder.ReplaceAll(orderedTiles.Select(i => i.Name).ToList()); // bezpiecznie podmień elementy żeby uniknąć zepsucia idniesień przekazanych do metod

                    if (_cvMovingTile != null)
                        _tlToMove?.FindLogicalAncestor<Grid>().Children.Remove(_cvMovingTile);
                    _matcMainDraggingStartPoint = null;
                    _cvMovingTile = null;
                    _tlMoving = null;

                    _emptyPosY = null;
                    for (var i = 0; i < MenuTiles.Length; i++)
                        MenuTiles[i].Opacity = _mainMenuTilesOpacities[i];
                    _tlToMove = null;

                    _menuMouseUp_Finished = true;


                    _menuMouseDown_Finished = false;
                    _menuMouseMove_BeforeAnimationFinished = false;
                    _menuMouseMove_Finished = false;
                    _menuMouseUp_BeforeAnimationFinished = false;
                    _menuMouseUp_Finished = false;
                }
            }
        }

        private async void tlResizeMainMenu_Click(object sender, RoutedEventArgs e)
        {
            var resizeTile = (Tile)sender;
            var icon = resizeTile.FindLogicalDescendants<PackIconModern>().Single();
            if (!IsFullSize)
            {
                IsFullSize = true;
                var resizeAni = new DoubleAnimation(DefaultTileWidth + ResizeValue, new Duration(TimeSpan.FromMilliseconds(100)));
                icon.Kind = PackIconModernKind.ArrowLeft;
                foreach (var tl in TilesOrder.Select(o => MenuTiles.Single(tl => tl.Name == o)))
                {
                    if (IsFullSize) // nie kolejkuj kolejnych wywołań rozciągnięcia menu, jeżeli w międzyczasie zostało wywołane zwinięcie
                    {
                        await tl.AnimateAsync(FrameworkElement.WidthProperty, resizeAni);
                        tl.Width = DefaultTileWidth + ResizeValue;
                    }
                }
            }
            else
            {
                IsFullSize = false;
                var resizeAni = new DoubleAnimation(DefaultTileWidth, new Duration(TimeSpan.FromMilliseconds(100)));
                icon.Kind = PackIconModernKind.DoorLeave;
                foreach (var tl in TilesOrder.Select(o => MenuTiles.Single(tl => tl.Name == o)).Reverse())
                {
                    if (!IsFullSize)
                    {
                        await tl.AnimateAsync(FrameworkElement.WidthProperty, resizeAni);
                        tl.Width = DefaultTileWidth;
                    }
                }
            }
        }

        private void tlResizeMainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Tile)sender).Highlight(ResizeMouseOverColor);
        }

        private void tlResizeMainMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Tile)sender).Highlight(ResizeMouseOutColor);
        }

        public void Extend()
        {
            foreach (var tl in MenuTiles)
                tl.Width = DefaultTileWidth + ResizeValue;
            IsFullSize = true;
            var icon = ResizeTile.FindLogicalDescendants<PackIconModern>().Single();
            icon.Kind = PackIconModernKind.ArrowLeft;
        }

        public void Shrink()
        {
            foreach (var tl in MenuTiles)
                tl.Width = DefaultTileWidth;
            IsFullSize = false;
            var icon = ResizeTile.FindLogicalDescendants<PackIconModern>().Single();
            icon.Kind = PackIconModernKind.DoorLeave;
        }

        public void Reorder(IEnumerable<Tile> reorderedTiles)
        {
            var container = MenuTiles.First().FindLogicalAncestor<Panel>();
            var utilTiles = container.FindLogicalDescendants<Tile>().Except(MenuTiles).ToList();
            container.Children.ReplaceAll(reorderedTiles);
            container.Children.AddRange(utilTiles);
        }

        public void Reorder(IEnumerable<string> orderByName)
        {
            var arrOrder = orderByName.ToArray();
            TilesOrder.ReplaceAll(arrOrder); // nie rebinduj listy po zepsułoby to odniesienia
            var container = MenuTiles.First().FindLogicalAncestor<Panel>();
            var utilTiles = container.FindLogicalDescendants<Tile>().Except(MenuTiles).ToList();
            var orderedTiles = MenuTiles.OrderByWith(t => t.Name, arrOrder).ToList();
            container.Children.ReplaceAll(orderedTiles);
            container.Children.AddRange(utilTiles);
        }

        public event MenuTileClickedEventHandler MenuTileClick;

        protected virtual void OnMenuTIleClicking(MenuTileClickedEventArgs e) => MenuTileClick?.Invoke(this, e);
    }

    public delegate void MenuTileClickedEventHandler(object sender, MenuTileClickedEventArgs e);

    public class MenuTileClickedEventArgs
    {
        public Tile TileClicked { get; }

        public MenuTileClickedEventArgs(Tile tileClicked)
        {
            TileClicked = tileClicked;
        }
    }
}
