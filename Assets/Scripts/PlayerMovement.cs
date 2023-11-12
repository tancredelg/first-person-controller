using System;
using System.Runtime.CompilerServices;
using UnityEditor.PackageManager;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform orientation;
    [SerializeField] private Transform childTransform;
    
    [Header("Movement")]
    [SerializeField] private float sprintSpeed;
    [SerializeField] private float walkSpeedMultiplier;
    [SerializeField] private float groundDrag;
    private Vector2 _currentMoveInput;
    private Vector3 _moveDirection;
    private float _moveSpeed;
    
    [Header("Jumping")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpCooldown;
    [SerializeField] private float airMultiplier;
    [SerializeField] private float addedGravity;
    private bool _isGrounded;
    private bool _canJump;

    [Header("Crouching")]
    [SerializeField] private float crouchSpeedMultiplier;
    [SerializeField] private float crouchYScale;
    [SerializeField] private float slideSpeedMultiplier;
    [SerializeField] private float slideDuration;
    private float _yScaleBeforeCrouch;
    private bool _isSliding;

    [Header("Slope Handling")]
    [SerializeField] private float maxSlopeAngle;
    private RaycastHit _slopeHit;
    
    [Header("Keybindings")]
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode walkKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Ground Checking")]
    [SerializeField] private LayerMask groundLayer;

    private enum MoveState
    {
        Sprinting,
        Walking,
        Crouching,
        Sliding
    }
    private MoveState _moveState;
    private Rigidbody _rigidbody;
    private CapsuleCollider _playerCollider;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
        _playerCollider = GetComponentInChildren<CapsuleCollider>();
        _canJump = true;
        _yScaleBeforeCrouch = transform.localScale.y;
    }

    private void Update()
    {
        GetInputs();
        HandleState();
        ClampMoveSpeed();
        _isGrounded = Physics.Raycast(childTransform.position, Vector3.down,
            _playerCollider.height * 0.5f * transform.localScale.y + 0.2f, groundLayer);
        _rigidbody.drag = _isGrounded ? groundDrag : 0;
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void GetInputs()
    {
        _currentMoveInput.x = Input.GetAxisRaw("Horizontal");
        _currentMoveInput.y = Input.GetAxisRaw("Vertical");
        
        if (Input.GetKey(jumpKey) && _isGrounded && _canJump)
        {
            _canJump = false;
            Jump();
        }

        if (Input.GetKeyDown(crouchKey))
        {
            if (!_isGrounded && childTransform.localPosition.y > 0)
            {
                transform.position += Vector3.up * (1 + transform.localScale.y);
                childTransform.localPosition *= -1;
            }
            transform.localScale = new Vector3(transform.localScale.x, crouchYScale, transform.localScale.z);
            
            
            if ((_isGrounded && GetHorizontalVelocity().magnitude > sprintSpeed * 0.95f) ||
                (OnSlope(out var slopeHitInfo) && Vector3.Dot(slopeHitInfo.normal, _moveDirection) > 0))
            {
                _isSliding = true;
                Invoke(nameof(StopSliding), slideDuration);
            }
        }
        
        if (Input.GetKeyUp(crouchKey))
        {
            if (childTransform.localPosition.y < 0)
            {
                transform.position += Vector3.down * (1 + transform.localScale.y);
                childTransform.localPosition *= -1;
            }
            transform.localScale = new Vector3(transform.localScale.x ,_yScaleBeforeCrouch, transform.localScale.z);
            
            _isSliding = false;
        }
    }

    private void Move()
    {
        if (!_isSliding) // update direction if not sliding
        {
            _moveDirection = orientation.forward * _currentMoveInput.y + orientation.right * _currentMoveInput.x;
        }
        
        Vector3 moveForce = _moveDirection.normalized * (_moveSpeed * 10);

        if (OnSlope(out var slopeHitInfo)) // change moveForce to align with the slope
        {
            moveForce = GetSlopeMoveDirection(slopeHitInfo.normal) * (_moveSpeed * 10);
            
            if (moveForce.x == 0 && moveForce.z == 0) // add 'anti-slip' force if user isn't moving
            {
                moveForce += Vector3.up * 9;
            }

            if (_isSliding && Vector3.Dot(slopeHitInfo.normal, _moveDirection) < 0) // stop slides going up a slope
            {
                _isSliding = false;
            }
        }
        else if (!_isGrounded) // add airborne modifiers if needed
        {
            moveForce = moveForce * airMultiplier + Vector3.down * addedGravity;
        }

        _rigidbody.AddForce(moveForce, ForceMode.Force);
    }

    private void HandleState()
    {
        if (Input.GetKey(crouchKey))
        {
            _moveState = MoveState.Crouching;
            if (_isSliding)
            {
                _moveSpeed = sprintSpeed * slideSpeedMultiplier;
            }
            else if (_isGrounded)
            {
                _moveSpeed = sprintSpeed * crouchSpeedMultiplier;
            }
        }
        else if (Input.GetKey(walkKey))
        {
            _moveState = MoveState.Walking;
            _moveSpeed = sprintSpeed * walkSpeedMultiplier;
        }
        else
        {
            _moveState = MoveState.Sprinting;
            _moveSpeed = sprintSpeed;
        }
    }

    private void ClampMoveSpeed()
    {
        if (OnSlope(out _) && _canJump && _rigidbody.velocity.magnitude > _moveSpeed) // consider all 3 dimensions when on slope
        {
            _rigidbody.velocity = _rigidbody.velocity.normalized * _moveSpeed;
        }
        else
        {
            Vector3 horizontalVelolcity = GetHorizontalVelocity();

            if (horizontalVelolcity.magnitude > _moveSpeed)
            {
                horizontalVelolcity = horizontalVelolcity.normalized * _moveSpeed;
                _rigidbody.velocity = new Vector3(horizontalVelolcity.x, _rigidbody.velocity.y, horizontalVelolcity.z);
            }
        }
    }

    private void Jump()
    {
        _rigidbody.velocity = GetHorizontalVelocity();
        _rigidbody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        Invoke(nameof(ResetJump), jumpCooldown);
    }
    
    private void ResetJump() => _canJump = true;

    private void StopSliding() => _isSliding = false;

    private bool OnSlope(out RaycastHit slopeHitInfo)
    {
        if (Physics.Raycast(childTransform.position, Vector3.down, out slopeHitInfo, _playerCollider.height * 0.5f * transform.localScale.y + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHitInfo.normal);
            return angle < maxSlopeAngle && angle > 0;
        }
        
        return false;
    }

    private Vector3 GetHorizontalVelocity() => new(_rigidbody.velocity.x, 0, _rigidbody.velocity.z);

    private Vector3 GetSlopeMoveDirection(Vector3 slopeNormal) => Vector3.ProjectOnPlane(_moveDirection, slopeNormal).normalized;
}