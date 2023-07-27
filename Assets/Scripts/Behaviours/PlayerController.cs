using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Behaviours
{
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(Animator))]
    public class PlayerController : MonoBehaviour, InputSettings.IPlayerActions
    {
        private InputSettings _controls;

        [Header("Components")] private CharacterMovement _movement;
        private Animator _animator;
        private LayerMask _groundLayer;

        [Header("Inputs")] private Vector2 _movementInput, _pointerInput;

        private void Awake()
        {
            _movement = GetComponent<CharacterMovement>();
            _animator = GetComponent<Animator>();

            if (_controls is not null)
                return;

            _controls = new InputSettings();
            _controls.Player.SetCallbacks(this);
        }

        public void OnEnable() => _controls.Player.Enable();

        public void OnDisable() => _controls.Player.Disable();

        private void Update()
        {
            _movement.ProcessMovementInput(_movementInput);
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            _movementInput = context.ReadValue<Vector2>();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if(context.ReadValueAsButton())
                _movement.Jump();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        public void OnDodge(InputAction.CallbackContext context)
        {
            throw new NotImplementedException();
        }

        public void OnPointerPosition(InputAction.CallbackContext context)
        {
            var mousePosition = context.ReadValue<Vector2>();
        }
    }
}