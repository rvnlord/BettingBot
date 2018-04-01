using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using BettingBot.Models;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MoreLinq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Telerik.Windows;
using Telerik.Windows.Data;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Models.DataLoaders;
using BettingBot.Models.ViewModels;
using BettingBot.Models.ViewModels.Collections;
using MahApps.Metro.IconPacks;
using static BettingBot.Common.StringUtils;
using Button = System.Windows.Controls.Button;
using DColor = System.Drawing.Color;
using Color = System.Windows.Media.Color;
using Tile = MahApps.Metro.Controls.Tile;
using Control = System.Windows.Controls.Control;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = BettingBot.Common.UtilityClasses.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using GridViewDeletedEventArgs = Telerik.Windows.Controls.GridViewDeletedEventArgs;
using GridViewRow = Telerik.Windows.Controls.GridView.GridViewRow;
using Path = System.IO.Path;
using Binding = System.Windows.Data.Binding;
using Extensions = BettingBot.Common.Extensions;

namespace BettingBot
{
    public partial class MainWindow
    {
        #region Constants

        private const string betshoot = "betshoot";
        private const string hintwise = "hintwise";

        #endregion

        #region Fields

        private static readonly object _lock = new object();
        private NotifyIcon _notifyIcon;

        private List<UIElement> _lowestHighestOddsByPeriodFilterControls = new List<UIElement>();
        private List<UIElement> _odssLesserGreaterThanFilterControls = new List<UIElement>();
        private List<UIElement> _selectionFilterControls = new List<UIElement>();
        private List<UIElement> _tipsterFilterControls = new List<UIElement>();
        private List<UIElement> _fromDateFilterControls = new List<UIElement>();
        private List<UIElement> _toDateFilterControls = new List<UIElement>();
        private List<UIElement> _pickFilterControls = new List<UIElement>();
        private readonly List<object> _buttonsAndContextMenus = new List<object>();
        
        private Color _mouseOverMainMenuTileColor;
        private Color _defaultMainMenuTileColor;
        private Color _mouseOverMainMenuResizeTileColor;
        private Color _defaultMainMenuResizeTileColor;

        private Color _defaultFlyoutHeaderTileColor;
        private Color _mouseOverFlyoutHeaderTileColor;
        private Color _defaultFlyoutHeaderIconColor;

        private Color _defaultMainGridTabTileColor;
        private Color _mouseOverMainGridTabTileColor;

        private Color _defaultDatabaseTabTileColor;
        private Color _mouseOverDatabaseTabTileColor;

        private Color _defaultOptionsTabTileColor;
        private Color _mouseOverOptionsTabTileColor;



        private readonly ObservableCollection<UserRgvVM> _ocLogins = new ObservableCollection<UserRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedLogins = new ObservableCollection<object>();
        private readonly ObservableCollection<TipsterRgvVM> _ocTipsters = new ObservableCollection<TipsterRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedTipsters = new ObservableCollection<object>();
        private readonly ObservableCollection<BetToDisplayRgvVM> _ocBetsToDisplayRgvVM = new ObservableCollection<BetToDisplayRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedBetsToDisplayRgvVM = new ObservableCollection<object>();
        private readonly ObservableCollection<ProfitByPeriodStatisticRgvVM> _ocProfitByPeriodStatistics = new ObservableCollection<ProfitByPeriodStatisticRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedProfitByPeriodStatistics = new ObservableCollection<object>();
        private readonly ObservableCollection<AggregatedWinLoseStatisticRgvVM> _ocAggregatedWinLoseStatisticsRgvVM = new ObservableCollection<AggregatedWinLoseStatisticRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedAggregatedWinLoseStatisticsRgvVM = new ObservableCollection<object>();
        private readonly ObservableCollection<GeneralStatisticRgvVM> _ocGeneralStatistics = new ObservableCollection<GeneralStatisticRgvVM>();
        private readonly ObservableCollection<object> _ocSelectedGeneralStatistics = new ObservableCollection<object>();

        private BettingSystem _bs;
        private TilesMenu _mainMenu;

        #endregion

        #region Properties

        public static string AppDirPath { get; set; }
        public static string ErrorLogPath { get; set; }
        public ViewState GuiState { get; set; }

        #endregion

        #region Constructors

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        #endregion

        #region Events

        #region - MainWindow Events

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridMain.ShowLoader();
            try
            {
                await Task.Run(() =>
                {
                    SQLiteConnection.ClearAllPools();
                    GC.Collect();
                    AutoMapperConfiguration.Configure();
                    AppDirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    ErrorLogPath = $@"{AppDirPath}\ErrorLog.log";

                    SetupNotifyIcon();

                    Dispatcher.Invoke(() =>
                    {
                        SetupTiles();
                        SetupGrids();
                        SetupTextBoxes();
                        SetupDatePickers();
                        SetupUpDowns();
                        SetupDropdowns();
                        SetupGridviews();
                        SetupTabControls();
                        InitializeContextMenus();

                        InitializeControlGroups();

                        SetupZIndexes(this);

                        UpdateGuiWithNewTipsters();
                        LoadOptions();
                        RunOptionsDependentAdjustments();
                    });

                    CalculateBets();
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
            gridMain.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                SaveOptions();
                SeleniumDriverManager.CloseAllDrivers();
            }
            catch (Exception ex)
            {
                e.Cancel = true;
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
        }

        private async void MainWindow_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mouseHoveredElements = this.FindLogicalDescendants<FrameworkElement>() // TextBox, RadDatePicker, RadGridView
                .Where(f =>
                    f.GetType() != typeof(Grid) && f.GetType() != typeof(MetroAnimatedTabControl) &&
                    (f.FindLogicalAncestor<Grid>() == null || f.FindLogicalAncestor<Grid>().IsVisible) &&
                    (f.FindLogicalAncestor<MetroTabItem>() == null || f.FindLogicalAncestor<MetroTabItem>().IsSelected) &&
                    f.HasClientRectangle(this) && f.ClientRectangle(this).Contains(e.GetPosition(this))).ToList();

            mouseHoveredElements = mouseHoveredElements.GroupBy(Panel.GetZIndex).MaxBy(g => g.Key).ToList();
            if (mouseHoveredElements.Any(f => f.FindLogicalAncestor<Grid>(anc => anc.Name.EndsWith("Flyout")) != null))
                mouseHoveredElements.RemoveBy(f => f.FindLogicalAncestor<Grid>(anc => anc.Name.EndsWith("Flyout")) == null);

            if (mouseHoveredElements.Count > 1)
            {
                var message =
                    "Występuje wiele elementów do wyświetlenia menu kontekstowego (conajmniej dwa mają jednakowe ZIndeksy):\n" +
                    string.Join("\n", mouseHoveredElements.Select(el => $"({Panel.GetZIndex(el)}: {el.Name})"));
                File.WriteAllText(ErrorLogPath, message);
                await this.ShowMessageAsync("Wystąpił Błąd", message);
                return;
            }

            foreach (var c in mouseHoveredElements)
            {
                e.Handled = true;
                if (c.HasContextMenu() && c.IsWithinBounds(this))
                    HandleContextMenu(c, e);
            }
        }

        #endregion

        #region - Button Events

        private async void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _buttonsAndContextMenus.DisableControls();
                gridCalculationsFlyout.ShowLoader();
                await Task.Run(() => CalculateBets());
                gridCalculationsFlyout.HideLoader();
                if (!gridMain.HasLoader())
                    _buttonsAndContextMenus.EnableControls();
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
        }

        private async void btnClearDatabase_Click(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridMain.ShowLoader();

            await Task.Run(async () =>
            {
                var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Czyszczenie CAŁEJ bazy danych", $"Czy na pewno chcesz usunąć WSZYSTKIE rekordy? ({rgvData.Items.Count})", MessageDialogStyle.AffirmativeAndNegative));
                if (result == MessageDialogResult.Affirmative)
                {
                    var db = new LocalDbContext();
                    db.Database.ExecuteSqlCommand("DELETE FROM tblTipsters");
                    db.Database.ExecuteSqlCommand("DELETE FROM tblBets");
                    db.Database.ExecuteSqlCommand("DELETE FROM tblLogins");
                    db.Database.ExecuteSqlCommand("DELETE FROM tblWebsites");
                    db.Database.ExecuteSqlCommand("DELETE FROM tblPicks");
                    db.SaveChanges();
                    Dispatcher.Invoke(() =>
                    {
                        ClearGridViews();
                        UpdateGuiWithNewTipsters();
                        txtNotes.ResetValue();
                    });
                }
            });

            gridMain.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private void btnMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private async void btnAddTipster_Click(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                if (!txtLoad.Text.IsUrl()) throw new Exception("To nie jest poprawny adres");

                var newTipstersCount = 0;
                await Task.Run(() =>
                {
                    var selectedTipsterIdsDdl = mddlTipsters.SelectedCustomIds();
                    DownloadTipsterToDb();
                    newTipstersCount = UpdateGuiWithNewTipsters();
                    Dispatcher.Invoke(() => mddlTipsters.SelectByCustomIds(selectedTipsterIdsDdl));
                });
                if (newTipstersCount <= 0)
                    await this.ShowMessageAsync("Wystąpił Błąd", "Tipster znajduje się już w bazie danych");
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private async void btnDownloadTips_Click(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                await Task.Run(() => DownloadTipsToDb());
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private void btnCalculatorCalculate_Click(object sender, RoutedEventArgs e)
        {
            txtCalculatorResult.ClearValue(true);

            var odds = new double[3];
            var chance = new double[3];
            var stake = new double[3];
            var stakePerc = new double[3];

            var totalStake = rnumCalculatorStake.Value ?? 0;
            var minStake = rnumCalculatorMinStake.Value ?? 0;
            odds[0] = rnumCalculatorOdds1.Value ?? 0;
            odds[1] = rnumCalculatorOdds2.Value ?? 0;
            odds[2] = rnumCalculatorOdds3.Value ?? 0;

            var calcSurebet = odds[2] > 0;

            for (var i = 0; i < odds.Length; i++)
                if (odds[i] > 0)
                    chance[i] = 1 / odds[i];

            var totalChance = chance.Sum();
            var totalOdds = 1 / totalChance;

            for (var i = 0; i < stakePerc.Length; i++)
            {
                stakePerc[i] = chance[i] / totalChance;
                stake[i] = stakePerc[i] * totalStake;
            }

            var lowestStake = stake.Where(s => s > 0).Min();
            var lowestStakeIndex = Array.IndexOf(stake, lowestStake);

            if (lowestStake < minStake)
            {
                stake[lowestStakeIndex] = minStake;
                totalStake = minStake / stakePerc[lowestStakeIndex];

                for (var i = 0; i < stake.Length; i++)
                    if (i != lowestStakeIndex)
                        stake[i] = stakePerc[i] * totalStake;
            }

            var sb = new StringBuilder();
            for (var i = 0; i < odds.Length; i++)
                if (calcSurebet || i < 2)
                    sb.Append($"{i + 1}: {odds[i]:0.000} x {stake[i]:0.00} zł = {odds[i] * stake[i]:0.00} zł\n");
            sb.Append($"Ogółem: {totalOdds:0.000} x {totalStake:0.00} zł = {totalOdds * totalStake:0.00} zł");
            txtCalculatorResult.Text = sb.ToString();
        }

        private async void btnSaveLogin_Click(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                if (txtLoadDomain.IsNullWhitespaceOrTag() || txtLoadLogin.IsNullWhitespaceOrTag() || txtLoadPassword.IsNullWhitespaceOrTag() || txtLoadDomain.Text.Remove(Space).Contains(",,"))
                    throw new Exception("Wszystkie pola muszą być poprawnie wypełnione");

                await Task.Run(() =>
                {
                    var selLogins = _ocSelectedLogins.Cast<UserRgvVM>().ToList();
                    if (selLogins.Count != 1) return;
                    var selLogin = selLogins.Single();
                    var db = new LocalDbContext();
                    var dbLogin = db.Logins.Single(l => l.Id == selLogin.Id);
                    Dispatcher.Invoke(() =>
                    {
                        dbLogin.Name = txtLoadLogin.Text;
                        dbLogin.Password = txtLoadPassword.Text;
                        Website.AddNewByAddress(db, txtLoadDomain.Text.Split(", "), selLogin.Id);
                    });
                    db.SaveChanges();

                    Dispatcher.Invoke(() =>
                    {
                        _ocLogins.ReplaceAll(db.Logins.MapTo<UserRgvVM>());
                        rgvLogins.ScrollToEnd();
                        var userToSelect = _ocLogins.Single(l => l.Id == selLogin.Id);
                        _ocSelectedLogins.ReplaceAll(userToSelect);
                    });

                    var me = Tipster.Me();
                    var tipstersButSelf = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
                    var tipstersVM = tipstersButSelf.MapTo<TipsterRgvVM>();

                    Dispatcher.Invoke(() =>
                    {
                        _ocTipsters.ReplaceAll(tipstersVM);
                        rgvTipsters.ScrollToEnd();
                    });
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);

                if (ex is DbUpdateException && ex.InnerException is UpdateException)
                {
                    var sqlException = ex.InnerException?.InnerException as SQLiteException;
                    if (sqlException != null && sqlException.ErrorCode == 19)
                        await this.ShowMessageAsync("Wystąpił Błąd", "Nie można zmienić wartości, ponieważ taki użytkownik już istnieje");
                }
                else
                    await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private async void btnAddNewLogin_Click(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                if (txtLoadDomain.IsNullWhitespaceOrTag() || txtLoadLogin.IsNullWhitespaceOrTag() || txtLoadPassword.IsNullWhitespaceOrTag())
                    throw new Exception("Wszystkie pola muszą być poprawnie wypełnione");

                await Task.Run(() =>
                {
                    var db = new LocalDbContext();

                    var nextLId = db.Logins.Next(ent => ent.Id);

                    db.Logins.Add(new User(nextLId, Dispatcher.Invoke(() => txtLoadLogin.Text), Dispatcher.Invoke(() => txtLoadPassword.Text)));
                    Website.AddNewByAddress(db, Dispatcher.Invoke(() => txtLoadDomain.Text).Split(", "), nextLId);
                    db.SaveChanges();

                    Dispatcher.Invoke(() =>
                    {
                        _ocLogins.ReplaceAll(db.Logins.MapTo<UserRgvVM>());
                        var userToSelect = _ocLogins.Single(l => l.Id == nextLId);
                        _ocSelectedLogins.ReplaceAll(userToSelect);
                    });

                    var me = Tipster.Me();
                    var tipstersButSelf = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
                    var tipstersVM = tipstersButSelf.MapTo<TipsterRgvVM>();

                    Dispatcher.Invoke(() =>
                    {
                        _ocTipsters.ReplaceAll(tipstersVM);
                        rgvTipsters.ScrollToEnd();
                    });
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);

                if (ex is DbUpdateException && ex.InnerException is UpdateException)
                {
                    if (ex.InnerException?.InnerException is SQLiteException sqlException && sqlException.ErrorCode == 19)
                        await this.ShowMessageAsync("Wystąpił Błąd", "Nie można dodać dwóch takich samych użytkowników");
                }
                else
                    await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        #endregion

        #region - TileMenu Events

        private void tmMainMenu_MenuTIleClick(object sender, MenuTileClickedEventArgs e)
        {
            var flyouts = gridMain.FindLogicalDescendants<Grid>().Where(fo => fo.Name.EndsWith("Flyout")).ToList();
            var flyout = flyouts.Single(fo => fo.Name.Between("grid", "Flyout") == e.TileClicked.Name.AfterFirst("tl"));
            var otherFlyouts = flyouts.Except(flyout);
            foreach (var ofo in otherFlyouts)
                ofo.SlideHide();
            flyout.SlideToggle();
        }

        #endregion

        #region - Tile Events

        private void tlFlyoutHeader_Click(object sender, RoutedEventArgs e)
        {
            var tile = (Tile) sender;
            var flyout = tile.FindLogicalAncestor<Grid>(grid => grid.Name.EndsWith("Flyout"));
            flyout.SlideHide();
        }

        private void tlFlyoutHeader_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Tile) sender).Highlight(_mouseOverFlyoutHeaderTileColor);
        }

        private void tlFlyoutHeader_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Tile) sender).Unhighlight(_defaultFlyoutHeaderTileColor);
        }

        private void tlMainGridTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab((Tile) sender);
        }

        private void tlMainGridTab_MouseEnter(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _mouseOverMainGridTabTileColor);
        }

        private void tlMainGridTab_MouseLeave(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _defaultMainGridTabTileColor);
        }

        private void tlDatabaseTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab((Tile) sender);
        }

        private void tlDatabaseTab_MouseEnter(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _mouseOverDatabaseTabTileColor);
        }

        private void tlDatabaseTab_MouseLeave(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _defaultDatabaseTabTileColor);
        }

        private void tlOptionsTab_Click(object sender, RoutedEventArgs e)
        {
            SelectTab((Tile) sender);
        }

        private void tlOptionsTab_MouseEnter(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _mouseOverOptionsTabTileColor);
        }

        private void tlOptionsTab_MouseLeave(object sender, MouseEventArgs e)
        {
            HighlightTabTile((Tile) sender, _defaultOptionsTabTileColor);
        }

        #endregion

        #region - Checkbox Events

        private void cbLHOddsByPeriodFilter_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_lowestHighestOddsByPeriodFilterControls);
        }

        private void cbLHOddsByPeriodFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_lowestHighestOddsByPeriodFilterControls);
        }

        private void cbOddsLesserGreaterThan_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_odssLesserGreaterThanFilterControls);
        }

        private void cbOddsLesserGreaterThan_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_odssLesserGreaterThanFilterControls);
        }

        private void cbSelection_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_selectionFilterControls);
        }

        private void cbSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_selectionFilterControls);
        }

        private void cbTipster_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_tipsterFilterControls);
        }

        private void cbTipster_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_tipsterFilterControls);
        }

        private void cbSinceDate_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_fromDateFilterControls);
        }

        private void cbSinceDate_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_fromDateFilterControls);
        }

        private void cbToDate_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_toDateFilterControls);
        }

        private void cbToDate_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_toDateFilterControls);
        }

        private void cbPick_Checked(object sender, RoutedEventArgs e)
        {
            Extensions.EnableControls(_pickFilterControls);
        }

        private void cbPick_Unchecked(object sender, RoutedEventArgs e)
        {
            Extensions.DisableControls(_pickFilterControls);
        }

        private void cbLoadTipsFromDate_Checked(object sender, RoutedEventArgs e)
        {
            dpLoadTipsFromDate.IsEnabled = true;
        }

        private void cbLoadTipsFromDate_Unchecked(object sender, RoutedEventArgs e)
        {
            dpLoadTipsFromDate.IsEnabled = false;
        }

        private void cbHideLoginPasswordsOption_Checked(object sender, RoutedEventArgs e)
        {
            var pwCol = (GridViewDataColumn) rgvLogins.Columns["Password"];
            pwCol.DataMemberBinding = new Binding("HiddenPassword");
        }

        private void cbHideLoginPasswordsOption_Unchecked(object sender, RoutedEventArgs e)
        {
            var pwCol = (GridViewDataColumn) rgvLogins.Columns["Password"];
            pwCol.DataMemberBinding = new Binding("Password");
        }

        #endregion

        #region - Textbox Events

        private static void TxtAll_GotFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ClearValue();
        }

        private static void TxtAll_LostFocus(object sender, RoutedEventArgs e)
        {
            (sender as TextBox)?.ResetValue();
        }

        #endregion

        #region - RadNumeriUpDown Events

        private void rnumAll_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText) return;
            var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            var value = text?.ToDoubleN();
            if (value == null) return;

            var rnum = ((RadNumericUpDown) sender);
            rnum.Value = value;
            rnum.Focus();
        }

        #endregion

        #region - Dropdown Events

        private void rddlStakingTypeOnLose_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stakingType = (StakingTypeOnLose) ((RadComboBox) sender).SelectedValue;
            rnumCoeffOnLose.IsEnabled = new[] { StakingTypeOnLose.Flat, StakingTypeOnLose.Previous }.All(st => st != stakingType);
        }

        private void rddlStakingTypeOnWin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stakingType = (StakingTypeOnWin) ((RadComboBox) sender).SelectedValue;
            rnumCoeffOnWin.IsEnabled = new[] { StakingTypeOnWin.Flat, StakingTypeOnWin.Previous }.All(st => st != stakingType);
        }

        private void rddlProfitByPeriodStatistics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bs == null) return;
            _ocProfitByPeriodStatistics.ReplaceAll(new ProfitByPeriodStatisticsRgvVM(_bs.Bets, rddlProfitByPeriodStatistics.SelectedEnumValue<Period>()));
            rgvProfitByPeriodStatistics.ScrollToEnd();
        }

        #endregion

        #region - RadGridview Events

        private void rgvData_Sorting(object sender, GridViewSortingEventArgs e)
        {
            var betsVM = e.DataControl.ItemsSource as List<BetToDisplayRgvVM>;

            if (betsVM == null)
            {
                e.Cancel = true;
                return;
            }

            BetToDisplayRgvVM firstBet;
            var columnName = (e.Column as GridViewDataColumn).GetDataMemberName();

            // WŁASNE SORTOWANIE
            var dateName = nameof(firstBet.DateString);
            var betResultName = nameof(firstBet.BetResultString);
            var matchResultName = nameof(firstBet.MatchResult);
            var oddsName = nameof(firstBet.OddsString);
            var stakeName = nameof(firstBet.StakeString);
            var profitName = nameof(firstBet.ProfitString);
            var budgetName = nameof(firstBet.BudgetString);
            var tipsterName = $"{nameof(firstBet.Tipster)}.{nameof(firstBet.Tipster.Name)}";
            var pickName = nameof(firstBet.PickString);

            // DOMYŚLNE SORTOWANIE
            var nrName = nameof(firstBet.Nr);
            var matchName = nameof(firstBet.Match);

            // WYŁĄCZONE SORTOWANIE


            if (e.OldSortingState == SortingState.None) // sortuj rosnąco
            {
                e.NewSortingState = SortingState.Ascending;

                if (columnName == dateName) // Data
                    betsVM = betsVM.OrderBy(bet => bet.Date).ToList();
                else if (columnName == betResultName) // Zakład
                    betsVM = betsVM.OrderBy(bet => bet.BetResult).ToList();
                else if (columnName == matchResultName) // Wynik
                    betsVM = betsVM.OrderBy(bet => bet.MatchResult).ToList();
                else if (columnName == oddsName) // Kurs
                    betsVM = betsVM.OrderBy(bet => bet.Odds).ToList();
                else if (columnName == stakeName) // Stawka
                    betsVM = betsVM.OrderBy(bet => bet.Stake).ToList();
                else if (columnName == profitName) // Profit
                    betsVM = betsVM.OrderBy(bet => bet.Profit).ToList();
                else if (columnName == budgetName) // Budżet
                    betsVM = betsVM.OrderBy(bet => bet.Budget).ToList();
                else if (columnName == tipsterName) // Tipster
                    betsVM = betsVM.OrderBy(bet => bet.Tipster.Name).ToList();
                else if (columnName == pickName) // Pick
                    betsVM = betsVM.OrderBy(bet => bet.Pick.Choice).ThenBy(bet => bet.Pick.Value).ToList();
                else if (columnName == nrName || columnName == matchName) // Domyślnie sortowane
                    betsVM = betsVM.OrderBy(bet => bet.GetType()
                        .GetProperty((e.Column as GridViewDataColumn).GetDataMemberName())
                        .GetValue(bet, null)).ToList();
            }
            else if (e.OldSortingState == SortingState.Ascending) // soprtuj malejąco
            {
                e.NewSortingState = SortingState.Descending;

                if (columnName == dateName) // Data
                    betsVM = betsVM.OrderByDescending(bet => bet.Date).ToList();
                else if (columnName == betResultName) // Zakład
                    betsVM = betsVM.OrderByDescending(bet => bet.BetResult).ToList();
                else if (columnName == matchResultName) // Wynik
                    betsVM = betsVM.OrderByDescending(bet => bet.MatchResult).ToList();
                else if (columnName == oddsName) // Kurs
                    betsVM = betsVM.OrderByDescending(bet => bet.Odds).ToList();
                else if (columnName == stakeName) // Stawka
                    betsVM = betsVM.OrderByDescending(bet => bet.Stake).ToList();
                else if (columnName == profitName) // Profit
                    betsVM = betsVM.OrderByDescending(bet => bet.Profit).ToList();
                else if (columnName == budgetName) // Budżet
                    betsVM = betsVM.OrderByDescending(bet => bet.Budget).ToList();
                else if (columnName == tipsterName) // Tipster
                    betsVM = betsVM.OrderByDescending(bet => bet.Tipster.Name).ToList();
                else if (columnName == pickName) // Pick
                    betsVM = betsVM.OrderByDescending(bet => bet.Pick.Choice).ThenByDescending(bet => bet.Pick.Value).ToList();
                else if (columnName == nrName || columnName == matchName) // Domyślnie sortowane
                    betsVM = betsVM.OrderByDescending(bet => bet.GetType()
                        .GetProperty((e.Column as GridViewDataColumn).GetDataMemberName())
                        .GetValue(bet, null)).ToList();
            }
            else // resetuj sortowanie
            {
                e.NewSortingState = SortingState.None;
                betsVM = betsVM.OrderBy(bet => bet.Nr).ToList();
            }

            e.DataControl.ItemsSource = betsVM;
            e.Cancel = true;
        }

        private async void rgvData_Copying(object sender, GridViewClipboardEventArgs e)
        {
            e.Cancel = true;
            var selectedBets = (e.OriginalSource as RadGridView)?.SelectedItems.Cast<BetToDisplayRgvVM>().ToArray();
            if (selectedBets?.Length == 1)
            {
                var searchTerm = selectedBets.Single().Match.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";
                var copyToCB = ClipboardWrapper.TrySetText(searchTerm);
                if (copyToCB.IsFailure)
                    await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message);
            }
            e.Handled = true;
        }

        private void rgvData_RadContextMenuOpening(object sender, RoutedEventArgs e) // Wywoływane przez Reflection
        {
            var cm = (sender as FrameworkElement).ContextMenu();
            cm.EnableAll();
            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayRgvVM>().ToArray();
            if (selectedBets.Length == 0)
            {
                e.Handled = true;
                return;
            }
            if (selectedBets.Length >= 2)
                cm.Disable("Znajdź", "Kopiuj do wyszukiwania");

            cm.IsOpen = true;
        }

        private void rgvData_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            this.FindLogicalDescendants<Grid>().Where(g => g.Name.EndsWith("Flyout")).ForEach(f => f.SlideHide());
            var selBets = rgvData.SelectedItems.Cast<BetToDisplayRgvVM>().ToList();
            if (selBets.Count == 1)
            {
                var selBet = selBets.Single();
                var additionalInfo =
                    $"Oryginalny Mecz: {selBet.Match}\n" +
                    $"Oryginalny Zakład: {selBet.PickOriginalString}\n";
                lblAdditionalInfo.Content = new TextBlock { Text = additionalInfo };
            }
            else
                lblAdditionalInfo.Content = null;
        }

        private void rgvGeneralStatistics_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            rgvData.SelectionChanged -= rgvData_SelectionChanged;
            
            if (_ocSelectedGeneralStatistics.Count != 1)
            {
                rgvData.SelectionChanged += rgvData_SelectionChanged;
                return;
            }
                
            var selectedItem = (GeneralStatisticRgvVM) _ocSelectedGeneralStatistics.Single();
            if (!selectedItem.Value.HasValueBetween("(", ")")) return;

            var valStr = selectedItem.Value.Between("(", ")");
            if (!int.TryParse(valStr, out int val)) return;

            var betToFind = _ocBetsToDisplayRgvVM.Single(s => s.Nr == val);
            
            _ocSelectedBetsToDisplayRgvVM.ReplaceAll(betToFind);
            rgvData.ScrollToAsync(betToFind, () =>
            {
                _buttonsAndContextMenus.DisableControls();
                gridStatisticsContent.ShowLoader();
                gridData.ShowLoader();
            }, () =>
            {
                gridStatisticsContent.HideLoader();
                gridData.HideLoader();
                if (!gridMain.HasLoader())
                    _buttonsAndContextMenus.EnableControls();
            });

            rgvData.SelectionChanged += rgvData_SelectionChanged;
        }

        private void rgvProfitByPeriodStatistics_Sorting(object sender, GridViewSortingEventArgs e)
        {
            var pbpStats = e.DataControl.ItemsSource as ProfitByPeriodStatisticsRgvVM;

            if (pbpStats == null)
            {
                e.Cancel = true;
                return;
            }

            ProfitByPeriodStatisticRgvVM firstStat;
            var columnName = (e.Column as GridViewDataColumn).GetDataMemberName();

            // WŁASNE SORTOWANIE
            var periodName = nameof(firstStat.Period);
            var profitName = nameof(firstStat.ProfitStr);
            var countName = nameof(firstStat.Count);

            // DOMYŚLNE SORTOWANIE

            // WYŁĄCZONE SORTOWANIE

            if (e.OldSortingState == SortingState.None) // sortuj rosnąco
            {
                e.NewSortingState = SortingState.Ascending;

                if (columnName == periodName) // Okres
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderBy(bet => bet.PeriodId));
                else if (columnName == profitName) // Profit
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderBy(bet => bet.Profit));
                else if (columnName == countName) // Count
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderBy(bet => bet.Count));
            }
            else if (e.OldSortingState == SortingState.Ascending) // soprtuj malejąco
            {
                e.NewSortingState = SortingState.Descending;

                if (columnName == periodName) // Okres
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderByDescending(bet => bet.PeriodId));
                else if (columnName == profitName) // Profit
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderByDescending(bet => bet.Profit));
                else if (columnName == countName) // Count
                    pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderByDescending(bet => bet.Count));
            }
            else // resetuj sortowanie
            {
                e.NewSortingState = SortingState.None;
                pbpStats = new ProfitByPeriodStatisticsRgvVM(pbpStats.OrderBy(s => s.PeriodId));
            }

            e.DataControl.ItemsSource = pbpStats;
            e.Cancel = true;
        }

        private async void rgvTipsters_Deleted(object sender, GridViewDeletedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                await Task.Run(() =>
                {
                    var deletedTipsters = e.Items.Cast<TipsterRgvVM>().ToArray();
                    var db = new LocalDbContext();
                    if (deletedTipsters.Any())
                    {
                        var selectedTipsterIdsDdl = mddlTipsters.SelectedCustomIds();
                        var ids = deletedTipsters.Select(t => t.Id).ToArray();
                        var newIds = selectedTipsterIdsDdl.Except(ids);
                        db.Bets.RemoveByMany(b => b.TipsterId, ids);
                        db.Tipsters.RemoveByMany(t => t.Id, ids);
                        db.SaveChanges();
                        UpdateGuiWithNewTipsters();
                        Dispatcher.Invoke(() => mddlTipsters.SelectByCustomIds(newIds));
                        if (db.Bets.Any())
                            CalculateBets();
                    }
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private async void rgvLogins_Deleted(object sender, GridViewDeletedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                await Task.Run(() =>
                {
                    var deletedLogins = e.Items.Cast<UserRgvVM>().ToArray();
                    var db = new LocalDbContext();
                    if (deletedLogins.Any())
                    {
                        var ids = deletedLogins.Select(l => (int?) l.Id).ToArray();
                        foreach (var w in db.Websites)
                            if (ids.Any(id => id == w.LoginId))
                                w.LoginId = null;
                        db.Logins.RemoveByMany(b => b.Id, ids);
                        db.SaveChanges();
                        db.Websites.RemoveUnused(db.Tipsters.ButSelf());
                        db.SaveChanges();

                        var tipstersButSelf = db.Tipsters.ButSelf().Include(t => t.Website).DistinctBy(t => t.Id).ToList();
                        var tipstersVM = tipstersButSelf.MapTo<TipsterRgvVM>();

                        Dispatcher.Invoke(() =>
                        {
                            _ocTipsters.ReplaceAll(tipstersVM);
                            rgvTipsters.ScrollToEnd();
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            gridTipsters.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        private void rgvLogins_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            var selLogins = rgvLogins.SelectedItems.Cast<UserRgvVM>().ToList();
            if (selLogins.Count == 1)
            {
                txtLoadLogin.ClearValue(true);
                txtLoadPassword.ClearValue(true);
                txtLoadDomain.ClearValue(true);
                var selLogin = selLogins.Single();
                txtLoadLogin.Text = selLogin.Name;
                txtLoadPassword.Text = selLogin.Password;
                txtLoadDomain.Text = selLogin.Addresses;
            }
            else
            {
                txtLoadLogin.ResetValue(true);
                txtLoadPassword.ResetValue(true);
                txtLoadDomain.ResetValue(true);
            }
        }

        #endregion

        #region - Detepicker Events

        #endregion

        #region - Notifyicon Events

        private void notifyIcon_Click(object sender, EventArgs e)
        {
            ShowInTaskbar = true;
            _notifyIcon.Visible = false;
            WindowState = WindowState.Normal;

            if (IsVisible)
                Activate();
            else
                Show();
        }

        #endregion

        #region - ContextMenu Events

        private void cmForMddlTipsters_ItemClick(object sender, RoutedEventArgs e)
        {
            var item = (e.OriginalSource as RadMenuItem)?.DataContext as MenuItem;
            if (item == null) return;
            var ddlitems = mddlTipsters.Items.Cast<DdlItem>();
            var idsExceptMine = ddlitems.Where(i => i.Index != -1).Select(i => i.Index);
            mddlTipsters.UnselectAll();
            switch (item.Text)
            {
                case "Tylko moje":
                    mddlTipsters.SelectByCustomId(-1);
                    break;
                case "Tylko pozostałe":
                    mddlTipsters.SelectByCustomIds(idsExceptMine);
                    break;
                case "Wszystkie":
                    mddlTipsters.SelectAll();
                    break;
                case "Żaden":
                    mddlTipsters.UnselectAll();
                    break;
            }
            mddlTipsters.ScrollToStart();
        }

        private void cmForMddlPickTypes_ItemClick(object sender, RadRoutedEventArgs e)
        {
            var item = (e.OriginalSource as RadMenuItem)?.DataContext as MenuItem;
            if (item == null) return;
            mddlPickTypes.UnselectAll();
            switch (item.Text)
            {
                case "Wszystkie":
                    mddlPickTypes.SelectAll();
                    break;
                case "Żaden":
                    mddlPickTypes.UnselectAll();
                    break;
            }
            mddlPickTypes.ScrollToStart();
        }

        private void cmForDatePickers_ItemClick(object sender, RoutedEventArgs e)
        {
            var item = (e.OriginalSource as RadMenuItem)?.DataContext as MenuItem;
            if (item == null) return;

            var cm = (RadContextMenu) sender;
            var dp = (RadDatePicker) cm.UIElement;

            switch (item.Text)
            {
                case "Kopiuj":
                    ClipboardWrapper.TrySetText(dp.SelectedDate != null ? dp.SelectedDate?.ToString("dd-MM-yyyy") : "");
                    break;
                case "Wyczyść":
                    dp.SelectedDate = null;
                    break;
                case "Dzisiejsza data":
                    dp.SelectedDate = DateTime.Now.ToDMY();
                    break;
            }
            mddlTipsters.ScrollToStart();
        }

        private void cmForTextBoxes_ItemClick(object sender, RoutedEventArgs e)
        {
            var item = (e.OriginalSource as RadMenuItem)?.ContextItem();
            if (item == null) return;

            var cm = (RadContextMenu) sender;
            var txt = (TextBox) cm.UIElement;

            switch (item.Text)
            {
                case "Kopiuj":
                    ClipboardWrapper.TrySetText(txt.Text ?? "");
                    break;
                case "Wyczyść":
                    txt.ResetValue(true);
                    break;
            }
        }

        private async void cmForRgvData_ItemClick(object sender, RoutedEventArgs e)
        {
            _buttonsAndContextMenus.DisableControls();
            gridData.ShowLoader();

            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayRgvVM>().ToArray();
            var matchesStr = string.Join("\n", selectedBets.Select(b => $"{b.Match} - {b.PickString}"));
            var searchTerm = selectedBets.FirstOrDefault()?.Match.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";

            var item = (e.OriginalSource as RadMenuItem)?.DataContext as MenuItem;
            if (item == null) return;

            await Task.Run(async () =>
            {
                switch (item.Text)
                {
                    case "Znajdź":
                    {
                        var se = new SearchEngine();
                        se.FindBet(selectedBets.Single());
                        var odds = string.Join("\n", se.FoundBets.Select(b => b.ToString()));
                        var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Znalezione zakłady", odds, MessageDialogStyle.AffirmativeAndNegative));
                        if (result == MessageDialogResult.Affirmative)
                            SeleniumDriverManager.CloseAllDrivers();

                        break;
                    }
                    case "Kopiuj do wyszukiwania":
                    {
                        var copyToCB = Dispatcher.Invoke(() => ClipboardWrapper.TrySetText(searchTerm));
                        if (copyToCB.IsFailure)
                            await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message));
                        break;
                    }
                    case "Kopiuj całość":
                    {
                        var copyToCB = Dispatcher.Invoke(() => ClipboardWrapper.TrySetText(matchesStr));
                        if (copyToCB.IsFailure)
                            await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message));
                        break;
                    }
                    case "Do notatek":
                    {
                        var text = Dispatcher.Invoke(() => txtNotes.Text);
                        var tag = Dispatcher.Invoke(() => txtNotes.Tag.ToString());
                        Dispatcher.Invoke(() => txtNotes.ClearValue(true));
                        if (!string.IsNullOrEmpty(text)) text += "\n";
                        text += matchesStr;
                        var split = text.Split("\n").Where(s => s != tag);
                        var fixedSplit = new List<string>();

                        foreach (var s in split)
                            if (!fixedSplit.Any(el => el.Contains(s)))
                                fixedSplit.Add(s);

                        text = string.Join("\n", fixedSplit);

                        Dispatcher.Invoke(() => txtNotes.Text = text);
                        if (Dispatcher.Invoke(() => cbWithoutMatchesFromNotesFilter.IsChecked == true))
                            CalculateBets();
                        break;
                    }
                    case "Usuń z bazy":
                    {
                        var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Usuwanie z bazy danych", $"Czy na pewno chcesz usunąć wybrane rekordy? ({selectedBets.Length})", MessageDialogStyle.AffirmativeAndNegative));
                        if (result == MessageDialogResult.Affirmative)
                        {
                            var db = new LocalDbContext();
                            db.Bets.RemoveByMany(b => b.Id, selectedBets.Select(b => b.Id));
                            db.SaveChanges();
                            CalculateBets();
                        }

                        break;
                    }
                }
            });
            gridData.HideLoader();
            if (!gridMain.HasLoader())
                _buttonsAndContextMenus.EnableControls();
        }

        #endregion

        #region - Grid Events

        private void gridFlyout_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {
            var fo = sender as Grid;
            if (fo == null) return;

            var tile = spMenu.FindLogicalDescendants<Tile>().Single(tl => tl.Name.AfterFirst("tl") == fo.Name.Between("grid", "Flyout"));
            if (!fo.IsVisible) // zamykanie flyouta
            {
                tile.Unhighlight(_defaultMainMenuTileColor);
                _mainMenu.SelectedTile = null;
            }
            else // otwieranie flyouta
            {
                tile.Highlight(_mouseOverMainMenuTileColor);
                _mainMenu.SelectedTile = tile;
            }
        }

        #endregion

        #region - TabControl Events

        private void matcMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectTabTile((MetroAnimatedTabControl) sender, _defaultMainGridTabTileColor, _mouseOverMainGridTabTileColor);
        }

        private void matcDatabase_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectTabTile((MetroAnimatedTabControl) sender, _defaultDatabaseTabTileColor, _mouseOverDatabaseTabTileColor);
        }

        private void matcOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectTabTile((MetroAnimatedTabControl) sender, _defaultOptionsTabTileColor, _mouseOverOptionsTabTileColor);
        }

        #endregion

        #region - DataLoader Events

        private void dl_InformationSent(object sender, InformationSentEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                var loaderStatuses = this.FindLogicalDescendants<TextBlock>().Where(c => c.Name == "prLoaderStatus").ToArray();
                foreach (var tb in loaderStatuses)
                    tb.Text = e.Information;
            });
        }

        #endregion

        #endregion

        #region Methods

        #region - Controls Management

        private static void SetupZIndexes(FrameworkElement container, int i = 0)
        {
            foreach (var f in LogicalTreeHelper.GetChildren(container).OfType<FrameworkElement>())
            {
                Panel.SetZIndex(f, i++);
                SetupZIndexes(f, i);
            }
        }

        private void SetupDropdowns()
        {
            rddlStakingTypeOnLose.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) StakingTypeOnLose.Flat, "płaska stawka"),
                new DdlItem((int) StakingTypeOnLose.Add, "dodaj"),
                new DdlItem((int) StakingTypeOnLose.Subtract, "odejmij"),
                new DdlItem((int) StakingTypeOnLose.Multiply, "mnóż x"),
                new DdlItem((int) StakingTypeOnLose.Divide, "dziel przez"),
                new DdlItem((int) StakingTypeOnLose.CoverPercentOfLoses, "pokryj % strat"),
                new DdlItem((int) StakingTypeOnLose.UsePercentOfBudget, "użyj % budżetu")
            };

            rddlStakingTypeOnLose.DisplayMemberPath = "Text";
            rddlStakingTypeOnLose.SelectedValuePath = "Index";
            rddlStakingTypeOnLose.SelectByCustomId(-1);
            rddlStakingTypeOnLose.SelectionChanged += rddlStakingTypeOnLose_SelectionChanged;

            rddlStakingTypeOnWin.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) StakingTypeOnWin.Flat, "płaska stawka"),
                new DdlItem((int) StakingTypeOnWin.Add, "dodaj"),
                new DdlItem((int) StakingTypeOnWin.Subtract, "odejmij"),
                new DdlItem((int) StakingTypeOnWin.Multiply, "mnóż x"),
                new DdlItem((int) StakingTypeOnWin.Divide, "dziel przez"),
                new DdlItem((int) StakingTypeOnWin.UsePercentOfBudget, "użyj % budżetu")
            };

            rddlStakingTypeOnWin.DisplayMemberPath = "Text";
            rddlStakingTypeOnWin.SelectedValuePath = "Index";
            rddlStakingTypeOnWin.SelectByCustomId(-1);
            rddlStakingTypeOnWin.SelectionChanged += rddlStakingTypeOnWin_SelectionChanged;

            rddlOddsLesserGreaterThan.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) OddsLesserGreaterThanFilterChoice.GreaterThan, ">="),
                new DdlItem((int) OddsLesserGreaterThanFilterChoice.LesserThan, "<="),
            };

            rddlOddsLesserGreaterThan.DisplayMemberPath = "Text";
            rddlOddsLesserGreaterThan.SelectedValuePath = "Index";
            rddlOddsLesserGreaterThan.SelectByCustomId(-1);

            rddlLoseCondition.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) LoseCondition.PreviousPeriodLost, "Przegrany poprzedni okres"),
                new DdlItem((int) LoseCondition.BudgetLowerThanMax, "Budżet niższy od największego"),
            };

            rddlLoseCondition.DisplayMemberPath = "Text";
            rddlLoseCondition.SelectedValuePath = "Index";
            rddlLoseCondition.SelectByCustomId(-1);

            rddlBasicStake.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) BasicStake.Base, "bazowa"),
                new DdlItem((int) BasicStake.Previous, "poprzednia"),
            };

            rddlBasicStake.DisplayMemberPath = "Text";
            rddlBasicStake.SelectedValuePath = "Index";
            rddlBasicStake.SelectByCustomId(-1);

            rddlProfitByPeriodStatistics.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) Period.Month, "Zysk miesięczny"),
                new DdlItem((int) Period.Week, "Zysk tygodniowy"),
                new DdlItem((int) Period.Day, "Zysk dzienny"),
            };

            rddlProfitByPeriodStatistics.DisplayMemberPath = "Text";
            rddlProfitByPeriodStatistics.SelectedValuePath = "Index";
            rddlProfitByPeriodStatistics.SelectByCustomId(-1);
            rddlProfitByPeriodStatistics.SelectionChanged += rddlProfitByPeriodStatistics_SelectionChanged;

            var ddlitems = EnumUtils.EnumToDdlItems<PickChoice>(Pick.ConvertChoiceToString);
            mddlPickTypes.ItemsSource = ddlitems;
            mddlPickTypes.DisplayMemberPath = "Text";
            mddlPickTypes.SelectedValuePath = "Index";
            mddlPickTypes.SelectAll();
        }

        private void SetupGrids()
        {
            foreach (var fo in this.FindLogicalDescendants<Grid>().Where(g => g.Name.EndsWith("Flyout")))
            {
                var margin = fo.Margin;
                fo.Margin = new Thickness(0, margin.Top, 0, 0);
                fo.IsVisibleChanged += gridFlyout_VisibilityChanged;
                fo.Visibility = Visibility.Collapsed;
            }
        }

        private void SetupNotifyIcon()
        {
            var iconHandle = Properties.Resources.NotifyIcon.GetHicon();
            var icon = System.Drawing.Icon.FromHandle(iconHandle);

            _notifyIcon = new NotifyIcon
            {
                BalloonTipTitle = @"Symulator Zakładów",
                BalloonTipText = @"Tutaj się schował",
                Icon = icon
            };
            _notifyIcon.Click += notifyIcon_Click;
        }

        private void SetupTextBoxes()
        {
            foreach (var txtB in this.FindLogicalDescendants<TextBox>().Where(t => t.Tag != null))
            {
                txtB.GotFocus += TxtAll_GotFocus;
                txtB.LostFocus += TxtAll_LostFocus;

                var currBg = ((SolidColorBrush) txtB.Foreground).Color;
                txtB.FontStyle = FontStyles.Italic;
                txtB.Text = txtB.Tag.ToString();
                txtB.Foreground = new SolidColorBrush(Color.FromArgb(128, currBg.R, currBg.G, currBg.B));
            }
        }

        private void SetupGridviews()
        {
            rgvLogins.ItemsSource = _ocLogins;
            rgvLogins.SetSelecteditemsSource(_ocSelectedLogins);
            rgvTipsters.ItemsSource = _ocTipsters;
            rgvTipsters.SetSelecteditemsSource(_ocSelectedTipsters);
            rgvData.ItemsSource = _ocBetsToDisplayRgvVM;
            rgvData.SetSelecteditemsSource(_ocSelectedBetsToDisplayRgvVM);
            rgvAggregatedWinsLosesStatistics.ItemsSource = _ocAggregatedWinLoseStatisticsRgvVM;
            rgvAggregatedWinsLosesStatistics.SetSelecteditemsSource(_ocSelectedAggregatedWinLoseStatisticsRgvVM);
            rgvProfitByPeriodStatistics.ItemsSource = _ocProfitByPeriodStatistics;
            rgvProfitByPeriodStatistics.SetSelecteditemsSource(_ocSelectedProfitByPeriodStatistics);
            rgvGeneralStatistics.ItemsSource = _ocGeneralStatistics;
            rgvGeneralStatistics.SetSelecteditemsSource(_ocSelectedGeneralStatistics);

            var loginsVM = new LocalDbContext().Logins.Include(l => l.Websites).MapTo<UserRgvVM>();
            _ocLogins.ReplaceAll(loginsVM);
            rgvLogins.ScrollToEnd();
        }

        private void SetupUpDowns()
        {
            foreach (var ud in this.FindLogicalDescendants<RadNumericUpDown>())
            {
                DataObject.AddPastingHandler(ud, rnumAll_Pasting);
            }
        }

        private void SetupDatePickers()
        {
            var cultureInfo = new CultureInfo("pl-PL");
            var dateInfo = new DateTimeFormatInfo
            {
                ShortDatePattern = "dd-MM-yyyy"
            };
            cultureInfo.DateTimeFormat = dateInfo;

            foreach (var dp in this.FindLogicalDescendants<RadDatePicker>())
                dp.Culture = cultureInfo;

            dpLoadTipsFromDate.SelectedDate = DateTime.Now.Subtract(new TimeSpan(2, 0, 0, 0));
        }

        private void SetupTiles()
        {
            _mouseOverMainMenuTileColor = ((SolidColorBrush) FindResource("MouseOverMainMenuTileBrush")).Color;
            _defaultMainMenuTileColor = ((SolidColorBrush) FindResource("DefaultMainMenuTileBrush")).Color;
            _mouseOverMainMenuResizeTileColor = ((SolidColorBrush) FindResource("MouseOverMainMenuResizeTileBrush")).Color;
            _defaultMainMenuResizeTileColor = ((SolidColorBrush) FindResource("DefaultMainMenuResizeTileBrush")).Color;

            _mainMenu = spMenu.TilesMenu(false, 150, 
                _mouseOverMainMenuTileColor, _defaultMainMenuTileColor, 
                _mouseOverMainMenuResizeTileColor, _defaultMainMenuResizeTileColor);
            _mainMenu.MenuTileClick += tmMainMenu_MenuTIleClick;

            _defaultFlyoutHeaderTileColor = ((SolidColorBrush) FindResource("DefaultFlyoutHeaderTileBrush")).Color;
            _mouseOverFlyoutHeaderTileColor = ((SolidColorBrush) FindResource("MouseOverFlyoutHeaderTileBrush")).Color;
            _defaultFlyoutHeaderIconColor = ((SolidColorBrush) FindResource("DefaultFlyoutHeaderIconBrush")).Color;

            var flyoutCloseTiles = this.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("FlyoutHeader")).ToArray();
            foreach (var tl in flyoutCloseTiles)
            {
                tl.Background = new SolidColorBrush(_defaultFlyoutHeaderTileColor);
                tl.Click += tlFlyoutHeader_Click;
                tl.MouseEnter += tlFlyoutHeader_MouseEnter;
                tl.MouseLeave += tlFlyoutHeader_MouseLeave;
            }


            _defaultMainGridTabTileColor = ((SolidColorBrush) FindResource("DefaultMainGridTabTileBrush")).Color;
            _mouseOverMainGridTabTileColor = ((SolidColorBrush) FindResource("MouseOverMainGridTabTileBrush")).Color;

            var mainGridTabTiles = this.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("MainGridTab")).ToArray();
            foreach (var tl in mainGridTabTiles)
            {
                tl.Background = new SolidColorBrush(_defaultMainGridTabTileColor);
                tl.Click += tlMainGridTab_Click;
                tl.MouseEnter += tlMainGridTab_MouseEnter;
                tl.MouseLeave += tlMainGridTab_MouseLeave;
            }


            _defaultDatabaseTabTileColor = ((SolidColorBrush) FindResource("DefaultDatabaseTabTileBrush")).Color;
            _mouseOverDatabaseTabTileColor = ((SolidColorBrush) FindResource("MouseOverDatabaseTabTileBrush")).Color;

            var DatabaseTabTiles = this.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("DatabaseTab")).ToArray();
            foreach (var tl in DatabaseTabTiles)
            {
                tl.Background = new SolidColorBrush(_defaultDatabaseTabTileColor);
                tl.Click += tlDatabaseTab_Click;
                tl.MouseEnter += tlDatabaseTab_MouseEnter;
                tl.MouseLeave += tlDatabaseTab_MouseLeave;
            }


            _defaultOptionsTabTileColor = ((SolidColorBrush) FindResource("DefaultOptionsTabTileBrush")).Color;
            _mouseOverOptionsTabTileColor = ((SolidColorBrush) FindResource("MouseOverOptionsTabTileBrush")).Color;

            var OptionsTabTiles = this.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("OptionsTab")).ToArray();
            foreach (var tl in OptionsTabTiles)
            {
                tl.Background = new SolidColorBrush(_defaultOptionsTabTileColor);
                tl.Click += tlOptionsTab_Click;
                tl.MouseEnter += tlOptionsTab_MouseEnter;
                tl.MouseLeave += tlOptionsTab_MouseLeave;
            }
        }

        private void SetupTabControls()
        {
            matcMain_SelectionChanged(matcMain, null); // wywołaj sztucznie raz na początku
            matcMain.SelectionChanged += matcMain_SelectionChanged;

            matcDatabase_SelectionChanged(matcDatabase, null);
            matcDatabase.SelectionChanged += matcDatabase_SelectionChanged;

            matcOptions_SelectionChanged(matcOptions, null);
            matcOptions.SelectionChanged += matcOptions_SelectionChanged;
        }

        private void RunOptionsDependentAdjustments()
        {
            if (_mainMenu.IsFullSize)
                this.CenterOnScreen();
        }

        private void InitializeControlGroups()
        {
            _lowestHighestOddsByPeriodFilterControls = new List<UIElement>
            {
                rbHighestOddsByPeriod,
                rbLowestOddsByPeriod,
                rbSumOddsByPeriod,
                rnumLHOddsPeriodInDays,
            };

            _odssLesserGreaterThanFilterControls = new List<UIElement>
            {
                rnumOddsLesserGreaterThan,
                rddlOddsLesserGreaterThan
            };

            _selectionFilterControls = new List<UIElement>
            {
                rbSelected,
                rbUnselected,
                rbVisible
            };

            _tipsterFilterControls = new List<UIElement>
            {
                mddlTipsters
            };

            _fromDateFilterControls = new List<UIElement>
            {
                dpSinceDate
            };

            _toDateFilterControls = new List<UIElement>
            {
                dpToDate
            };

            _pickFilterControls = new List<UIElement>
            {
                mddlPickTypes,
            };

            var buttons = this.FindLogicalDescendants<Button>().Where(b => b.GetType() != typeof(Tile)).ToList();
            var contextMenus = this.FindLogicalDescendants<FrameworkElement>().Select(fe => fe.ContextMenu()).Where(cm => cm != null);
            _buttonsAndContextMenus.ReplaceAll(buttons).AddRange(contextMenus);
        }

        private void InitializeContextMenus()
        {
            InitializeCmForMddlTipsters();
            InitializeCmForMddlPickTypes();
            InitializeCmForRgvData();
            InitializeCmForDatePickers();
            InitializeCmForTextBoxes();
        }

        private void InitializeCmForMddlTipsters()
        {
            var items = new ObservableCollection<MenuItem>
            {
                new MenuItem("Tylko moje"),
                new MenuItem("Tylko pozostałe"),
                new MenuItem("Wszystkie"),
                new MenuItem("Żaden")
            };

            cmForMddlTipsters.ItemsSource = items;
            cmForMddlTipsters.ItemClick += cmForMddlTipsters_ItemClick;
        }

        private void InitializeCmForMddlPickTypes()
        {
            var items = new ObservableCollection<MenuItem>
            {
                new MenuItem("Wszystkie"),
                new MenuItem("Żaden")
            };

            cmForMddPickTypes.ItemsSource = items;
            cmForMddPickTypes.ItemClick += cmForMddlPickTypes_ItemClick;
        }

        private void InitializeCmForRgvData()
        {
            var items = new ObservableCollection<MenuItem>
            {
                new MenuItem("Znajdź"),
                new MenuItem("Kopiuj do wyszukiwania"),
                new MenuItem("Kopiuj całość"),
                new MenuItem("Do notatek"),
                new MenuItem("Usuń z bazy"),
            };

            cmForRgvData.ItemsSource = items;
            cmForRgvData.ItemClick += cmForRgvData_ItemClick;
        }

        private void InitializeCmForDatePickers()
        {
            var items = new ObservableCollection<MenuItem>
            {
                new MenuItem("Kopiuj"),
                new MenuItem("Wyczyść"),
                new MenuItem("Dzisiejsza data")
            };

            foreach (var dp in this.FindLogicalDescendants<RadDatePicker>())
            {
                var cm = RadContextMenu.GetContextMenu(dp);
                if (cm != null)
                {
                    cm.ItemsSource = items;
                    cm.ItemClick += cmForDatePickers_ItemClick;
                }
            }
        }

        private void InitializeCmForTextBoxes()
        {
            var items = new ObservableCollection<MenuItem>
            {
                new MenuItem("Kopiuj"),
                new MenuItem("Wyczyść")
            };

            foreach (var txt in this.FindLogicalDescendants<TextBox>())
            {
                var cm = RadContextMenu.GetContextMenu(txt);
                if (cm != null)
                {
                    cm.ItemsSource = items;
                    cm.ItemClick += cmForTextBoxes_ItemClick;
                }
            }
        }

        private void ClearGridViews()
        {
            _ocLogins.Clear();
            _ocSelectedLogins.Clear();
            _ocTipsters.Clear();
            _ocSelectedTipsters.Clear();
            _ocBetsToDisplayRgvVM.Clear();
            _ocSelectedBetsToDisplayRgvVM.Clear();
            _ocProfitByPeriodStatistics.Clear();
            _ocSelectedProfitByPeriodStatistics.Clear();
            _ocAggregatedWinLoseStatisticsRgvVM.Clear();
            _ocSelectedAggregatedWinLoseStatisticsRgvVM.Clear();
            _ocGeneralStatistics.Clear();
            _ocSelectedGeneralStatistics.Clear();
        }

        private void HandleContextMenu(FrameworkElement fe, MouseEventArgs e)
        {
            var cm = fe.ContextMenu();
            
            var pos = e.GetPosition(fe).TranslateY(-fe.Height);
            //var menuRightLowerCornerScreenPos = fe.PointToScreen(pos).Translate(cm.ActualWidth, cm.ActualHeight);
            cm.VerticalOffset = pos.Y;
            if (pos.Y + cm.ActualHeight > 0) cm.VerticalOffset = 0 - cm.ActualHeight;
            cm.HorizontalOffset = pos.X;
            if (pos.X + cm.ActualWidth > fe.ActualWidth) cm.HorizontalOffset = fe.ActualWidth - cm.ActualWidth;
            e.Handled = true;

            var handler = GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == $"{fe.Name}_RadContextMenuOpening");
            if (handler != null)
                handler.Invoke(this, new object[] { fe, e });
            else
                cm.IsOpen = true;
        }

        private static void SelectTab(DependencyObject tile)
        {
            var tabItem = tile.FindLogicalAncestor<MetroTabItem>();
            var tabControl = tabItem.FindLogicalAncestor<MetroAnimatedTabControl>();
            tabControl.SelectedItem = tabItem;
        }

        private static void HighlightTabTile(Control tile, Color color)
        {
            var tabControl = tile.FindLogicalAncestor<MetroAnimatedTabControl>();
            var gridTabTiles = tabControl.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("Tab")).ToArray();
            var selectedTile = gridTabTiles.Single(tl => tl.FindLogicalAncestor<MetroTabItem>().IsSelected);
            if (Equals(tile, selectedTile))
                return;

            tile.Highlight(color);
        }

        private static void SelectTabTile(Selector tabControl, Color defaultCOlor, Color mouseOverCOlor)
        {
            var selectedTab = (MetroTabItem)tabControl.SelectedItem;
            var selectedTile = selectedTab.FindLogicalDescendants<Tile>().Single();
            var gridTabTiles = tabControl.FindLogicalDescendants<Tile>().Where(tl => tl.Name.EndsWith("Tab")).ToArray();
            var otherTiles = gridTabTiles.Except(selectedTile);

            foreach (var tl in otherTiles)
                tl.Unhighlight(defaultCOlor);
            selectedTile.Highlight(mouseOverCOlor);
        }

        #endregion
        
        #region - Core Functionality

        public int UpdateGuiWithNewTipsters()
        {
            var db = new LocalDbContext();
            var ddlTipsters = new List<DdlItem> { new DdlItem(-1, "(Moje Zakłady)") };
            var tipsters = db.Tipsters.Include(t => t.Website);
            var tipstersButSelf = tipsters.ButSelf().DistinctBy(t => t.Id).ToList();
            ddlTipsters.AddRange(tipstersButSelf.Select(t => new DdlItem(t.Id, $"{t.Name} ({t.Website.Address})")));
            Dispatcher.Invoke(() =>
            {
                mddlTipsters.ItemsSource = ddlTipsters;
                mddlTipsters.DisplayMemberPath = "Text";
                mddlTipsters.SelectedValuePath = "Index";
            });
            
            var tipstersVM = tipstersButSelf.OrderBy(t => t.Name).MapTo<TipsterRgvVM>();
            var addedTipsters = new List<TipsterRgvVM>();
            Dispatcher.Invoke(() =>
            {
                addedTipsters = tipstersVM.Except(_ocTipsters).OrderBy(t => t.Name).ToList();
                var oldTIpstersCount = _ocTipsters.Count; // lub sklonować kolekcję jeśli potrzebne będzie coś oprócz ilości
                _ocTipsters.ReplaceAll(tipstersVM);
                if (addedTipsters.Any())
                {
                    var firstAddedTipster = addedTipsters.First();
                    rgvTipsters.ScrollToAsync(firstAddedTipster, () =>
                    {
                        _buttonsAndContextMenus.DisableControls();
                        gridTipsters.ShowLoader();
                    }, () =>
                    {
                        gridTipsters.HideLoader();
                        if (!gridMain.HasLoader())
                            _buttonsAndContextMenus.EnableControls();
                    });
                    if (oldTIpstersCount > 0)
                        _ocSelectedTipsters.ReplaceAll(addedTipsters);
                }
                txtLoad.ResetValue(true);
            });

            db.Websites.RemoveUnused(db.Tipsters.ButSelf());
            return addedTipsters.Count;
        }

        public void DownloadTipsterToDb()
        {
            try
            {
                string newLink = null;
                string placeholder = null;
                var headlessMode = false;
                Dispatcher.Invoke(() =>
                {
                    headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                    newLink = txtLoad.Text;
                    placeholder = txtLoad.Tag?.ToString();
                });

                if (!newLink.IsNullWhiteSpaceOrDefault(placeholder))
                {
                    DataLoader dl;
                    if (new Uri(newLink).Host.ToLower().Contains(betshoot))
                        dl = new BetShootLoader(newLink);
                    else if (new Uri(newLink).Host.ToLower().Contains(hintwise))
                        dl = new HintWiseLoader(newLink, headlessMode);
                    else
                        throw new Exception($"Nie istnieje loader dla strony: {newLink}");
                    dl.InformationSent += dl_InformationSent;
                    dl.DownloadNewTipster();
                }
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
            }
        }

        private void DownloadTipsToDb()
        {
            var db = new LocalDbContext();

            try
            {
                var me = Tipster.Me();
                var tipsters = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).ToArray();
                var selectedTipstersIds = rgvTipsters.SelectedItems.Cast<TipsterRgvVM>().Select(t => t.Id).ToArray();
                var selectedTipsters = tipsters.WhereByMany(t => t.Id, selectedTipstersIds).ToArray();

                var headlessMode = false;
                var onlySelected = false;
                DateTime? fromDate = null;
                Dispatcher.Invoke(() =>
                {
                    headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                    fromDate = cbLoadTipsFromDate.IsChecked == true
                        ? dpLoadTipsFromDate.SelectedDate.ToDMY()
                        : null;
                    onlySelected = cbLoadTipsOnlySelected.IsChecked == true;
                });
                
                foreach (var t in onlySelected ? selectedTipsters : tipsters)
                {
                    db.Entry(t.Website).Reference(e => e.Login).Load();
                    DataLoader dl;
                    if (t.Website.Address.ToLower() == betshoot)
                    {
                        dl = new BetShootLoader(t.Link);
                        dl.InformationSent += dl_InformationSent;
                        dl.DownloadTips();
                    }
                    else if (t.Website.Address.ToLower() == hintwise)
                    {
                        dl = new HintWiseLoader(t.Link, t.Website.Login?.Name, t.Website.Login?.Password, headlessMode);
                        dl.InformationSent += dl_InformationSent;
                        ((HintWiseLoader) dl).DownloadTips(fromDate);
                    }
                    else
                        throw new Exception($"Nie istnieje loader dla strony: {t.Website.Address}");
                }
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
                db.Dispose();
            }
        }

        private void CalculateBets()
        {
            var db = new LocalDbContext();
            var stakingTypeOnLose = (StakingTypeOnLose) Dispatcher.Invoke(() => rddlStakingTypeOnLose.SelectedValue);
            var stakingTypeOnWin = (StakingTypeOnWin) Dispatcher.Invoke(() => rddlStakingTypeOnWin.SelectedValue);

            var betsByDate = db.Bets.Include(b => b.Tipster).Include(b => b.Pick)
                .OrderBy(b => b.Date).ThenBy(b => b.Match);

            var pendingBets = betsByDate.Where(b => b.BetResult == (int) Result.Pending).ToList();
            var bets = betsByDate.ExceptBy(pendingBets, b => b.Id).ToList();
            bets = bets.Concat(pendingBets).ToList(); //.Where(b => !b.Pick.ToLower().Contains("both"))
            var initStake = Dispatcher.Invoke(() => rnumInitialStake.Value ?? 0);
            var initialBudget = Dispatcher.Invoke(() => rnumBudget.Value ?? 0);
            var budgetIncreaseRef = Dispatcher.Invoke(() => rnumBudgetIncrease.Value ?? 0);
            var stakeIncrease = Dispatcher.Invoke(() => rnumStakeIncrease.Value ?? 0);
            var budgetDecreaseRef = Dispatcher.Invoke(() => rnumBudgetDecrease.Value ?? 0);
            var stakeDecrease = Dispatcher.Invoke(() => rnumStakeDecrease.Value ?? 0);
            var loseCoeff = Dispatcher.Invoke(() => rnumCoeffOnLose.Value ?? 1);
            var winCoeff = Dispatcher.Invoke(() => rnumCoeffOnWin.Value ?? 1);
            var loseCondition = (LoseCondition) Dispatcher.Invoke(() => rddlLoseCondition.SelectedValue);
            var maxStake = Dispatcher.Invoke(() => rnumMaxStake.Value ?? 0);
            var resetStake = (BasicStake) Dispatcher.Invoke(() => rddlBasicStake.SelectedValue) == BasicStake.Base;

            if (bets.Count == 0) throw new Exception("W bazie danych nie ma żadnych zakładów");
            if (stakeIncrease > budgetIncreaseRef) throw new Exception("Stawka nie może rosnąć szybciej niż budżet");

            var visibleBets = rgvData.Items.Cast<BetToDisplayRgvVM>().ToList();
            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayRgvVM>().ToList();
            var applySelectionFilter = Dispatcher.Invoke(() => cbSelection.IsChecked) == true;
            var getSelectedBets = Dispatcher.Invoke(() => rbSelected.IsChecked) == true;
            var getUnselectedBets = Dispatcher.Invoke(() => rbUnselected.IsChecked) == true;

            var fromCkd = Dispatcher.Invoke(() => cbSinceDate.IsChecked) == true;
            var toCkd = Dispatcher.Invoke(() => cbToDate.IsChecked) == true;
            var applyDateFilter = fromCkd || toCkd;
            var fromDate = fromCkd ? Dispatcher.Invoke(() => dpSinceDate.SelectedDate).ToDMY() : null;
            var toDate = toCkd ? Dispatcher.Invoke(() => dpToDate.SelectedDate).ToDMY() : null;

            var applyNotesFilter = Dispatcher.Invoke(() => cbWithoutMatchesFromNotesFilter.IsChecked) == true;

            var applyPickFilter = Dispatcher.Invoke(() => cbPick.IsChecked) == true;
            var selectedPickTypes = Dispatcher.Invoke(() => mddlPickTypes.SelectedCustomIds());

            var applyTipsterFilter = Dispatcher.Invoke(() => cbTipster.IsChecked) == true;
            var tipsterIds = Dispatcher.Invoke(() => mddlTipsters.SelectedCustomIds());
            var tipsters = new List<Tipster>();
            if (tipsterIds.Length == 1 && tipsterIds.Single() == -1)
            {
                db.Tipsters.AddOrUpdate(Tipster.Me());
                db.SaveChanges();
            }
            else
                tipsters = db.Tipsters.Where(t => tipsterIds.Any(id => id == t.Id)).ToList();

            var oddRef = Dispatcher.Invoke(() => rnumOddsLesserGreaterThan.Value) ?? 0;
            var applyOddsLesserGreaterThanFilter = Dispatcher.Invoke(() => cbOddsLesserGreaterThan.IsChecked) == true;
            var oddsLesserGreaterThanSelected = (OddsLesserGreaterThanFilterChoice)Dispatcher.Invoke(() => rddlOddsLesserGreaterThan.SelectedValue);
            var getOddsGreaterThan = oddsLesserGreaterThanSelected == OddsLesserGreaterThanFilterChoice.GreaterThan;

            var applyLowestHighestOddsByPeriodFilter = Dispatcher.Invoke(() => cbLHOddsByPeriodFilter.IsChecked) == true;
            var getHighestOddsByPeriod = Dispatcher.Invoke(() => rbHighestOddsByPeriod.IsChecked) == true;
            var getLowestOddsByPeriod = Dispatcher.Invoke(() => rbLowestOddsByPeriod.IsChecked) == true;
            var period = (int) (Dispatcher.Invoke(() => rnumLHOddsPeriodInDays.Value) ?? 1);

            var dataFiltersList = new List<DataFilter>();

            if (applyNotesFilter)
                dataFiltersList.Add(new NotesFilter(Dispatcher.Invoke(() => txtNotes.IsNullWhitespaceOrTag()) ? null : Dispatcher.Invoke(() => txtNotes.Text)));

            if (applySelectionFilter)
            {
                dataFiltersList.Add(getSelectedBets
                    ? new SelectionFilter(SelectionFilterChoice.Selected, selectedBets, visibleBets)
                    : getUnselectedBets
                        ? new SelectionFilter(SelectionFilterChoice.Unselected, selectedBets, visibleBets)
                        : new SelectionFilter(SelectionFilterChoice.Visible, selectedBets, visibleBets));
                Dispatcher.Invoke(() => rbVisible.IsChecked = true);
            }

            if (applyPickFilter)
                dataFiltersList.Add(new PickFilter(selectedPickTypes));

            if (applyDateFilter)
                dataFiltersList.Add(new DateFilter(fromDate, toDate));

            if (applyTipsterFilter)
                dataFiltersList.Add(new TipsterFilter(tipsters));

            if (applyOddsLesserGreaterThanFilter)
                dataFiltersList.Add(getOddsGreaterThan
                    ? new OddsLesserGreaterThanFilter(OddsLesserGreaterThanFilterChoice.GreaterThan, oddRef)
                    : new OddsLesserGreaterThanFilter(OddsLesserGreaterThanFilterChoice.LesserThan, oddRef));

            if (applyLowestHighestOddsByPeriodFilter)
                dataFiltersList.Add(getHighestOddsByPeriod
                    ? new LowestHighestSumOddsByPeriodFilter(LowestHighestSumOddsByPeriodFilterChoice.HighestByPeriod, period)
                    : getLowestOddsByPeriod
                        ? new LowestHighestSumOddsByPeriodFilter(LowestHighestSumOddsByPeriodFilterChoice.LowestByPeriod, period)
                        : new LowestHighestSumOddsByPeriodFilter(LowestHighestSumOddsByPeriodFilterChoice.SumByPeriod, period));

            var bs = new BettingSystem(
                initStake,
                initialBudget,
                stakingTypeOnLose,
                stakingTypeOnWin,
                bets,
                dataFiltersList,
                budgetIncreaseRef,
                stakeIncrease,
                budgetDecreaseRef,
                stakeDecrease,
                loseCoeff,
                winCoeff,
                loseCondition,
                maxStake,
                resetStake);

            bs.ApplyFilters();
            bs.ApplyStaking();
            
            Dispatcher.Invoke(() =>
            {
                _ocBetsToDisplayRgvVM.ReplaceAll(bs.Bets);
                rgvData.ScrollToEnd();
                _ocAggregatedWinLoseStatisticsRgvVM.ReplaceAll(new AggregatedWinLoseStatisticsRgvVM(bs.LosesCounter, bs.WinsCounter));
                _ocProfitByPeriodStatistics.ReplaceAll(new ProfitByPeriodStatisticsRgvVM(bs.Bets, rddlProfitByPeriodStatistics.SelectedEnumValue<Period>()));
                rgvProfitByPeriodStatistics.ScrollToEnd();
            });

            if (bs.Bets.Any())
            {
                var maxStakeBet = bs.Bets.MaxBy(b => b.Stake);
                var minBudgetBet = bs.Bets.MinBy(b => b.Budget);
                var maxBudgetBet = bs.Bets.MaxBy(b => b.Budget);
                var minBudgetInclStakeBet = bs.Bets.MinBy(b => b.BudgetBeforeResult);
                var losesInRow = bs.LosesCounter.Any(c => c.Value > 0) ? bs.LosesCounter.MaxBy(c => c.Value).ToString() : "-";
                var winsInRow = bs.WinsCounter.Any(c => c.Value > 0) ? bs.WinsCounter.MaxBy(c => c.Value).ToString() : "-";

                var lostOUfromWonBTTS = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                    (b.Pick.Value.ToDouble().Eq(0) && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() > 2 ||
                    b.Pick.Value.ToDouble().Eq(1) && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() <= 2));

                var wonOUfromLostBTTS = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                    (b.Pick.Value.ToDouble().Eq(0) && b.BetResult == Result.Lose && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() <= 2 ||
                    b.Pick.Value.ToDouble().Eq(1) && b.BetResult == Result.Lose && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() > 2));

                var wonBTTSwithOU = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                    (b.Pick.Value.ToDouble().Eq(0) && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() <= 2 ||
                    b.Pick.Value.ToDouble().Eq(1) && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => x.ToInt()).Sum() > 2));

                var gs = new GeneralStatisticsRgvVM();
                gs.Add(new GeneralStatisticRgvVM("Maksymalna stawka:", $"{maxStakeBet.Stake:0.00} zł ({maxStakeBet.Nr})"));
                gs.Add(new GeneralStatisticRgvVM("Najwyższy budżet:", $"{maxBudgetBet.Budget:0.00} zł ({maxBudgetBet.Nr})"));
                gs.Add(new GeneralStatisticRgvVM("Najniższy budżet:", $"{minBudgetBet.Budget:0.00} zł ({minBudgetBet.Nr})"));
                gs.Add(new GeneralStatisticRgvVM("Najniższy budżet po odjęciu stawki:", $"{minBudgetInclStakeBet.BudgetBeforeResult:0.00} zł ({minBudgetInclStakeBet.Nr})"));
                gs.Add(new GeneralStatisticRgvVM("Porażki z rzędu:", $"{losesInRow}"));
                gs.Add(new GeneralStatisticRgvVM("Zwycięstwa z rzędu:", $"{winsInRow}"));
                gs.Add(new GeneralStatisticRgvVM("Nierozstrzygnięte:", $"{bs.Bets.Count(b => b.BetResult == Result.Pending)} [dziś: {bs.Bets.Count(b => b.BetResult == Result.Pending && b.Date.ToDMY() == DateTime.Now.ToDMY())}]"));
                gs.Add(new GeneralStatisticRgvVM("BTTS y/n => o/u 2.5 [L - W]:", $"{lostOUfromWonBTTS} - {wonOUfromLostBTTS} [{wonOUfromLostBTTS - lostOUfromWonBTTS}]"));
                if (lostOUfromWonBTTS + wonBTTSwithOU != 0)
                    gs.Add(new GeneralStatisticRgvVM("BTTS y/n i o/u 2.5 [L/W]:", $"{lostOUfromWonBTTS} / {wonBTTSwithOU} [{lostOUfromWonBTTS / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}% / {wonBTTSwithOU / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}%]"));
                
                Dispatcher.Invoke(() =>
                {
                    _ocGeneralStatistics.ReplaceAll(gs.ToList());

                    var flyouts = gridMain.FindLogicalDescendants<Grid>().Where(fo => fo.Name.EndsWith("Flyout")).ToList();
                    var otherFlyouts = flyouts.Except(gridStatisticsFlyout);

                    if (cbShowStatisticsOnEvaluateOption.IsChecked == true)
                    {
                        foreach (var ofo in otherFlyouts)
                            ofo.SlideHide();
                        gridStatisticsFlyout.SlideShow();
                    }
                        
                });
            }

            _bs = bs;
        }

        #endregion

        #region - Options

        private void LoadOptions()
        {
            GuiState = new ViewState(
                new TextBoxState("Notes", txtNotes),
                new RnumState("StakingStake", rnumInitialStake),
                new RnumState("StakingBudget", rnumBudget),
                new RddlState("StakingOnLoseChoice", rddlStakingTypeOnLose),
                new RnumState("StakingOnLoseValue", rnumCoeffOnLose),
                new RddlState("StakingOnWinChoice", rddlStakingTypeOnWin),
                new RnumState("StakingOnWinValue", rnumCoeffOnWin),
                new RnumState("StakingIncreaseBudgetRef", rnumBudgetIncrease),
                new RnumState("StakingIncreaseStake", rnumStakeIncrease),
                new RnumState("StakingDecreaseBudgetRef", rnumBudgetDecrease),
                new RnumState("StakingDecreaseStake", rnumStakeDecrease),
                new RddlState("StakingLoseCondition", rddlLoseCondition),
                new RnumState("StakingMaxStake", rnumMaxStake),
                new RddlState("StakingBasicStake", rddlBasicStake),

                new CbState("FilterLHSByPeriodEnabled", cbLHOddsByPeriodFilter),
                new RbsState("FilterLHSByPeriodChoice", _lowestHighestOddsByPeriodFilterControls.OfType<RadioButton>().ToArray()),
                new RnumState("FilterLHSByPeriodValue", rnumLHOddsPeriodInDays),

                new CbState("FilterTipsterEnabled", cbTipster),
                new MddlState("FilterTipsterChoice", mddlTipsters),

                new CbState("FilterOddsLGThanEnabled", cbOddsLesserGreaterThan),
                new RddlState("FilterOddsLGThanSign", rddlOddsLesserGreaterThan),
                new RnumState("FilterOddsLGThanValue", rnumOddsLesserGreaterThan),

                new CbState("FilterSelectionEnabled", cbSelection),
                new RbsState("FilterSelectionChoice", _selectionFilterControls.OfType<RadioButton>().ToArray()),

                new CbState("FilterFromDateEnabled", cbSinceDate),
                new DpState("FilterFromDateValue", dpSinceDate),

                new CbState("FilterToDateEnabled", cbToDate),
                new DpState("FilterToDateValue", dpToDate),

                new CbState("FilterPickEnabled", cbPick),
                new MddlState("FilterPickValue", mddlPickTypes),

                new CbState("FilterWithoutMatchesFromNotesEnabled", cbWithoutMatchesFromNotesFilter),

                new RddlState("StatisticsProfitByPeriod", rddlProfitByPeriodStatistics),

                new CbState("ShowStatisticsOnEvaluateOption", cbShowStatisticsOnEvaluateOption),
                new CbState("HideLoginPasswordsOption", cbHideLoginPasswordsOption),
                new CbState("ShowBrowserOnDataLoadingOption", cbShowBrowserOnDataLoadingOption),

                new CbState("DataLoadingLoadTipsFromDate", cbLoadTipsFromDate),
                new CbState("DataLoadingLoadTipsOnlySelected", cbLoadTipsOnlySelected),
                new RgvSelectionState("DataLoadingSelectedTipsters", rgvTipsters),

                new TilesOrderState("MainMenuTabOrder", _mainMenu, _mainMenu.TilesOrder),
                new MenuExtendedState("MainMenuExtended", _mainMenu) // nie przekazuje _isMainMenuExtended bo ta zmienna będzie zawsze przypisana raz, przy uruchomieniu, a potrzebna jest aktualna wartośc przy zamykaniu aplikacji
            );
            var db = new LocalDbContext();
            GuiState.Load(db, db.Options);
        }

        private void SaveOptions()
        {
            var db = new LocalDbContext();
            GuiState.Save(db, db.Options);
        }

        #endregion

        #endregion
    }

    #region Enums

    public enum BasicStake
    {
        Base = -1,
        Previous
    }

    public enum StakingTypeOnLose
    {
        Flat = -1,
        Add,
        Multiply,
        CoverPercentOfLoses,
        Subtract,
        CoverPercentOfLosesOnlyPreviousPeriod,
        AddToPrevious,
        Previous,
        Divide,
        UsePercentOfBudget
    }

    public enum StakingTypeOnWin
    {
        Flat = -1,
        Add,
        Multiply,
        UseWonMoney,
        Subtract,
        Previous,
        AddToPrevious,
        Divide,
        UsePercentOfBudget
    }

    public enum LoseCondition
    {
        BudgetLowerThanMax = -1,
        PreviousPeriodLost
    }

    #endregion
}
