using UnityEngine;

namespace Behaviours
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterMovement : MonoBehaviour
    {
        [Header("Movement")] [SerializeField] float groundMaxSpeed = 7.0f;
        [SerializeField] float groundAcceleration = 100.0f;
        [SerializeField] float groundDeacceleration = 50.0f;

        [Header("Jump")] [SerializeField] float maxJumpHeight = 4.0f;
        [SerializeField] float jumpPeakTime = 0.4f;
        [SerializeField] float jumpAbortDeacceleration = 100.0f;

        [Header("Crouch")] [Range(0.1f, 1.0f)] [SerializeField]
        float crouchCapsuleHeightPercent = 0.5f;

        [Range(0.0f, 1.0f)] [SerializeField] float crouchGroundSpeedPercent = 0.3f;

        [Space] [Header("Collision")] [SerializeField]
        LayerMask groundedLayerMask;

        [SerializeField] float groundedRaycastDistance = 0.1f;

        private Rigidbody2D _rigidbody2D;
        private ContactFilter2D _contactFilter;
        private SpriteRenderer _spriteRenderer;

        private Vector2 _currentVelocity;
        private Vector2 _previousPosition;
        private Vector2 _currentPosition;

        private bool _isGrounded;
        private bool _isCrouching;
        private bool _wantsToUnCrouch;
        private bool _wasGroundedLastFrame;
        private bool _isFacingRight = true;

        public IColliderInfo ColliderInfo { get; private set; }

        public bool IsGrounded => _isGrounded == _wasGroundedLastFrame && _isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsJumping => _currentVelocity.y > 0;
        public float GroundRaycastDistance => groundedRaycastDistance;
        public Vector2 CurrentVelocity => _currentVelocity;
        public Rigidbody2D RigidBody => _rigidbody2D;

        public float MaxGroundSpeed
        {
            get => groundMaxSpeed * (IsCrouching ? crouchGroundSpeedPercent : 1.0f);
            set => groundMaxSpeed = value;
        }

        public float Gravity => maxJumpHeight * 2 / (jumpPeakTime * jumpPeakTime);
        public float JumpSpeed => Gravity * jumpPeakTime;

        private void Awake()
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
            _spriteRenderer = GetComponent<SpriteRenderer>();

            ColliderInfo = ColliderInfoFactory.NewColliderInfo(GetComponent<Collider2D>());

            _rigidbody2D.gravityScale = 0;
            _rigidbody2D.freezeRotation = true;

            _contactFilter.layerMask = groundedLayerMask;
            _contactFilter.useLayerMask = true;
            _contactFilter.useTriggers = false;

            Physics2D.queriesStartInColliders = false;
        }

        private void FixedUpdate()
        {
            ApplyGravity();

            _previousPosition = _rigidbody2D.position;
            _currentPosition = _previousPosition + _currentVelocity * Time.fixedDeltaTime;

            _rigidbody2D.MovePosition(_currentPosition);

            CheckCapsuleCollisionsBottom();
            CheckUnCrouch();
        }

        private void ApplyGravity()
        {
            //TODO: Se estiver On ground, nao aplicar forca de gravidade
            if (_currentVelocity.y > 0)
                _currentVelocity.y -= Gravity * Time.fixedDeltaTime;
        }

        protected bool CanJump()
        {
            return IsGrounded && !IsJumping && !IsCrouching;
        }

        public void ProcessMovementInput(Vector2 movementInput)
        {
            var desiredHorizontalSpeed = movementInput.x * MaxGroundSpeed;
            
            _currentVelocity.x = Mathf.MoveTowards(_currentVelocity.x,
                desiredHorizontalSpeed,
                groundAcceleration * Time.deltaTime);

            FlipCharacter(movementInput.x);
        }

        // public void MoveCharacter(Vector2 direction)
        // {
        //     //_rigidbody2D.MovePosition((Vector2)transform.position + direction * (MaxGroundSpeed * Time.deltaTime));
        //     
        // }

        public void FlipCharacter(float x)
        {
            _spriteRenderer.flipX = _isFacingRight switch
            {
                true when x < 0f => true,
                false when x > 0f => false,
                _ => _spriteRenderer.flipX
            };

            _isFacingRight = !_isFacingRight;
        }

        public void StopImmediately()
        {
            _currentVelocity = Vector2.zero;
        }

        public void Jump()
        {
            _currentVelocity.y = JumpSpeed;
            
            if (CanJump())
            {
                _currentVelocity.y = JumpSpeed;
            }
        }

        public void UpdateJumpAbort()
        {
            if (IsJumping)
            {
                _currentVelocity.y -= jumpAbortDeacceleration * Time.deltaTime;
            }
        }

        private void SetCapsuleHeight(float newHeight)
        {
            float capsuleCurrentHeight = ColliderInfo.Size.y;

            ColliderInfo.Size = new Vector2(ColliderInfo.Size.x, newHeight);
            ColliderInfo.Offset += new Vector2(0.0f, (newHeight - capsuleCurrentHeight) * 0.5f);
        }

        public void Crouch()
        {
            _wantsToUnCrouch = false;
        
            if (CanCrouch() == false)
            {
                return;
            }
        
            _isCrouching = true;
        
            SetCapsuleHeight(ColliderInfo.Size.y * crouchCapsuleHeightPercent);
        }

        public void UnCrouch()
        {
            _wantsToUnCrouch = true;
            
            if (!CanUncrouch())
                return;

            _wantsToUnCrouch = false;
            _isCrouching = false;
            
            SetCapsuleHeight(ColliderInfo.Size.y / crouchCapsuleHeightPercent);
        }

        void CheckCapsuleCollisionsBottom()
        {
            int raycastCount = 3;
            Vector2[] raycastPositions = new Vector2[raycastCount];

            raycastPositions[0] = GetColliderBottom() + Vector2.left * (ColliderInfo.Size.x * 0.5f);
            raycastPositions[1] = GetColliderBottom();
            raycastPositions[2] = GetColliderBottom() + Vector2.right * (ColliderInfo.Size.x * 0.5f);

            RaycastHit2D[] hitBuffer = new RaycastHit2D[5];
            float raycastDistance = ColliderInfo.Size.x * 0.5f + groundedRaycastDistance * 2f;
            Vector2 raycastDirection = Vector2.down;

            _wasGroundedLastFrame = _isGrounded;
            _isGrounded = false;

            int hitCount = 0;

            foreach (var vector in raycastPositions)
            {
                Debug.DrawLine(vector, vector + raycastDirection * raycastDistance);

                if (Physics2D.Raycast(vector, raycastDirection, _contactFilter, hitBuffer, raycastDistance) > 0)
                {
                    ++hitCount;
                }
            }

            _isGrounded = _currentVelocity.magnitude > 10.0f ? hitCount == 3 : hitCount > 0;

            if (_isGrounded && !IsJumping)
            {
                _currentVelocity.y = 0;
            }
        }

        bool CheckCapsuleCollisionsTop()
        {
            int raycastCount = 3;
            Vector2[] raycastPositions = new Vector2[raycastCount];

            raycastPositions[0] = GetColliderTop() + Vector2.left * (ColliderInfo.Size.x * 0.5f);
            raycastPositions[1] = GetColliderTop();
            raycastPositions[2] = GetColliderTop() + Vector2.right * (ColliderInfo.Size.x * 0.5f);

            RaycastHit2D[] hitBuffer = new RaycastHit2D[5];
            float raycastDistance = ColliderInfo.Size.x * 0.5f + groundedRaycastDistance * 2f;
            Vector2 raycastDirection = Vector2.up;

            int hitCount = 0;

            foreach (var vector in raycastPositions)
            {
                Debug.DrawLine(vector, vector + raycastDirection * raycastDistance);
                if (Physics2D.Raycast(vector, raycastDirection, _contactFilter, hitBuffer, raycastDistance) > 0)
                {
                    ++hitCount;
                }
            }

            return hitCount > 0;
        }

        bool CanCrouch()
        {
            return IsCrouching == false;
        }

        bool CanUncrouch()
        {
            return IsCrouching && CheckCapsuleCollisionsTop() == false;
        }
        
        void CheckUnCrouch()
        {
            if (_wantsToUnCrouch && CheckCapsuleCollisionsTop() == false)
            {
                UnCrouch();
            }
        }

        public int RaycastAgainstGround(Vector2 raycastOrigin, Vector2 raycastDirection, float raycastDistance,
            RaycastHit2D[] hitBuffer)
        {
            Debug.DrawLine(raycastOrigin, raycastOrigin + raycastDirection * raycastDistance);
            return Physics2D.Raycast(raycastOrigin, raycastDirection, _contactFilter, hitBuffer, raycastDistance);
        }

        public int RaycastAgainstGround(Vector2 raycastOrigin, Vector2 raycastDirection, float raycastDistance)
        {
            RaycastHit2D[] hitResults = new RaycastHit2D[1];
            return RaycastAgainstGround(raycastOrigin, raycastDirection, raycastDistance, hitResults);
        }

        public Vector2 GetColliderBottom()
        {
            return _rigidbody2D.position + ColliderInfo.Offset +
                   Vector2.down * (ColliderInfo.Size.y * 0.5f - ColliderInfo.Size.x * 0.5f);
        }

        public Vector2 GetColliderTop()
        {
            return _rigidbody2D.position + ColliderInfo.Offset +
                   Vector2.up * (ColliderInfo.Size.y * 0.5f - ColliderInfo.Size.x * 0.5f);
        }
    }
}