using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerActions _inputActions;
    CharacterController _characterController;

    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _appliedMovement;
    bool _isMovementPressed;
    float _rotationFactorPerFrame = 15f;

    bool _isRunPressed = false;
    float _runMovementMultiplier = 2f;

    float _gravity = -9.8f;
    float _groundedGravity = -.05f;

    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    float _maxJumpHeight = 2f;
    float _maxJumpTime = 0.75f;
    bool _isJumping = false;
    bool _isJumpAnimating = false;
    int _jumpCount = 0;
    Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();

    Coroutine _currentJumpResetRoutine = null;

    void Awake()
    {
        _inputActions = new PlayerActions();
        _characterController = GetComponent<CharacterController>();

        _inputActions.Gameplay.Move.started += OnMovementInput;
        _inputActions.Gameplay.Move.canceled += OnMovementInput;
        _inputActions.Gameplay.Move.performed += OnMovementInput;
        _inputActions.Gameplay.Run.started += OnRun;
        _inputActions.Gameplay.Run.canceled += OnRun;
        _inputActions.Gameplay.Jump.started += OnJump;
        _inputActions.Gameplay.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    void Update()
    {
        HandleRotation();

        _appliedMovement.x = _currentMovement.x;
        _appliedMovement.z = _currentMovement.z;
        _characterController.Move(_appliedMovement * Time.deltaTime);
        HandleGravity();
        HandleJump();
        Debug.Log(_jumpCount);
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _isMovementPressed = _currentMovement.x != 0 || _currentMovement.z != 0;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();
    }

    void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);
        Debug.Log(_initialJumpVelocity);
        Debug.Log(secondJumpInitialVelocity);
        Debug.Log(thirdJumpInitialVelocity);

        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _jumpGravities.Add(0, _gravity);
        _jumpGravities.Add(1, _gravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
    }

    void HandleJump()
    {
        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            if (_jumpCount < 3 && _currentJumpResetRoutine != null)
            {
                StopCoroutine(_currentJumpResetRoutine);
            }
            _isJumping = true;
            _isJumpAnimating = true;
            _jumpCount += 1;
            _currentMovement.y = _initialJumpVelocities[_jumpCount];
            _appliedMovement.y = _initialJumpVelocities[_jumpCount];
        }
        else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
    }

    IEnumerator jumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        _jumpCount = 0;
        Debug.Log("Jump Count Reset");
    }

    void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = _currentMovement.x;
        positionToLookAt.z = _currentMovement.z;
        positionToLookAt.y = 0.0f;
        Quaternion currentRotation = transform.rotation;

        if (_isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        bool isFalling = _currentMovement.y <= 0f || !_isJumpPressed;
        float fallMultiplier = 2f;
        if (_characterController.isGrounded)
        {
            if (_isJumpAnimating)
            {
                _isJumpAnimating = false;
                _currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
            }
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;
        }
        else if (isFalling)
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
            _appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * .5f, -20.0f);
        }
        else
        {
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (_jumpGravities[_jumpCount] * Time.deltaTime);
            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * .5f;
        }
    }

    void OnEnable()
    {
        _inputActions.Gameplay.Enable();
    }

    void OnDisable()
    {
        _inputActions.Gameplay.Disable();
    }
}
