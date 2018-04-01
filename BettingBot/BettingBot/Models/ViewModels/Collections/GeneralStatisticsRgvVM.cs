using System.Collections.Generic;
using BettingBot.Common.UtilityClasses;

namespace BettingBot.Models.ViewModels.Collections
{
    public class GeneralStatisticsRgvVM : CustomNameValueCollection
    {
        public GeneralStatisticsRgvVM(bool isReadOnly = false)
            : base(isReadOnly)
        {

        }

        public void Add(GeneralStatisticRgvVM gs)
        {
            _customNVC.Add(gs.Name, gs.Value);
        }

        public void Set(GeneralStatisticRgvVM gs)
        {
            _customNVC.Set(gs.Name, gs.Value);
        }

        public List<GeneralStatisticRgvVM> ToList()
        {
            var tmpList = new List<GeneralStatisticRgvVM>();
            for (var i = 0; i < _customNVC.Count; i++)
                tmpList.Add(new GeneralStatisticRgvVM(_customNVC.GetKey(i), _customNVC[i]));

            return tmpList;
        }
    }
}
