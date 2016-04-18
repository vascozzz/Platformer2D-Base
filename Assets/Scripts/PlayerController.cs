using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Basic Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float jumpApexDelay = 0.4f;
    [SerializeField] private float accelerationGrounded = 0f;
    [SerializeField] private float accelerationJump = 0f;

    [Header("Variable Jump Height")]
    [SerializeField] private bool enableVarJumpHeight;
    [SerializeField] private float minJumpHeight = 2f;

    // Utility
    private Vector2 input;
    private CharacterController2D characterController;

    // Movement
    private Vector3 velocity;
    private float gravity;
    private float horizontalSmoothing;
    private float jumpVelocity;
    private float minJumpVelocity;

    void Start()
    {
        characterController = GetComponent<CharacterController2D>();

        // set gravity and jump velocity based on desired height and apex time
        gravity = -(2 * jumpHeight) / Mathf.Pow(jumpApexDelay, 2);
        jumpVelocity = Mathf.Abs(gravity * jumpApexDelay);

        // likewise for minimum jump velocity
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update()
    {
        // get user input
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        // target velocity
        float acceleration = GetAcceleration();
        float horizontalVel = input.x * moveSpeed;

        // smoothly accelerate towards the target
        velocity.x = Mathf.SmoothDamp(velocity.x, horizontalVel, ref horizontalSmoothing, acceleration);

        // reset vertical velocity on vertical collisions
        if (characterController.CollisionState.below || characterController.CollisionState.above)
        {
            velocity.y = 0;
        }

        // jumping
        Jump();

        // apply gravity
        velocity.y += gravity * Time.deltaTime;

        // attempt to move
        characterController.Move(velocity * Time.deltaTime, input.y == -1f);
    }

    private float GetAcceleration()
    {
        if (characterController.CollisionState.below)
        {
            return accelerationGrounded;
        }
        else
        {
            return accelerationJump;
        }
    }

    private void Jump()
    {
        // perform regular jump
        if (Input.GetKey(KeyCode.Space) && characterController.CollisionState.below)
        {
            velocity.y = jumpVelocity;
        }
        // if variable jump height is enabled, clamp vertical velocity
        if (enableVarJumpHeight && Input.GetKeyUp(KeyCode.Space))
        {
            if (velocity.y > minJumpVelocity)
            {
                velocity.y = minJumpVelocity;
            }
        }
    }
}