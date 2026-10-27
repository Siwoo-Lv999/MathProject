using UnityEngine;
using UnityEngine.InputSystem;

namespace _01.Code.Player
{
    [CreateAssetMenu(fileName = "Player Input", menuName = "SO/Player Input", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        public Vector2 InputDirection { get; private set; }
        
        private Controls _controls;

        public void SetEnable()
        {
            _controls ??= new Controls();
            _controls.Player.Enable();
            _controls.Player.SetCallbacks(this);
        }

        public void SetDisable()
        {
            if(_controls != null)
                _controls.Player.Disable();
            _controls = null;
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            InputDirection = context.ReadValue<Vector2>();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            
        }
    }
}