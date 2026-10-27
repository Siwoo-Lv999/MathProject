using System;
using System.Collections.Generic;
using UnityEngine;

namespace _LumenLib.CoreSystem.EventChannelSystem.Runtime
{
    public abstract class GameEvent
    {
        
    }
    
    [CreateAssetMenu(fileName = "EventChannel", menuName = "Lib/EventChannel", order = 0)]
    public class EventChannelSO : ScriptableObject
    {
        private readonly Dictionary<Type, Action<GameEvent>> _events = new();
        private readonly Dictionary<Delegate, Action<GameEvent>> _lookup = new();

        public void AddListener<T>(Action<T> handler) where T : GameEvent {
            if (_lookup.ContainsKey(handler)) return;

            Action<GameEvent> wrappedHandler = e => handler(e as T);
            _lookup[handler] = wrappedHandler;
            
            Type evtType = typeof(T);
            if (!_events.TryAdd(evtType, wrappedHandler)) {
                _events[evtType] += wrappedHandler;
            }
        }

        public void RemoveListener<T>(Action<T> handler) where T : GameEvent {
            Type evtType = typeof(T);
            if (!_lookup.TryGetValue(handler, out Action<GameEvent> wrappedHandler))
                return;

            if (_events.TryGetValue(evtType, out Action<GameEvent> evtHandler)) {
                evtHandler -= wrappedHandler;
                if (evtHandler == null)
                    _events.Remove(evtType);
                else
                    _events[evtType] = evtHandler;
            }
            
            _lookup.Remove(handler);
        }

        public void RaiseEvent(GameEvent evt) {
            if(_events.TryGetValue(evt.GetType(), out Action<GameEvent> handler)) 
                handler?.Invoke(evt);
        }

        public void Clear() { //리로드 도메인이 없을 경우 수동으로 초기화해야한다.
            _events.Clear();
            _lookup.Clear();
        }
    }
}