using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BettingBot.Source.Common;
using MahApps.Metro.IconPacks;
using MoreLinq;
using Point = System.Windows.Point;
using Tile = MahApps.Metro.Controls.Tile;

namespace BettingBot.Source.Controls
{
    public partial class TilesMenu
    {
        private static readonly object _lock = new object();
        private Tile _tlToMove;
        private Tile _tlMoving;
        private double _menuTilesOpacity;
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

        public static readonly DependencyProperty OptionsDp = DependencyProperty.Register(nameof(Options), 
            typeof(ObservableCollection<TilesMenuOption>), typeof(TilesMenu), new UIPropertyMetadata(null));
        public ObservableCollection<TilesMenuOption> Options
        {
            get => (ObservableCollection<TilesMenuOption>)GetValue(OptionsDp);
            set => SetValue(OptionsDp, value);
        }

        public Tile[] MenuTiles { get; private set; }
        public List<string> TilesOrder { get; private set; }
        public Tile ResizeTile { get; private set; }
        public static readonly DependencyProperty IsFullSizeDp = DependencyProperty.Register(nameof(IsFullSize), 
            typeof(bool), typeof(TilesMenu), new UIPropertyMetadata(null));
        public bool IsFullSize
        {
            get => (bool)GetValue(IsFullSizeDp);
            set => SetValue(IsFullSizeDp, value);
        }
        public int DefaultTileWidth { get; private set; }
        
        public static readonly DependencyProperty ResizeValueDp = DependencyProperty.Register(nameof(ResizeValue), 
            typeof(int), typeof(TilesMenu), new UIPropertyMetadata(100, null));
        public int ResizeValue
        {
            get => (int)GetValue(ResizeValueDp);
            set => SetValue(ResizeValueDp, value);
        }

        public static readonly DependencyProperty MouseOverColorDp = DependencyProperty.Register(nameof(MouseOverColor), 
            typeof(Brush), typeof(TilesMenu), new UIPropertyMetadata(null));
        public SolidColorBrush MouseOverColor
        {
            get => (SolidColorBrush)GetValue(MouseOverColorDp);
            set => SetValue(MouseOverColorDp, value);
        }

        public static readonly DependencyProperty MouseOutColorDp =
            DependencyProperty.Register(nameof(MouseOutColor), typeof(Brush),
                typeof(TilesMenu), new UIPropertyMetadata(null));
        public SolidColorBrush MouseOutColor
        {
            get => (SolidColorBrush)GetValue(MouseOutColorDp);
            set => SetValue(MouseOutColorDp, value);
        }

        public static readonly DependencyProperty ResizeMouseOverColorDp =
            DependencyProperty.Register(nameof(ResizeMouseOverColor), typeof(Brush),
                typeof(TilesMenu), new UIPropertyMetadata(null));
        public SolidColorBrush ResizeMouseOverColor
        {
            get => (SolidColorBrush)GetValue(ResizeMouseOverColorDp);
            set => SetValue(ResizeMouseOverColorDp, value);
        }

        public static readonly DependencyProperty ResizeMouseOutColorDp =
            DependencyProperty.Register(nameof(ResizeMouseOutColor), typeof(Brush),
                typeof(TilesMenu), new UIPropertyMetadata(null));
        public SolidColorBrush ResizeMouseOutColor
        {
            get => (SolidColorBrush)GetValue(ResizeMouseOutColorDp);
            set => SetValue(ResizeMouseOutColorDp, value);
        }

        public static readonly DependencyProperty ShowResizeTileDp =
            DependencyProperty.Register(nameof(ShowResizeTile), typeof(bool),
                typeof(TilesMenu), new UIPropertyMetadata(true));
        public bool ShowResizeTile
        {
            get => (bool)GetValue(ShowResizeTileDp);
            set
            {
                SetValue(ShowResizeTileDp, value);
                ResizeTile.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public TilesMenuOption SelectedOption => Options.SingleOrDefault(o => o.Selected);
        public Tile SelectedTile
        {
            get => _spMenu.LogicalDescendants<Tile>().SingleOrDefault(tl => tl.Name == SelectedOption?.TileName);
            set => Select(value);
        }
        
        public TilesMenu()
        {
            //DataContext = this;
            Options = new ObservableCollection<TilesMenuOption>();
            Loaded += TilesMenu_Loaded;
            InitializeComponent();
        }

        private void TilesMenu_Loaded(object sender, RoutedEventArgs e)
        {
            _spMenu = this.LogicalDescendants<StackPanel>().Single();
            var resizeTile = _spMenu.LogicalDescendants<Tile>().Single(tl => tl.Name.StartsWith("tlResize"));
            var allTiles = Options.Select(o => o.LogicalDescendants<Tile>().Single()).ToArray();
            var menuTiles = allTiles.Except(resizeTile).ToArray();
            MenuTiles = menuTiles;
            ResizeTile = resizeTile;
            
            foreach (var menuOption in Options)
            {
                var tl = menuOption.LogicalDescendants<Tile>().Single();
                tl.Name = menuOption.TileName;

                menuOption.Tile = tl;
                menuOption.MouseOutColor = MouseOutColor.Color;
                menuOption.MouseOverColor = MouseOverColor.Color;
                tl.MenuTileDescription(menuOption.Description);
                tl.MenuTileIcon(menuOption.Icon);
            }

            menuTiles.DetachFromParent();
            _spMenu.Children.PrepandAllBefore(menuTiles, resizeTile);

            ResizeMouseOverColor = (SolidColorBrush)FindResource("MouseOverMainMenuResizeTileBrush");
            ResizeMouseOutColor = (SolidColorBrush)FindResource("DefaultMainMenuResizeTileBrush");

            DefaultTileWidth = menuTiles[0].Width.ToInt();
            if (IsFullSize)
                Expand();

            foreach (var tl in menuTiles)
            {
                tl.Click += tlTab_Click;
                tl.MouseEnter += tlTab_MouseEnter;
                tl.MouseLeave += tlTab_MouseLeave;
                tl.PreviewMouseLeftButtonDown += tlTab_PreviewMouseLeftButtonDown;
                tl.PreviewMouseLeftButtonUp += tlTab_PreviewMouseLeftButtonUp;
                tl.PreviewMouseMove += tlTab_PreviewMouseMove;
                tl.Width = IsFullSize ? DefaultTileWidth + ResizeValue : DefaultTileWidth;
            }

            resizeTile.Background = ResizeMouseOutColor;
            resizeTile.Click += tlResizeMainMenu_Click;
            resizeTile.MouseEnter += tlResizeMainMenu_MouseEnter;
            resizeTile.MouseLeave += tlResizeMainMenu_MouseLeave;

            if (!ShowResizeTile)
                resizeTile.Visibility = Visibility.Collapsed;

            TilesOrder = Options.Select(o => o.TileName).ToList();

            Select(Options.SingleOrDefault(o => o.Selected), true); // can't be done in Selected property in TilesMenuOption because setter gets called before TileName is initially set 
        }
        
        private void tlTab_Click(object sender, RoutedEventArgs e)
        {
            if (!new[] { false, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
                return;

            var clickedTile = (Tile)sender;
            var previouslySelectedTile = SelectedTile;
            SelectedTile = !Equals(SelectedTile, clickedTile) ? clickedTile : null; // using property to force the animation
            
            OnMenuTileClicking(new MenuTileClickedEventArgs(clickedTile, previouslySelectedTile));
        }

        private void tlTab_MouseEnter(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            if (Equals(tile, SelectedTile)) return;
            tile.Highlight(MouseOverColor.Color);
        }

        private void tlTab_MouseLeave(object sender, MouseEventArgs e)
        {
            var tile = (Tile)sender;
            if (Equals(tile, SelectedTile)) return;
            tile.Highlight(MouseOutColor.Color);
        }

        private void tlTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (new[] { false, _menuMouseMove_BeforeAnimationFinished, _menuMouseMove_Finished, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    _tlToMove = (Tile)sender;
                    var parentGrid = _tlToMove.LogicalAncestor<StackPanel>().LogicalAncestor<Grid>();
                    _matcMainDraggingStartPoint = e.GetPosition(parentGrid);

                    _menuMouseDown_Finished = true;
                }
            }
        }

        private async void tlTab_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Tile matchingDummyTile = null;
            DoubleAnimation moveAni = null;
            double? emptyPosYLocal = null;

            if (new[] { true, _menuMouseDown_Finished }.AllEqual() &&
                new[] { false, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    if (e.LeftButton != MouseButtonState.Pressed || _matcMainDraggingStartPoint == null)
                        return;

                    var tileToMove = (Tile)sender;
                    var parentGrid = tileToMove.LogicalAncestor<StackPanel>().LogicalAncestor<Grid>();
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
                            var dataTemplate = (DataTemplate)FindResource("TilesMenuOptionTileDataTemplate");

                            var clonedTile = new Tile // cloning using xaml serialization is much slower than this
                            {
                                Name = $"{tl.Name}_Clone",
                                Style = tl.Style,
                                ContentTemplate = dataTemplate,
                                Width = tl.Width,
                                Height = tl.Height
                            };
                            clonedTile.ApplyTemplate();
                            var clonedContentPresenter = clonedTile.VisualDescendants<ContentPresenter>().First();
                            clonedContentPresenter.ApplyTemplate();
                            
                            var clonedTb = clonedTile.VisualDescendants<TextBlock>().First();
                            clonedTb.Text = tl.MenuTileDescription();
                            
                            var clonedIcon = clonedTile.VisualDescendants<PackIconModern>().First();
                            clonedIcon.Kind = tl.MenuTileIcon();

                            _cvMovingTile.Children.Add(clonedTile);
                            
                            clonedTile.PositionY(i++ * (clonedTile.Height + clonedTile.Margin.Top + clonedTile.Margin.Bottom));
                            if (Equals(tl, tileToMove))
                            {
                                clonedTile.ZIndex(MenuTiles.Select(x => x.ZIndex()).Max() + 1);
                                _tlMoving = clonedTile;
                                _emptyPosY = _tlMoving.PositionY();
                            }
                            clonedTile.Background = Equals(tl, SelectedTile) ? MouseOverColor : MouseOutColor;
                        }

                        _menuTilesOpacity = MenuTiles[0].Opacity;
                        foreach (var tl in MenuTiles)
                            tl.Opacity = 0;
                    }
                    if (_tlMoving == null || _emptyPosY == null) throw new NullReferenceException($"{nameof(_tlMoving)} or {nameof(_emptyPosY)} is still null after the loop");

                    var initPosRelToTile = parentGrid.TranslatePoint(draggingStartPoint, _tlToMove);
                    var lastTilePos = (_cvMovingTile.LogicalDescendants<Tile>().Count() - 1) * (tileToMove.Height + tileToMove.Margin.Top + tileToMove.Margin.Bottom);
                    var rawMovingTilePos = mousePos.Y - initPosRelToTile.Y;
                    var movingTilePos = Math.Min(lastTilePos, Math.Max(0, rawMovingTilePos));

                    _tlMoving.PositionY(movingTilePos);

                    var dummyTiles = _cvMovingTile.Children.OfType<Tile>().Except(_tlMoving).ToArray();
                    var tileHalfH = _tlMoving.Height / 2;

                    if (_tlMoving == null) return;
                    var tilesMovingUp = dummyTiles.Where(tl => (_tlMoving.PositionY() + _tlMoving.Height - (tl.PositionY() + tileHalfH)).BetweenExcl(0, tileHalfH)).ToArray();
                    var tilesMovingDown = dummyTiles.Where(tl => (tl.PositionY() + tileHalfH - _tlMoving.PositionY()).BetweenExcl(0, tileHalfH)).ToArray();
                    var matchingDummyTIles = tilesMovingUp.Concat(tilesMovingDown).Except(_switchingTIles).ToArray();
                    if (matchingDummyTIles.Length > 1) throw new ArgumentException($"{nameof(matchingDummyTIles)}.Length is greater than 1");
                    if (matchingDummyTIles.Length == 1)
                    {
                        emptyPosYLocal = _emptyPosY;
                        matchingDummyTile = matchingDummyTIles.Single();
                        moveAni = new DoubleAnimation(emptyPosYLocal.ToDouble(), new Duration(TimeSpan.FromMilliseconds(200)));
                        var newEmptyPosY = matchingDummyTile.PositionY();
                        _switchingTIles.Add(matchingDummyTile);
                        _emptyPosY = newEmptyPosY;
                    }

                    _menuMouseMove_BeforeAnimationFinished = true;
                }

                if (matchingDummyTile != null)
                    await matchingDummyTile.AnimateAsync(Canvas.TopProperty, moveAni);
            }

            if (new[] { true, _menuMouseDown_Finished, _menuMouseMove_BeforeAnimationFinished }.AllEqual() &&
                new[] { false, _menuMouseUp_BeforeAnimationFinished, _menuMouseUp_Finished }.AllEqual())
            {
                lock (_lock)
                {
                    if (matchingDummyTile != null)
                    {
                        _switchingTIles.Remove(matchingDummyTile);
                        matchingDummyTile.PositionY(emptyPosYLocal.ToDouble());
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

                    var orderedDummyTiles = _cvMovingTile.LogicalDescendants<Tile>().OrderBy(tl => tl.PositionY());
                    var orderedTiles = orderedDummyTiles.Select(dtl => MenuTiles.Single(tl => dtl.Name.Contains(tl.Name))).ToList();
                    var utilTiles = _spMenu.LogicalDescendants<Tile>().Except(orderedTiles).ToList();
                    _spMenu.Children.ReplaceAll(orderedTiles);
                    _spMenu.Children.AddRange(utilTiles);
                    TilesOrder.ReplaceAll(orderedTiles.Select(i => i.Name).ToList()); // bezpiecznie podmień elementy żeby uniknąć zepsucia odniesień przekazanych do metod

                    if (_cvMovingTile != null)
                        _tlToMove?.LogicalAncestor<Grid>().Children.Remove(_cvMovingTile);
                    _matcMainDraggingStartPoint = null;
                    _cvMovingTile = null;
                    _tlMoving = null;

                    _emptyPosY = null;
                    foreach (var tl in MenuTiles)
                        tl.Opacity = _menuTilesOpacity;

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
            var icon = resizeTile.LogicalDescendants<PackIconModern>().Single();
            if (!IsFullSize)
            {
                IsFullSize = true;
                var resizeAni = new DoubleAnimation(DefaultTileWidth + ResizeValue, new Duration(TimeSpan.FromMilliseconds(100)));
                icon.Kind = PackIconModernKind.ArrowLeft;
                foreach (var tl in TilesOrder.Select(o => MenuTiles.Single(tl => tl.Name == o)))
                {
                    if (IsFullSize) // nie kolejkuj kolejnych wywołań rozciągnięcia menu, jeżeli w międzyczasie zostało wywołane zwinięcie
                    {
                        await tl.AnimateAsync(WidthProperty, resizeAni);
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
                        await tl.AnimateAsync(WidthProperty, resizeAni);
                        tl.Width = DefaultTileWidth;
                    }
                }
            }
        }

        private void tlResizeMainMenu_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Tile)sender).Highlight(ResizeMouseOverColor.Color);
        }

        private void tlResizeMainMenu_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Tile)sender).Highlight(ResizeMouseOutColor.Color);
        }

        public void Expand()
        {
            foreach (var tl in MenuTiles)
                tl.Width = DefaultTileWidth + ResizeValue;
            IsFullSize = true;
            var icon = ResizeTile.LogicalDescendants<PackIconModern>().Single();
            icon.Kind = PackIconModernKind.ArrowLeft;
        }

        public void Shrink()
        {
            foreach (var tl in MenuTiles)
                tl.Width = DefaultTileWidth;
            IsFullSize = false;
            var icon = ResizeTile.LogicalDescendants<PackIconModern>().Single();
            icon.Kind = PackIconModernKind.DoorLeave;
        }

        public void Reorder(IEnumerable<Tile> reorderedTiles)
        {
            var container = MenuTiles.First().LogicalAncestor<Panel>();
            var utilTiles = container.LogicalDescendants<Tile>().Except(MenuTiles).ToList();
            container.Children.ReplaceAll(reorderedTiles);
            container.Children.AddRange(utilTiles);
        }

        public void Reorder(IEnumerable<string> orderByName)
        {
            var arrOrderByName = orderByName.ToArray();
            var tilesNotSavedInDb = MenuTiles.Where(tl => !tl.Name.EqualsAny(arrOrderByName)).ToArray();
            var tileNamesNotSavedInDb = tilesNotSavedInDb.Select(tl => tl.Name).ToArray();
            TilesOrder.ReplaceAll(arrOrderByName.Concat(tileNamesNotSavedInDb)); // nie rebinduj listy po zepsułoby to odniesienia
            var container = MenuTiles.First().LogicalAncestor<Panel>();
            var utilTiles = container.LogicalDescendants<Tile>().Except(MenuTiles).ToList();
            var orderedTiles = MenuTiles.OrderByWith(t => t.Name, TilesOrder).ToList();
            container.Children.ReplaceAll(orderedTiles);
            container.Children.AddRange(utilTiles);
        }

        public void DeselectAll()
        {
            Options.ForEach(o => o.Selected = false);
        }

        public void Select(TilesMenuOption option, bool forceReselect = false)
        {
            var oldSelected = Options.SingleOrDefault(o => o.Selected);
            var newSelected = option;

            if (forceReselect || !Equals(oldSelected, newSelected))
            {
                DeselectAll();

                if (newSelected != null)
                    newSelected.Selected = true;
            }
        }

        public void Select(Tile tile)
        {
            if (tile != null)
                Select(Options.Single(o => o.TileName == MenuTiles.Single(tl => tl.Name == tile.Name).Name));
            else
                DeselectAll();
        }

        public void SelectByName(string tileName)
        {
            Select(Options.Single(o => o.TileName == MenuTiles.Single(tl => tl.Name == tileName).Name));
        }

        public event MenuTileClickedEventHandler MenuTileClick;

        protected virtual void OnMenuTileClicking(MenuTileClickedEventArgs e) => MenuTileClick?.Invoke(this, e);


    }

    public delegate void MenuTileClickedEventHandler(object sender, MenuTileClickedEventArgs e);

    public class MenuTileClickedEventArgs
    {
        public Tile TileClicked { get; }
        public Tile PreviouslySelectedTile { get; }

        public MenuTileClickedEventArgs(Tile tileClicked, Tile previouslySelectedTile)
        {
            TileClicked = tileClicked;
            PreviouslySelectedTile = previouslySelectedTile;
        }
    }
}
