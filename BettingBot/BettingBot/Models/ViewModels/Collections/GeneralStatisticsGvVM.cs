using System.Collections.Generic;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Models.ViewModels.Collections
{
    public class GeneralStatisticsGvVM : CustomNameValueCollection
    {
        public GeneralStatisticsGvVM(bool isReadOnly = false)
            : base(isReadOnly)
        {

        }

        public void Add(GeneralStatisticGvVM gs)
        {
            _customNVC.Add(gs.Name, gs.Value);
        }

        public void Set(GeneralStatisticGvVM gs)
        {
            _customNVC.Set(gs.Name, gs.Value);
        }

        public List<GeneralStatisticGvVM> ToList()
        {
            var tmpList = new List<GeneralStatisticGvVM>();
            for (var i = 0; i < _customNVC.Count; i++)
                tmpList.Add(new GeneralStatisticGvVM(_customNVC.GetKey(i), _customNVC[i]));

            return tmpList;
        }
    }
}
