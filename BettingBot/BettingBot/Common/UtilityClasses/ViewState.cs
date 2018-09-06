using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using BettingBot.Source.DbContext.Models;
using MahApps.Metro.Controls;

namespace BettingBot.Common.UtilityClasses
{
    public class ViewState
    {
        public ControlState[] ControlStates { get; set; }

        public ViewState(params ControlState[] controlStates)
        {
            ControlStates = controlStates;
        }

        public void Load(DbContext db, DbSet<DbOption> dbOptions)
        {
            foreach (var cs in ControlStates)
                cs.Load(dbOptions);
        }

        public void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveEachControlInstantly = false)
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

        public abstract void Load(DbSet<DbOption> dbOptions);
        public abstract void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false);
    }

    public class TextBoxState : ControlState
    {
        public TextBox TxtB { get; set; }

        public TextBoxState(string key, TextBox textBox) : base(key)
        {
            TxtB = textBox;
        }

        public override void Load(DbSet<DbOption> dbOptions)
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

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            const string defaultValue = "";
            if (TxtB.Tag != null && TxtB.Tag.ToString() == TxtB.Text)
                dbOptions.AddOrUpdate(new DbOption(Key, defaultValue));
            else
                dbOptions.AddOrUpdate(new DbOption(Key, TxtB.Text ?? defaultValue));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class NumState : ControlState
    {
        public NumericUpDown Num { get; set; }

        public NumState(string key, NumericUpDown num) : base(key)
        {
            Num = num;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Num.Value = dbOptions.Single(o => o.Key == Key).Value.ToDouble();
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, Num.Value.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class DdlState : ControlState
    {
        public Selector Ddl { get; set; }

        public DdlState(string key, Selector rddl) : base(key)
        {
            Ddl = rddl;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Ddl.SelectedItem = Ddl.Items.SourceCollection.Cast<DdlItem>().Single(i => i.Index == dbOptions.Single(o => o.Key == Key).Value.ToInt());
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, ((DdlItem)Ddl.SelectedItem).Index.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class MddlState : ControlState
    {
        public ListBox Mddl { get; set; }

        public MddlState(string key, ListBox mddl) : base(key)
        {
            Mddl = mddl;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                Mddl.UnselectAll();
                var val = dbOptions.Single(o => o.Key == Key).Value;
                var ids = string.IsNullOrWhiteSpace(val) ? new[] { -1 } : val.Split(',').Select(v => v.ToInt());
                foreach (var item in Mddl.Items.SourceCollection.Cast<DdlItem>().Where(i => ids.Any(id => id == i.Index)).ToList())
                    Mddl.SelectedItems.Add(item);
            }
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, string.Join(",", Mddl.SelectedItems.Cast<DdlItem>().Select(item => item.Index))));
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

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Cb.IsChecked = dbOptions.Single(o => o.Key == Key).Value.ToBool();
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, (Cb.IsChecked == true).ToInt().ToString()));
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
       
        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
                Rbs[dbOptions.Single(o => o.Key == Key).Value.ToInt()].IsChecked = true;
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, Rbs.Select((rb, i) =>
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
        public DatePicker Dp { get; set; }

        public DpState(string key, DatePicker dp) : base(key)
        {
            Dp = dp;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                var val = dbOptions.Single(o => o.Key == Key).Value;
                Dp.SelectedDate = string.IsNullOrWhiteSpace(val) ? (DateTime?)null : val.ToDateTime();
            }
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, Dp.SelectedDate != null ? Dp.SelectedDate.ToString() : null));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class GvSelectionState : ControlState
    {
        public DataGrid Gv { get; set; }
        public string ByProperty { get; set; }

        public GvSelectionState(string key, DataGrid gv, string byProperty = "Id") : base(key)
        {
            Gv = gv;
            ByProperty = byProperty;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (dbOptions.Any(o => o.Key == Key))
            {
                var val = dbOptions.Single(o => o.Key == Key).Value;
                var ids = val.Split(",").Select(v => v.ToInt()).ToArray();
                foreach (var item in Gv.Items)
                {
                    var itemId = item.GetType().GetProperty(ByProperty)?.GetValue(item, null);
                    if (itemId.ToInt().EqualsAny(ids))
                        Gv.SelectedItems.Add(item);
                }
            }
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, string.Join(",", Gv.SelectedItems.IColToArray().Select(i => i.GetType().GetProperty(ByProperty)?.GetValue(i, null)).OrderBy(id => id))));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class TilesOrderState : ControlState
    {
        public TilesMenu TilesMenu { get; set; }
        public IEnumerable<string> TabOrder { get; set; }

        public TilesOrderState(string key, TilesMenu tilesMenu, IEnumerable<string> tabOrder) : base(key)
        {
            TilesMenu = tilesMenu;
            TabOrder = tabOrder;
        }

        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (!dbOptions.Any(o => o.Key == Key)) return;
            var val = dbOptions.Single(o => o.Key == Key).Value;
            var valSplit = val.Split(",");
            if (valSplit.Any(x => !x.EqualsAny(TilesMenu.MenuTiles.Select(t => t.Name).ToArray()))) return;

            TilesMenu.Reorder(valSplit);
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            dbOptions.AddOrUpdate(new DbOption(Key, TabOrder.JoinAsString(",")));
            if (saveInstantly) db.SaveChanges();
        }
    }

    public class MenuExtendedState : ControlState
    {
        public TilesMenu TilesMenu { get; set; }

        public MenuExtendedState(string key, TilesMenu tilesMenu) : base(key)
        {
            TilesMenu = tilesMenu;
        }
        
        public override void Load(DbSet<DbOption> dbOptions)
        {
            if (!dbOptions.Any(o => o.Key == Key)) return;

            var val = dbOptions.Single(o => o.Key == Key).Value;
            var valSplit = val.Split(",");

            var isExtended = valSplit[0].ToBool();

            if (isExtended)
                TilesMenu.Extend();
            else
                TilesMenu.Shrink();
        }

        public override void Save(DbContext db, DbSet<DbOption> dbOptions, bool saveInstantly = false)
        {
            var isExtended = TilesMenu.IsFullSize;
            dbOptions.AddOrUpdate(new DbOption(Key, isExtended.ToString()));
            if (saveInstantly) db.SaveChanges();
        }
    }

}
