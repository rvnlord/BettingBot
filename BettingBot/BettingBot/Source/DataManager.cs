using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BettingBot.Common;
using BettingBot.Source.Converters;
using BettingBot.Source.DbContext;
using BettingBot.Source.DbContext.Models;
using MoreLinq;

namespace BettingBot.Source
{
    public class DataManager : InformationSender
    {
        private static readonly object _lock = new object();
        private readonly LocalDbContext _db;

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

        public virtual void UpsertBets(DbTipster tipster, List<DbBet> bets, bool addOnly = false, bool dontRemoveTwoDaysPeriod = false)
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

            if (!addOnly)
            {
                var minDate = bets.Select(b => b.OriginalDate).Min(); // min d z nowych, zawiera wszystkie z tą datą
                _db.Bets.RemoveBy(b => b.OriginalDate >= minDate && b.TipsterId == tipsterId);

                if (!dontRemoveTwoDaysPeriod)
                {
                    var plusOneDay = minDate.AddDays(1);
                    var minusOneDay = minDate.AddDays(-1);
                    var twoDaysBets = _db.Bets.Where(b => b.OriginalDate < plusOneDay && b.OriginalDate > minusOneDay && b.TipsterId == tipsterId).ToList(); // bez between, bo musi być przetłumaczalne na Linq to Entities
                    var sameMatchesInTwoDays = twoDaysBets.Where(b => twoDaysBets.Any(tdb => tdb.EqualsWoOriginalDate(b))).ToList();
                    _db.Bets.RemoveRange(sameMatchesInTwoDays); // fix dla niespodziewanej zmiany strefy czasowej przez hintwise
                } // TODO: czy to na pewno działa poprawnie dla hintwise
            }

            _db.Bets.AddRange(bets.Distinct());
                // 1. jeśli obstawiono 2x ten sam mecz, ale tip jest ukryty to powstanie duplikat, możemy go odrzucić, bo kiedy poznamy zakład, to i tak obydwa mecze zostaną załadowane.
                // 2. bug hintwise mecze o tym samym czasie mogą być posortowane w dowolnej kolejności, czyli np na początku jednej strony i na końcu nastepnej mogą wystąpić te same, optymalnie poskakac po stronach pagera tam i z powrotem kilka razy
            _db.SaveChanges();
            
            OnInformationSending("Zapisano zakłady");
        }

        public void UpsertBet(DbTipster tipster, DbBet bet, bool addOnly = false)
        {
            UpsertBets(tipster, new[] { bet }.ToList(), addOnly);
        }

        public void UpsertMyBets(List<DbBet> bets, bool addOnly = false)
        {
            UpsertBets(DbTipster.Me(), bets, addOnly);
        }

        public void UpsertMyBet(DbBet bet, bool addOnly = false)
        {
            UpsertBet(DbTipster.Me(), bet, addOnly);
        }

        public void AddBets(DbTipster tipster, List<DbBet> bets)
        {
            UpsertBets(tipster, bets, true);
        }

        public void AddMyBets(List<DbBet> bets)
        {
            UpsertMyBets(bets, true);
        }

        public void AddBet(DbTipster tipster, DbBet bet)
        {
            UpsertBet(tipster, bet, true);
        }

        public void AddMyBet(DbBet bet)
        {
            UpsertMyBet(bet, true);
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
                .Include(b => b.Match.League.Discipline)
                .AsEnumerable()
                .OrderBy(b => b.Match?.Date ?? b.OriginalDate).ThenBy(b => b.Match?.Home?.Name ?? b.OriginalHomeName).ToList();
        }

        public virtual DbBet GetBetById(int id)
        {
            return _db.Bets.Include(b => b.Tipster)
                .Include(b => b.Match.Home)
                .Include(b => b.Match.Away)
                .Include(b => b.Pick)
                .Include(b => b.Match.League.Discipline)
                .Single(b => b.Id == id);
        }

        public virtual List<DbMatch> GetMatches()
        {
            return _db.Matches.Include(m => m.League.Discipline)
                .Include(m => m.Home)
                .Include(m => m.Away)
                .AsEnumerable()
                .OrderBy(m => m.Date).ThenBy(m => m.Home.Name).ToList();
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
            return _db.Logins.Include(l => l.Websites).OrderBy(l => l.Name).ToList();
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

        public DateTime? GetOldestUnassociatedMatchDate()
        {
            if (!_db.Bets.Any())
                return null;
            return _db.Bets.Where(b => b.MatchId == null).Select(b => b.OriginalDate).Min();
        }

        public DateTime? GetNewestUnassociatedMatchDate()
        {
            if (!_db.Bets.Any())
                return null;
            return _db.Bets.Where(b => b.MatchId == null).Select(b => b.OriginalDate).Max();
        }

        public void UpsertMatches(List<DbMatch> newMatches)
        {
            OnInformationSending($"Aktualizowanie meczy {_db.Matches.Count()} (dodawane: {newMatches.Count})");

            if (!newMatches.Any())
            {
                OnInformationSending("Zaaktualizowano mecze");
                return;
            }

            var matchesToAdd = newMatches.Distinct().Where(m => m.HomeId != m.AwayId).ToArray();
                // fix dla Unique (PRIMARY KEY) Constraint failed, ponieważ baza Football-Data może zwrócić tymczasowy mecz
                // o takim samym id jak istniejący gdzie obie drużyny są takie same (null)
            var newMatchIds = matchesToAdd.Select(m => m.Id).ToArray();
            var matchesToRemoveOriginal = _db.Matches.Include(m => m.Bets).Where(m => newMatchIds.Contains(m.Id)).ToList();
            var matchesToRemove = matchesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var matches = matchesToRemove.Union(matchesToAdd.Except(matchesToRemove)).ToList();

            var betsToAdd = matchesToAdd.SelectMany(m => m.Bets).Distinct().ToArray();
            var betsToRemoveOriginal = matchesToRemoveOriginal.SelectMany(m => m.Bets).Distinct().ToList();
            var betsToRemove = betsToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var bets = betsToRemove.Union(betsToAdd.Except(betsToRemove)).ToArray();
            bets.ForEach(b => b.TriedAssociateWithMatch = false.ToInt());

            var matchesDistinctById = matches.DistinctBy(m => m.Id).ToArray();
            if (matches.Count != matchesDistinctById.Length)
            {
                var sameIds = matches.Except(matchesDistinctById).Select(m => m.Id).ToArray();
                foreach (var id in sameIds)
                {
                    var duplicatesById = matches.Where(m => m.Id == id).ToArray();
                    var correctMatch = duplicatesById.MaxBy(m => m.Date);
                    var incorrectmatches = duplicatesById.ExceptBy(correctMatch, m => m.Date).ToArray();
                    matches.RemoveRange(incorrectmatches);
                }
            }
                // fix dla Unique (PRIMARY KEY) Constraint failed, ponieważ baza Football-Data potrafi zwrócić dwa mecze o tym samym Id
                // jeden jest już w bazie z takim samym id jak dodawany
                // matches.Where(m => matches.Any(om => om.Id == m.Id && m != om)).ToArray()
                // [0]: {22-04-2018 13:00 107 () - 450 () - 456 ()
                // [1]: {23-04-2018 18:45 107 () - 450 () - 456 ()

            matches = matches.Where(m => m.HomeId != m.AwayId).ToList();
                // fix dla Unique (PRIMARY KEY) Constraint failed, ponieważ baza Football-Data potrafi zwrócić sztuczny mecz gdzie obie drużyny
                // mają takie same sztuczne id w dwóch różnych godzinach

            _db.Bets.RemoveRange(betsToRemoveOriginal);
            _db.Matches.RemoveRange(matchesToRemoveOriginal);
            _db.SaveChanges();
            
            foreach (var m in matches)
                m.Bets = bets.Where(b => b.MatchId == m.Id).ToList();
            _db.Matches.AddRange(matches);
            _db.SaveChanges();

            OnInformationSending($"Zaaktualizowano mecze ({_db.Matches.Count()})");
        }

        public void UpsertLeagues(List<DbLeague> newLeagues)
        {
            OnInformationSending("Aktualizowanie lig...");

            if (!newLeagues.Any())
            {
                OnInformationSending("Zaaktualizowano ligi...");
                return;
            }

            var leaguesToAdd = newLeagues.Distinct().ToArray();
            var newLeagueIds = newLeagues.Select(l => l.Id).ToArray();
            var leaguesToRemoveOriginal = _db.Leagues.Where(l => newLeagueIds.Contains(l.Id)).ToArray(); // uq: l.Name, l.Season, l.DisciplineId
            var leaguesToRemove = leaguesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var leagues = leaguesToRemove.Union(leaguesToAdd.Except(leaguesToRemove)).ToArray();

            var leagueAlternateNamesToAdd = leaguesToAdd.SelectMany(l => l.LeagueAlternateNames).Distinct().ToArray();
            var leagueAlternateNamesToRemoveOriginal = leaguesToRemoveOriginal.SelectMany(l => l.LeagueAlternateNames).Distinct().ToArray(); // uq: id, altname
            var leagueAlternateNamesToRemove = leagueAlternateNamesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var leagueAlternatenames = leagueAlternateNamesToRemove.Union(leagueAlternateNamesToAdd.Except(leagueAlternateNamesToRemove)).ToArray();

            var disciplinesToAdd = leaguesToAdd.Select(l => l.Discipline).Where(d => d != null).Distinct().ToArray();
            var disciplinesToRemoveOriginal = leaguesToRemoveOriginal.Select(l => l.Discipline).Where(d => d != null).Distinct().ToArray(); // uq: d.Id, d.Name
            var disciplinesToRemove = disciplinesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var disciplines = disciplinesToRemove.Union(disciplinesToAdd.Except(disciplinesToRemove)).ToArray();
            
            _db.Disciplines.RemoveRange(disciplinesToRemoveOriginal);
            _db.LeagueAlternateNames.RemoveRange(leagueAlternateNamesToRemoveOriginal);
            _db.Leagues.RemoveRange(leaguesToRemoveOriginal);
            _db.SaveChanges();

            foreach (var l in leagues)
            {
                l.Discipline = disciplines.Single(d => d.Id == l.DisciplineId);
                l.LeagueAlternateNames = leagueAlternatenames.Where(la => la.LeagueId == l.Id).ToList();
            }
            _db.Leagues.AddRange(leagues);
            _db.SaveChanges();

            OnInformationSending("Zaaktualizowano ligi...");
        }

        public void UpsertTeams(List<DbTeam> newTeams)
        {
            OnInformationSending("Aktualizowanie zespołów...");

            if (!newTeams.Any())
            {
                OnInformationSending("Zaaktualizowano zespoły...");
                return;
            }

            var teamsToAdd = newTeams.Distinct().ToArray();
            var teamsToAddids = teamsToAdd.Select(m => m.Id).ToArray(); // unique id, name
            var teamsToRemoveOriginal = _db.Teams.Include(t => t.HomeMatches).Include(t => t.AwayMatches).Where(t => teamsToAddids.Contains(t.Id)).ToArray();
            var teamsToRemove = teamsToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var teams = teamsToRemove.Union(teamsToAdd.Except(teamsToRemove)).ToArray();

            var teamAlternateNamesToAdd = teamsToAdd.SelectMany(t => t.TeamAlternateNames).Distinct().ToArray();
            var teamAlternateNamesToRemoveOriginal = teamsToRemoveOriginal.SelectMany(t => t.TeamAlternateNames).Distinct().ToArray(); // uq: altname
            var teamAlternateNamesToRemove = teamAlternateNamesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var teamAlternatenames = teamAlternateNamesToRemove.Union(teamAlternateNamesToAdd.Except(teamAlternateNamesToRemove)).ToArray();
            
            var matchesToAdd = teamsToAdd.SelectMany(t => t.HomeMatches).Union(teamsToRemove.SelectMany(t => t.AwayMatches)).Distinct().ToList();
            var matchesToRemoveOriginal = teamsToRemoveOriginal.SelectMany(t => t.HomeMatches).Union(teamsToRemoveOriginal.SelectMany(t => t.AwayMatches)).Distinct().ToList();
            var matchesToRemove = matchesToRemoveOriginal.CopyCollectionWithoutNavigationProperties();
            var matches = matchesToRemove.Union(matchesToAdd.Except(matchesToRemove)).ToArray();
            
            _db.TeamAlternateNames.RemoveRange(teamAlternateNamesToRemoveOriginal);
            _db.Matches.RemoveRange(matchesToRemoveOriginal);
            _db.Teams.RemoveRange(teamsToRemoveOriginal);
            _db.SaveChanges();

            var teamAlternateNamesRemainingInDb = _db.TeamAlternateNames.Select(ta => ta.AlternateName).ToArray();

            foreach (var t in teams)
            {
                t.HomeMatches = matches.Where(m => m.HomeId == t.Id).ToList();
                t.AwayMatches = matches.Where(m => m.AwayId == t.Id).ToList();
                t.TeamAlternateNames = teamAlternatenames.Where(ta => ta.TeamId == t.Id && !ta.AlternateName.EqAnyIgnoreCase(teamAlternateNamesRemainingInDb)).ToList(); // fix: np Ac Ajaccio (510) i GFC Ajaccio (555) mają ten sam skrót
            }
            _db.Teams.AddRange(teams);
            _db.SaveChanges();
            
            OnInformationSending("Zaaktualizowano zespoły...");
        }
        
        public List<DbLeague> GetLeaguesByYear(int year)
        {
            return !_db.Leagues.Any()
                ? new List<DbLeague>()
                : _db.Leagues.Where(l => l.Season == year).ToList();
        }

        public List<DbLeague> GetLeaguesBetweenYears(int yearFrom, int yearTo)
        {
            return !_db.Leagues.Any()
                ? new List<DbLeague>()
                : _db.Leagues
                    .Include(l => l.Matches)
                    .Where(l => l.Season >= yearFrom && l.Season <= yearTo).ToList();
        }
        
        public int[] GetLeagueIdsBetweenYears(int yearFrom, int yearTo)
        {
            return !_db.Leagues.Any()
                ? new int[0] 
                : _db.Leagues.Where(l => l.Season >= yearFrom && l.Season <= yearTo).Select(l => l.Id).ToArray();
        }

        public int[] GetLeagueIds()
        {
            return !_db.Leagues.Any()
                ? new int[0]
                : _db.Leagues.Select(l => l.Id).ToArray();
        }

        public DateTime? GetOldestUnfinishedMatchDate()
        {
            if (!_db.Matches.Any())
                return null;
            var finished = MatchStatus.Finished.ToInt();
            return _db.Matches.Where(m => m.Status != finished).Select(b => (DateTime?) b.Date).Min() // jesli null to wszystkie są skończone
                ?? _db.Matches.Where(m => m.Status == finished).Select(b => b.Date).Max(); // wtedy weż datę ostatniego ukończonego
        }

        public List<int> AssociateBetsWithFootballDataMatchesAutomatically(bool ignoreBetsTriedToAssociateBefore)
        {
            OnInformationSending("Powiązywanie zakładów z meczami (Football-Data)...");
            
            var unassociatedIds = new List<int>();

            List<DbBet> dbBets;

            if (ignoreBetsTriedToAssociateBefore)
            {
                dbBets = _db.Bets.Include(b => b.Match.League.Discipline).Where(b => b.MatchId == null
                    && (b.TriedAssociateWithMatch == null || b.TriedAssociateWithMatch == 0)
                    && (b.Match == null || b.Match.League == null || b.Match.League.DisciplineId == null
                        || b.Match.League.DisciplineId == 0)).ToList();
            }
            else
            {
                dbBets = _db.Bets.Include(b => b.Match.League.Discipline).Where(b => b.MatchId == null
                    && (b.Match == null || b.Match.League == null || b.Match.League.DisciplineId == null 
                        || b.Match.League.DisciplineId == 0)).ToList();
            }
            
            var dbMatches = _db.Matches.Include(m => m.Home.TeamAlternateNames)
                .Include(m => m.Away.TeamAlternateNames)
                .Include(m => m.League).ToList();
            var dbAllAltNames = _db.TeamAlternateNames.Select(ta => ta.AlternateName).ToList();

            var i = 0;
            var bCount = dbBets.Count;
            var assosCount = 0;
            var unassCount = 0;

            var alternateNames = new List<DbTeamAlternateName>();

            foreach (var b in dbBets)
            {
                OnInformationSending($"Powiązywanie zakładów z meczami ({++i} z {bCount}: p: {assosCount}, n: {unassCount})...");
                var matchesWithMatchingName = dbMatches.Where(m =>
                    m.Home.Name.EqIgnoreCase(b.OriginalHomeName)
                    || b.OriginalHomeName.EqAnyIgnoreCase(m.Home.TeamAlternateNames.Select(ta => ta.AlternateName))
                    || m.Away.Name.EqIgnoreCase(b.OriginalAwayName)
                    || b.OriginalAwayName.EqAnyIgnoreCase(m.Away.TeamAlternateNames.Select(ta => ta.AlternateName))).ToList();

                var matchesWithMatchingDates = matchesWithMatchingName.Where(m => b.OriginalDate.Between(m.Date.SubtractDays(1), m.Date.AddDays(1)));

                var match = matchesWithMatchingDates.OrderBy(m => m.Date).ThenBy(m => m.League.Season).FirstOrDefault(); // fix: football-data bug - pojedynczy mecz należy do dwóch lig np: Ligue 2 17/18 i 16/17

                if (match != null)
                {
                    b.MatchId = match.Id;

                    if (!b.OriginalHomeName.EqIgnoreCase(b.OriginalAwayName))
                    {
                        if (!b.OriginalHomeName.EqAnyIgnoreCase(match.Home.TeamAlternateNames.Select(ta => ta.AlternateName))
                            && !b.OriginalHomeName.EqAnyIgnoreCase(dbAllAltNames))
                        {
                            alternateNames.Add(new DbTeamAlternateName
                            {
                                TeamId = match.HomeId,
                                AlternateName = b.OriginalHomeName
                            });
                        }
                        if (!b.OriginalAwayName.EqAnyIgnoreCase(match.Away.TeamAlternateNames.Select(ta => ta.AlternateName))
                            && !b.OriginalAwayName.EqAnyIgnoreCase(dbAllAltNames))
                        {
                            alternateNames.Add(new DbTeamAlternateName
                            {
                                TeamId = match.AwayId,
                                AlternateName = b.OriginalAwayName
                            });
                        }
                    }

                    assosCount++;

                    OnInformationSending("Zapiosywanie powiązań zakładów z meczami...");

                    if (assosCount % 100 == 0)
                    {
                        _db.TeamAlternateNames.AddRange(alternateNames.Distinct());
                        _db.SaveChanges();
                        dbAllAltNames.AddRange(alternateNames.Distinct().Select(ta => ta.AlternateName));
                        alternateNames.Clear();
                    }

                    OnInformationSending("Zapiosano powiązania zakładów z meczami");
                }
                else // if (match == null)
                {
                    unassociatedIds.Add(b.Id);
                    unassCount++;
                }

                b.TriedAssociateWithMatch = true.ToInt();
            }

            OnInformationSending("Zapiosywanie powiązań zakładów z meczami...");
            
            _db.TeamAlternateNames.AddRange(alternateNames.Distinct());
            _db.SaveChanges();

            OnInformationSending("Powiązano zakłady z meczami (Football-Data)");

            return unassociatedIds;
        }

        public void AssociateBetWithMatchById(int betId, int matchId)
        {
            _db.Bets.Single(b => b.Id == betId).MatchId = matchId;
            _db.SaveChanges();
        }

        public void RemoveMatchIdFromBetById(int betId)
        {
            _db.Bets.Single(b => b.Id == betId).MatchId = null;
            _db.SaveChanges();
        }

        public DbLogin GetLoginByWebsite(string url)
        {
            var domain = url.UrlToDomain().ToLower();
            return _db.Websites.Include(w => w.Login).SingleOrDefault(w => w.Address.ToLower() == domain)?.Login;
        }

        //public void AddBet(DbBet bet)
        //{
        //    if (bet.Tipster == null)
        //        bet.TipsterId = -1;
        //    bet.Id = _db.Bets.Next(b => b.Id);

        //    var pick = bet.Pick;

        //    var pickId = _db.Picks.SingleOrDefault(p => p.Choice == pick.Choice && p.Value == pick.Value)?.Id;
        //    if (pickId == null)
        //    {
        //        pick.Id = _db.Picks.Next(p => p.Id);
        //        _db.Picks.Add(pick);
        //        _db.SaveChanges();
        //        pickId = pick.Id;
        //    }
        //    bet.PickId = pickId.ToInt();
        //    bet.Pick = null;

        //    _db.Bets.Add(bet);
        //    _db.SaveChanges();
        //}

        public DbBet[] GetMyBets()
        {
            return _db.Bets.Where(b => b.TipsterId == -1).Include(b => b.Pick).ToArray();
        }

        //private void WithDisabledConstraints(Action action)
        //{
        //    _db.Database.Connection.Open();

        //    _db.Database.ExecuteSqlCommand("PRAGMA foreign_keys=OFF;");
        //    _db.Database.ExecuteSqlCommand("PRAGMA ignore_check_constraints=true;");

        //    action();

        //    _db.Database.ExecuteSqlCommand("PRAGMA foreign_keys=ON;");
        //    _db.Database.ExecuteSqlCommand("PRAGMA ignore_check_constraints=false;");

        //    _db.Database.Connection.Close();
        //}

        //private void SaveChangesWithDisabledConstraints()
        //{
        //    WithDisabledConstraints(() => _db.SaveChanges());
        //}

        public List<DbLocalizedString> GetLocalizedStrings(Lang language)
        {
            var strLang = $"{language.EnumToString().Take(3).ToUpper()}_";
            var db = new LocalDbContext();
            return db.LocalizedStrings.Where(ls => ls.Key.StartsWith(strLang)).ToList();
        }
    }
}
