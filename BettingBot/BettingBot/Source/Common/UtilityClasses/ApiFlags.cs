using System;
using System.Collections;
using System.Collections.Generic;

namespace BettingBot.Source.Common.UtilityClasses
{
    public class ApiFlags : IEnumerable<ApiFlag>
    {
        private readonly Dictionary<ApiFlagType, ApiFlag> _dictFlags = new Dictionary<ApiFlagType, ApiFlag>();
        private readonly List<ApiFlagType> _paramsOrder = new List<ApiFlagType>();

        public ApiFlag this[ApiFlagType flagType] => _dictFlags[flagType];
        public ApiFlag this[int i] => _dictFlags[_paramsOrder[i]];
        public ApiFlagType K(ApiFlagType flagType) => _dictFlags[flagType].FlagType;
        public bool V(ApiFlagType flagType) => _dictFlags[flagType].Value;


        public ApiFlags(params ApiFlag[] ApiFlags)
        {
            foreach (var p in ApiFlags)
            {
                _dictFlags[p.FlagType] = p;
                _paramsOrder.Add(p.FlagType);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<ApiFlag> IEnumerable<ApiFlag>.GetEnumerator() => GetEnumerator();
        public ApiFlagsEnumerator GetEnumerator() => new ApiFlagsEnumerator(_dictFlags, _paramsOrder);
    }

    public class ApiFlagsEnumerator : IEnumerator<ApiFlag>
    {
        private int _position = -1;
        private readonly Dictionary<ApiFlagType, ApiFlag> _dictFlags;
        private readonly List<ApiFlagType> _flagsOrder;

        public ApiFlagsEnumerator(Dictionary<ApiFlagType, ApiFlag> dictParams, List<ApiFlagType> paramsOrder)
        {
            _dictFlags = dictParams;
            _flagsOrder = paramsOrder;
        }

        public bool MoveNext() => ++_position < _dictFlags.Count;
        public void Reset() => _position = -1;
        public void Dispose() { }
        object IEnumerator.Current => Current;

        public ApiFlag Current
        {
            get
            {
                try
                {
                    return _dictFlags[_flagsOrder[_position]];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }

    public enum ApiFlagType
    {
        OmitVersion
    }
}
