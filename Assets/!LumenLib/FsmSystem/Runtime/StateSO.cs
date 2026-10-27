using _LumenLib.AnimatorSystem;
using UnityEngine;

namespace _LumenLib.FsmSystem.Runtime
{
    [CreateAssetMenu(fileName = "State data", menuName = "Lib/FSM/State data", order = 0)]
    public class StateSO : ScriptableObject
    {
        public string stateName;
        public string className;
        public int assetIndex;
        public AnimationHashSO animHash;
    }
}
