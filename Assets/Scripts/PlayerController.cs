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

    [Header("Wall Jumping")]
    [SerializeField] private bool enableWallJump;
    [SerializeField] private float wallSlideSpeed = 3f;
    [SerializeField] private float wallStickDuration = 0.25f;
    [SerializeField] private float accelerationWallJump = 0.2f;
    [SerializeField] private Vector2 wallJumpClimb;
    [SerializeField] private Vector2 wallJumpOff;
    [SerializeField] private Vector2 wallJumpLeap;

    // Utility
    private Vector2 input;
    private CharacterController2D characterController;

    // Movement
    private Vector3 velocity;
    private float gravity;
    private float horizontalSmoothing;

    // Jumping
    private float jumpVelocity;
    private float minJumpVelocity;

    // Wall Jumping
    private bool wallJumping;
    private bool wallSliding;
    private float wallUnstickTime;

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

        // wall jumping
        if (enableWallJump)
        {
            WallJump();
        }

        // reset vertical velocity on vertical collisions
        if (characterController.CollisionState.below || characterController.CollisionState.above)
        {
            velocity.y = 0;
        }

        // regular jumping
        Jump();

        // apply gravity
        velocity.y += gravity * Time.deltaTime;

        // attempt to move
        characterController.Move(velocity * Time.deltaTime, input.y == -1f);
    }

    private float GetAcceleration()
    {
        if (wallJumping)
        {
            return accelerationWallJump;
        }
        else if (characterController.CollisionState.below)
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

    private void WallJump()
    {
        int wallDir = (characterController.CollisionState.left) ? -1 : 1;

        // reset wallsliding status every update
        wallSliding = false;

        // reset walljumping status on any collision
        if (characterController.CollisionState.IsColliding())
        {
            wallJumping = false;
        }

        // sliding down along an horizontal wall
        if ((characterController.CollisionState.left || characterController.CollisionState.right) && 
            !characterController.CollisionState.below && 
            velocity.y < 0f)
        {
            wallSliding = true;

            // clamp vertical velocity
            if (velocity.y < -wallSlideSpeed)
            {
                velocity.y = -wallSlideSpeed;
            }

            // when sliding, player is stuck for a brief period of time to facilitate walljumping
            if (wallUnstickTime > 0f)
            {
                velocity.x = 0f;
                horizontalSmoothing = 0f;

                if (input.x != wallDir && input.x != 0)
                {
                    wallUnstickTime -= Time.deltaTime;
                }
                else
                {
                    wallUnstickTime = wallStickDuration;
                }
            }
            // reset for the following update
            else
            {
                wallUnstickTime = wallStickDuration;
            }
        }

        // perform walljump, but only when already wallsliding
        if (Input.GetKeyDown(KeyCode.Space) && wallSliding)
        {
            wallJumping = true;

            // when jumping against the wall, go up
            if (wallDir == input.x)
            {
                velocity.x = -wallDir * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            }

            // when performing a static jump, go down
            else if (input.x == 0f)
            {
                velocity.x = -wallDir * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            }

            // otherwise, jump from the wall
            else
            {
                velocity.x = -wallDir * wallJumpLeap.x;
                velocity.y = wallJumpLeap.y;
            }
        }
    }
}