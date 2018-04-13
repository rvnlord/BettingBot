using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
using System.Threading;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MoreLinq;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using BettingBot.Source;
using BettingBot.Source.Clients;
using BettingBot.Source.Clients.Agility;
using BettingBot.Source.Clients.Agility.Betshoot;
using BettingBot.Source.Clients.Selenium.Hintwise;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels;
using BettingBot.Source.ViewModels.Collections;
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
using CustomMenuItem = BettingBot.Common.UtilityClasses.CustomMenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using Path = System.IO.Path;
using Binding = System.Windows.Data.Binding;
using ComboBox = System.Windows.Controls.ComboBox;
using ContextMenu = BettingBot.Common.UtilityClasses.ContextMenu;
using Extensions = BettingBot.Common.Extensions;
using DataGrid = System.Windows.Controls.DataGrid;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MenuItem = System.Windows.Controls.MenuItem;
using NumericUpDown = MahApps.Metro.Controls.NumericUpDown;
using ListBox = System.Windows.Controls.ListBox;

namespace BettingBot
{
    public partial class MainWindow
    {
        #region Constants

        private const string strBetshoot = "betshoot";
        private const string strHintwise = "hintwise";

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

        private readonly ObservableCollection<LoginGvVM> _ocLogins = new ObservableCollection<LoginGvVM>();
        private readonly ObservableCollection<object> _ocSelectedLogins = new ObservableCollection<object>();
        private readonly ObservableCollection<TipsterGvVM> _ocTipsters = new ObservableCollection<TipsterGvVM>();
        private readonly ObservableCollection<object> _ocSelectedTipsters = new ObservableCollection<object>();
        private readonly ObservableCollection<BetToDisplayGvVM> _ocBetsToDisplayGvVM = new ObservableCollection<BetToDisplayGvVM>();
        private readonly ObservableCollection<object> _ocSelectedBetsToDisplayGvVM = new ObservableCollection<object>();
        private readonly ObservableCollection<ProfitByPeriodStatisticGvVM> _ocProfitByPeriodStatistics = new ObservableCollection<ProfitByPeriodStatisticGvVM>();
        private readonly ObservableCollection<object> _ocSelectedProfitByPeriodStatistics = new ObservableCollection<object>();
        private readonly ObservableCollection<AggregatedWinLoseStatisticGvVM> _ocAggregatedWinLoseStatisticsGvVM = new ObservableCollection<AggregatedWinLoseStatisticGvVM>();
        private readonly ObservableCollection<object> _ocSelectedAggregatedWinLoseStatisticsGvVM = new ObservableCollection<object>();
        private readonly ObservableCollection<GeneralStatisticGvVM> _ocGeneralStatistics = new ObservableCollection<GeneralStatisticGvVM>();
        private readonly ObservableCollection<object> _ocSelectedGeneralStatistics = new ObservableCollection<object>();

        private BettingSystem _bs;
        private TilesMenu _mainMenu;
        private bool _raisingEventImplicitlyFromCode;

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
            var actuallyDisabledControls = new List<object>();
            gridMain.ShowLoader();
            try
            {
                await Task.Run(() =>
                {
                    SQLiteConnection.ClearAllPools();
                    GC.Collect();
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
                        SetupContextMenus();

                        InitializeControlGroups();
                        actuallyDisabledControls = _buttonsAndContextMenus.DisableControls(); // tutaj dopiero zainicjalizowane są grupy

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
                actuallyDisabledControls.EnableControls();
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
        
        #endregion

        #region - Button Events

        private async void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
                gridCalculationsFlyout.ShowLoader();
                await Task.Run(() => CalculateBets());
                gridCalculationsFlyout.HideLoader();
                if (!gridMain.HasLoader())
                    actuallyDisabledControls.EnableControls();
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
        }

        private async void btnClearDatabase_Click(object sender, RoutedEventArgs e)
        {
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridMain.ShowLoader();

            await Task.Run(async () =>
            {
                var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Czyszczenie CAŁEJ bazy danych", $"Czy na pewno chcesz usunąć WSZYSTKIE rekordy? ({gvData.Items.Count})", MessageDialogStyle.AffirmativeAndNegative));
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
                actuallyDisabledControls.EnableControls();
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
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                var newTipstersCount = 0;
                var selectedTipsterIdsDdl = mddlTipsters.SelectedCustomIds();
                await Task.Run(() =>
                {
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
                actuallyDisabledControls.EnableControls();
        }

        private async void btnDownloadTips_Click(object sender, RoutedEventArgs e)
        {
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
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
                actuallyDisabledControls.EnableControls();
        }

        private void btnCalculatorCalculate_Click(object sender, RoutedEventArgs e)
        {
            txtCalculatorResult.ClearValue(true);

            var odds = new double[3];
            var chance = new double[3];
            var stake = new double[3];
            var stakePerc = new double[3];

            var totalStake = numCalculatorStake.Value ?? 0;
            var minStake = numCalculatorMinStake.Value ?? 0;
            odds[0] = numCalculatorOdds1.Value ?? 0;
            odds[1] = numCalculatorOdds2.Value ?? 0;
            odds[2] = numCalculatorOdds3.Value ?? 0;

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
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                if (txtLoadDomain.IsNullWhitespaceOrTag() || txtLoadLogin.IsNullWhitespaceOrTag() || txtLoadPassword.IsNullWhitespaceOrTag() || txtLoadDomain.Text.Remove(Space).Contains(",,"))
                    throw new Exception("Wszystkie pola muszą być poprawnie wypełnione");

                await Task.Run(() =>
                {
                    var dm = new DataManager();
                    var selLogins = _ocSelectedLogins.Cast<LoginGvVM>().ToList();
                    if (selLogins.Count != 1) return;

                    var loadDomain = Dispatcher.Invoke(() => txtLoadDomain.Text);
                    var loadLogin = Dispatcher.Invoke(() => txtLoadLogin.Text);
                    var loadPassword = Dispatcher.Invoke(() => txtLoadPassword.Text);

                    var updatedDbLogin = dm.UpdateLogin(new DbLogin
                    {
                        Id = selLogins.Single().Id,
                        Name = loadLogin,
                        Password = loadPassword,
                        Websites = loadDomain.Split(", ").Select(s => new DbWebsite
                        {
                            Address = s
                        }).ToList()
                    });

                    Dispatcher.Invoke(() =>
                    {
                        _raisingEventImplicitlyFromCode = true;

                        _ocLogins.ReplaceAll(dm.GetLogins().ToLoginsGvVM());
                        gvLogins.ScrollToEnd();
                        var loginToSelect = _ocLogins.Single(l => l.Id == updatedDbLogin.Id);
                        _ocSelectedLogins.ReplaceAll(loginToSelect);

                        var tipstersVM = dm.GetTipstersExceptDefault().ToTipstersGvVM();
                        _ocTipsters.ReplaceAll(tipstersVM);

                        gvTipsters.ScrollToEnd();
                        FillLoginTextboxes();

                        _raisingEventImplicitlyFromCode = false;
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
                actuallyDisabledControls.EnableControls();
        }

        private async void btnAddNewLogin_Click(object sender, RoutedEventArgs e)
        {
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                if (txtLoadDomain.IsNullWhitespaceOrTag() || txtLoadLogin.IsNullWhitespaceOrTag() || txtLoadPassword.IsNullWhitespaceOrTag())
                    throw new Exception("Wszystkie pola muszą być poprawnie wypełnione");

                await Task.Run(() =>
                {
                    var dm = new DataManager();
                    var loadDomain = Dispatcher.Invoke(() => txtLoadDomain.Text);
                    var loadLogin = Dispatcher.Invoke(() => txtLoadLogin.Text);
                    var loadPassword = Dispatcher.Invoke(() => txtLoadPassword.Text);

                    var addedDbLogin = dm.AddLogin(new DbLogin
                    {
                        Name = loadLogin,
                        Password = loadPassword,
                        Websites = loadDomain.Split(", ").Select(s => new DbWebsite() { Address = s }).ToList()
                    });

                    Dispatcher.Invoke(() =>
                    {
                        _raisingEventImplicitlyFromCode = true;

                        _ocLogins.ReplaceAll(dm.GetLogins().ToLoginsGvVM());
                        var userToSelect = _ocLogins.Single(l => l.Id == addedDbLogin.Id);
                        _ocSelectedLogins.ReplaceAll(userToSelect);

                        var tipstersVM = dm.GetTipstersExceptDefault().ToTipstersGvVM();
                        _ocTipsters.ReplaceAll(tipstersVM);

                        gvTipsters.ScrollToEnd();
                        FillLoginTextboxes();

                        _raisingEventImplicitlyFromCode = false;
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
                actuallyDisabledControls.EnableControls();
        }

        #endregion

        #region - TilesMenu Events

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
            var headerTile = (Tile) sender;
            var flyout = headerTile.FindLogicalAncestor<Grid>(grid => grid.Name.EndsWith("Flyout"));
            _mainMenu.SelectedTile = null;
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
            _lowestHighestOddsByPeriodFilterControls.EnableControls();
        }

        private void cbLHOddsByPeriodFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            _lowestHighestOddsByPeriodFilterControls.DisableControls();
        }

        private void cbOddsLesserGreaterThan_Checked(object sender, RoutedEventArgs e)
        {
            _odssLesserGreaterThanFilterControls.EnableControls();
        }

        private void cbOddsLesserGreaterThan_Unchecked(object sender, RoutedEventArgs e)
        {
            _odssLesserGreaterThanFilterControls.DisableControls();
        }

        private void cbSelection_Checked(object sender, RoutedEventArgs e)
        {
            _selectionFilterControls.EnableControls();
        }

        private void cbSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            _selectionFilterControls.DisableControls();
        }

        private void cbTipster_Checked(object sender, RoutedEventArgs e)
        {
            _tipsterFilterControls.EnableControls();
        }

        private void cbTipster_Unchecked(object sender, RoutedEventArgs e)
        {
            _tipsterFilterControls.DisableControls();
        }

        private void cbSinceDate_Checked(object sender, RoutedEventArgs e)
        {
            _fromDateFilterControls.EnableControls();
        }

        private void cbSinceDate_Unchecked(object sender, RoutedEventArgs e)
        {
            _fromDateFilterControls.DisableControls();
        }

        private void cbToDate_Checked(object sender, RoutedEventArgs e)
        {
            _toDateFilterControls.EnableControls();
        }

        private void cbToDate_Unchecked(object sender, RoutedEventArgs e)
        {
            _toDateFilterControls.DisableControls();
        }

        private void cbPick_Checked(object sender, RoutedEventArgs e)
        {
            _pickFilterControls.EnableControls();
        }

        private void cbPick_Unchecked(object sender, RoutedEventArgs e)
        {
            _pickFilterControls.DisableControls();
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
            var pwCol = gvLogins.Columns.Cast<DataGridTextColumn>().ByDataMemberName("Password");
            pwCol.Binding = new Binding("HiddenPassword");
        }

        private void cbHideLoginPasswordsOption_Unchecked(object sender, RoutedEventArgs e)
        {
            var pwCol = gvLogins.Columns.Cast<DataGridTextColumn>().ByDataMemberName("HiddenPassword");
            pwCol.Binding = new Binding("Password");
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

        #region - NumeriUpDown Events

        private void numAll_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            var isText = e.SourceDataObject.GetDataPresent(DataFormats.UnicodeText, true);
            if (!isText) return;
            var text = e.SourceDataObject.GetData(DataFormats.UnicodeText) as string;
            var value = text?.ToDoubleN();
            if (value == null) return;

            var num = (NumericUpDown) sender;
            num.Value = value;
            num.Focus();
        }

        #endregion

        #region - Dropdown Events

        private void ddlStakingTypeOnLose_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stakingType = (StakingTypeOnLose) ((ComboBox) sender).SelectedValue;
            numCoeffOnLose.IsEnabled = new[] { StakingTypeOnLose.Flat, StakingTypeOnLose.Previous }.All(st => st != stakingType);
        }

        private void ddlStakingTypeOnWin_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var stakingType = (StakingTypeOnWin) ((ComboBox) sender).SelectedValue;
            numCoeffOnWin.IsEnabled = new[] { StakingTypeOnWin.Flat, StakingTypeOnWin.Previous }.All(st => st != stakingType);
        }

        private void ddlProfitByPeriodStatistics_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_bs == null) return;
            _ocProfitByPeriodStatistics.ReplaceAll(new ProfitByPeriodStatisticsGvVM(_bs.Bets, ddlProfitByPeriodStatistics.SelectedEnumValue<Period>()));
            gvProfitByPeriodStatistics.ScrollToEnd();
        }

        #endregion

        #region - Gridview Events

        #region - - Sorting

        private void gvData_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var betsVM = _ocBetsToDisplayGvVM;

            if (betsVM == null || !betsVM.Any())
            {
                e.Handled = true;
                return;
            }

            BetToDisplayGvVM firstBet;
            var column = (DataGridTextColumn)e.Column;
            var columnName = column.DataMemberName();
            
            // WŁASNE SORTOWANIE
            var tipsterName = $"{nameof(firstBet.TipsterString)}";
            var dateName = nameof(firstBet.DateString);
            var matchHomeName = nameof(firstBet.MatchHomeName);
            var matchAwayName = nameof(firstBet.MatchAwayName);
            var pickName = nameof(firstBet.PickString);
            var betResultName = nameof(firstBet.BetResultString);
            var matchResultName = nameof(firstBet.MatchResultString);

            var oddsName = nameof(firstBet.OddsString);
            var stakeName = nameof(firstBet.StakeString);
            var profitName = nameof(firstBet.ProfitString);
            var budgetName = nameof(firstBet.BudgetString);
            
            // DOMYŚLNE SORTOWANIE
            var nrName = nameof(firstBet.Nr);
            
            if (column.SortDirection == null) // sortuj rosnąco
            {
                column.SortDirection = ListSortDirection.Ascending;
                
                if (columnName == tipsterName) // Tipster
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.TipsterName).ThenBy(bet => bet.TipsterWebsite).ToList());
                else if (columnName == dateName) // Data
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.LocalTimestamp).ToList());
                else if (columnName == matchHomeName) // U Siebie
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.MatchHomeName).ToList());
                else if (columnName == matchAwayName) // Na Wyjeździe
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.MatchAwayName).ToList());
                else if (columnName == pickName) // Typ
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.PickChoice).ThenBy(bet => bet.PickValue).ToList());
                else if (columnName == betResultName) // Zakład
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.BetResult).ToList());
                else if (columnName == matchResultName) // Wynik
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.MatchHomeScore).ThenBy(bet => bet.MatchAwayScore).ToList());
                else if (columnName == oddsName) // Kurs
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.Odds).ToList());
                else if (columnName == stakeName) // Stawka
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.Stake).ToList());
                else if (columnName == profitName) // Profit
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.Profit).ToList());
                else if (columnName == budgetName) // Budżet
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.Budget).ToList());

                else if (columnName == nrName) // Domyślnie sortowane: Nr
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.GetType()
                        .GetProperty(columnName)?
                        .GetValue(bet, null)).ToList());
            }
            else if (column.SortDirection == ListSortDirection.Ascending) // sortuj malejąco
            {
                column.SortDirection = ListSortDirection.Descending;

                if (columnName == tipsterName) // Tipster
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.TipsterName).ThenByDescending(bet => bet.TipsterWebsite).ToList());
                else if (columnName == dateName) // Data
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.LocalTimestamp).ToList());
                else if (columnName == matchHomeName) // U Siebie
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.MatchHomeName).ToList());
                else if (columnName == matchAwayName) // Na Wyjeździe
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.MatchAwayName).ToList());
                else if (columnName == pickName) // Typ
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.PickChoice).ThenByDescending(bet => bet.PickValue).ToList());
                else if (columnName == betResultName) // Zakład
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.BetResult).ToList());
                else if (columnName == matchResultName) // Wynik
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.MatchHomeScore).ThenByDescending(bet => bet.MatchAwayScore).ToList());
                else if (columnName == oddsName) // Kurs
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.Odds).ToList());
                else if (columnName == stakeName) // Stawka
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.Stake).ToList());
                else if (columnName == profitName) // Profit
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.Profit).ToList());
                else if (columnName == budgetName) // Budżet
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.Budget).ToList());

                else if (columnName == nrName) // Domyślnie sortowane: Nr
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.GetType()
                        .GetProperty(columnName)?
                        .GetValue(bet, null)).ToList());
            }
            else // resetuj sortowanie
            {
                column.SortDirection = null;
                betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.Nr).ToList());
            }
            
            e.Handled = true;
        }

        private void gvAggregatedWinsLosesStatistics_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var aggrWLStats = _ocAggregatedWinLoseStatisticsGvVM;

            if (aggrWLStats == null || !aggrWLStats.Any())
            {
                e.Handled = true;
                return;
            }

            var column = (DataGridTextColumn)e.Column;
            var columnName = column.DataMemberName();

            if (column.SortDirection == null) // sortuj rosnąco
            {
                column.SortDirection = ListSortDirection.Ascending;

                aggrWLStats.ReplaceAll(aggrWLStats.OrderBy(s => s.GetType()
                    .GetProperty(columnName)?
                    .GetValue(s, null)));
            }
            else if (column.SortDirection == ListSortDirection.Ascending) // soprtuj malejąco
            {
                column.SortDirection = ListSortDirection.Descending;

                aggrWLStats.ReplaceAll(aggrWLStats.OrderByDescending(s => s.GetType()
                    .GetProperty(columnName)?
                    .GetValue(s, null)));
            }
            else // resetuj sortowanie
            {
                column.SortDirection = null;

                aggrWLStats.ReplaceAll(aggrWLStats.OrderByDescending(s => s.Count));
            }

            e.Handled = true;
        }

        private void gvProfitByPeriodStatistics_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var pbpStats = _ocProfitByPeriodStatistics;

            if (pbpStats == null || !pbpStats.Any())
            {
                e.Handled = true;
                return;
            }

            ProfitByPeriodStatisticGvVM firstStat;
            var column = (DataGridTextColumn)e.Column;
            var columnName = column.DataMemberName();

            // WŁASNE SORTOWANIE
            var periodName = nameof(firstStat.Period);
            var profitName = nameof(firstStat.ProfitStr);
            var countName = nameof(firstStat.Count);

            // DOMYŚLNE SORTOWANIE

            // WYŁĄCZONE SORTOWANIE
            if (column.SortDirection == null) // sortuj rosnąco
            {
                column.SortDirection = ListSortDirection.Ascending;

                if (columnName == periodName) // Okres
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderBy(bet => bet.PeriodId)));
                else if (columnName == profitName) // Profit
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderBy(bet => bet.Profit)));
                else if (columnName == countName) // Count
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderBy(bet => bet.Count)));
            }
            else if (column.SortDirection == ListSortDirection.Ascending) // soprtuj malejąco
            {
                column.SortDirection = ListSortDirection.Descending;

                if (columnName == periodName) // Okres
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderByDescending(bet => bet.PeriodId)));
                else if (columnName == profitName) // Profit
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderByDescending(bet => bet.Profit)));
                else if (columnName == countName) // Count
                    pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderByDescending(bet => bet.Count)));
            }
            else // resetuj sortowanie
            {
                column.SortDirection = null;
                pbpStats.ReplaceAll(new ProfitByPeriodStatisticsGvVM(pbpStats.OrderBy(s => s.PeriodId)));
            }

            e.Handled = true;
        }

        private void gvTipsters_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var tipstersVM = _ocTipsters;

            if (tipstersVM == null || !tipstersVM.Any())
            {
                e.Handled = true;
                return;
            }

            TipsterGvVM tipster;
            var column = (DataGridTextColumn)e.Column;
            var columnName = column.DataMemberName();

            // WŁASNE SORTOWANIE
            var domainWithOpName = nameof(tipster.DomainWithOp);

            // DOMYŚLNE SORTOWANIE
            var tipsterName = $"{nameof(tipster.Name)}";
            var linkName = $"{nameof(tipster.Link)}";

            if (column.SortDirection == null) // sortuj rosnąco
            {
                column.SortDirection = ListSortDirection.Ascending;

                if (columnName == domainWithOpName) // Domain
                    tipstersVM.ReplaceAll(tipstersVM.OrderBy(t => t.WebsiteAddress).ToList());

                else if (columnName.EqualsAny(tipsterName, linkName)) // Domyślnie sortowane: Name, Link
                    tipstersVM.ReplaceAll(tipstersVM.OrderBy(t => t.GetType()
                        .GetProperty(columnName)?
                        .GetValue(t, null)).ToList());
            }
            else if (column.SortDirection == ListSortDirection.Ascending) // sortuj malejąco
            {
                column.SortDirection = ListSortDirection.Descending;

                if (columnName == domainWithOpName) // Domain
                    tipstersVM.ReplaceAll(tipstersVM.OrderByDescending(t => t.WebsiteAddress).ToList());

                else if (columnName.EqualsAny(tipsterName, linkName)) // Domyślnie sortowane: Name, Link
                    tipstersVM.ReplaceAll(tipstersVM.OrderByDescending(t => t.GetType()
                        .GetProperty(columnName)?
                        .GetValue(t, null)).ToList());
            }
            else // resetuj sortowanie
            {
                column.SortDirection = null;
                tipstersVM.ReplaceAll(tipstersVM.OrderBy(t => t.Name).ToList());
            }

            e.Handled = true;
        }

        private void gvLogins_Sorting(object sender, DataGridSortingEventArgs e)
        {
            var loginsVM = _ocLogins;

            if (loginsVM == null || !loginsVM.Any())
            {
                e.Handled = true;
                return;
            }

            LoginGvVM login;
            var column = (DataGridTextColumn)e.Column;
            var columnName = column.DataMemberName();

            // WŁASNE SORTOWANIE
            var addressesStringName = nameof(login.AddressesString);

            // DOMYŚLNE SORTOWANIE
            var loginNameName = $"{nameof(login.Name)}";
            var loginPasswordName = $"{nameof(login.Password)}";

            if (column.SortDirection == null) // sortuj rosnąco
            {
                column.SortDirection = ListSortDirection.Ascending;

                if (columnName == addressesStringName) // Addresses
                    loginsVM.ReplaceAll(loginsVM.OrderBy(t => t.WebsiteAddresses.OrderBy(a => a).JoinAsString()).ToList());

                else if (columnName.EqualsAny(loginNameName, loginPasswordName)) // Domyślnie sortowane: LoginName, LoginPassword
                    loginsVM.ReplaceAll(loginsVM.OrderBy(t => t.GetType()
                        .GetProperty(columnName)?
                        .GetValue(t, null)).ToList());
            }
            else if (column.SortDirection == ListSortDirection.Ascending) // sortuj malejąco
            {
                column.SortDirection = ListSortDirection.Descending;

                if (columnName == addressesStringName) // Addresses
                    loginsVM.ReplaceAll(loginsVM.OrderByDescending(t => t.WebsiteAddresses.OrderBy(a => a).JoinAsString()).ToList());

                else if (columnName.EqualsAny(loginNameName, loginPasswordName)) // Domyślnie sortowane: LoginName, LoginPassword
                    loginsVM.ReplaceAll(loginsVM.OrderByDescending(t => t.GetType()
                        .GetProperty(columnName)?
                        .GetValue(t, null)).ToList());
            }
            else // resetuj sortowanie
            {
                column.SortDirection = null;
                loginsVM.ReplaceAll(loginsVM.OrderBy(l => l.Name).ToList());
            }

            e.Handled = true;
        }

        #endregion

        #region - - PreviewKeyDown

        private async void gvData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)))
            {
                var selectedBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToArray();
                if (selectedBets.Length == 1)
                {
                    var searchTerm = selectedBets.Single().MatchHomeName.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";
                    var copyToCB = ClipboardWrapper.TrySetText(searchTerm);
                    if (copyToCB.IsFailure)
                        await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message);
                }
            }
        }

        private async void gvTipsters_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
                gridTipsters.ShowLoader();

                try
                {
                    await Task.Run(() =>
                    {
                        var deletedTipsters = _ocSelectedTipsters.Cast<TipsterGvVM>().ToArray();
                        var dm = new DataManager();
                        if (deletedTipsters.Any())
                        {
                            var selectedTipsterIdsDdl = Dispatcher.Invoke(() => mddlTipsters.SelectedCustomIds());
                            var idsToDelete = deletedTipsters.Select(t => t.Id).ToArray();
                            var newSelectedIds = selectedTipsterIdsDdl.Except(idsToDelete);

                            dm.RemoveTipstersById(idsToDelete);

                            UpdateGuiWithNewTipsters();

                            Dispatcher.Invoke(() => mddlTipsters.SelectByCustomIds(newSelectedIds));
                            
                            if (_ocBetsToDisplayGvVM.Any(bet => deletedTipsters.Any(dt => dt.Name.EqIgnoreCase(bet.TipsterName) && dt.Link.UrlToDomain().EqIgnoreCase(bet.TipsterWebsite))))
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
                    actuallyDisabledControls.EnableControls();
            }
        }

        private async void gvLogins_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridTipsters.ShowLoader();

            try
            {
                await Task.Run(() =>
                {
                    var dm = new DataManager();
                    var deletedLogins = _ocSelectedLogins.Cast<LoginGvVM>().ToArray();
                    if (deletedLogins.Any())
                    {
                        var ids = deletedLogins.Select(l => l.Id).ToArray();

                        dm.RemoveLoginsById(ids);
                        
                        Dispatcher.Invoke(() =>
                        {
                            _raisingEventImplicitlyFromCode = true;

                            _ocLogins.ReplaceAll(dm.GetLogins().ToLoginsGvVM());

                            _ocTipsters.ReplaceAll(dm.GetTipstersExceptDefault().ToTipstersGvVM());
                            gvTipsters.ScrollToEnd();

                            _raisingEventImplicitlyFromCode = false;
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
                actuallyDisabledControls.EnableControls();
        }

        #endregion

        #region - - SelectionChanged

        private void gvData_SelectionChanged(object sender, SelectionChangedEventArgs e) // wywoływane przez reflection, bo musi zostać uruchomione dopiero po zaaktualizowaniu kolekcji zaznaczonych elementów gridview
        {
            if (_raisingEventImplicitlyFromCode)
                return;

            this.FindLogicalDescendants<Grid>().Where(g => g.Name.EndsWith("Flyout")).ForEach(f => f.SlideHide());
            _mainMenu.SelectedTile = null;

            var selBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToList();
            if (selBets.Count == 1)
            {
                var selBet = selBets.Single();
                var additionalInfo =
                    $"Oryginalny Mecz: {selBet.GetUnparsedMatchString()}\n" +
                    $"Oryginalny Zakład: {selBet.GetUnparsedPickString()}\n";
                lblAdditionalInfo.Content = new TextBlock { Text = additionalInfo };
            }
            else
                lblAdditionalInfo.Content = null;

            if (gvData.HasContextMenu() && gvData.ContextMenu().IsOpen())
            {
                HandleGvDataContextMenu();
            }
        }

        private void gvGeneralStatistics_SelectionChanged(object sender, SelectionChangedEventArgs e) // wywoływane przez reflection, bo musi zostać uruchomione dopiero po zaaktualizowaniu kolekcji zaznaczonych elementów gridview
        {
            if (_raisingEventImplicitlyFromCode)
                return;

            gvData.SelectionChanged -= gvData_SelectionChanged;

            if (_ocSelectedGeneralStatistics.Count != 1)
            {
                gvData.SelectionChanged += gvData_SelectionChanged;
                return;
            }

            var selectedItem = (GeneralStatisticGvVM)_ocSelectedGeneralStatistics.Single();
            if (!selectedItem.Value.HasValueBetween("(", ")")) return;

            var valStr = selectedItem.Value.Between("(", ")");
            if (!int.TryParse(valStr, out int val)) return;

            var betToFind = _ocBetsToDisplayGvVM.Single(s => s.Nr == val);

            _ocSelectedBetsToDisplayGvVM.ReplaceAll(betToFind);

            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridStatisticsContent.ShowLoader();
            gridData.ShowLoader();

            gvData.ScrollTo(betToFind);

            gridStatisticsContent.HideLoader();
            gridData.HideLoader();
            if (!gridMain.HasLoader())
                actuallyDisabledControls.EnableControls();

            gvData.SelectionChanged += gvData_SelectionChanged;
        }
        
        private void gvLogins_SelectionChanged(object sender, SelectionChangedEventArgs e) // wywoływane przez reflection, bo musi zostać uruchomione dopiero po zaaktualizowaniu kolekcji zaznaczonych elementów gridview
        {
            if (_raisingEventImplicitlyFromCode)
                return;

            FillLoginTextboxes();
        }

        #endregion

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

        private void cmGvData_Open(object sender, ContextMenuOpenEventArgs e)
        {
            HandleGvDataContextMenu();
        }

        private void HandleGvDataContextMenu()
        {
            var cm = gvData.ContextMenu();
            if (!cm.IsEnabled) return;

            var selectedBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToArray();
            if (selectedBets.Length == 0)
            {
                cm.Close();
                return;
            }
            cm.EnableAll();
            if (selectedBets.Length >= 2)
                cm.Disable("Znajdź", "Kopiuj do wyszukiwania");
        }

        private void cmMddlTipsters_Click(object sender, ContextMenuClickEventArgs e)
        {
            var ddlitems = mddlTipsters.Items.Cast<DdlItem>();
            var idsExceptMine = ddlitems.Where(i => i.Index != -1).Select(i => i.Index);
            mddlTipsters.UnselectAll();
            switch (e.ClickedItem.Text)
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

        private void cmMddlPickTypes_Click(object sender, ContextMenuClickEventArgs e)
        {
            mddlPickTypes.UnselectAll();
            switch (e.ClickedItem.Text)
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

        private void cmDatePickers_Click(object sender, ContextMenuClickEventArgs e)
        {
            var cm = (BettingBot.Common.UtilityClasses.ContextMenu) sender;
            var dp = (DatePicker) cm.Control;

            switch (e.ClickedItem.Text)
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

        private void cmTextBoxes_Click(object sender, ContextMenuClickEventArgs e)
        {
            var cm = (ContextMenu) sender;
            var txt = (TextBox) cm.Control;

            switch (e.ClickedItem.Text)
            {
                case "Kopiuj":
                    ClipboardWrapper.TrySetText(txt.Text ?? "");
                    break;
                case "Wyczyść":
                    txt.ResetValue(true);
                    break;
            }
        }

        private async void cmGvData_Click(object sender, ContextMenuClickEventArgs e)
        {
            var actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
            gridData.ShowLoader();

            var selectedBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToArray();
            var matchesStr = string.Join("\n", selectedBets.Select(b => $"{b.MatchHomeName} - {b.MatchAwayName}: {b.PickString}"));
            var searchTerm = selectedBets.FirstOrDefault()?.MatchHomeName.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";
            
            await Task.Run(async () =>
            {
                switch (e.ClickedItem.Text)
                {
                    case "Znajdź":
                    {
                        //var se = new SearchEngine();
                        //se.FindBet(selectedBets.Single());
                        //var odds = string.Join("\n", se.FoundBets.Select(b => b.ToString()));
                        //var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Znalezione zakłady", odds, MessageDialogStyle.AffirmativeAndNegative));
                        //if (result == MessageDialogResult.Affirmative)
                        //    SeleniumDriverManager.CloseAllDrivers();

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
                actuallyDisabledControls.EnableControls();
        }

        #endregion

        #region - Grid Events

        private void gridFlyout_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {

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
                f.ZIndex(i);
                i++;
                SetupZIndexes(f, i);
            }
        }

        private void SetupDropdowns()
        {
            foreach (var ddl in this.FindLogicalDescendants<ComboBox, ListBox>().Cast<Selector>())
            {
                ddl.DisplayMemberPath = "Text";
                ddl.SelectedValuePath = "Index";
            }

            ddlStakingTypeOnLose.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) StakingTypeOnLose.Flat, "płaska stawka"),
                new DdlItem((int) StakingTypeOnLose.Add, "dodaj"),
                new DdlItem((int) StakingTypeOnLose.Subtract, "odejmij"),
                new DdlItem((int) StakingTypeOnLose.Multiply, "mnóż x"),
                new DdlItem((int) StakingTypeOnLose.Divide, "dziel przez"),
                new DdlItem((int) StakingTypeOnLose.CoverPercentOfLoses, "pokryj % strat"),
                new DdlItem((int) StakingTypeOnLose.UsePercentOfBudget, "użyj % budżetu")
            };
            
            ddlStakingTypeOnLose.SelectByIndex(-1);
            ddlStakingTypeOnLose.SelectionChanged += ddlStakingTypeOnLose_SelectionChanged;

            ddlStakingTypeOnWin.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) StakingTypeOnWin.Flat, "płaska stawka"),
                new DdlItem((int) StakingTypeOnWin.Add, "dodaj"),
                new DdlItem((int) StakingTypeOnWin.Subtract, "odejmij"),
                new DdlItem((int) StakingTypeOnWin.Multiply, "mnóż x"),
                new DdlItem((int) StakingTypeOnWin.Divide, "dziel przez"),
                new DdlItem((int) StakingTypeOnWin.UsePercentOfBudget, "użyj % budżetu")
            };
            
            ddlStakingTypeOnWin.SelectByIndex(-1);
            ddlStakingTypeOnWin.SelectionChanged += ddlStakingTypeOnWin_SelectionChanged;

            ddlOddsLesserGreaterThan.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) OddsLesserGreaterThanFilterChoice.GreaterThan, ">="),
                new DdlItem((int) OddsLesserGreaterThanFilterChoice.LesserThan, "<="),
            };
            
            ddlOddsLesserGreaterThan.SelectByIndex(-1);

            ddlLoseCondition.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) LoseCondition.PreviousPeriodLost, "Przegrany poprzedni okres"),
                new DdlItem((int) LoseCondition.BudgetLowerThanMax, "Budżet niższy od największego"),
            };
            
            ddlLoseCondition.SelectByIndex(-1);

            ddlBasicStake.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) BasicStake.Base, "bazowa"),
                new DdlItem((int) BasicStake.Previous, "poprzednia"),
            };
            
            ddlBasicStake.SelectByIndex(-1);

            ddlProfitByPeriodStatistics.ItemsSource = new List<DdlItem>
            {
                new DdlItem((int) Period.Month, "Zysk miesięczny"),
                new DdlItem((int) Period.Week, "Zysk tygodniowy"),
                new DdlItem((int) Period.Day, "Zysk dzienny"),
            };
            
            ddlProfitByPeriodStatistics.SelectByIndex(-1);
            ddlProfitByPeriodStatistics.SelectionChanged += ddlProfitByPeriodStatistics_SelectionChanged;

            var ddlPickTypes = EnumUtils.EnumToDdlItems<PickChoice>(PickConverter.PickChoiceToString);
            mddlPickTypes.ItemsSource = ddlPickTypes;
            mddlPickTypes.SelectAll();

            var ddlTipsterDomains = EnumUtils.EnumToDdlItems<DomainType>(TipsterConverter.TipsterDomainTypeToString);
            ddlTipsterDomain.ItemsSource = ddlTipsterDomains;
            ddlTipsterDomain.Select(ddlTipsterDomains.First());
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
            gvLogins.ItemsSource = _ocLogins;
            gvLogins.SetSelecteditemsSource(_ocSelectedLogins);

            gvTipsters.ItemsSource = _ocTipsters;
            gvTipsters.SetSelecteditemsSource(_ocSelectedTipsters);

            gvData.ItemsSource = _ocBetsToDisplayGvVM;
            gvData.SetSelecteditemsSource(_ocSelectedBetsToDisplayGvVM);

            gvAggregatedWinsLosesStatistics.ItemsSource = _ocAggregatedWinLoseStatisticsGvVM;
            gvAggregatedWinsLosesStatistics.SetSelecteditemsSource(_ocSelectedAggregatedWinLoseStatisticsGvVM);

            gvProfitByPeriodStatistics.ItemsSource = _ocProfitByPeriodStatistics;
            gvProfitByPeriodStatistics.SetSelecteditemsSource(_ocSelectedProfitByPeriodStatistics);

            gvGeneralStatistics.ItemsSource = _ocGeneralStatistics;
            gvGeneralStatistics.SetSelecteditemsSource(_ocSelectedGeneralStatistics);

            var loginsVM = new LocalDbContext().Logins.Include(l => l.Websites).ToLoginsGvVM();
            _ocLogins.ReplaceAll(loginsVM);
            gvLogins.ScrollToEnd();
        }

        private void SetupUpDowns()
        {
            foreach (var ud in this.FindLogicalDescendants<NumericUpDown>())
            {
                DataObject.AddPastingHandler(ud, numAll_Pasting);
            }
        }

        private void SetupDatePickers()
        {
            dpLoadTipsFromDate.SelectedDate = DateTime.Now.Subtract(TimeSpan.FromDays(2));
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
            if (_ocSelectedTipsters.Any())
                gvTipsters.ScrollTo(_ocSelectedTipsters.First());
        }

        private void InitializeControlGroups()
        {
            _lowestHighestOddsByPeriodFilterControls = new List<UIElement>
            {
                rbHighestOddsByPeriod,
                rbLowestOddsByPeriod,
                rbSumOddsByPeriod,
                numLHOddsPeriodInDays,
            };

            _odssLesserGreaterThanFilterControls = new List<UIElement>
            {
                rnumOddsLesserGreaterThan,
                ddlOddsLesserGreaterThan
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
            var contextMenus = ContextMenusManager.ContextMenus.Select(kvp => kvp.Value).ToList();
            _buttonsAndContextMenus.ReplaceAll(buttons).AddRange(contextMenus);
        }

        private void SetupContextMenus()
        {
            ContextMenusManager.ContextMenusContainer = gridMain;

            var cmMddlTipsters = mddlTipsters.ContextMenu().Create(
                new ContextMenuItem("Tylko moje", PackIconModernKind.PeopleMagnify),
                new ContextMenuItem("Tylko pozostałe", PackIconModernKind.PeopleMultipleMagnify),
                new ContextMenuItem("Wszystkie", PackIconModernKind.PeopleMultiple),
                new ContextMenuItem("Żaden", PackIconModernKind.ReplyPeople));
            cmMddlTipsters.ContextMenuClick += cmMddlTipsters_Click;

            var cmMddlPickTypes = mddlPickTypes.ContextMenu().Create(
                new ContextMenuItem("Wszystkie", PackIconModernKind.PeopleMultiple),
                new ContextMenuItem("Żaden", PackIconModernKind.ReplyPeople));
            cmMddlPickTypes.ContextMenuClick += cmMddlPickTypes_Click;

            var cmGvData = gvData.ContextMenu().Create(
                new ContextMenuItem("Znajdź", PackIconModernKind.Magnify),
                new ContextMenuItem("Kopiuj do wyszukiwania", PackIconModernKind.PageCopy),
                new ContextMenuItem("Kopiuj całość", PackIconModernKind.ListCheck),
                new ContextMenuItem("Do notatek", PackIconModernKind.PageOnenote),
                new ContextMenuItem("Usuń z bazy", PackIconModernKind.LayerDelete));
            cmGvData.ContextMenuOpen += cmGvData_Open;
            cmGvData.ContextMenuClick += cmGvData_Click;

            foreach (var dp in this.FindLogicalDescendants<DatePicker>())
            {
                var cmDp = dp.ContextMenu().Create(
                    new ContextMenuItem("Kopiuj", PackIconModernKind.PageCopy),
                    new ContextMenuItem("Wyczyść", PackIconModernKind.AppRemove),
                    new ContextMenuItem("Dzisiejsza data", PackIconModernKind.Calendar));
                cmDp.ContextMenuClick += cmDatePickers_Click;
            }

            foreach (var txt in this.FindLogicalDescendants<TextBox>())
            {
                var cmTxt = txt.ContextMenu().Create(
                    new ContextMenuItem("Kopiuj", PackIconModernKind.PageCopy),
                    new ContextMenuItem("Wyczyść", PackIconModernKind.AppRemove));
                cmTxt.ContextMenuClick += cmTextBoxes_Click;
            }
        }
        
        private void ClearGridViews()
        {
            _ocLogins.Clear();
            _ocSelectedLogins.Clear();
            _ocTipsters.Clear();
            _ocSelectedTipsters.Clear();
            _ocBetsToDisplayGvVM.Clear();
            _ocSelectedBetsToDisplayGvVM.Clear();
            _ocProfitByPeriodStatistics.Clear();
            _ocSelectedProfitByPeriodStatistics.Clear();
            _ocAggregatedWinLoseStatisticsGvVM.Clear();
            _ocSelectedAggregatedWinLoseStatisticsGvVM.Clear();
            _ocGeneralStatistics.Clear();
            _ocSelectedGeneralStatistics.Clear();
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

        private void FillLoginTextboxes()
        {
            var selLogins = _ocSelectedLogins.Cast<LoginGvVM>().ToList();
            if (selLogins.Count == 1)
            {
                txtLoadLogin.ClearValue(true);
                txtLoadPassword.ClearValue(true);
                txtLoadDomain.ClearValue(true);
                var selLogin = selLogins.Single();
                txtLoadLogin.Text = selLogin.Name;
                txtLoadPassword.Text = selLogin.Password;
                txtLoadDomain.Text = selLogin.AddressesString;
            }
            else
            {
                txtLoadLogin.ResetValue(true);
                txtLoadPassword.ResetValue(true);
                txtLoadDomain.ResetValue(true);
            }
        }

        #endregion

        #region - Core Functionality

        public int UpdateGuiWithNewTipsters()
        {
            var dm = new DataManager();
            var ddlTipsters = new List<DdlItem> { new DdlItem(-1, "(Moje Zakłady)") };
            var tipstersVM = dm.GetTipstersExceptDefault().ToTipstersGvVM();
            ddlTipsters.AddRange(tipstersVM.Select(t => new DdlItem(t.Id, $"{t.Name} ({t.WebsiteAddress})")));
            Dispatcher.Invoke(() => mddlTipsters.ItemsSource = ddlTipsters);
            
            var addedTipsters = new List<TipsterGvVM>();
            Dispatcher.Invoke(() =>
            {
                addedTipsters = tipstersVM.Except(_ocTipsters).OrderBy(t => t.Name).ToList();
                var oldTIpstersCount = _ocTipsters.Count; // lub sklonować kolekcję jeśli potrzebne będzie coś oprócz ilości
                _ocTipsters.ReplaceAll(tipstersVM);
                if (addedTipsters.Any())
                {
                    if (oldTIpstersCount > 0)
                        _ocSelectedTipsters.ReplaceAll(addedTipsters);
                    if (_ocSelectedTipsters.Any())
                        gvTipsters.ScrollTo(_ocSelectedTipsters.First());
                }
                txtTipsterName.ResetValue(true);
            });

            dm.RemoveUnusedWebsites();
            return addedTipsters.Count;
        }

        public void DownloadTipsterToDb()
        {
            try
            {
                string tipsterName = null;
                string tipsterNamePlaceholder = null;
                DomainType? tipsterDomain = null;
                var headlessMode = false;
                Dispatcher.Invoke(() =>
                {
                    headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                    tipsterName = txtTipsterName.Text.Trim(); // nie usuwać spacji, bo Hintwise pozwala na spacje w nazwie
                    tipsterNamePlaceholder = txtTipsterName.Tag?.ToString();
                    tipsterDomain = ddlTipsterDomain.SelectedEnumValue<DomainType>();
                });

                if (tipsterName.IsNullWhiteSpaceOrDefault(tipsterNamePlaceholder))
                    throw new Exception("Wpisany ciąg znaków jest niepoprawny");
                if (tipsterDomain == DomainType.Custom && !tipsterName.IsUrl())
                    throw new Exception("Wpisany ciąg znaków nie jest adresem Url");

                var dm = new DataManager().ReceiveInfoWith<DataManager>(dl_InformationSent);
                if (tipsterDomain == DomainType.Betshoot
                    || tipsterDomain == DomainType.Custom && tipsterName.ToLower().Contains(strBetshoot))
                {
                    var betshoot = new BetshootClient();
                    var tipsterAddress = tipsterDomain != DomainType.Custom 
                        ? betshoot.ReceiveInfoWith<BetshootClient>(dl_InformationSent).TipsterAddress(tipsterName).Address
                        : tipsterName;
                    var tipsterResponse = betshoot.ReceiveInfoWith<BetshootClient>(dl_InformationSent).Tipster(tipsterAddress);
                    dm.AddTipsterIfNotExists(tipsterResponse.ToDbTipster()); // wywołuje TipsterConverter
                }
                else if (tipsterDomain == DomainType.Hintwise
                    || tipsterDomain == DomainType.Custom && tipsterName.ToLower().Contains(strHintwise))
                {
                    var login = dm.WebsiteLogin(DomainType.Hintwise);
                    var hintwise = new HintwiseClient(login.Name, login.Password, headlessMode);
                    var tipsterAddress = tipsterDomain != DomainType.Custom
                        ? hintwise.ReceiveInfoWith<HintwiseClient>(dl_InformationSent).TipsterAddress(tipsterName).Address
                        : tipsterName;
                    var tipsterResponse = hintwise.ReceiveInfoWith<HintwiseClient>(dl_InformationSent).Tipster(tipsterAddress);
                    dm.AddTipsterIfNotExists(tipsterResponse.ToDbTipster());
                }
                else
                    throw new Exception($"Nie istnieje loader dla: {tipsterDomain} {tipsterName}");
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
            }
        }

        private void DownloadTipsToDb()
        {
            try
            {
                var dm = new DataManager().ReceiveInfoWith<DataManager>(dl_InformationSent);
                var dbTipsters = dm.GetTipstersExceptDefault();
                var selectedTipstersIds = _ocSelectedTipsters.Cast<TipsterGvVM>().Select(t => t.Id).ToArray();
                var selectedDbTipsters = dbTipsters.WhereByMany(t => t.Id, selectedTipstersIds).ToList();

                var headlessMode = false;
                var onlySelected = false;
                ExtendedTime fromDate = null;
                Dispatcher.Invoke(() =>
                {
                    headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                    fromDate = cbLoadTipsFromDate.IsChecked == true
                        ? dpLoadTipsFromDate.SelectedDate?.ToDMY().ToExtendedTime(TimeZoneKind.CurrentLocal)
                        : null;
                    onlySelected = cbLoadTipsOnlySelected.IsChecked == true;
                });
                
                foreach (var t in onlySelected ? selectedDbTipsters : dbTipsters)
                {
                    //var footballdataClient = new FootballdataClient();
                    //var fixturesResponse = footballdataClient.Fixtures();
                    //dm.UpsertMatches(fixturesResponse.ToFixtures());
                    
                    var domain = t.Website.Address.ToLower();
                    if (domain == strBetshoot)
                    {
                        var tipsResponse = new BetshootClient().ReceiveInfoWith<BetshootClient>(dl_InformationSent)
                            .Tips(t.ToBetshootTipsterResponse(), fromDate);
                        dm.UpsertBets(tipsResponse.Tipster.ToDbTipster(), tipsResponse.ToDbBets());
                    }
                    else if (domain == strHintwise)
                    {
                        var tipsResponse = new HintwiseClient(
                            t.Website.Login.Name, 
                            t.Website.Login.Password,
                            headlessMode
                        )
                            .ReceiveInfoWith<HintwiseClient>(dl_InformationSent)
                            .Tips(t.ToHintwiseTipsterResponse(), fromDate);
                        dm.UpsertBets(tipsResponse.Tipster.ToDbTipster(), tipsResponse.ToDbBets());
                    }
                    else
                        throw new Exception($"Nie istnieje loader dla strony: {t.Website.Address}");
                }
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
            }
        }

        private void CalculateBets()
        {
            var dm = new DataManager();
            var stakingTypeOnLose = (StakingTypeOnLose) Dispatcher.Invoke(() => ddlStakingTypeOnLose.SelectedValue);
            var stakingTypeOnWin = (StakingTypeOnWin) Dispatcher.Invoke(() => ddlStakingTypeOnWin.SelectedValue);

            var betsByDate = dm.GetBets().ToBetsToDisplayGvVM();

            var pendingBets = betsByDate.Where(b => b.BetResult == BetResult.Pending).ToList();
            var bets = betsByDate.ExceptBy(pendingBets, b => b.Id).ToList();
            bets = bets.Concat(pendingBets).ToList(); //.Where(b => !b.Pick.ToLower().Contains("both"))
            var initStake = Dispatcher.Invoke(() => numInitialStake.Value ?? 0);
            var initialBudget = Dispatcher.Invoke(() => numBudget.Value ?? 0);
            var budgetIncreaseRef = Dispatcher.Invoke(() => numBudgetIncrease.Value ?? 0);
            var stakeIncrease = Dispatcher.Invoke(() => numStakeIncrease.Value ?? 0);
            var budgetDecreaseRef = Dispatcher.Invoke(() => numBudgetDecrease.Value ?? 0);
            var stakeDecrease = Dispatcher.Invoke(() => numStakeDecrease.Value ?? 0);
            var loseCoeff = Dispatcher.Invoke(() => numCoeffOnLose.Value ?? 1);
            var winCoeff = Dispatcher.Invoke(() => numCoeffOnWin.Value ?? 1);
            var loseCondition = (LoseCondition) Dispatcher.Invoke(() => ddlLoseCondition.SelectedValue);
            var maxStake = Dispatcher.Invoke(() => numMaxStake.Value ?? 0);
            var resetStake = (BasicStake) Dispatcher.Invoke(() => ddlBasicStake.SelectedValue) == BasicStake.Base;

            if (bets.Count == 0) throw new Exception("W bazie danych nie ma żadnych zakładów");
            if (stakeIncrease > budgetIncreaseRef) throw new Exception("Stawka nie może rosnąć szybciej niż budżet");

            var visibleBets = _ocBetsToDisplayGvVM.ToList();
            var selectedBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToList();
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
            var selectedMddlTipsterIds = Dispatcher.Invoke(() => mddlTipsters.SelectedCustomIds());

            dm.EnsureDefaultTipsterExists();

            var tipsters = dm.GetTipstersById(selectedMddlTipsterIds).ToTipstersGvVM();

            var oddRef = Dispatcher.Invoke(() => rnumOddsLesserGreaterThan.Value) ?? 0;
            var applyOddsLesserGreaterThanFilter = Dispatcher.Invoke(() => cbOddsLesserGreaterThan.IsChecked) == true;
            var oddsLesserGreaterThanSelected = (OddsLesserGreaterThanFilterChoice)Dispatcher.Invoke(() => ddlOddsLesserGreaterThan.SelectedValue);
            var getOddsGreaterThan = oddsLesserGreaterThanSelected == OddsLesserGreaterThanFilterChoice.GreaterThan;

            var applyLowestHighestOddsByPeriodFilter = Dispatcher.Invoke(() => cbLHOddsByPeriodFilter.IsChecked) == true;
            var getHighestOddsByPeriod = Dispatcher.Invoke(() => rbHighestOddsByPeriod.IsChecked) == true;
            var getLowestOddsByPeriod = Dispatcher.Invoke(() => rbLowestOddsByPeriod.IsChecked) == true;
            var period = (Dispatcher.Invoke(() => numLHOddsPeriodInDays.Value) ?? 1).ToInt();

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
                _raisingEventImplicitlyFromCode = true; // zapobiegnij wywołaniu gvData_SelectionChanged podczas przeładowania zakładów
                
                _ocBetsToDisplayGvVM.ReplaceAll(bs.Bets);
                gvData.ScrollToEnd();
                _ocAggregatedWinLoseStatisticsGvVM.ReplaceAll(new AggregatedWinLoseStatisticsGvVM(bs.LosesCounter, bs.WinsCounter));
                _ocProfitByPeriodStatistics.ReplaceAll(new ProfitByPeriodStatisticsGvVM(bs.Bets, ddlProfitByPeriodStatistics.SelectedEnumValue<Period>()));
                gvProfitByPeriodStatistics.ScrollToEnd();

                _raisingEventImplicitlyFromCode = false;
            });

            if (bs.Bets.Any())
            {
                var maxStakeBet = bs.Bets.MaxBy(b => b.Stake);
                var minBudgetBet = bs.Bets.MinBy(b => b.Budget);
                var maxBudgetBet = bs.Bets.MaxBy(b => b.Budget);
                var minBudgetInclStakeBet = bs.Bets.MinBy(b => b.BudgetBeforeResult);
                var losesInRow = bs.LosesCounter.Any(c => c.Value > 0) ? bs.LosesCounter.MaxBy(c => c.Value).ToString() : "-";
                var winsInRow = bs.WinsCounter.Any(c => c.Value > 0) ? bs.WinsCounter.MaxBy(c => c.Value).ToString() : "-";

                var lostOUfromWonBTTS = bs.Bets.Count(b => b.PickChoice == PickChoice.BothToScore && b.BetResult != BetResult.Pending && b.MatchHomeScore != null && b.MatchAwayScore != null &&
                    (b.PickValue.ToDouble().Eq(0) && b.BetResult == BetResult.Win && b.MatchHomeScore + b.MatchAwayScore > 2 ||
                    b.PickValue.ToDouble().Eq(1) && b.BetResult == BetResult.Win && b.MatchHomeScore + b.MatchAwayScore <= 2));

                var wonOUfromLostBTTS = bs.Bets.Count(b => b.PickChoice == PickChoice.BothToScore && b.BetResult != BetResult.Pending && b.MatchHomeScore != null && b.MatchAwayScore != null &&
                    (b.PickValue.ToDouble().Eq(0) && b.BetResult == BetResult.Lose && b.MatchHomeScore + b.MatchAwayScore <= 2 ||
                    b.PickValue.ToDouble().Eq(1) && b.BetResult == BetResult.Lose && b.MatchHomeScore + b.MatchAwayScore > 2));

                var wonBTTSwithOU = bs.Bets.Count(b => b.PickChoice == PickChoice.BothToScore && b.BetResult != BetResult.Pending && b.MatchHomeScore != null && b.MatchAwayScore != null &&
                    (b.PickValue.ToDouble().Eq(0) && b.BetResult == BetResult.Win && b.MatchHomeScore + b.MatchAwayScore <= 2 ||
                    b.PickValue.ToDouble().Eq(1) && b.BetResult == BetResult.Win && b.MatchHomeScore + b.MatchAwayScore > 2));

                var gs = new GeneralStatisticsGvVM();
                gs.Add(new GeneralStatisticGvVM("Maksymalna stawka:", $"{maxStakeBet.Stake:0.00} zł ({maxStakeBet.Nr})"));
                gs.Add(new GeneralStatisticGvVM("Najwyższy budżet:", $"{maxBudgetBet.Budget:0.00} zł ({maxBudgetBet.Nr})"));
                gs.Add(new GeneralStatisticGvVM("Najniższy budżet:", $"{minBudgetBet.Budget:0.00} zł ({minBudgetBet.Nr})"));
                gs.Add(new GeneralStatisticGvVM("Najniższy budżet po odjęciu stawki:", $"{minBudgetInclStakeBet.BudgetBeforeResult:0.00} zł ({minBudgetInclStakeBet.Nr})"));
                gs.Add(new GeneralStatisticGvVM("Porażki z rzędu:", $"{losesInRow}"));
                gs.Add(new GeneralStatisticGvVM("Zwycięstwa z rzędu:", $"{winsInRow}"));
                gs.Add(new GeneralStatisticGvVM("Nierozstrzygnięte:", $"{bs.Bets.Count(b => b.BetResult == BetResult.Pending)} [dziś: {bs.Bets.Count(b => b.BetResult == BetResult.Pending && b.LocalTimestamp.Rfc1123.ToDMY() == DateTime.Now.ToDMY())}]"));
                gs.Add(new GeneralStatisticGvVM("BTTS y/n => o/u 2.5 [L - W]:", $"{lostOUfromWonBTTS} - {wonOUfromLostBTTS} [{wonOUfromLostBTTS - lostOUfromWonBTTS}]"));
                if (lostOUfromWonBTTS + wonBTTSwithOU != 0)
                    gs.Add(new GeneralStatisticGvVM("BTTS y/n i o/u 2.5 [L/W]:", $"{lostOUfromWonBTTS} / {wonBTTSwithOU} [{lostOUfromWonBTTS / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}% / {wonBTTSwithOU / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}%]"));
                
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
                        _mainMenu.SelectedTile = tlStatistics;
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
                new NumState("StakingStake", numInitialStake),
                new NumState("StakingBudget", numBudget),
                new DdlState("StakingOnLoseChoice", ddlStakingTypeOnLose),
                new NumState("StakingOnLoseValue", numCoeffOnLose),
                new DdlState("StakingOnWinChoice", ddlStakingTypeOnWin),
                new NumState("StakingOnWinValue", numCoeffOnWin),
                new NumState("StakingIncreaseBudgetRef", numBudgetIncrease),
                new NumState("StakingIncreaseStake", numStakeIncrease),
                new NumState("StakingDecreaseBudgetRef", numBudgetDecrease),
                new NumState("StakingDecreaseStake", numStakeDecrease),
                new DdlState("StakingLoseCondition", ddlLoseCondition),
                new NumState("StakingMaxStake", numMaxStake),
                new DdlState("StakingBasicStake", ddlBasicStake),

                new CbState("FilterLHSByPeriodEnabled", cbLHOddsByPeriodFilter),
                new RbsState("FilterLHSByPeriodChoice", _lowestHighestOddsByPeriodFilterControls.OfType<RadioButton>().ToArray()),
                new NumState("FilterLHSByPeriodValue", numLHOddsPeriodInDays),

                new CbState("FilterTipsterEnabled", cbTipster),
                new MddlState("FilterTipsterChoice", mddlTipsters),

                new CbState("FilterOddsLGThanEnabled", cbOddsLesserGreaterThan),
                new DdlState("FilterOddsLGThanSign", ddlOddsLesserGreaterThan),
                new NumState("FilterOddsLGThanValue", rnumOddsLesserGreaterThan),

                new CbState("FilterSelectionEnabled", cbSelection),
                new RbsState("FilterSelectionChoice", _selectionFilterControls.OfType<RadioButton>().ToArray()),

                new CbState("FilterFromDateEnabled", cbSinceDate),
                new DpState("FilterFromDateValue", dpSinceDate),

                new CbState("FilterToDateEnabled", cbToDate),
                new DpState("FilterToDateValue", dpToDate),

                new CbState("FilterPickEnabled", cbPick),
                new MddlState("FilterPickValue", mddlPickTypes),

                new CbState("FilterWithoutMatchesFromNotesEnabled", cbWithoutMatchesFromNotesFilter),

                new DdlState("StatisticsProfitByPeriod", ddlProfitByPeriodStatistics),

                new CbState("ShowStatisticsOnEvaluateOption", cbShowStatisticsOnEvaluateOption),
                new CbState("HideLoginPasswordsOption", cbHideLoginPasswordsOption),
                new CbState("ShowBrowserOnDataLoadingOption", cbShowBrowserOnDataLoadingOption),

                new CbState("DataLoadingLoadTipsFromDate", cbLoadTipsFromDate),
                new CbState("DataLoadingLoadTipsOnlySelected", cbLoadTipsOnlySelected),
                new GvSelectionState("DataLoadingSelectedTipsters", gvTipsters),

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

    public enum DomainType
    {
        Betshoot,
        Hintwise,
        Custom
    }

    #endregion
}
