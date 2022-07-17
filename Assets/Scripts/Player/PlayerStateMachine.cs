using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerStateMachine : MonoBehaviour
{
    // private variables
    Animator _animator;
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

    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    float _maxJumpHeight = 2f;
    float _maxJumpTime = 0.75f;
    bool _isJumping = false;
    bool _requireNewJumpPress = false;
    int _jumpCount = 0;
    Dictionary<int, float> _initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> _jumpGravities = new Dictionary<int, float>();
    Coroutine _currentJumpResetRoutine = null;

    int _isRunningHash;

    // state variables
    PlayerBaseState _currentState;
    PlayerStateFactory _states;

    // public fields
    public Animator Animator { get => _animator; set => _animator = value; }
    public CharacterController CharacterController { get => _characterController; set => _characterController = value; }
    public Vector2 CurrentMovementInput { get => _currentMovementInput; set => _currentMovementInput = value; }
    public Vector3 CurrentMovement { get => _currentMovement; set => _currentMovement = value; }
    public Vector3 AppliedMovement { get => _appliedMovement; set => _appliedMovement = value; }
    public float CurrentMovementX { get => _currentMovement.x; set => _currentMovement.x = value; }
    public float CurrentMovementY { get => _currentMovement.y; set => _currentMovement.y = value; }
    public float CurrentMovementZ { get => _currentMovement.z; set => _currentMovement.z = value; }
    public float AppliedMovementX { get => _appliedMovement.x; set => _appliedMovement.x = value; }
    public float AppliedMovementY { get => _appliedMovement.y; set => _appliedMovement.y = value; }
    public float AppliedMovementZ { get => _appliedMovement.z; set => _appliedMovement.z = value; }

    public bool IsMovementPressed { get => _isMovementPressed; set => _isMovementPressed = value; }
    public float RunMovementMultiplier { get => _runMovementMultiplier; set => _runMovementMultiplier = value; }
    public float RotationFactorPerFrame { get => _rotationFactorPerFrame; set => _rotationFactorPerFrame = value; }
    public float Gravity { get => _gravity; set => _gravity = value; }
    public bool IsJumpPressed { get { return _isJumpPressed; } set { _isJumpPressed = value; } }
    public float InitialJumpVelocity { get => _initialJumpVelocity; set => _initialJumpVelocity = value; }
    public float MaxJumpHeight { get => _maxJumpHeight; set => _maxJumpHeight = value; }
    public float MaxJumpTime { get => _maxJumpTime; set => _maxJumpTime = value; }
    public bool IsJumping { get => _isJumping; set => _isJumping = value; }
    public bool RequireNewJumpPress { get => _requireNewJumpPress; set => _requireNewJumpPress = value; }
    public int JumpCount { get => _jumpCount; set => _jumpCount = value; }
    public Dictionary<int, float> InitialJumpVelocities { get => _initialJumpVelocities; set => _initialJumpVelocities = value; }
    public Dictionary<int, float> JumpGravities { get => _jumpGravities; set => _jumpGravities = value; }
    public Coroutine CurrentJumpResetRoutine { get => _currentJumpResetRoutine; set => _currentJumpResetRoutine = value; }

    public PlayerBaseState CurrentState { get => _currentState;  set => _currentState = value;  }
    
    public int IsRunningHash { get => _isRunningHash; set => _isRunningHash = value; }

    void Awake()
    {
        _inputActions = new PlayerActions();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponentInChildren<Animator>();

        _states = new PlayerStateFactory(this);
        _currentState = _states.Grounded();
        _currentState.EnterState();

        // subscribing to input events
        _inputActions.Gameplay.Move.started += OnMovementInput;
        _inputActions.Gameplay.Move.canceled += OnMovementInput;
        _inputActions.Gameplay.Move.performed += OnMovementInput;
        _inputActions.Gameplay.Run.started += OnRun;
        _inputActions.Gameplay.Run.canceled += OnRun;
        _inputActions.Gameplay.Jump.started += OnJump;
        _inputActions.Gameplay.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    void Start()
    {
        //_characterController.Move(AppliedMovement * Time.deltaTime);
    }

    void Update()
    {
        HandleRotation();
        _currentState.UpdateStates();
        _characterController.Move(_appliedMovement * Time.deltaTime);
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
        _requireNewJumpPress = false;
    }

    void OnRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        float initialGravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

        _initialJumpVelocities.Add(1, _initialJumpVelocity);
        _initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        _initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        _jumpGravities.Add(0, initialGravity);
        _jumpGravities.Add(1, initialGravity);
        _jumpGravities.Add(2, secondJumpGravity);
        _jumpGravities.Add(3, thirdJumpGravity);
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

    void OnEnable()
    {
        _inputActions.Gameplay.Enable();
    }

    void OnDisable()
    {
        _inputActions.Gameplay.Disable();
    }
}