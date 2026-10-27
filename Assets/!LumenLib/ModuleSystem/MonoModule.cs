using UnityEngine;

namespace _LumenLib.ModuleSystem
{
    public class MonoModule : MonoBehaviour, IModule
    {
        public ModuleOwner Owner { get; private set; }


        public virtual void Initialize(ModuleOwner owner)
        {
            Owner = owner;
        }
    }
}