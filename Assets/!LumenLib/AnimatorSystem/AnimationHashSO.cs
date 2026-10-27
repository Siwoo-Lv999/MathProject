using UnityEngine;

namespace _LumenLib.AnimatorSystem
{
    [CreateAssetMenu(fileName = "Hash Data", menuName = "Lib/Hash Data", order = 0)]
    public class AnimationHashSO : ScriptableObject
    {
        [field: SerializeField] public string HashName { get; private set; }
        [field: SerializeField] public int HashValue { get; private set; }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(HashName))
            {
                HashValue = 0;
                return;
            }

            HashValue = Animator.StringToHash(HashName);
        }
    }
}