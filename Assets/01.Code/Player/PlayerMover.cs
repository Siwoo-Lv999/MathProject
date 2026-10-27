using _LumenLib.ModuleSystem;
using UnityEngine;

namespace _01.Code.Player
{
    public class PlayerMover : MonoModule
    {
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private Transform movementCamera;

        private PlayerController _player;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _player = owner as PlayerController;
        }

        public void SetMoveDir(Vector2 dir)
        {
            Vector2 input = Vector2.ClampMagnitude(dir, 1f);
            Vector3 forward = Vector3.ProjectOnPlane(movementCamera.forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Vector3 velocity = (right * input.x + forward * input.y) * moveSpeed;
            velocity.y = _player.Rb.linearVelocity.y;
            _player.Rb.linearVelocity = velocity;
        }
    }
}
