using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MoreLinq;

namespace BettingBot.Common.UtilityClasses
{
    public class Parameters : IEnumerable<Parameter>
    {
        private readonly Dictionary<string, Parameter> _dictParams = new Dictionary<string, Parameter>();
        private readonly List<string> _paramsOrder = new List<string>();

        public Parameter this[string name] => _dictParams[name];
        public Parameter this[int i] => _dictParams[_paramsOrder[i]];
        public string K(string name) => _dictParams[name].Name;
        public string V(string name) => _dictParams[name].Value;

        public Parameters(params Parameter[] parameters)
        {
            foreach (var p in parameters)
            {
                _dictParams[p.Name] = p;
                _paramsOrder.Add(p.Name);
            }
        }

        public void Add(string name, string value)
        {
            _dictParams.Add(name, new Parameter(name, value));
            _paramsOrder.Add(name);
        }

        public void Remove(string name)
        {
            _dictParams.RemoveIfExists(name);
            _paramsOrder.RemoveIfExists(name);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        IEnumerator<Parameter> IEnumerable<Parameter>.GetEnumerator() => GetEnumerator();
        public ParametersEnumerator GetEnumerator() => new ParametersEnumerator(_dictParams, _paramsOrder);

        public string JsonSerialize() // własna metoda, żeby zachować oryginalny porządek parametrów
        {
            var sb = new StringBuilder();
            foreach (var p in this)
                sb.Append($"\"{p.Name}\":\"{p.Value}\",");
            if (sb.Length <= 0)
                return null;
            return "{" + sb.ToString().SkipLast(1) + "}";
        }
    }

    public class ParametersEnumerator : IEnumerator<Parameter>
    {
        private int _position = -1;
        private readonly Dictionary<string, Parameter> _dictParams;
        private readonly List<string> _paramsOrder;

        public ParametersEnumerator(Dictionary<string, Parameter> dictParams, List<string> paramsOrder)
        {
            _dictParams = dictParams;
            _paramsOrder = paramsOrder;
        }

        public bool MoveNext() => ++_position < _dictParams.Count;
        public void Reset() => _position = -1;
        public void Dispose() { }
        object IEnumerator.Current => Current;

        public Parameter Current
        {
            get
            {
                try
                {
                    return _dictParams[_paramsOrder[_position]];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
    }
}
