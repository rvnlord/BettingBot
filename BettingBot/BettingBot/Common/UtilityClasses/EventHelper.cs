using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace BettingBot.Common.UtilityClasses
{
    public static class EventHelper
    {
        private static BindingFlags AllBindings => BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
        private static readonly Dictionary<Type, List<FieldInfo>> _dicEventFieldInfos = new Dictionary<Type, List<FieldInfo>>();

        public static void AddEventHandlers(object o, string eventName, List<Delegate> eventHandlers)
        {
            var ei = o.GetType().GetEvents().Single(e => e.Name == eventName);
            foreach (var eventHandler in eventHandlers)
                ei.AddEventHandler(o, eventHandler);
        }

        private static EventHandlerList GetStaticEventHandlerList(Type t, object obj)
        {
            var mi = t.GetMethod("get_Events", AllBindings);
            return (EventHandlerList) mi?.Invoke(obj, new object[] { });
        }

        private static IEnumerable<FieldInfo> GetTypeEventFields(Type t)
        {
            if (_dicEventFieldInfos.ContainsKey(t))
                return _dicEventFieldInfos[t];

            List<FieldInfo> lst = new List<FieldInfo>();
            BuildEventFields(t, lst);
            _dicEventFieldInfos.Add(t, lst);
            return lst;
        }

        private static void BuildEventFields(Type t, List<FieldInfo> lst)
        {
            foreach (var ei in t.GetEvents(AllBindings))
            {
                var dt = ei.DeclaringType;
                var fi = dt?.GetField(ei.Name, AllBindings);

                if (fi != null)
                    lst.Add(fi);
                else
                {
                    var fi2 = dt?.GetField(ei.Name + "Event", AllBindings);
                    if (fi2 != null)
                        lst.Add(fi2);
                }
            }
        }

        public static List<Delegate> RemoveEventHandlers(object obj, string EventName)
        {
            var delegates = new List<Delegate>();
            if (obj == null) return delegates;
            
            var t = obj.GetType();
            var eventFields = GetTypeEventFields(t);
            EventHandlerList static_event_handlers = null;

            foreach (var fi in eventFields)
            {
                if (EventName != "" && (string.Compare(EventName, fi.Name, StringComparison.OrdinalIgnoreCase) != 0 && string.Compare(EventName + "Event", fi.Name, StringComparison.OrdinalIgnoreCase) != 0))
                    continue;

                if (fi.FieldType == typeof(RoutedEvent))
                {
                    var wrt = fi.GetValue(obj);
                    var EventHandlersStoreType = t.GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic);
                    var EventHandlersStore = EventHandlersStoreType?.GetValue(obj, null);
                    var storeType = EventHandlersStore?.GetType();
                    var getRoutedEventHandlers = storeType?.GetMethod("GetRoutedEventHandlers", BindingFlags.Instance | BindingFlags.Public);
                    var Params = new[] { wrt };
                    var ret = (RoutedEventHandlerInfo[]) getRoutedEventHandlers?.Invoke(EventHandlersStore, Params);
                    var ei = t.GetEvent(fi.Name.Substring(0, fi.Name.Length - 5), AllBindings);
                    if (ret == null) continue;
                    foreach (var routedEventHandlerInfo in ret)
                    {
                        delegates.Add(routedEventHandlerInfo.Handler);
                        ei?.RemoveEventHandler(obj, routedEventHandlerInfo.Handler);
                    }
                        
                }
                else if (fi.IsStatic)
                {
                    if (static_event_handlers == null)
                        static_event_handlers = GetStaticEventHandlerList(t, obj);
                    var idx = fi.GetValue(obj);
                    var eh = static_event_handlers[idx];
                    var dels = eh?.GetInvocationList();
                    if (dels == null) continue;
                    var ei = t.GetEvent(fi.Name, AllBindings);
                    foreach (var del in dels)
                    {
                        delegates.Add(del);
                        ei?.RemoveEventHandler(obj, del);
                    }
                }
                else
                {
                    var ei = t.GetEvent(fi.Name, AllBindings);
                    if (ei == null) continue;
                    var val = fi.GetValue(obj);
                    if (!(val is Delegate mdel)) continue;
                    foreach (var del in mdel.GetInvocationList())
                    {
                        delegates.Add(del);
                        ei.RemoveEventHandler(obj, del);
                    }
                }
            }

            return delegates;
        }
    }
}