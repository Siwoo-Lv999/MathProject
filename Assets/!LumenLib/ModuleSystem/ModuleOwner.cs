using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _LumenLib.ModuleSystem
{
    public abstract class ModuleOwner : MonoBehaviour
    {
        private Dictionary<Type, IModule> _moduleDict;

        protected virtual void Awake()
        {
            _moduleDict = GetComponentsInChildren<IModule>(true).ToDictionary(m => m.GetType());
            
            InitModule();
            AfterInit();
        }

        protected virtual void InitModule()
        {
            foreach(IModule module in _moduleDict.Values)
                module.Initialize(this);
        }

        protected virtual void AfterInit()
        {
            foreach(IAfterInit module in _moduleDict.Values.OfType<IAfterInit>())
                module.AfterInit();
        }

        public T GetModule<T>()
        {
            if(_moduleDict.TryGetValue(typeof(T), out IModule module))
                return (T)module;

            IModule findModule = _moduleDict.Values.FirstOrDefault(m => m is T);

            if (findModule is T casted)
                return casted;

            return default;
        }
    }
}
