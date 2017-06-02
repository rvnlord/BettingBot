using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using DomainParser.Library;
using HtmlAgilityPack;
using MoreLinq;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using BettingBot.Common;
using BettingBot.Models.Interfaces;

namespace BettingBot.Models.DataLoaders
{
    public abstract class DataLoader : ISeleniumConnectable
    {
        protected Uri Url { get; set; }
        protected HtmlDocument Doc { get; set; }
        protected HtmlNode Root { get; set; }
        protected User User { get; set; }

        public SeleniumDriverManager Sdm { get; set; } = new SeleniumDriverManager();

        protected DataLoader(string url)
        {
            if (!url.StartsWithAny("http://", "https://"))
            {
                if (!url.StartsWith("www"))
                    url = "www" + url;
                url = "http://" + url;
            }
            
            Url = new Uri(url);
            Doc = new HtmlDocument();
            Doc.LoadHtml(new WebClient().DownloadString(url));
            Root = Doc.DocumentNode;
        }

        public virtual Tipster DownloadNewTipster(bool loadToDb = true)
        {
            var db = new LocalDbContext();
            var tipsterName = DownloadTipsterName();
            var domain = DownloadTipsterDomain();

            if (db.Tipsters.Any() && db.Tipsters.Any(t => t.Name + t.Website.Address == tipsterName + domain))
                return db.Tipsters.Include(t => t.Website).Single(t => t.Name + t.Website.Address == tipsterName + domain);

            var websiteId = db.Websites.SingleOrDefault(w => w.Address == domain)?.Id;
            if (websiteId == null)
            {
                var newWId = db.Websites.Next(w => w.Id);
                var website = new Website(newWId, domain, null);
                db.Websites.Add(website);
                db.SaveChanges();
                websiteId = newWId;
            }

            var tipster = new Tipster
            {
                Id = db.Tipsters.Next(v => v.Id),
                Name = tipsterName,
                Link = Url.ToString(),
                WebsiteId = websiteId
            };

            if (loadToDb)
            {
                db.Tipsters.AddOrUpdate(tipster);
                db.SaveChanges();
            }
            db.Entry(tipster).Reference(e => e.Website).Load();
            return tipster;
        }

        public abstract string DownloadTipsterName();
        public abstract string DownloadTipsterDomain();
        public abstract List<Bet> DownloadTips(bool loadToDb = true);
        public abstract void EnsureLogin();
        public abstract bool IsLogged();
        public abstract void Login();
    }
}
