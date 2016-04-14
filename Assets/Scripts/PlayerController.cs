using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float timeToJumpApex = 0.4f;

    private Vector2 input;
    private CharacterController2D characterController;

    private Vector3 velocity;
    private float gravity;
    private float jumpVelocity; 

    void Start()
    {
        characterController = GetComponent<CharacterController2D>();

        gravity = -(2 * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        jumpVelocity = Mathf.Abs(gravity * timeToJumpApex);
    }

    void Update()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        velocity.x = input.x * moveSpeed;

        if (characterController.CollisionState.below || characterController.CollisionState.above)
        {
            velocity.y = 0;
        }

        if (Input.GetKey(KeyCode.Space) && characterController.CollisionState.below)
        {
            velocity.y = jumpVelocity;
        }

        velocity.y += gravity * Time.deltaTime;

        characterController.Move(velocity * Time.deltaTime, input.y == -1f);
    }
}