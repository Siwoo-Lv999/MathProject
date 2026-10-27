using System;
using _LumenLib.ModuleSystem;
using UnityEngine;

namespace _01.Code.Player
{
    public class PlayerController : ModuleOwner
    {
        [field:SerializeField] public PlayerInputSO Input { get; private set; }
        public Rigidbody Rb { get; private set; }

        private PlayerMover _mover;
        private PlayerAnimator _animator;

        protected override void InitModule()
        {
            Rb = GetComponent<Rigidbody>();
            
            base.InitModule();
            
            _mover = GetModule<PlayerMover>();
            _animator = GetModule<PlayerAnimator>();
        }

        private void OnEnable()
        {
            Input.SetEnable();
        }

        private void OnDisable()
        {
            Input.SetDisable();
        }

        private void Update()
        {
            _mover.SetMoveDir(Input.InputDirection);
            _animator.SetMoveVelocity(Rb.linearVelocity);
        }
    }
}
