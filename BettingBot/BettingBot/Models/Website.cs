using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BettingBot.Common;

namespace BettingBot.Models
{
    [Serializable]
    public class Website
    {
        public int Id { get; set; }
        public string Address { get; set; }

        public int? LoginId { get; set; }

        public virtual User Login { get; set; }
        public virtual ICollection<Tipster> Tipsters { get; set; }

        public Website(int id, string address, int? loginId)
        {
            Id = id;
            Address = address;
            LoginId = loginId;

            Tipsters = new List<Tipster>();
        }

        public Website()
        {
            Tipsters = new List<Tipster>();
        }

        public static void AddNewByAddress(LocalDbContext db, IEnumerable<string> addresses, int loginId)
        {
            var newAddresses = new List<string>();
            var requestedIds = new List<int>();
            foreach (var addr in addresses)
            {
                var ws = db.Websites.SingleOrDefault(w => w.Address == addr);
                if (ws != null)
                {
                    if (requestedIds.All(id => id != ws.Id))
                    {
                        requestedIds.Add(ws.Id);
                        ws.LoginId = loginId;
                        foreach (var t in db.Tipsters.ButSelf().AsEnumerable().Where(t => string.Equals(t.Link.UrlToDomain(), addr, StringComparison.CurrentCultureIgnoreCase)))
                            t.WebsiteId = ws.Id;
                    }
                }
                else
                    newAddresses.Add(addr);
            }

            var nextWId = db.Websites.Next(e => e.Id);
            foreach (var nAddr in newAddresses)
            {
                requestedIds.Add(nextWId);
                db.Websites.Add(new Website(nextWId, nAddr, loginId));
                foreach (var t in db.Tipsters.ButSelf().AsEnumerable().Where(t => string.Equals(t.Link.UrlToDomain(), nAddr, StringComparison.CurrentCultureIgnoreCase)))
                    t.WebsiteId = nextWId;
                nextWId++;
            }

            var oldIds = db.Websites.Where(ws => ws.LoginId == loginId).Select(ws => ws.Id);
            var idsToRemove = oldIds.Except(requestedIds).ToArray();
            db.Websites.RemoveByMany(ws => ws.Id, idsToRemove);

            db.Websites.RemoveUnused(db.Tipsters.ButSelf());
        }
    }
}
