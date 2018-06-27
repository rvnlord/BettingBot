using System;
using System.Collections;
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
using BettingBot.Source.Clients.Api.FootballData;
using BettingBot.Source.Clients.Api.FootballData.Responses;
using BettingBot.Source.Clients.Selenium;
using BettingBot.Source.Clients.Selenium.Asianodds;
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
        private string[] prevMatchBetAssocSearchTerm = new string[0];

        private Color _mouseOverBlueColor;
        private Color _defaultBlueColor;
        private Color _defaultWindowColor;

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
        private readonly ObservableCollection<BetToAssociateGvVM> _ocBetsToAssociate = new ObservableCollection<BetToAssociateGvVM>();
        private readonly ObservableCollection<object> _ocSelectedBetsToAssociate = new ObservableCollection<object>();
        private readonly ObservableCollection<MatchToAssociateGvVM> _ocMatchesToAssociate = new ObservableCollection<MatchToAssociateGvVM>();
        private readonly ObservableCollection<object> _ocSelectedMatchesToAssociate = new ObservableCollection<object>();
        private readonly ObservableCollection<SentBetGvVM> _ocSentBets = new ObservableCollection<SentBetGvVM>();
        private readonly ObservableCollection<object> _ocSelectedSentBets = new ObservableCollection<object>();
        
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
                    AutoMapperConfiguration.Configure();
                    AppDirPath = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                    ErrorLogPath = $@"{AppDirPath}\ErrorLog.log";

                    SetupNotifyIcon();

                    Dispatcher.Invoke(() =>
                    {
                        SetupWindow();
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

                    CalculateAndBindBets();
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

        private void btnTEST_Click(object sender, RoutedEventArgs e)
        {
            //var fdApiKey =  txtFootballDataApiPublicKey.Text;
            //var footballdata = new FootballDataClient(fdApiKey);
            //var dm = new DataManager();

            //var bets = dm.GetBets().ToBetsToDisplayGvVM()
            //    .Where(b => b.TipsterName.EqIgnoreCase("Dajkula"))
            //    .OrderByDescending(b => b.LocalTimestamp).ToList();

            //var competitions = footballdata.Competitions(2006);
            //var teams = footballdata.Teams(466);
            //var fixtures = footballdata.Fixtures(440);
            //var fixtures2 = footballdata.Fixtures(466, 25, FixturesPeriodType.Future);
            //var fixtures3 = footballdata.Fixtures(466, 7, FixturesPeriodType.Past);
            //var fixtures4 = footballdata.Fixtures(466, 8, FixturesPeriodType.Future, 27);
            //var fixtures5 = footballdata.Fixtures(466, null, null, 27);

            //var fixtures6 = footballdata.Fixtures();
            //var fixtures7 = footballdata.Fixtures(10, FixturesPeriodType.Future);
            //var fixtures8 = footballdata.Fixtures(
            //    new ExtendedTime(DateTime.Today.SubtractDays(120)), 
            //    new ExtendedTime(DateTime.Today.SubtractDays(120).AddDays(21)));
            //var fixtures9 = footballdata.Fixtures(
            //    new ExtendedTime(DateTime.Today.SubtractDays(320)),
            //    new ExtendedTime(DateTime.Today.SubtractDays(320).AddDays(21)));
            //var fixtures10 = footballdata.Fixtures(
            //    new ExtendedTime(DateTime.Today.SubtractDays(520)),
            //    new ExtendedTime(DateTime.Today.SubtractDays(520).AddDays(21)));
            //var fixtures11 = footballdata.Fixtures(
            //    new ExtendedTime(DateTime.Today.SubtractDays(720)),
            //    new ExtendedTime(DateTime.Today.SubtractDays(720).AddDays(21)));

            //var dbLeagues = footballdata.Competitions().Competitions.ToDbLeagues();
            //dm.UpsertLeagues(dbLeagues);

            var et1 = ExtendedTime.LocalNow;
            var et2 = ExtendedTime.UtcNow - TimeSpan.FromDays(2);

            var t1 = et1 - et2;
            var t2 = et2 - et1;
        }

        private async void btnCalculate_Click(object sender, RoutedEventArgs e)
        {
            List<object> actuallyDisabledControls = null;
            try
            {
                actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
                gridCalculationsFlyout.ShowLoader();
                await Task.Run(() => CalculateAndBindBets());

            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
            finally
            {
                gridCalculationsFlyout.HideLoader();
                if (!gridMain.HasLoader())
                    actuallyDisabledControls.EnableControls();
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
                        Websites = loadDomain.Split(", ").Select(a => new DbWebsite { Address = a }).ToList()
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

        private void btnCancelAssociatingBetWithMatch_Click(object sender, RoutedEventArgs e)
        {
            HideMatchBetManualAssociationPrompt();
        }
        
        private void btnAssociateBetWithMatch_Click(object sender, RoutedEventArgs e)
        {
            var betId = _ocBetsToAssociate.Single().Id;
            var matchId = _ocSelectedMatchesToAssociate.Cast<MatchToAssociateGvVM>().Single().Id;
            AsyncWithLoader(gridData, () =>
            {
                new DataManager().AssociateBetWithMatchById(betId, matchId);
                CalculateAndBindBets();
            });

            var betToSelect = _ocBetsToDisplayGvVM.Single(b => b.Id == betId);
            _ocSelectedBetsToDisplayGvVM.ReplaceAll(betToSelect);
            gvData.ScrollTo(betToSelect);

            _buttonsAndContextMenus.EnableControls();
            HideMatchBetManualAssociationPrompt();
        }

        private void btnRemoveAssociationBetWithMatch_Click(object sender, RoutedEventArgs e)
        {
            var betId = _ocBetsToAssociate.Single().Id;
            AsyncWithLoader(gridData, () =>
            {
                new DataManager().RemoveMatchIdFromBetById(betId);
                CalculateAndBindBets();
            });

            var betToSelect = _ocBetsToDisplayGvVM.Single(b => b.Id == betId);
            _ocSelectedBetsToDisplayGvVM.ReplaceAll(betToSelect);
            gvData.ScrollTo(betToSelect);

            _buttonsAndContextMenus.EnableControls();
            HideMatchBetManualAssociationPrompt();
        }
        
        private async void btnSearchMatchToAssociateWithBet_Click(object sender, RoutedEventArgs e)
        {
            if (_raisingEventImplicitlyFromCode)
                return;

            var dm = new DataManager();
            var searchTerms = txtSearchMatchToAssociate.Text.Trim().Split(" ").Select(w => w.Trim()).Where(w => w.Length > 2).ToArray();
            if (!txtSearchMatchToAssociate.IsNullWhitespaceOrTag() 
                && searchTerms.Any() 
                && !searchTerms.CollectionEqual(prevMatchBetAssocSearchTerm))
            {
                IEnumerable<MatchToAssociateGvVM> matchesMatchingSearchTerms = null;
                await AsyncWithLoader(gridData, () =>
                {
                    matchesMatchingSearchTerms = dm.GetMatches().ToMatchesToAssociateGvVM().Where(m =>
                        $"{m.LeagueName} {m.MatchHomeName} {m.MatchAwayName}".ContainsAll(searchTerms));
                });

                _ocMatchesToAssociate.ReplaceAll(matchesMatchingSearchTerms);
                prevMatchBetAssocSearchTerm = searchTerms;
            }
            else
            {
                IEnumerable<MatchToAssociateGvVM> allMatches = null;
                await AsyncWithLoader(gridData, () => allMatches = dm.GetMatches().ToMatchesToAssociateGvVM());
                _ocMatchesToAssociate.ReplaceAll(allMatches);
            }  
        }

        private async void btnStakeNewCalculator_Click(object sender, RoutedEventArgs e)
        {
            List<object> actuallyDisabledControls = null;
            try
            {
                actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
                gridCalculatorContent.ShowLoader();
                await Task.Run(() =>
                {
                    var tbs = CreateBettingSystem();
                    var stakeNew = Dispatcher.Invoke(() => txtStakeNew.Text);
                    var oddsN = stakeNew.Remove(" ").Split(",").Select(x => x.ToDoubleN()).ToArray();
                    if (oddsN.Any(x => x == null))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            txtStakeNewResult.Text = "Niepoprawny ciąg wejściowy (kursy oddzielone ',')";
                        });
                        return;
                    }
                    
                    tbs.ApplyFilters();

                    var odds = oddsN.Select(x => x.ToDouble()).ToArray();
                    var localNow = ExtendedTime.LocalNow;
                    foreach (var odd in odds)
                    {
                        tbs.FilteredBets.Add(new BetToDisplayGvVM
                        {
                            LocalTimestamp = localNow,
                            BetResult = BetResult.Pending,
                            Odds = odd
                        });
                    }

                    tbs.ApplyStaking();

                    var newBets = tbs.Bets.TakeLast(odds.Length).Reverse().ToArray();

                    Dispatcher.Invoke(() =>
                    {
                        txtStakeNewResult.Text = newBets.Select((x, i) => $"{i + 1}: {x.Odds:0.000} - {x.Stake:0.##} zł").JoinAsString("\n");
                    });
                });

            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
            finally
            {
                gridCalculatorContent.HideLoader();
                if (!gridMain.HasLoader())
                    actuallyDisabledControls.EnableControls();
            }
        }

        private void btnMinimizeToTray_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            ShowInTaskbar = false;
            _notifyIcon.Visible = true;
            _notifyIcon.ShowBalloonTip(1500);
        }

        private void btnMinimizeToTray_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Button) sender).Background = new SolidColorBrush(_mouseOverBlueColor);
            //((Button) sender).Highlight(_mouseOverBlueColor);
        }

        private void btnMinimizeToTray_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Button)sender).Background = Brushes.Transparent;
            //((Button) sender).Highlight(Colors.Transparent);
        }

        private void btnMinimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void btnMinimize_MouseEnter(object sender, MouseEventArgs e)
        {
            //((Button)sender).Highlight(Color.FromRgb(76, 76, 76));
            ((Button)sender).Background = new SolidColorBrush(Color.FromRgb(76, 76, 76));
        }

        private void btnMinimize_MouseLeave(object sender, MouseEventArgs e)
        {
            //((Button)sender).Highlight(_defaultWindowColor);
            ((Button)sender).Background = Brushes.Transparent;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnClose_MouseEnter(object sender, MouseEventArgs e)
        {
            //((Button)sender).Highlight(Color.FromRgb(255, 50, 50));
            ((Button) sender).Background = new SolidColorBrush(Color.FromRgb(76, 76, 76));
            ((Button) sender).Foreground = Brushes.Black;
        }

        private void btnClose_MouseLeave(object sender, MouseEventArgs e)
        {
            //((Button)sender).Highlight(_defaultWindowColor);
            ((Button) sender).Background = Brushes.Transparent;
            ((Button) sender).Foreground = Brushes.White;
        }

        #endregion

        #region - TilesMenu Events

        private void tmMainMenu_MenuTileClick(object sender, MenuTileClickedEventArgs e)
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

        private void TxtAll_GotFocus(object sender, RoutedEventArgs e)
        {
            _raisingEventImplicitlyFromCode = true;
            (sender as TextBox)?.ClearValue();
            _raisingEventImplicitlyFromCode = false;
        }

        private void TxtAll_LostFocus(object sender, RoutedEventArgs e)
        {
            _raisingEventImplicitlyFromCode = true;
            (sender as TextBox)?.ResetValue();
            _raisingEventImplicitlyFromCode = false;
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
            var disciplineName = nameof(firstBet.DisciplineString);
            var leagueName = nameof(firstBet.LeagueName);
            var tipsterName = nameof(firstBet.TipsterString);
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
                
                if (columnName == disciplineName) // Dyscyplina
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.DisciplineString?.ToLower()).ToList());
                if (columnName == leagueName) // Liga
                    betsVM.ReplaceAll(betsVM.OrderBy(bet => bet.LeagueName?.ToLower()).ToList());
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

                if (columnName == disciplineName) // Dyscyplina
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.DisciplineString?.ToLower()).ToList());
                if (columnName == leagueName) // Liga
                    betsVM.ReplaceAll(betsVM.OrderByDescending(bet => bet.LeagueName?.ToLower()).ToList());
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

        private void gvSentBets_Sorting(object sender, DataGridSortingEventArgs e)
        {
            throw new NotImplementedException();
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
                                CalculateAndBindBets();
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

        private void gvMatchesToAssociate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_raisingEventImplicitlyFromCode)
                return;

            var selectedMatches = _ocSelectedMatchesToAssociate.Cast<MatchToAssociateGvVM>().ToArray();
            if (selectedMatches.Length == 1)
                btnAssociateBetWithMatch.IsEnabled = true;
            else
                btnAssociateBetWithMatch.IsEnabled = false;
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
            var selectedBets = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().ToArray();
            var matchesStr = string.Join("\n", selectedBets.Select(b => $"{b.MatchHomeName} - {b.MatchAwayName}: {b.PickString}"));
            var searchTerm = selectedBets.FirstOrDefault()?.MatchHomeName.Split(' ').FirstOrDefault(w => w.Length > 3) ?? "";
            
            switch (e.ClickedItem.Text)
            {
                case "Znajdź i postaw zakład":
                {
                    await MakeBet();
                    break;
                }
                case "Powiąż ręcznie z meczem":
                {
                    ShowMatchBetManualAssociationPrompt();
                    break;
                }
                case "Kopiuj do wyszukiwania":
                {
                    await AsyncWithLoader(gridData, async () =>
                    {
                        var copyToCB = Dispatcher.Invoke(() => ClipboardWrapper.TrySetText(searchTerm));
                        if (copyToCB.IsFailure)
                            await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message));
                    });

                    break;
                }
                case "Kopiuj całość":
                {
                    await AsyncWithLoader(gridData, async () =>
                    {
                        var copyToCB = Dispatcher.Invoke(() => ClipboardWrapper.TrySetText(matchesStr));
                        if (copyToCB.IsFailure)
                            await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Wystąpił Błąd", copyToCB.Message));
                    });

                    break;
                }
                case "Do notatek":
                {
                    await AsyncWithLoader(gridData, () =>
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
                            CalculateAndBindBets();
                    });

                    break;
                }
                case "Usuń z bazy":
                {
                    await AsyncWithLoader(gridData, async () =>
                    {
                        var result = await Dispatcher.Invoke(async () => await this.ShowMessageAsync("Usuwanie z bazy danych", $"Czy na pewno chcesz usunąć wybrane rekordy? ({selectedBets.Length})", MessageDialogStyle.AffirmativeAndNegative));
                        if (result == MessageDialogResult.Affirmative)
                        {
                            var db = new LocalDbContext();
                            db.Bets.RemoveByMany(b => b.Id, selectedBets.Select(b => b.Id));
                            db.SaveChanges();
                            CalculateAndBindBets();
                        }
                    });

                    break;
                }
            }
        }

        private async void cmGvSentBets_Click(object sender, ContextMenuClickEventArgs e)
        {
            try
            {
                switch (e.ClickedItem.Text)
                {
                    case "Odśwież ze strony brokera":
                    {

                        throw new NotImplementedException();

                        //var headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;

                        //await AsyncWithLoader(gridData, () =>
                        //{ // TODO:

                        //    //var dm = new DataManager();

                        //    //var login = dm.GetLoginByWebsite("https://www.asianodds88.com/");
                        //    //if (login == null) throw new NullReferenceException("Brak loginu dla żądanej strony");

                        //    //var asianodds = new AsianoddsClient(login.Name, login.Password, headlessMode)
                        //    //    .ReceiveInfoWith<AsianoddsClient>(client_InformationReceived);
                        //    //var placedBets = asianodds.GetPlacedBets();
                        //    //dm.UpsertMyBets(placedBets.ToDbBets());
                        //    //_ocSentBets.ReplaceAll(dm.GetMyBets().ToSentBetsGvVM());
                        //    //CalculateBets();
                        //});

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
        }

        #endregion

        #region - Grid Events

        private void gridFlyout_VisibilityChanged(object sender, DependencyPropertyChangedEventArgs dependencyPropertyChangedEventArgs)
        {

        }

        private void gridTitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void gridTitleBar_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Grid) sender).Highlight(_defaultBlueColor);
        }

        private void gridTitleBar_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Grid) sender).Highlight(_defaultWindowColor);
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

        private void client_InformationReceived(object sender, InformationSentEventArgs e)
        {
            UpdateLoaderStatusText(e.Information);
        }
        
        #endregion

        #endregion

        #region Methods

        #region - Controls Management

        private Task AsyncWithLoader(Panel loaderContainer, Action action)
        {
            var task = Task.Run(() =>
            {
                List<object> actuallyDisabledControls = null;
                Dispatcher.Invoke(() =>
                {
                    actuallyDisabledControls = _buttonsAndContextMenus.DisableControls();
                    loaderContainer.ShowLoader();
                });

                try
                {
                    action();
                }
                finally
                {
                    Dispatcher.Invoke(() =>
                    {
                        loaderContainer.HideLoader();
                        if (!gridMain.HasLoader())
                            actuallyDisabledControls.EnableControls();
                    });
                }
            });
            return task;
        }

        private static void SetupZIndexes(FrameworkElement container, int i = 0)
        {
            foreach (var f in LogicalTreeHelper.GetChildren(container).OfType<FrameworkElement>())
            {
                f.ZIndex(i);
                i++;
                SetupZIndexes(f, i);
            }
        }

        private void SetupWindow()
        {
            _mouseOverBlueColor = ((SolidColorBrush)FindResource("MouseOverBlueBrush")).Color;
            _defaultBlueColor = ((SolidColorBrush)FindResource("DefaultBlueBrush")).Color;
            _defaultWindowColor = ((SolidColorBrush)FindResource("DefaultWindowBrush")).Color;
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

            gridAssociateMatchPromptOuterContainer.Visibility = Visibility.Hidden;
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
            _raisingEventImplicitlyFromCode = true;

            foreach (var txtB in this.FindLogicalDescendants<TextBox>().Where(t => t.Tag != null))
            {
                txtB.GotFocus += TxtAll_GotFocus;
                txtB.LostFocus += TxtAll_LostFocus;

                var currBg = ((SolidColorBrush) txtB.Foreground).Color;
                txtB.FontStyle = FontStyles.Italic;
                txtB.Text = txtB.Tag.ToString();
                txtB.Foreground = new SolidColorBrush(Color.FromArgb(128, currBg.R, currBg.G, currBg.B));
            }

            _raisingEventImplicitlyFromCode = false;
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

            gvBetsToAssociate.ItemsSource = _ocBetsToAssociate;
            gvBetsToAssociate.SetSelecteditemsSource(_ocSelectedBetsToAssociate);

            gvMatchesToAssociate.ItemsSource = _ocMatchesToAssociate;
            gvMatchesToAssociate.SetSelecteditemsSource(_ocSelectedMatchesToAssociate);

            gvSentBets.ItemsSource = _ocSentBets;
            gvSentBets.SetSelecteditemsSource(_ocSelectedSentBets);

            var dm = new DataManager();
            _ocLogins.ReplaceAll(dm.GetLogins().ToLoginsGvVM());
            gvLogins.ScrollToEnd();

            RefreshBets(dm);
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
            _mainMenu.MenuTileClick += tmMainMenu_MenuTileClick;

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
                new ContextMenuItem("Znajdź i postaw zakład", PackIconModernKind.Magnify),
                new ContextMenuItem("Powiąż ręcznie z meczem", PackIconModernKind._3dCollada),
                new ContextMenuItem("Kopiuj do wyszukiwania", PackIconModernKind.PageCopy),
                new ContextMenuItem("Kopiuj całość", PackIconModernKind.ListCheck),
                new ContextMenuItem("Do notatek", PackIconModernKind.PageOnenote),
                new ContextMenuItem("Usuń z bazy", PackIconModernKind.LayerDelete));
            cmGvData.ContextMenuOpen += cmGvData_Open;
            cmGvData.ContextMenuClick += cmGvData_Click;

            var cmGvSentBets = gvSentBets.ContextMenu().Create(
                new ContextMenuItem("Odśwież ze strony brokera", PackIconModernKind.Refresh));
            cmGvSentBets.ContextMenuClick += cmGvSentBets_Click;

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
            if (selectedBets.Length == 1)
            {
                var selectedBet = selectedBets.Single();
                if (selectedBet.BetResult != BetResult.Pending)
                    cm.Disable("Znajdź i postaw zakład");
            }
            if (selectedBets.Length >= 2)
                cm.Disable("Znajdź i postaw zakład", "Powiąż ręcznie z meczem", "Kopiuj do wyszukiwania");
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

        private async void ShowMatchBetManualAssociationPrompt()
        {
            gridAssociateMatchPromptOuterContainer.Visibility = Visibility.Visible;
            var zIndex = this.FindLogicalDescendants<FrameworkElement>().MaxBy(Panel.GetZIndex).ZIndex() + 1;
            gridAssociateMatchPromptOuterContainer.ZIndex(zIndex);

            var selectedBet = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().Single();
            
            List<MatchToAssociateGvVM> allMatches = null;
            int? matchId = null;
            BetToAssociateGvVM bet = null;
            await AsyncWithLoader(gridData, () =>
            {
                var dm = new DataManager();
                bet = dm.GetBetById(selectedBet.Id).ToBetToAssociateGvVM();
                allMatches = dm.GetMatches().ToMatchesToAssociateGvVM();
                matchId = bet.MatchId;
            });

            _ocBetsToAssociate.ReplaceAll(bet);
            _ocMatchesToAssociate.ReplaceAll(allMatches);

            if (matchId != null)
            {
                var associatedmatch = _ocMatchesToAssociate.Single(m => m.Id == matchId);
                _ocSelectedMatchesToAssociate.ReplaceAll(associatedmatch);
                gvMatchesToAssociate.ScrollTo(associatedmatch);
                _buttonsAndContextMenus.Except(btnSearchMatchToAssociateWithBet, btnAssociateBetWithMatch, btnCancelAssociatingBetWithMatch, btnRemoveAssociationBetWithMatch).DisableControls();
            }
            else
            {
                gvMatchesToAssociate.ScrollToStart();
                _buttonsAndContextMenus.Except(btnSearchMatchToAssociateWithBet, btnCancelAssociatingBetWithMatch, btnRemoveAssociationBetWithMatch).DisableControls();
            }
        }

        private void HideMatchBetManualAssociationPrompt()
        {
            _buttonsAndContextMenus.EnableControls();
            gridAssociateMatchPromptOuterContainer.Visibility = Visibility.Hidden;
        }

        public void UpdateLoaderStatusText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                var loaderStatuses = this.FindLogicalDescendants<TextBlock>().Where(c => c.Name == "prLoaderStatus").ToArray();
                foreach (var tb in loaderStatuses)
                    tb.Text = text;
            });
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
                addedTipsters = tipstersVM.Except(_ocTipsters).OrderBy(t => t.Name.ToLower()).ToList();
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

                var dm = new DataManager().ReceiveInfoWith<DataManager>(client_InformationReceived);
                if (tipsterDomain == DomainType.Betshoot
                    || tipsterDomain == DomainType.Custom && tipsterName.ToLower().Contains(strBetshoot))
                {
                    var betshoot = new BetshootClient();
                    var tipsterAddress = tipsterDomain != DomainType.Custom 
                        ? betshoot.ReceiveInfoWith<BetshootClient>(client_InformationReceived).TipsterAddress(tipsterName).Address
                        : tipsterName;
                    var tipsterResponse = betshoot.ReceiveInfoWith<BetshootClient>(client_InformationReceived).Tipster(tipsterAddress);
                    dm.AddTipsterIfNotExists(tipsterResponse.ToDbTipster()); // wywołuje TipsterConverter
                }
                else if (tipsterDomain == DomainType.Hintwise
                    || tipsterDomain == DomainType.Custom && tipsterName.ToLower().Contains(strHintwise))
                {
                    var login = dm.WebsiteLogin(DomainType.Hintwise);
                    var hintwise = new HintwiseClient(login.Name, login.Password, headlessMode);
                    var tipsterAddress = tipsterDomain != DomainType.Custom
                        ? hintwise.ReceiveInfoWith<HintwiseClient>(client_InformationReceived).TipsterAddress(tipsterName).Address
                        : tipsterName;
                    var tipsterResponse = hintwise.ReceiveInfoWith<HintwiseClient>(client_InformationReceived).Tipster(tipsterAddress);
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
                var dm = new DataManager().ReceiveInfoWith<DataManager>(client_InformationReceived);
                var dbTipsters = dm.GetTipstersExceptDefault();
                var selectedTipstersIds = _ocSelectedTipsters.Cast<TipsterGvVM>().Select(t => t.Id).ToArray();
                var selectedDbTipsters = dbTipsters.WhereByMany(t => t.Id, selectedTipstersIds).ToList();

                var headlessMode = false;
                var onlySelected = false;
                ExtendedTime fromDate = null;
                var ignoreAutoAssociatingBetsThatWereAlreadyTried = false;
                var includeMine = false;
                Dispatcher.Invoke(() =>
                {
                    headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                    fromDate = cbLoadTipsFromDate.IsChecked == true
                        ? dpLoadTipsFromDate.SelectedDate?.ToDMY().ToExtendedTime(TimeZoneKind.CurrentLocal)
                        : null;
                    onlySelected = cbLoadTipsOnlySelected.IsChecked == true;
                    ignoreAutoAssociatingBetsThatWereAlreadyTried = cbIgnoreAssociatingTried.IsChecked == true;
                    includeMine = cbLoadTipsMine.IsChecked == true;
                });
                
                foreach (var t in onlySelected ? selectedDbTipsters : dbTipsters)
                {
                    var domain = t.Website.Address.ToLower();
                    if (domain == strBetshoot)
                    {
                        var tipsResponse = new BetshootClient().ReceiveInfoWith<BetshootClient>(client_InformationReceived)
                            .Tips(t.ToBetshootTipsterResponse(), fromDate);
                        
                        dm.UpsertBets(tipsResponse.Tipster.ToDbTipster(), tipsResponse.ToDbBets());
                    }
                    else if (domain == strHintwise)
                    {
                        var tipsResponse = new HintwiseClient(
                            t.Website.Login.Name, 
                            t.Website.Login.Password,
                            headlessMode
                        ).ReceiveInfoWith<HintwiseClient>(client_InformationReceived)
                            .Tips(t.ToHintwiseTipsterResponse(), fromDate);
                        dm.UpsertBets(tipsResponse.Tipster.ToDbTipster(), tipsResponse.ToDbBets());
                    }
                    else
                        throw new Exception($"Nie istnieje loader dla strony: {t.Website.Address}");
                }

                if (includeMine)
                {
                    var login = dm.GetLoginByWebsite("https://www.asianodds88.com/");
                    if (login == null) throw new NullReferenceException("Brak loginu dla żądanej strony");

                    var asianodds = new AsianoddsClient(login.Name, login.Password, headlessMode)
                        .ReceiveInfoWith<AsianoddsClient>(client_InformationReceived);
                    var bets = asianodds.HistoricalBets(fromDate);
                    dm.UpsertBets(DbTipster.Me(), bets.ToDbBets(), false, true);
                }

                try
                {
                    ImportMatchesFromFootballData(dm);
                }
                catch (FootballDataException ex)
                {
                    UpdateLoaderStatusText($"Football-Data Api zwróciło błąd, mecze nie zostaną zaaktualizowane. Błąd: {ex.Message}");
                }
                
                dm.AssociateBetsWithFootballDataMatchesAutomatically(ignoreAutoAssociatingBetsThatWereAlreadyTried);
                CalculateAndBindBets();
                Dispatcher.Invoke(() => RefreshBets(dm));
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
            }
        }

        private void ImportMatchesFromFootballData(DataManager dm)
        {
            var fdApiKey = Dispatcher.Invoke(() => txtFootballDataApiPublicKey.Text);
            var footballdata = new FootballDataClient(fdApiKey).ReceiveInfoWith<FootballDataClient>(client_InformationReceived);
            var today = DateTime.Today;
            var year = today.Year;
            
            var oldestUnfinishedMatchDate = dm.GetOldestUnfinishedMatchDate() ?? dm.GetOldestUnassociatedMatchDate() ?? today.SubtractYears(10);

            if (oldestUnfinishedMatchDate > today.SubtractDays(100))
            {
                const int timeFrame = 21;
                var from = oldestUnfinishedMatchDate.SubtractDays(2);
                var to = today.AddDays(2);
                var localFrom = from;
                var localTo = from.AddDays(timeFrame); // maksymalny przedział

                do
                {
                    var fixtures = footballdata.Fixtures(localFrom.ToExtendedTime(), localTo.ToExtendedTime());
                    var fixtureCompetitionIds = fixtures.Fixtures.Select(f => f.CompetitionId).ToArray();
                    var dbLeagueIds = dm.GetLeagueIds();

                    if (!dbLeagueIds.ContainsAll(fixtureCompetitionIds)) // uzupełnij bd jeśli mecze należą do lig z nowego roku
                    {
                        var dbLeagues = footballdata.Competitions(DateTime.Today.Year).Competitions.ToDbLeagues().WithDiscipline(DisciplineType.Football);

                        var leagueids = dbLeagues.Select(l => l.Id).ToArray();
                        dm.UpsertLeagues(dbLeagues);
                        foreach (var lId in leagueids)
                            dm.UpsertTeams(footballdata.Teams(lId).ToDbTeams());
                    }

                    dm.UpsertMatches(fixtures.ToDbMatches());

                    localFrom = localFrom.AddDays(timeFrame);
                    localTo = localTo.AddDays(timeFrame);
                }
                while (localFrom < to);
            }
            else
            {
                var competitions = new List<CompetitionResponse>();
                int leaguesInYearNum;
                do
                {
                    leaguesInYearNum = dm.GetLeaguesByYear(year).Count;
                    if (year == today.Year || leaguesInYearNum == 0)
                    {
                        var yearCompetitions = footballdata.Competitions(year).Competitions;
                        competitions.AddRange(yearCompetitions);
                        leaguesInYearNum = yearCompetitions.Count;
                    }
                }
                while (year-- >= oldestUnfinishedMatchDate.Year && leaguesInYearNum > 0); // drużyny z 2018 biorą udział w ligach z 2017 i 2018
                year++;

                var dbLeagues = competitions.ToDbLeagues().WithDiscipline(DisciplineType.Football);
                dm.UpsertLeagues(dbLeagues);

                var allDbLeagues = dm.GetLeaguesBetweenYears(year, today.Year); // nie tylko dodane, ale wszystkie z bazy
                foreach (var l in allDbLeagues)
                {
                    if (l.Season == today.Year || l.Matches.Count == 0)
                    {
                        var teams = footballdata.Teams(l.Id);
                        dm.UpsertTeams(teams.ToDbTeams());
                        var fixtures = footballdata.Fixtures(l.Id);
                        dm.UpsertMatches(fixtures.ToDbMatches());
                    }
                }
            }
        }

        private void CalculateAndBindBets()
        {
            var bs = CreateBettingSystem();

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

        private BettingSystem CreateBettingSystem()
        {
            var dm = new DataManager();
            var stakingTypeOnLose = (StakingTypeOnLose)Dispatcher.Invoke(() => ddlStakingTypeOnLose.SelectedValue);
            var stakingTypeOnWin = (StakingTypeOnWin)Dispatcher.Invoke(() => ddlStakingTypeOnWin.SelectedValue);

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
            var loseCondition = (LoseCondition)Dispatcher.Invoke(() => ddlLoseCondition.SelectedValue);
            var maxStake = Dispatcher.Invoke(() => numMaxStake.Value ?? 0);
            var resetStake = (BasicStake)Dispatcher.Invoke(() => ddlBasicStake.SelectedValue) == BasicStake.Base;

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
            
            return bs;
        }

        private async Task MakeBet()
        {
            try
            {
                var dm = new DataManager();
                var headlessMode = cbShowBrowserOnDataLoadingOption.IsChecked != true;
                var betRequest = _ocSelectedBetsToDisplayGvVM.Cast<BetToDisplayGvVM>().Single().ToBetRequest();

                await AsyncWithLoader(gridData, () =>
                {
                    var login = dm.GetLoginByWebsite("https://www.asianodds88.com/");
                    if (login == null) throw new NullReferenceException("Brak loginu dla żądanej strony");

                    var asianodds = new AsianoddsClient(login.Name, login.Password, headlessMode)
                        .ReceiveInfoWith<AsianoddsClient>(client_InformationReceived);
                    var betResponse = asianodds.MakeBet(betRequest);
                    dm.AddMyBet(betResponse.ToDbBet());
                    CalculateAndBindBets();
                });

                RefreshBets(dm);
            }
            catch (Exception ex)
            {
                File.WriteAllText(ErrorLogPath, ex.StackTrace);
                await this.ShowMessageAsync("Wystąpił Błąd", ex.Message);
            }
            finally
            {
                SeleniumDriverManager.CloseAllDrivers();
            }
        }

        private void RefreshBets(DataManager dm)
        {
            var originallySelectedTab = matcMain.SelectedItem;
            matcMain.SelectedItem = tabBets;

            _ocSentBets.ReplaceAll(dm.GetMyBets().ToSentBetsGvVM());
            gvSentBets.Refresh().ScrollToEnd();

            matcMain.SelectedItem = originallySelectedTab;
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
                new TextBoxState("FootballDataApiPublicKeyOption", txtFootballDataApiPublicKey),
                new CbState("IgnoreAutoAssociatingBetsThatWereTriedBefore", cbIgnoreAssociatingTried),

                new CbState("DataLoadingLoadTipsFromDate", cbLoadTipsFromDate),
                new CbState("DataLoadingLoadTipsOnlySelected", cbLoadTipsOnlySelected),
                new CbState("DataLoadingLoadTipsIncludeMine", cbLoadTipsMine),
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
