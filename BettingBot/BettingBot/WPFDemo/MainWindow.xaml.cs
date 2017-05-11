using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Telerik.Windows.Controls;
using WPFDemo.Models;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.ServiceModel;
using System.ServiceModel.Security.Tokens;
using System.Text;
using AutoMapper;
using HtmlAgilityPack;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using MoreLinq;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Telerik.Windows;
using Telerik.Windows.Data;
using WPFDemo.Common;
using WPFDemo.Common.UtilityClasses;
using WPFDemo.Models.DataLoaders;
using static WPFDemo.Common.Extensions;
using static WPFDemo.Common.Utilities;
using CheckBox = System.Windows.Controls.CheckBox;
using Button = System.Windows.Controls.Button;
using Color = System.Drawing.Color;
using Tile = MahApps.Metro.Controls.Tile;
using WPFColor = System.Windows.Media.Color;
using Control = System.Windows.Controls.Control;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using Clipboard = System.Windows.Clipboard;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = WPFDemo.Common.UtilityClasses.MenuItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Panel = System.Windows.Controls.Panel;
using PlacementMode = System.Windows.Controls.Primitives.PlacementMode;
using DataObject = System.Windows.DataObject;
using DataFormats = System.Windows.DataFormats;
using GridViewDeletedEventArgs = Telerik.Windows.Controls.GridViewDeletedEventArgs;
using GridViewRow = Telerik.Windows.Controls.GridView.GridViewRow;
using Path = System.IO.Path;

namespace WPFDemo
{
    public partial class MainWindow
    {
        #region Constants

        private const string betshoot = "betshoot";
        private const string hintwise = "hintwise";

        #endregion

        #region Fields

        private NotifyIcon _notifyIcon;

        private List<UIElement> _lowestHighestOddsByPeriodFilterControls = new List<UIElement>();
        private List<UIElement> _odssLesserGreaterThanFilterControls = new List<UIElement>();
        private List<UIElement> _selectionFilterControls = new List<UIElement>();
        private List<UIElement> _tipsterFilterControls = new List<UIElement>();
        private List<UIElement> _fromDateFilterControls = new List<UIElement>();
        private List<UIElement> _toDateFilterControls = new List<UIElement>();
        private List<UIElement> _pickFilterControls = new List<UIElement>();
        private List<Button> _buttons = new List<Button>();

        #endregion

        #region Properties

        public static string AppDirPath { get; set; }
        public static string ErrorLogPath { get; set; }

        public BettingSystem BettingSystem { get; set; }
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
            try
            {
                SQLiteConnection.ClearAllPools();
                GC.Collect();
                AutoMapperConfiguration.Configure();
                AppDirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                ErrorLogPath = $@"{AppDirPath}\ErrorLog.log";

                SetupNotifyIcon();
                SetupFlyouts();
                SetupTextBoxes();
                SetupDatePickers();
                SetupUpDowns();
                SetupDropdowns();
                SetupGridviews();
                InitializeControlGroups();

                SetupZIndexes(this);

                //DownloadTipstersAndBetsToDb();
                UpdateGuiWithNewTipsters();
                LoadOptions();
                InitializeContextMenus();
                EvaluateBets();
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
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

        private async void MainWindow_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            var mouseHoveredElements = this.FindLogicalChildren<FrameworkElement>() // TextBox, RadDatePicker, Flyout
                .Where(f =>
                    f.GetType() != typeof(Flyout) && f.GetType() != typeof(MetroAnimatedTabControl) &&
                    (f.FindLogicalParent<Flyout>() == null || f.FindLogicalParent<Flyout>().IsOpen) &&
                    (f.FindLogicalParent<MetroTabItem>() == null || f.FindLogicalParent<MetroTabItem>().IsSelected) &&
                    f.HasClientRectangle(this) && f.ClientRectangle(this).Contains(e.GetPosition(this))).ToList();

            mouseHoveredElements = mouseHoveredElements.GroupBy(Panel.GetZIndex).MaxBy(g => g.Key).ToList();
            if (mouseHoveredElements.Any(f => f.FindLogicalParent<Flyout>() != null))
                mouseHoveredElements.RemoveBy(f => f.FindLogicalParent<Flyout>() == null);

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

        private async void btnEvaluate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DisableControls(_buttons);
                ShowLoader(gridCalculations);
                await Task.Run(() => EvaluateBets());
                HideLoader(gridCalculations);
                EnableControls(_buttons);
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
        }

        private void btnClearDatabase_Click(object sender, RoutedEventArgs e)
        {
            var db = new LocalDbContext();
            db.Database.ExecuteSqlCommand("DELETE FROM tblTipsters");
            db.Database.ExecuteSqlCommand("DELETE FROM tblBets");
            db.SaveChanges();
            ClearGridViews();
            UpdateGuiWithNewTipsters();
            txtNotes.ResetValue();
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
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

            try
            {
                if (!txtLoad.Text.IsUrl()) throw new Exception("To nie jest poprawny adres");

                await Task.Run(() =>
                {
                    var selectedTipsterIdsDdl = mddlTipsters.SelectedCustomIds();
                    DownloadTipsterToDb();
                    UpdateGuiWithNewTipsters();
                    Dispatcher.Invoke(() => mddlTipsters.SelectByCustomIds(selectedTipsterIdsDdl));
                });

            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            HideLoader(gridTipsters);
            EnableControls(_buttons);
        }

        private async void btnDownloadTips_Click(object sender, RoutedEventArgs e)
        {
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

            try
            {
                await Task.Run(() => DownloadTipsToDb()); 
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            HideLoader(gridTipsters);
            EnableControls(_buttons);
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
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

            try
            {
                if (txtLoadDomain.IsNullWhitespaceOrTag() || txtLoadLogin.IsNullWhitespaceOrTag() || txtLoadPassword.IsNullWhitespaceOrTag() || txtLoadDomain.Text.Remove(Space).Contains(",,"))
                    throw new Exception("Wszystkie pola muszą być poprawnie wypełnione");

                await Task.Run(() =>
                {
                    var selLogins = rgvLogins.SelectedItems.Cast<UserForGvVM>().ToList();
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
                        rgvLogins.RefreshWith(db.Logins.MapToVMCollection<UserForGvVM>(), true, false);
                        rgvLogins.SelectedItems.Clear();
                        var userToSelect = rgvLogins.Items.Cast<UserForGvVM>().Single(l => l.Id == selLogin.Id);
                        rgvLogins.SelectedItems.Add(userToSelect);
                    });

                    var me = Tipster.Me();
                    var tipstersButSelf = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
                    var tipstersVM = tipstersButSelf.MapToVMCollection<TipsterForGvVM>();
                    Dispatcher.Invoke(() => rgvTipsters.RefreshWith(tipstersVM, true, false));
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
            
            HideLoader(gridTipsters);
            EnableControls(_buttons);
        }

        private async void btnAddNewLogin_Click(object sender, RoutedEventArgs e)
        {
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

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
                        rgvLogins.RefreshWith(db.Logins.MapToVMCollection<UserForGvVM>(), true, false);
                        rgvLogins.SelectedItems.Clear();
                        var userToSelect = rgvLogins.Items.Cast<UserForGvVM>().Single(l => l.Id == nextLId);
                        rgvLogins.SelectedItems.Add(userToSelect);
                    });

                    var me = Tipster.Me();
                    var tipstersButSelf = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
                    var tipstersVM = tipstersButSelf.MapToVMCollection<TipsterForGvVM>();
                    Dispatcher.Invoke(() => rgvTipsters.RefreshWith(tipstersVM, true, false));
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);

                if (ex is DbUpdateException && ex.InnerException is UpdateException)
                {
                    var sqlException = ex.InnerException?.InnerException as SQLiteException;
                    if (sqlException != null && sqlException.ErrorCode == 19)
                        await this.ShowMessageAsync("Wystąpił Błąd", "Nie można dodać dwóch takich samych użytkowników");
                }
                else
                    await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            HideLoader(gridTipsters);
            EnableControls(_buttons);
        }

        #endregion

        #region - Tile Events

        private void tlCalculations_Click(object sender, RoutedEventArgs e)
        {
            foCalculations.IsOpen = !foCalculations.IsOpen;
        }

        private void tlStatistics_Click(object sender, RoutedEventArgs e)
        {
            foStatistics.IsOpen = !foStatistics.IsOpen;
        }

        private void tlDatabase_Click(object sender, RoutedEventArgs e)
        {
            foDatabase.IsOpen = !foDatabase.IsOpen;
        }

        private void tlOptions_Click(object sender, RoutedEventArgs e)
        {
            foOptions.IsOpen = !foOptions.IsOpen;
        }

        private void tlCalculator_Click(object sender, RoutedEventArgs e)
        {
            foCalculator.IsOpen = !foCalculator.IsOpen;
        }
        
        #endregion

        #region - Checkbox Events

        private void cbLHOddsByPeriodFilter_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_lowestHighestOddsByPeriodFilterControls);
        }

        private void cbLHOddsByPeriodFilter_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_lowestHighestOddsByPeriodFilterControls);
        }

        private void cbOddsLesserGreaterThan_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_odssLesserGreaterThanFilterControls);
        }

        private void cbOddsLesserGreaterThan_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_odssLesserGreaterThanFilterControls);
        }

        private void cbSelection_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_selectionFilterControls);
        }

        private void cbSelection_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_selectionFilterControls);
        }

        private void cbTipster_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_tipsterFilterControls);
        }

        private void cbTipster_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_tipsterFilterControls);
        }

        private void cbSinceDate_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_fromDateFilterControls);
        }

        private void cbSinceDate_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_fromDateFilterControls);
        }

        private void cbToDate_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_toDateFilterControls);
        }

        private void cbToDate_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_toDateFilterControls);
        }

        private void cbPick_Checked(object sender, RoutedEventArgs e)
        {
            EnableControls(_pickFilterControls);
        }

        private void cbPick_Unchecked(object sender, RoutedEventArgs e)
        {
            DisableControls(_pickFilterControls);
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
            pwCol.DataMemberBinding =  new System.Windows.Data.Binding("HiddenPassword");
            rgvLogins.RefreshWith(new LocalDbContext().Logins.Include(l => l.Websites).MapToVMCollection<UserForGvVM>(), true, false);
        }

        private void cbHideLoginPasswordsOption_Unchecked(object sender, RoutedEventArgs e)
        {
            var pwCol = (GridViewDataColumn) rgvLogins.Columns["Password"];
            pwCol.DataMemberBinding = new System.Windows.Data.Binding("Password");
            rgvLogins.RefreshWith(new LocalDbContext().Logins.Include(l => l.Websites).MapToVMCollection<UserForGvVM>(), true, false);
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
            var value = text?.TryToDouble();
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
            if (BettingSystem == null) return;
            rgvProfitByPeriodStatistics.RefreshWith(new ProfitByPeriodStatistics(BettingSystem.Bets, rddlProfitByPeriodStatistics.SelectedEnumValue<Period>()));
        }

        #endregion

        #region - RadGridview Events
        
        private void rgvData_Sorting(object sender, GridViewSortingEventArgs e)
        {
            var betsVM = e.DataControl.ItemsSource as List<BetToDisplayVM>;

            if (betsVM == null)
            {
                e.Cancel = true;
                return;
            }

            BetToDisplayVM firstBet;
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
            var selectedBets = (e.OriginalSource as RadGridView)?.SelectedItems.Cast<BetToDisplayVM>().ToArray();
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
            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayVM>().ToArray();
            if (selectedBets.Length == 0)
            {
                e.Handled = true;
                return;
            }
            if (selectedBets.Length >= 2)
                cm.Disable("Znajdź", "Kopiuj do wyszukiwania");

            cm.IsOpen = true;
        }

        private void RgvData_OnSelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            this.FindLogicalChildren<Flyout>().ForEach(f => f.IsOpen = false);
            var selBets = rgvData.SelectedItems.Cast<BetToDisplayVM>().ToList();
            if (selBets.Count == 1)
            {
                var selBet = selBets.Single();
                var additionalInfo =
                    $"Oryginalny Mecz: {selBet.Match}\n" +
                    $"Oryginalny Zakład: {selBet.PickOriginalString}\n";
                lblAdditionalInfo.Content = new TextBlock { Text = additionalInfo };
            }
            else
            {
                lblAdditionalInfo.Content = null;
            }
        }

        private void rgvGeneralStatistics_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            rgvData.SelectedItem = null;

            var selectedItems = rgvGeneralStatistics.SelectedItems;
            if (selectedItems.Count != 1) return;

            var selectedItem = (GeneralStatistic) selectedItems.Single();
            if (!selectedItem.Value.HasValueBetween("(", ")")) return;

            var valStr = selectedItem.Value.Between("(", ")");
            int val;
            if (!int.TryParse(valStr, out val)) return;

            rgvData.ScrollIntoViewAsync(
                rgvData.Items.Cast<BetToDisplayVM>().Single(s => s.Nr == val),
                rgvData.Columns[rgvData.Columns.Count - 1],
                (frameworkElement) =>
                {
                    var gridViewRow = frameworkElement as GridViewRow;
                    if (gridViewRow != null) gridViewRow.IsSelected = true;
                });
        }

        private void rgvProfitByPeriodStatistics_Sorting(object sender, GridViewSortingEventArgs e)
        {
            var pbpStats = e.DataControl.ItemsSource as ProfitByPeriodStatistics;

            if (pbpStats == null)
            {
                e.Cancel = true;
                return;
            }

            ProfitByPeriodStatistic firstStat;
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
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderBy(bet => bet.PeriodId));
                else if (columnName == profitName) // Profit
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderBy(bet => bet.Profit));
                else if (columnName == countName) // Count
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderBy(bet => bet.Count));
            }
            else if (e.OldSortingState == SortingState.Ascending) // soprtuj malejąco
            {
                e.NewSortingState = SortingState.Descending;

                if (columnName == periodName) // Okres
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderByDescending(bet => bet.PeriodId));
                else if (columnName == profitName) // Profit
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderByDescending(bet => bet.Profit));
                else if (columnName == countName) // Count
                    pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderByDescending(bet => bet.Count));
            }
            else // resetuj sortowanie
            {
                e.NewSortingState = SortingState.None;
                pbpStats = new ProfitByPeriodStatistics(pbpStats.OrderBy(s => s.PeriodId));
            }

            e.DataControl.ItemsSource = pbpStats;
            e.Cancel = true;
        }

        private async void rgvTipsters_Deleted(object sender, GridViewDeletedEventArgs e)
        {
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

            try
            {
                await Task.Run(() =>
                {
                    var deletedTipsters = e.Items.Cast<TipsterForGvVM>().ToArray();
                    var db = new LocalDbContext();
                    if (deletedTipsters.Any())
                    {
                        var selectedTipsterIdsDdl = mddlTipsters.SelectedCustomIds();
                        var ids = deletedTipsters.Select(t => t.Id).ToArray();
                        var newIds = selectedTipsterIdsDdl.Except(ids);
                        db.Bets.RemoveByMany(b => b.TipsterId, ids);
                        db.Tipsters.RemoveByMany(t => t.Id, ids);
                        db.SaveChanges();
                        Dispatcher.Invoke(ClearGridViews);
                        UpdateGuiWithNewTipsters();
                        Dispatcher.Invoke(() => mddlTipsters.SelectByCustomIds(newIds));
                        if (db.Bets.Any())
                            EvaluateBets();
                    }
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            HideLoader(gridTipsters);
            EnableControls(_buttons);
        }
        
        private async void rgvLogins_Deleted(object sender, GridViewDeletedEventArgs e)
        {
            DisableControls(_buttons);
            ShowLoader(gridTipsters);

            try
            {
                await Task.Run(() =>
                {
                    var deletedLogins = e.Items.Cast<UserForGvVM>().ToArray();
                    var db = new LocalDbContext();
                    if (deletedLogins.Any())
                    {
                        var ids = deletedLogins.Select(l => (int?)l.Id).ToArray();
                        foreach (var w in db.Websites)
                            if (ids.Any(id => id == w.LoginId))
                                w.LoginId = null;
                        db.Logins.RemoveByMany(b => b.Id, ids);
                        var unusedWebsites = db.Websites.WhereByMany(w => w.LoginId, ids).Where(w => !db.Tipsters.Any(t => t.WebsiteId == w.Id));
                        db.Websites.RemoveRange(unusedWebsites);
                        db.SaveChanges();

                        var me = Tipster.Me();
                        var tipstersButSelf = db.Tipsters.Include(t => t.Website).Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
                        var tipstersVM = tipstersButSelf.MapToVMCollection<TipsterForGvVM>();
                        Dispatcher.Invoke(() => rgvTipsters.RefreshWith(tipstersVM, true, false));
                    }
                });
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }

            HideLoader(gridTipsters);
            EnableControls(_buttons);
        }

        private void rgvLogins_SelectionChanged(object sender, SelectionChangeEventArgs e)
        {
            var selLogins = rgvLogins.SelectedItems.Cast<UserForGvVM>().ToList();
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
            var item = (e.OriginalSource as RadMenuItem)?.DataContext as MenuItem;
            if (item == null) return;

            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayVM>().ToArray();
            var matchesStr = string.Join("\n", selectedBets.Select(b => $"{b.Match} - {b.PickString}"));
            var searchTerm = selectedBets.FirstOrDefault()?.Match.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";

            switch (item.Text)
            {
                case "Znajdź":
                {
                    var se = new SearchEngine();
                    se.FindBet(selectedBets.Single());
                    var odds = string.Join("\n", se.FoundBets.Select(b => b.ToString()));
                    var result = await this.ShowMessageAsync("Znalezione zakłady", odds, MessageDialogStyle.AffirmativeAndNegative);
                    if (result == MessageDialogResult.Affirmative)
                        SeleniumDriverManager.CloseAllDrivers();

                    break;
                }
                case "Kopiuj do wyszukiwania":
                {
                    var copyToCB = ClipboardWrapper.TrySetText(searchTerm);
                    if (copyToCB.IsFailure)
                        await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message);
                    break;
                }
                case "Kopiuj całość":
                {
                    var copyToCB = ClipboardWrapper.TrySetText(matchesStr);
                    if (copyToCB.IsFailure)
                        await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message);
                    break;
                }
                case "Do notatek":
                {
                    var text = txtNotes.Text;
                    var tag = txtNotes.Tag.ToString();
                    txtNotes.ClearValue(true);
                    if (!string.IsNullOrEmpty(text)) text += "\n";
                    text += matchesStr;
                    var split = text.Split("\n").Where(s => s != tag);
                    var fixedSplit = new List<string>();

                    foreach (var s in split)
                        if (!fixedSplit.Any(el => el.Contains(s)))
                            fixedSplit.Add(s);

                    text = string.Join("\n", fixedSplit);

                    txtNotes.Text = text;
                    if (cbWithoutMatchesFromNotesFilter.IsChecked == true)
                        EvaluateBets();
                    break;
                }
                case "Usuń z bazy":
                {
                    var result = await this.ShowMessageAsync("Usuwanie z bazy danych", "Czy na pewno chcesz usunąć?", MessageDialogStyle.AffirmativeAndNegative);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        var db = new LocalDbContext();
                        db.Bets.RemoveByMany(b => b.Id, selectedBets.Select(b => b.Id));
                        db.SaveChanges();
                        EvaluateBets();
                    }
                    
                    break;
                }
            }
        }

        #endregion

        #region - Flyout Events

        private void foAll_OpenChanged(object sender, RoutedEventArgs e)
        {
            var fo = (sender as Flyout);
            if (fo == null) return;
            if (fo.IsOpen)
                foreach (var ofo in this.FindLogicalChildren<Flyout>().Where(f => f.Name != fo.Name))
                    ofo.IsOpen = false;
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

            var ddlitems = EnumToDdlItems<PickChoice>(Pick.ConvertChoiceToString);
            mddlPickTypes.ItemsSource = ddlitems;
            mddlPickTypes.DisplayMemberPath = "Text";
            mddlPickTypes.SelectedValuePath = "Index";
            mddlPickTypes.SelectAll();
        }

        private void SetupFlyouts()
        {
            foreach (var fo in this.FindLogicalChildren<Flyout>())
            {
                var margin = fo.Margin;
                fo.Margin = new Thickness(85, margin.Top, margin.Right, margin.Bottom);
                fo.IsOpenChanged += foAll_OpenChanged;
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
            foreach (var txtB in this.FindLogicalChildren<TextBox>().Where(t => t.Tag != null))
            {
                txtB.GotFocus += TxtAll_GotFocus;
                txtB.LostFocus += TxtAll_LostFocus;

                var currBg = ((SolidColorBrush) txtB.Foreground).Color;
                txtB.FontStyle = FontStyles.Italic;
                txtB.Text = txtB.Tag.ToString();
                txtB.Foreground = new SolidColorBrush(WPFColor.FromArgb(128, currBg.R, currBg.G, currBg.B));
            }
        }

        private void SetupGridviews()
        {
            var loginsVM = new LocalDbContext().Logins.Include(l => l.Websites).MapToVMCollection<UserForGvVM>();
            rgvLogins.RefreshWith(loginsVM, true, false);
        }

        private void SetupUpDowns()
        {
            foreach (var ud in this.FindLogicalChildren<RadNumericUpDown>())
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

            foreach (var dp in this.FindLogicalChildren<RadDatePicker>())
                dp.Culture = cultureInfo;

            dpLoadTipsFromDate.SelectedDate = DateTime.Now.Subtract(new TimeSpan(2, 0, 0, 0));
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

            _buttons = this.FindLogicalChildren<Button>().Where(b => b.GetType() != typeof(MahApps.Metro.Controls.Tile)).ToList();
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

            foreach (var dp in this.FindLogicalChildren<RadDatePicker>())
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

            foreach (var txt in this.FindLogicalChildren<TextBox>())
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
            foreach (var rgv in this.FindLogicalChildren<RadGridView>())
                rgv.ItemsSource = null;
        }

        private static void DisableControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = false;
        }

        private static void EnableControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = true;
        }

        private static void ToggleControls(IEnumerable<UIElement> controls)
        {
            foreach (var c in controls)
                c.IsEnabled = !c.IsEnabled;
        }

        private void HandleContextMenu(FrameworkElement fe, MouseEventArgs e)
        {
            var cm = fe.ContextMenu();
            var pos = e.GetPosition(fe);
            cm.VerticalOffset = pos.Y - fe.Height;
            cm.HorizontalOffset = pos.X;
            e.Handled = true;

            var handler = GetType().GetRuntimeMethods().FirstOrDefault(m => m.Name == $"{fe.Name}_RadContextMenuOpening");
            if (handler != null)
                handler.Invoke(this, new object[] { fe, e });
            else
                cm.IsOpen = true;
        }

        private void ShowLoader(Panel control)
        {
            var rect = new Rectangle
            {
                Margin = new Thickness(0),
                Fill = new SolidColorBrush(WPFColor.FromArgb(192, 40, 40, 40)),
                Name = "prLoaderContainer"
            };

            var loader = new ProgressRing
            {
                Foreground = (Brush)FindResource("AccentColorBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = 80,
                Height = 80,
                IsActive = true,
                Name = "prLoader"
            };

            Panel.SetZIndex(rect, 10000);
            Panel.SetZIndex(loader, 10001);

            control.Children.Add(rect);
            control.Children.Add(loader);
        }

        private static void HideLoader(Panel control)
        {
            var loaders = control.FindLogicalChildren<ProgressRing>().Where(c => c.Name == "prLoader" ).ToArray();
            var loaderContainers = control.FindLogicalChildren<Rectangle>().Where(c => c.Name == "prLoaderContainer" ).ToArray();

            loaders.ForEach(l => l.IsActive = false);

            loaders.ForEach(l => control.Children.Remove(l));
            loaderContainers.ForEach(r => control.Children.Remove(r));
        }

        #endregion

        #region - Core Functionality

        public void UpdateGuiWithNewTipsters()
        {
            var db = new LocalDbContext();
            var ddlTipsters = new List<DdlItem> { new DdlItem(-1, "(Moje Zakłady)") };
            var me = Tipster.Me();
            var tipsters = db.Tipsters.Include(t => t.Website);
            var tipstersButSelf = tipsters.Where(t => t.Name != me.Name).DistinctBy(t => t.Id).ToList();
            ddlTipsters.AddRange(tipstersButSelf.Select(t => new DdlItem(t.Id, $"{t.Name} ({t.Website.Address})")));
            Dispatcher.Invoke(() =>
            {
                mddlTipsters.ItemsSource = ddlTipsters;
                mddlTipsters.DisplayMemberPath = "Text";
                mddlTipsters.SelectedValuePath = "Index";
            });
            
            var tipstersVM = tipstersButSelf.MapToVMCollection<TipsterForGvVM>();
            Dispatcher.Invoke(() =>
            {
                rgvTipsters.RefreshWith(tipstersVM, true, false);
                txtLoad.ResetValue(true);
            });
        }

        public void DownloadTipsterToDb()
        {
            try
            {
                var newLink = Dispatcher.Invoke(() => txtLoad.Text);
                var placeholder = Dispatcher.Invoke(() => txtLoad.Tag)?.ToString();

                if (!newLink.IsNullWhiteSpaceOrDefault(placeholder))
                    if (new Uri(newLink).Host.ToLower().Contains(betshoot))
                        new BetShootLoader(newLink).DownloadNewTipster();
                    else if (new Uri(newLink).Host.ToLower().Contains(hintwise))
                        new HintWiseLoader(newLink).DownloadNewTipster();
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
                var selectedTipstersIds = rgvTipsters.SelectedItems.Cast<TipsterForGvVM>().Select(t => t.Id).ToArray();
                var selectedTipsters = tipsters.WhereByMany(t => t.Id, selectedTipstersIds).ToArray();
                foreach (var t in Dispatcher.Invoke(() => cbLoadTipsOnlySelected.IsChecked == true) ? selectedTipsters : tipsters)
                {
                    db.Entry(t.Website).Reference(e => e.Login).Load();
                    if (t.Website.Address.ToLower() == betshoot)
                        new BetShootLoader(t.Link).DownloadTips();
                    else if (t.Website.Address.ToLower() == hintwise)
                        new HintWiseLoader(t.Link, t.Website.Login?.Name, t.Website.Login?.Password).DownloadTips(Dispatcher.Invoke(() => cbLoadTipsFromDate.IsChecked == true) ? Dispatcher.Invoke(() => dpLoadTipsFromDate.SelectedDate.ToDMY()) : null);
                    else
                        throw new Exception($"Nie istnieje loader dla strony: {t.Website}");
                }
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
                db.Dispose();
            }
        }

        private void EvaluateBets()
        {
            var db = new LocalDbContext();
            var stakingTypeOnLose = (StakingTypeOnLose) Dispatcher.Invoke(() => rddlStakingTypeOnLose.SelectedValue);
            var stakingTypeOnWin = (StakingTypeOnWin) Dispatcher.Invoke(() => rddlStakingTypeOnWin.SelectedValue);
            var betsByDate = db.Bets.Include(b => b.Tipster).Include(b => b.Pick).OrderBy(b => b.Date).ThenBy(b => b.Match);
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

            var visibleBets = rgvData.Items.Cast<BetToDisplayVM>().ToList();
            var selectedBets = rgvData.SelectedItems.Cast<BetToDisplayVM>().ToList();
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
            var getLowestOddsByPeriod = Dispatcher.Invoke(() => rbHighestOddsByPeriod.IsChecked) == true;
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

            //ClearGridViews();
            Dispatcher.Invoke(() => rgvData.RefreshWith(bs.Bets, true, false));
            //rgvTipsters.RefreshWith(db.Tipsters.Where(t => t.Name.ToLower() != "my").ToList().MapToVMCollection<TipsterForGvVM>(), true, false);
            //rgvLogins.RefreshWith(db.Logins.MapToVMCollection<UserForGvVM>(), true, false);

            //txtNotes.ResetValue();
            if (bs.Bets.Any())
            {
                Dispatcher.Invoke(() => rgvAggregatedWinsLosesStatistics.RefreshWith(new AggregatedWinLoseStatistics(bs.LosesCounter, bs.WinsCounter), false));
                Dispatcher.Invoke(() => rgvProfitByPeriodStatistics.RefreshWith(new ProfitByPeriodStatistics(bs.Bets, rddlProfitByPeriodStatistics.SelectedEnumValue<Period>())));

                var maxStakeBet = bs.Bets.MaxBy(b => b.Stake);
                var minBudgetBet = bs.Bets.MinBy(b => b.Budget);
                var maxBudgetBet = bs.Bets.MaxBy(b => b.Budget);
                var minBudgetInclStakeBet = bs.Bets.MinBy(b => b.BudgetBeforeResult);
                var losesInRow = bs.LosesCounter.Any(c => c.Value > 0) ? bs.LosesCounter.MaxBy(c => c.Value).ToString() : "-";
                var winsInRow = bs.WinsCounter.Any(c => c.Value > 0) ? bs.WinsCounter.MaxBy(c => c.Value).ToString() : "-";

                var lostOUfromWonBTTS = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                                                           ((b.Pick.Value == 0 && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() > 2) ||
                                                            (b.Pick.Value == 1 && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() <= 2)));

                var wonOUfromLostBTTS = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                                                           ((b.Pick.Value == 0 && b.BetResult == Result.Lose && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() <= 2) ||
                                                            (b.Pick.Value == 1 && b.BetResult == Result.Lose && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() > 2)));

                var wonBTTSwithOU = bs.Bets.Count(b => b.Pick.Choice == PickChoice.BothToScore && b.BetResult != Result.Pending && b.MatchResult.Contains("-") &&
                                                       ((b.Pick.Value == 0 && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() <= 2) ||
                                                        (b.Pick.Value == 1 && b.BetResult == Result.Win && b.MatchResult.Remove(" ").Split("-").Select(x => Convert.ToInt32(x)).Sum() > 2)));

                var generalStatistics = new GeneralStatistics();
                generalStatistics.Add(new GeneralStatistic("Makymalna stawka:", $"{maxStakeBet.Stake:0.00} zł ({maxStakeBet.Nr})"));
                generalStatistics.Add(new GeneralStatistic("Najwyższy budżet:", $"{maxBudgetBet.Budget:0.00} zł ({maxBudgetBet.Nr})"));
                generalStatistics.Add(new GeneralStatistic("Najniższy budżet:", $"{minBudgetBet.Budget:0.00} zł ({minBudgetBet.Nr})"));
                generalStatistics.Add(new GeneralStatistic("Najniższy budżet po odjęciu stawki:", $"{minBudgetInclStakeBet.BudgetBeforeResult:0.00} zł ({minBudgetInclStakeBet.Nr})"));
                generalStatistics.Add(new GeneralStatistic("Porażki z rzędu:", $"{losesInRow}"));
                generalStatistics.Add(new GeneralStatistic("Zwycięstwa z rzędu:", $"{winsInRow}"));
                generalStatistics.Add(new GeneralStatistic("Nierozstrzygnięte:", $"{bs.Bets.Count(b => b.BetResult == Result.Pending)} [dziś: {bs.Bets.Count(b => b.BetResult == Result.Pending && b.Date.ToDMY() == DateTime.Now.ToDMY())}]"));
                generalStatistics.Add(new GeneralStatistic("BTTS y/n => o/u 2.5 [L - W]:", $"{lostOUfromWonBTTS} - {wonOUfromLostBTTS} [{wonOUfromLostBTTS - lostOUfromWonBTTS}]"));
                if (lostOUfromWonBTTS + wonBTTSwithOU != 0)
                    generalStatistics.Add(new GeneralStatistic("BTTS y/n i o/u 2.5 [L/W]:", $"{lostOUfromWonBTTS} / {wonBTTSwithOU} [{lostOUfromWonBTTS / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}% / {wonBTTSwithOU / (double) (lostOUfromWonBTTS + wonBTTSwithOU) * 100:0.00}%]"));

                Dispatcher.Invoke(() =>
                {
                    rgvGeneralStatistics.RefreshWith(generalStatistics.ToList());
                    foStatistics.IsOpen = cbShowStatisticsOnEvaluateOption.IsChecked == true;
                });
            }

            BettingSystem = bs;
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

                new CbState("DataLoadingLoadTipsFromDate", cbLoadTipsFromDate),
                new CbState("DataLoadingLoadTipsOnlySelected", cbLoadTipsOnlySelected),
                new RgvSelectionState("DataLoadingSelectedTipsters", rgvTipsters)
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
