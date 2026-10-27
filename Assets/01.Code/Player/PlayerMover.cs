using _LumenLib.ModuleSystem;
using UnityEngine;

namespace _01.Code.Player
{
    public class PlayerMover : MonoModule
    {
        [SerializeField] private float moveSpeed = 5f;
        
        private PlayerController _player;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            
            _player = owner as PlayerController;
        }

        public void SetMoveDir(Vector2 dir)
        {
            _player.Rb.linearVelocity = new Vector3(dir.x * moveSpeed,
                _player.Rb.linearVelocity.y, dir.y * moveSpeed);
        }
    }
}