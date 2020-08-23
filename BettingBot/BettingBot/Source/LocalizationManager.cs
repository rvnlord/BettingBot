using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using BettingBot.Source.Common;
using BettingBot.Source.Common.UtilityClasses;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext.Models;
using BettingBot.Source.ViewModels.Collections;
using BettingBot.Source.WIndows;
using MahApps.Metro.Controls;
using ContextMenu = BettingBot.Source.Common.UtilityClasses.ContextMenu;
using Extensions = BettingBot.Source.Common.Extensions;

namespace BettingBot.Source
{
    public static class LocalizationManager
    {
        public static MainWindow _wnd;

        public static Dictionary<Lang, CultureInfo> _cultures = new Dictionary<Lang, CultureInfo>()
        {
            [Lang.Polish] = new CultureInfo("pl-PL")
            {
                NumberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." },
                DateTimeFormat = { ShortDatePattern = "dd-MM-yyyy" } // nie tworzyć nowego obiektu DateTimeFormat tutaj tylko przypisać jego interesujące nas właściwości, bo nowy obiekt nieokreślone właściwości zainicjalizuje wartościami dla InvariantCulture, czyli angielskie nazwy dni, miesięcy itd.
            },
            [Lang.English] = new CultureInfo("en-GB")
            {
                NumberFormat = new NumberFormatInfo { NumberDecimalSeparator = "." },
                DateTimeFormat = { ShortDatePattern = "dd-MM-yyyy" } 
            },
        };

        public static Lang Language { get; private set; }
        public static List<DbLocalizedString> LocalizedStrings { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static CultureInfo Culture = _cultures[Lang.Polish];

        public static void Initialize(MainWindow wnd, Lang language)
        {
            _wnd = wnd;
            Language = language;
            Culture = _cultures[language];
            var dm = new DataManager();
            LocalizedStrings = dm.GetLocalizedStrings(Language);
            IsInitialized = true;
        }

        public static void SetCulture()
        {
            var culture = Culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
        }

        public static void Localize()
        {
            SetCulture();

            LocalizeDataGridHeaders(_wnd.gvSentBets);
            LocalizeDataGridHeaders(_wnd.gvData);
            LocalizeDataGridHeaders(_wnd.gvAggregatedWinsLosesStatistics);
            LocalizeDataGridHeaders(_wnd.gvProfitByPeriodStatistics);
            LocalizeDataGridHeaders(_wnd.gvGeneralStatistics);
            LocalizeDataGridHeaders(_wnd.gvTipsters);
            LocalizeDataGridHeaders(_wnd.gvLogins);
            LocalizeDataGridHeaders(_wnd.gvMatchesToAssociate);

            LocalizeTile(_wnd.tlSimulationsMainGridTab);
            LocalizeTile(_wnd.tlBetsMainGridTab);
            LocalizeTile(_wnd.tmMainMenu.MenuTiles.Single(mt => mt.Name == "tlCalculations"));
            LocalizeTile(_wnd.tmMainMenu.MenuTiles.Single(mt => mt.Name == "tlStatistics"));
            LocalizeTile(_wnd.tmMainMenu.MenuTiles.Single(mt => mt.Name == "tlDatabase"));
            LocalizeTile(_wnd.tmMainMenu.MenuTiles.Single(mt => mt.Name == "tlOptions"));
            LocalizeTile(_wnd.tmMainMenu.MenuTiles.Single(mt => mt.Name == "tlCalculator"));
            LocalizeTile(_wnd.tlGeneralOptionsTab);
            LocalizeTile(_wnd.tlCalculationsOptionsTab);
            LocalizeTile(_wnd.tlDatabaseOptionsTab);
            LocalizeTile(_wnd.tlTipstersDatabaseTab);
            LocalizeTile(_wnd.tlTeamsAndLeaguesDatabaseTab);
            LocalizeTile(_wnd.tlOptionsFlyoutHeader);
            LocalizeTile(_wnd.tlCalculationsFlyoutHeader);
            LocalizeTile(_wnd.tlCalculatorFlyoutHeader);
            LocalizeTile(_wnd.tlDatabaseFlyoutHeader);
            LocalizeTile(_wnd.tlStatisticsFlyoutHeader);

            LocalizeContextMenu(_wnd.mddlTipsters);
            LocalizeContextMenu(_wnd.mddlPickTypes);
            LocalizeContextMenu(_wnd.gvData);
            LocalizeContextMenus<DatePicker>();
            LocalizeContextMenus<TextBox>();

            LocalizeLabel(_wnd.lblWindowTitle);
            LocalizeLabel(_wnd.lblLanguage);
            LocalizeLabel(_wnd.lblFootballDataApiPublicKey);
            LocalizeLabel(_wnd.lblDoubleChance);
            LocalizeLabel(_wnd.lblStakeNew);
            LocalizeLabel(_wnd.lblCalculations);
            LocalizeLabel(_wnd.lblInitialStake);
            LocalizeLabel(_wnd.lblBudget);
            LocalizeLabel(_wnd.lblOnLose);
            LocalizeLabel(_wnd.lblOnWin);
            LocalizeLabel(_wnd.lblIfBudget);
            LocalizeLabel(_wnd.lblStakeAdd);
            LocalizeLabel(_wnd.lblIfBudgetLowerThan);
            LocalizeLabel(_wnd.lblStakeSubtract);
            LocalizeLabel(_wnd.lblLoseCondition);
            LocalizeLabel(_wnd.lblBasicStake);
            LocalizeLabel(_wnd.lblMaxStake);
            LocalizeLabel(_wnd.lblNotes);
            LocalizeLabel(_wnd.lblLHOddsByPeriodOdds);
            LocalizeLabel(_wnd.lblLHOddsPeriodInDays);
            LocalizeLabel(_wnd.lblOddsLesserGreaterThan);
            LocalizeLabel(_wnd.lblSelection);
            LocalizeLabel(_wnd.lblDate);
            LocalizeLabel(_wnd.lblToDate);
            LocalizeLabel(_wnd.lblTipster);
            LocalizeLabel(_wnd.lblPick);
            LocalizeLabel(_wnd.lblFilters);
            LocalizeLabel(_wnd.lblAssociateWithMatch);

            LocalizeCheckBox(_wnd.cbShowStatisticsOnEvaluateOption);
            LocalizeCheckBox(_wnd.cbHideLoginPasswordsOption);
            LocalizeCheckBox(_wnd.cbShowBrowserOnDataLoadingOption);
            LocalizeCheckBox(_wnd.cbIgnoreAssociatingTried);
            LocalizeCheckBox(_wnd.cbLoadTipsFromDate);
            LocalizeCheckBox(_wnd.cbLoadTipsOnlySelected);
            LocalizeCheckBox(_wnd.cbLoadTipsMine);
            LocalizeCheckBox(_wnd.cbWithoutMatchesFromNotesFilter);

            LocalizeRadioButton(_wnd.rbHighestOddsByPeriod);
            LocalizeRadioButton(_wnd.rbLowestOddsByPeriod);
            LocalizeRadioButton(_wnd.rbSumOddsByPeriod);
            LocalizeRadioButton(_wnd.rbSelected);
            LocalizeRadioButton(_wnd.rbUnselected);
            LocalizeRadioButton(_wnd.rbVisible);

            LocalizeButton(_wnd.btnAddTipster);
            LocalizeButton(_wnd.btnDownloadTips);
            LocalizeButton(_wnd.btnSaveLogin);
            LocalizeButton(_wnd.btnAddNewLogin);
            LocalizeButton(_wnd.btnCalculatorCalculate);
            LocalizeButton(_wnd.btnStakeNewCalculator);
            LocalizeButton(_wnd.btnClearDatabase);
            LocalizeButton(_wnd.btnGet);
            LocalizeButton(_wnd.btnSearchMatchToAssociateWithBet);
            LocalizeButton(_wnd.btnAssociateBetWithMatch);
            LocalizeButton(_wnd.btnRemoveAssociationBetWithMatch);
            LocalizeButton(_wnd.btnCancelAssociatingBetWithMatch);

            LocalizeDropdown(_wnd.ddlProfitByPeriodStatistics);
            LocalizeDropdown(_wnd.ddlTipsterDomain);
            LocalizeDropdown(_wnd.ddlLoseCondition);
            LocalizeDropdown(_wnd.ddlBasicStake);
            LocalizeDropdown(_wnd.ddlStakingTypeOnLose);
            LocalizeDropdown(_wnd.ddlStakingTypeOnWin);

            LocalizeListBox(_wnd.mddlTipsters, 0, 1);
            LocalizeListBox(_wnd.mddlPickTypes, 0, 1, true);

            LocalizeTextBlock(_wnd.tbUnimplemented1);

            LocalizeTooltip(_wnd.btnMinimizeToTray);
            LocalizeTooltip(_wnd.btnMinimize);
            LocalizeTooltip(_wnd.btnClose);

            LocalizeWatermark(_wnd.dpLoadTipsFromDate);
            LocalizeWatermark(_wnd.dpSinceDate);
            LocalizeWatermark(_wnd.dpToDate);

            LocalizeTag(_wnd.txtLoadDomain);
            LocalizeTag(_wnd.txtLoadLogin);
            LocalizeTag(_wnd.txtLoadPassword);
            LocalizeTag(_wnd.txtCalculatorResult);
            LocalizeTag(_wnd.txtStakeNew);
            LocalizeTag(_wnd.txtStakeNewResult);
            LocalizeTag(_wnd.txtTipsterName);
            LocalizeTag(_wnd.txtNotes);
            LocalizeTag(_wnd.txtSearchMatchToAssociate);
        }

        public static void LocalizeDataGridHeaders(DataGrid dg)
        {
            var headers = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "DataGrid_Headers_" + dg.Name).Value.Split(",", false, StringSplitOptions.None);
            for (var i = 0; i < dg.Columns.Count; i++)
                dg.Columns[i].Header = headers[i];
        }

        public static void LocalizeTile(Tile tile)
        {
            var tb = tile.VisualDescendants<TextBlock>().Single(t => !string.IsNullOrWhiteSpace(t.Text));
            tb.Text = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Tile_" + tile.Name).Value;
        }

        public static void LocalizeContextMenu(FrameworkElement fe)
        {
            var cm = fe.ContextMenu();
            var itemTexts = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "ContextMenu_Items_" + cm.Control.Name).Value.Split(",", false, StringSplitOptions.None);
            for (var i = 0; i < cm.Items.Count; i++)
            {
                var cmi = cm.Items[i];
                cmi.Text = itemTexts[i];
                cmi.Recreate();
            }
        }

        public static void LocalizeContextMenus<T>() where T : FrameworkElement
        {
            var menusForType = ContextMenusManager.ContextMenus.Where(kvp => kvp.Key is T).Select(kvp => kvp.Value).ToArray();
            var itemTexts = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "ContextMenu_Items_" + typeof(T).Name).Value.Split(",", false, StringSplitOptions.None);
            foreach (var cm in menusForType)
            {
                for (var i = 0; i < cm.Items.Count; i++)
                {
                    cm.Items[i].Text = itemTexts[i];
                    cm.Items[i].Recreate();
                }
            }
        }

        public static void LocalizeLabel(Label lbl)
        {
            lbl.Content = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Label_" + lbl.Name).Value;
        }

        public static void LocalizeButton(Button btn)
        {
            btn.Content = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Button_" + btn.Name).Value;
        }

        public static void LocalizeTooltip(FrameworkElement fe)
        {
            fe.ToolTip = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Tooltip_" + fe.Name).Value;
        }

        public static void LocalizeCheckBox(CheckBox cb)
        {
            cb.Content = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "CheckBox_" + cb.Name).Value;
        }

        public static void LocalizeWatermark(FrameworkElement fe)
        {
            fe.SetValue(TextBoxHelper.WatermarkProperty, LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Watermark_" + fe.Name).Value);
        }

        public static void LocalizeTag(FrameworkElement fe)
        {
            var isEmptyTextBox = fe is TextBox txtB && txtB.IsNullWhitespaceOrTag();
            fe.Tag = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Tag_" + fe.Name).Value;
            if (isEmptyTextBox)
                ((TextBox)fe).ResetValue(true); // force because localized placeholder is different than the previous one
        }

        public static void LocalizeDropdown(ComboBox ddl)
        {
            var eventHandlers = ddl.RemoveEventHandlers(nameof(ddl.SelectionChanged));

            var items = ddl.Items.Cast<DdlItem>().ToArray();
            var selectedItem = ddl.SelectedItem();
            ddl.ItemsSource = null;
            var itemTexts = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "ComboBox_Items_" + ddl.Name).Value.Split(",", false, StringSplitOptions.None);
            for (var i = 0; i < items.Length; i++)
            {
                if (!String.IsNullOrEmpty(itemTexts[i]))
                    items[i].Text = itemTexts[i];
            }
            ddl.ItemsSource = items;
            ddl.SelectByItem(selectedItem);

            ddl.AddEventHandlers(nameof(ddl.SelectionChanged), eventHandlers);
        }

        public static void LocalizeTextBlock(TextBlock tb)
        {
            tb.Text = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "TextBlock_" + tb.Name).Value;
        }

        public static void LocalizeRadioButton(RadioButton rb)
        {
            rb.Content = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "RadioButton_" + rb.Name).Value;
        }

        public static void LocalizeListBox(ListBox lb, int fromIndex = 0, int? toIndex = null, bool reverse = false)
        {
            var items = lb.Items.Cast<DdlItem>().ToArray();
            var selectedItems = lb.SelectedItems();
            lb.ItemsSource = null;
            var itemTexts = LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "ListBox_Items_" + lb.Name).Value.Split(",", false, StringSplitOptions.None);
            if (reverse)
                items = items.Reverse().ToArray();
            for (var i = fromIndex; i < (toIndex ?? items.Length); i++)
                if (!String.IsNullOrEmpty(itemTexts[i]))
                    items[i].Text = itemTexts[i];
            if (reverse)
                items = items.Reverse().ToArray();
            lb.ItemsSource = items;
            lb.SelectByItems(selectedItems);
        }

        public static string[] GetGeneralStatisticsLocalizedStrings()
        {
            return LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "GeneralStatistics").Value.Split(",");
        }

        public static string[] GetProfitByPeriodStatisticsLocalizedStrings()
        {
            return LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "ProfitByPeriodStatistics").Value.Split(",");
        }

        public static string GetGeneralLoaderLocalizedString()
        {
            return LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "Loader_General").Value;
        }

        public static string GetDisciplineTypeLocalizedString(DisciplineType? discipline)
        {
            return IsInitialized ? LocalizedStrings.Single(ls => ls.Key.AfterFirst("_") == "DisciplineType_" + discipline.EnumToString()).Value : null;
        }

        public static List<string[]> GetLoaderBetshootResponseParseLocalizedStrings()
        {
            return LocalizedStrings.Where(ls => ls.Key.AfterFirst("_").BeforeLastOrWhole("_") == "Loader_Betshoot_BetsResponse_Parse")
                .OrderBy(ls => ls.Key.AfterLast("_")).Select(ls => ls.Value.Split("|")).ToList();
        }
    }

    public enum Lang
    {
        Polish,
        English
    }
}
