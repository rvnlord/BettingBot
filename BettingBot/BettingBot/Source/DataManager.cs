using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Linq;
using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext;
using BettingBot.Source.DbContext.Models;
using MoreLinq;

namespace BettingBot.Source
{
    public class DataManager : InformationSender
    {
        private LocalDbContext _db;

        public DataManager()
        {
            _db = new LocalDbContext();
        }

        public virtual bool AddTipsterIfNotExists(DbTipster tipster)
        {
            OnInformationSending("Dodawanie tipstera...");
            if (_db.Tipsters.AsEnumerable().Any(t => t.NameDomainEquals(tipster)))
            {
                OnInformationSending("Dodano Tipstera");
                return false;
            }

            var tipsterDomain = tipster.Link.UrlToDomain();
            var websiteId = _db.Websites.SingleOrDefault(w => w.Address == tipsterDomain)?.Id;
            if (websiteId == null)
            {
                var newWId = _db.Websites.Next(w => w.Id);
                var website = new DbWebsite(newWId, tipsterDomain, null);
                _db.Websites.Add(website);
                _db.SaveChanges();
                websiteId = newWId;
            }

            var tipsterToAdd = new DbTipster
            {
                Id = _db.Tipsters.Next(v => v.Id),
                Name = tipster.Name,
                Link = tipster.Link,
                WebsiteId = websiteId
            };

            _db.Tipsters.AddOrUpdate(tipsterToAdd);
            _db.SaveChanges();
            
            OnInformationSending("Dodano Tipstera");
            return true;
        }

        public virtual void UpsertBets(DbTipster tipster, List<DbBet> bets)
        {
            OnInformationSending("Zapisywanie zakładów...");
            
            if (!bets.Any())
            {
                OnInformationSending("Zapisano zakłady");
                return;
            }

            
            var tipsterId = _db.Tipsters.Single(t => tipster.Name == t.Name && tipster.Link == t.Link).Id;
            var nextBetId = _db.Bets.Next(b => b.Id);
            var nextPickId = _db.Picks.Next(p => p.Id);

            foreach (var bet in bets)
            {
                bet.Id = nextBetId++;
                bet.TipsterId = tipsterId;

                var pick = bet.Pick;
                
                var pickId = _db.Picks.SingleOrDefault(p => p.Choice == pick.Choice && p.Value == pick.Value)?.Id;
                if (pickId == null)
                {
                    pick.Id = nextPickId++;
                    _db.Picks.Add(pick);
                    _db.SaveChanges();
                    pickId = pick.Id;
                }
                bet.PickId = pickId.ToInt();
                bet.Pick = null; // nie dodawaj do bazy właściwości z innych tabel
            }
            
            var minDate = bets.Select(b => b.OriginalDate).Min(); // min d z nowych, zawiera wszystkie z tą datą
            _db.Bets.RemoveBy(b => b.OriginalDate >= minDate && b.TipsterId == tipsterId);
            _db.Bets.AddRange(bets.Distinct()); 
                // 1. jeśli obstawiono 2x ten sam mecz, ale tip jest ukryty to powstanie duplikat, możemy go odrzucić, bo kiedy poznamy zakład, to i tak obydwa mecze zostaną załadowane.
                // 2. bug hintwise mecze o tym samym czasie mogą być posortowane w dowolnej kolejności, czyli np na początku jednej strony i na końcu nastepnej mogą wystąpić te same, optymalnie poskakac po stronach pagera tam i z powrotem kilka razy

            _db.SaveChanges();
            
            OnInformationSending("Zapisano zakłady");
        }

        public virtual List<DbTipster> GetTipstersExceptDefault()
        {
            var me = DbTipster.Me();
            var dbTipsters = _db.Tipsters.Include(t => t.Website.Login).Where(t => t.Id != me.Id).OrderBy(t => t.Name).ToList();
            //foreach (var t in dbTipsters)
            //    db.Entry(t.Website).Reference(e => e.Login).Load(); // zamiast explicitly loading tutaj, eager loading u góry, zostawione dla odniesienia

            return dbTipsters;
        }

        public virtual List<DbTipster> GetTipstersById(params int[] ids)
        {
            if (!ids.Any())
                return new List<DbTipster>();
            
            return _db.Tipsters.Include(t => t.Website.Login).WhereByMany(t => t.Id, ids).ToList();
        }

        public virtual List<DbTipster> GetTipstersExceptDefaultById(params int[] ids)
        {
            if (!ids.Any())
                return new List<DbTipster>();

            var me = DbTipster.Me();
            
            return _db.Tipsters.Include(t => t.Website.Login).WhereByMany(t => t.Id, ids).Where(t => t.Id != me.Id).ToList();
        }

        public virtual void EnsureDefaultTipsterExists()
        {
            var me = DbTipster.Me();
            if (_db.Tipsters.Any(t => t.Id == me.Id))
                return;

            _db.Tipsters.Add(me);
            _db.SaveChanges();
        }

        public virtual DbLogin WebsiteLogin(DomainType domain)
        {
            var domainStr = domain.ConvertToString().ToLower();
            
            return _db.Websites.Include(w => w.Login).Single(w => w.Address == domainStr).Login;
        }

        public virtual List<DbBet> GetBets()
        {
            
            return _db.Bets.Include(b => b.Tipster)
                .Include(b => b.Match.Home)
                .Include(b => b.Match.Away)
                .Include(b => b.Pick)
                .AsEnumerable()
                .OrderBy(b => b.Match?.Date ?? b.OriginalDate).ThenBy(b => b.Match?.Home?.Name ?? b.OriginalHomeName).ToList();
        }

        public virtual DbLogin AddLogin(DbLogin dbLogin)
        {
            var nextLId = _db.Logins.Next(ent => ent.Id);
            var websites = dbLogin.Websites.Select(w => w.Address).ToList();

            dbLogin.Id = nextLId;
            dbLogin.Websites = new List<DbWebsite>();
            _db.Logins.Add(dbLogin);
            AddWebsiteByAddress(websites, nextLId);

            _db.SaveChanges();

            return _db.Logins.Single(l => l.Id == nextLId);
        }

        public virtual DbLogin UpdateLogin(DbLogin dbLogin)
        {
            var websites = dbLogin.Websites.Select(w => w.Address).ToList();
            var dbLoginToUpdate = _db.Logins.Single(l => l.Id == dbLogin.Id);

            dbLoginToUpdate.Name = dbLogin.Name;
            dbLoginToUpdate.Password = dbLogin.Password;
            AddWebsiteByAddress(websites, dbLoginToUpdate.Id);

            _db.SaveChanges();
            return _db.Logins.Single(l => l.Id == dbLoginToUpdate.Id);
        }

        public virtual List<DbLogin> GetLogins()
        {
            return _db.Logins.OrderBy(l => l.Name).ToList();
        }

        private void AddWebsiteByAddress(IEnumerable<string> addresses, int loginId)
        {
            var newAddresses = new List<string>();
            var requestedIds = new List<int>();
            foreach (var addr in addresses)
            {
                var ws = _db.Websites.SingleOrDefault(w => w.Address == addr);
                if (ws != null)
                {
                    if (requestedIds.All(id => id != ws.Id))
                    {
                        requestedIds.Add(ws.Id);
                        ws.LoginId = loginId;
                        foreach (var t in _db.Tipsters.ButSelf().AsEnumerable().Where(t => t.Link.UrlToDomain().EqIgnoreCase(addr)))
                            t.WebsiteId = ws.Id;
                    }
                }
                else
                    newAddresses.Add(addr);
            }

            var nextWId = _db.Websites.Next(e => e.Id);
            foreach (var nAddr in newAddresses)
            {
                requestedIds.Add(nextWId);
                _db.Websites.Add(new DbWebsite(nextWId, nAddr, loginId));
                foreach (var t in _db.Tipsters.ButSelf().AsEnumerable().Where(t => string.Equals(t.Link.UrlToDomain(), nAddr, StringComparison.CurrentCultureIgnoreCase)))
                    t.WebsiteId = nextWId;
                nextWId++;
            }

            var oldIds = _db.Websites.Where(ws => ws.LoginId == loginId).Select(ws => ws.Id).ToArray();
            var idsToRemoveAssoc = oldIds.Except(requestedIds).ToArray();

            var websitesToUnassociate = _db.Websites.Where(w => idsToRemoveAssoc.Any(id => id == w.Id));
            foreach (var w in websitesToUnassociate)
                w.LoginId = null;

            _db.SaveChanges(); // żeby RemoveUnused miało aktualne dane

            _db.Websites.RemoveUnused(_db.Tipsters.ButSelf());
        }

        public void RemoveLoginsById(params int[] ids)
        {
            foreach (var w in _db.Websites)
                if (ids.Any(id => id == w.LoginId))
                    w.LoginId = null;
            _db.Logins.RemoveByMany(b => b.Id, ids);
            _db.SaveChanges();
            _db.Websites.RemoveUnused(_db.Tipsters.ButSelf());
            _db.SaveChanges();
        }

        public void RemoveTipstersById(params int[] ids)
        {
            _db.Bets.RemoveByMany(b => b.TipsterId, ids);
            _db.Tipsters.RemoveByMany(t => t.Id, ids);
            _db.SaveChanges();
        }

        public void RemoveUnusedWebsites()
        {
            _db.Websites.RemoveUnused(_db.Tipsters.ButSelf());
        }
    }
}
