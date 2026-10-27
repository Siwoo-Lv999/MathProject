using _LumenLib.ModuleSystem;
using UnityEngine;

namespace _01.Code.Player
{
    public class PlayerAnimator : MonoModule
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;
        [SerializeField, Min(0f)] private float turnSpeed = 900f;
        [SerializeField, Min(0f)] private float speedDampTime = 0.08f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            animator.applyRootMotion = false;
        }

        public void SetMoveVelocity(Vector3 velocity)
        {
            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed <= 0.05f)
                animator.SetFloat(SpeedHash, 0f);
            else
                animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);

            if (speed > 0.1f)
            {
                Quaternion target = Quaternion.LookRotation(velocity, Vector3.up);
                visualRoot.rotation = Quaternion.RotateTowards(
                    visualRoot.rotation, target, turnSpeed * Time.deltaTime);
            }
        }
    }
}
