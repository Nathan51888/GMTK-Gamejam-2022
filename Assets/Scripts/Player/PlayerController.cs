using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    PlayerActions inputActions;
    CharacterController characterController;

    Vector2 currentMovementInput;
    Vector3 currentMovement;
    Vector3 appliedMovement;
    bool isMovementPressed;
    float rotationFactorPerFrame = 15f;

    bool isRunPressed = false;
    float runMovementMultiplier = 2f;

    float gravity = -9.8f;
    float groundedGravity = -.05f;

    bool isJumpPressed = false;
    float initialJumpVelocity;
    float maxJumpHeight = 2f;
    float maxJumpTime = 0.75f;
    bool isJumping = false;
    bool isJumpAnimating = false;
    int jumpCount = 0;
    Dictionary<int, float> initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float> jumpGravities = new Dictionary<int, float>();

    Coroutine currentJumpResetRoutine = null;

    void Awake()
    {
        inputActions = new PlayerActions();
        characterController = GetComponent<CharacterController>();

        inputActions.Gameplay.Move.started += OnMovementInput;
        inputActions.Gameplay.Move.canceled += OnMovementInput;
        inputActions.Gameplay.Move.performed += OnMovementInput;
        inputActions.Gameplay.Run.started += OnRun;
        inputActions.Gameplay.Run.canceled += OnRun;
        inputActions.Gameplay.Jump.started += OnJump;
        inputActions.Gameplay.Jump.canceled += OnJump;

        SetupJumpVariables();
    }

    void Update()
    {
        HandleRotation();

        appliedMovement.x = currentMovement.x;
        appliedMovement.z = currentMovement.z;
        characterController.Move(appliedMovement * Time.deltaTime);
        HandleGravity();
        HandleJump();
        Debug.Log(jumpCount);
    }

    void OnMovementInput(InputAction.CallbackContext context)
    {
        currentMovementInput = context.ReadValue<Vector2>();
        currentMovement.x = currentMovementInput.x;
        currentMovement.z = currentMovementInput.y;
        isMovementPressed = currentMovement.x != 0 || currentMovement.z != 0;
    }

    void OnJump(InputAction.CallbackContext context)
    {
        isJumpPressed = context.ReadValueAsButton();
    }

    void OnRun(InputAction.CallbackContext context)
    {
        isRunPressed = context.ReadValueAsButton();
    }

    void SetupJumpVariables()
    {
        float timeToApex = maxJumpTime / 2;
        gravity = (-2 * maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        initialJumpVelocity = (2 * maxJumpHeight) / timeToApex;
        float secondJumpGravity = (-2 * (maxJumpHeight + 2)) / Mathf.Pow((timeToApex * 1.25f), 2);
        float secondJumpInitialVelocity = (2 * (maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (maxJumpHeight + 4)) / Mathf.Pow((timeToApex * 1.5f), 2);
        float thirdJumpInitialVelocity = (2 * (maxJumpHeight + 4)) / (timeToApex * 1.5f);
        Debug.Log(initialJumpVelocity);
        Debug.Log(secondJumpInitialVelocity);
        Debug.Log(thirdJumpInitialVelocity);

        initialJumpVelocities.Add(1, initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, gravity);
        jumpGravities.Add(1, gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    void HandleJump()
    {
        if (!isJumping && characterController.isGrounded && isJumpPressed)
        {
            if (jumpCount < 3 && currentJumpResetRoutine != null)
            {
                StopCoroutine(currentJumpResetRoutine);
            }
            isJumping = true;
            isJumpAnimating = true;
            jumpCount += 1;
            currentMovement.y = initialJumpVelocities[jumpCount];
            appliedMovement.y = initialJumpVelocities[jumpCount];
        }
        else if (!isJumpPressed && isJumping && characterController.isGrounded)
        {
            isJumping = false;
        }
    }

    IEnumerator jumpResetRoutine()
    {
        yield return new WaitForSeconds(.5f);
        jumpCount = 0;
        Debug.Log("Jump Count Reset");
    }

    void HandleRotation()
    {
        Vector3 positionToLookAt;
        positionToLookAt.x = currentMovement.x;
        positionToLookAt.z = currentMovement.z;
        positionToLookAt.y = 0.0f;
        Quaternion currentRotation = transform.rotation;

        if (isMovementPressed)
        {
            Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
            transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void HandleGravity()
    {
        bool isFalling = currentMovement.y <= 0f || !isJumpPressed;
        float fallMultiplier = 2f;
        if (characterController.isGrounded)
        {
            if (isJumpAnimating)
            {
                isJumpAnimating = false;
                currentJumpResetRoutine = StartCoroutine(jumpResetRoutine());
            }
            currentMovement.y = groundedGravity;
            appliedMovement.y = groundedGravity;
        }
        else if (isFalling)
        {
            float previousYVelocity = currentMovement.y;
            currentMovement.y = currentMovement.y + (jumpGravities[jumpCount] * fallMultiplier * Time.deltaTime);
            appliedMovement.y = Mathf.Max((previousYVelocity + currentMovement.y) * .5f, -20.0f);
        }
        else
        {
            float previousYVelocity = currentMovement.y;
            currentMovement.y = currentMovement.y + (jumpGravities[jumpCount] * Time.deltaTime);
            appliedMovement.y = (previousYVelocity + currentMovement.y) * .5f;
        }
    }

    void OnEnable()
    {
        inputActions.Gameplay.Enable();
    }

    void OnDisable()
    {
        inputActions.Gameplay.Disable();
    }
}
