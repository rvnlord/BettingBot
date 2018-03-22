using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Telerik.Windows.Controls;
using BettingBot.Common;
using BettingBot.Common.UtilityClasses;
using MahApps.Metro.IconPacks;
using Tile = MahApps.Metro.Controls.Tile;

namespace BettingBot.Models
{
    public class ViewState
    {
        public ControlState[] ControlStates { get; set; }

        public ViewState(params ControlState[] controlStates)
        {
            ControlStates = controlStates;
        }

        public void Load(DbContext db, DbSet<Option> dbOptions)
        {
            foreach (var cs in ControlStates)
                cs.Load(dbOptions);
        }

        public void Save(DbContext db, DbSet<Option> dbOptions, bool saveEachControlInstantly = false)
        {
            foreach (var cs in ControlStates)
                cs.Save(db, dbOptions, saveEachControlInstantly);
            if (!saveEachControlInstantly) db.SaveChanges();
        }
    }

    public abstract class ControlState
    {
        public string Key { get; set; }

        protected ControlState(string key)
        {
            Key = key;
        }

        public abstract void Load(DbSet<Option> dbOptions);
        public abstract void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false);
    }

    public class TextBoxState : ControlState
    {
        public TextBox TxtB { get; set; }

        public TextBoxState(string key, TextBox textBox) : base(key)
        {
            TxtB = textBox;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                var val = dbOptions.Single(o => o.Key == Key).Value;
                if (!string.IsNullOrEmpty(val) && (TxtB.Tag == null || (TxtB.Tag != null && val != TxtB.Tag.ToString())))
                {
                    TxtB.ClearValue(true);
                    TxtB.Text = val;
                }
            }
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            const string defaultValue = "";
            if (TxtB.Tag != null && TxtB.Tag.ToString() == TxtB.Text)
                dbOptions.AddOrUpdate(new Option(Key, defaultValue));
            else
                dbOptions.AddOrUpdate(new Option(Key, TxtB.Text ?? defaultValue));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class RnumState : ControlState
    {
        public RadRangeBase Rnum { get; set; }

        public RnumState(string key, RadRangeBase rnum) : base(key)
        {
            Rnum = rnum;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Rnum.Value = dbOptions.Single(o => o.Key == Key).Value.ToDouble();
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            const int defaultValue = 0;
            dbOptions.AddOrUpdate(new Option(Key, Rnum.Value?.ToString() ?? defaultValue.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class RddlState : ControlState
    {
        public Selector Rddl { get; set; }

        public RddlState(string key, Selector rddl) : base(key)
        {
            Rddl = rddl;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Rddl.SelectedItem = Rddl.Items.SourceCollection.Cast<DdlItem>().Single(i => i.Index == Convert.ToInt32(dbOptions.Single(o => o.Key == Key).Value));

        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, ((DdlItem)Rddl.SelectedItem).Index.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class MddlState : ControlState
    {
        public RadListBox Mddl { get; set; }

        public MddlState(string key, RadListBox mddl) : base(key)
        {
            Mddl = mddl;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                Mddl.UnselectAll();
                var val = dbOptions.Single(o => o.Key == Key).Value;
                var ids = string.IsNullOrWhiteSpace(val) ? new[] { -1 } : val.Split(',').Select(v => Convert.ToInt32(v));
                foreach (var item in Mddl.Items.SourceCollection.Cast<DdlItem>().Where(i => ids.Any(id => id == i.Index)).ToList())
                    Mddl.SelectedItems.Add(item);
            }
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, string.Join(",", Mddl.SelectedItems.Cast<DdlItem>().Select(item => item.Index))));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class CbState : ControlState
    {
        public ToggleButton Cb { get; set; }

        public CbState(string key, ToggleButton cb) : base(key)
        {
            Cb = cb;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Cb.IsChecked = Convert.ToBoolean(Convert.ToInt32(dbOptions.Single(o => o.Key == Key).Value));
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, Convert.ToInt32(Cb.IsChecked == true).ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class RbsState : ControlState
    {
        public IList<RadioButton> Rbs { get; set; }

        public RbsState(string key, IList<RadioButton> rbs) : base(key)
        {
            Rbs = rbs;
        }
       
        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Rbs[Convert.ToInt32(dbOptions.Single(o => o.Key == Key).Value)].IsChecked = true;
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, Rbs.Select((rb, i) =>
                new
                {
                    i,
                    rb
                }).Single(el => el.rb.IsChecked == true).i.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class DpState : ControlState
    {
        public RadDatePicker Dp { get; set; }

        public DpState(string key, RadDatePicker dp) : base(key)
        {
            Dp = dp;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                var val = dbOptions.Single(o => o.Key == Key).Value;
                Dp.SelectedDate = string.IsNullOrWhiteSpace(val) ? (DateTime?)null : Convert.ToDateTime(val);
            }
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, Dp.SelectedDate != null ? Dp.SelectedDate.ToString() : null));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class RgvSelectionState : ControlState
    {
        public RadGridView Rgv { get; set; }
        public string ByProperty { get; set; }

        public RgvSelectionState(string key, RadGridView rgv, string byProperty = "Id") : base(key)
        {
            Rgv = rgv;
            ByProperty = byProperty;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                var val = dbOptions.Single(o => o.Key == Key).Value;
                var ids = val.Split(",").Select(v => Convert.ToInt32(v)).ToArray();
                foreach (var item in Rgv.Items)
                {
                    var itemId = (int) item.GetType().GetProperty(ByProperty).GetValue(item, null);
                    if (itemId.EqualsAny(ids))
                        Rgv.SelectedItems.Add(item);
                }
            }
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, string.Join(",", Rgv.SelectedItems.Select(i => i.GetType().GetProperty(ByProperty).GetValue(i, null)).OrderBy(id => id))));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class TilesOrderState : ControlState
    {
        public List<Tile> Tiles { get; set; }
        public List<string> TabOrder { get; set; }

        public TilesOrderState(string key, List<Tile> tiles, List<string> tabOrder) : base(key)
        {
            Tiles = tiles;
            TabOrder = tabOrder;
        }

        public override void Load(DbSet<Option> dbOptions)
        {
            if (!dbOptions.Any(o => o.Key == Key)) return;
            var val = dbOptions.Single(o => o.Key == Key).Value;
            var valSplit = val.Split(",");
            if (valSplit.Any(x => !x.EqualsAny(Tiles.Select(t => t.Name).ToArray()))) return;

            TabOrder.ReplaceAll(valSplit); // nie rebinduj listy po zepsułoby to odniesienia
            var container = Tiles.First().FindLogicalAncestor<Panel>();
            var utilTiles = container.FindLogicalDescendants<Tile>().Except(Tiles).ToList();
            var orderedTiles = Tiles.OrderByWith(t => t.Name, TabOrder).ToList();
            container.Children.ReplaceAll(orderedTiles);
            container.Children.AddRange(utilTiles);
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new Option(Key, TabOrder.JoinAsString(",")));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class MenuExtendedState : ControlState
    {
        public StackPanel SpMenu { get; set; }
        public int DefaultWidth { get; }
        public int ResizeValue { get; }

        public MenuExtendedState(string key, StackPanel spMenu, int defaultWidth, int resizeVAlue) : base(key)
        {
            SpMenu = spMenu;
            DefaultWidth = defaultWidth;
            ResizeValue = resizeVAlue;
        }
        
        public override void Load(DbSet<Option> dbOptions)
        {
            if (!dbOptions.Any(o => o.Key == Key)) return;

            var val = dbOptions.Single(o => o.Key == Key).Value;
            var valSplit = val.Split(",");

            var isExtended = valSplit[0].ToBool();
            var defaultWidth = valSplit[1].ToInt();
            var resizeValue = valSplit[2].ToInt();

            var tiles = SpMenu.FindLogicalDescendants<Tile>();
            var resizeTile = SpMenu.FindLogicalDescendants<Tile>().Single(tl => tl.Name.StartsWith("tlResize"));
            foreach (var tile in tiles.Except(resizeTile))
                tile.Width = isExtended ? defaultWidth + resizeValue : defaultWidth;

            var icon = resizeTile.FindLogicalDescendants<PackIconModern>().Single();
            icon.Kind = isExtended ? PackIconModernKind.ArrowLeft : PackIconModernKind.DoorLeave;
        }

        public override void Save(DbContext db, DbSet<Option> dbOptions, bool saveInstantly = false)
        {
            var isExtended = SpMenu.FindLogicalDescendants<Tile>().First().ActualWidth >= DefaultWidth + ResizeValue;
            dbOptions.AddOrUpdate(new Option(Key, new[] { isExtended.ToString(), DefaultWidth.ToString(), ResizeValue.ToString() }.JoinAsString(",")));
            if (saveInstantly) db.SaveChanges();
        }
    }

}
