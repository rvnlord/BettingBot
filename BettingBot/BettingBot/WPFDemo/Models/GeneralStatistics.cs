using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreLinq;
using WPFDemo.Common.UtilityClasses;

namespace WPFDemo.Models
{
    public class GeneralStatistic
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public GeneralStatistic(string name, string value)
        {
            Name = name;
            Value = value;
        }

        //public static explicit operator GeneralStatistic(object obj)
        //{
            
        //}
    }

    public class GeneralStatistics : CustomNameValueCollection
    {
        public GeneralStatistics(bool isReadOnly = false)
            : base(isReadOnly)
        {

        }

        public void Add(GeneralStatistic gs)
        {
            _customNVC.Add(gs.Name, gs.Value);
        }

        public void Set(GeneralStatistic gs)
        {
            _customNVC.Set(gs.Name, gs.Value);
        }

        public List<GeneralStatistic> ToList()
        {
            var tmpList = new List<GeneralStatistic>();
            for (var i = 0; i < _customNVC.Count; i++)
                tmpList.Add(new GeneralStatistic(_customNVC.GetKey(i), _customNVC[i]));

            return tmpList;
        }
    }
}
