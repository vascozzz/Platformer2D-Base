using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider2D))]
public class CharacterController2D : MonoBehaviour
{
    public struct CharacterRaycastOrigins
    {
        public Vector2 topLeft;
        public Vector2 topRight;
        public Vector2 bottomLeft;
        public Vector2 bottomRight;
    }

    public struct CharacterCollisionState
    {
        public bool above;
        public bool below;
        public bool left;
        public bool right;

        public bool ascendingSlope;
        public bool descendingSlope;
        public float slopeAngle;
        public float slopeAnglePrev;

        public bool fallingThroughPlatform;
        public Collider2D throughPlatform;
        public bool ignoreOneWayPlatforms;

        public Vector3 initialVelocity;
        public int horizontalDir;

        public void Reset()
        {
            above = below = left = right = false;
            ascendingSlope = descendingSlope = false;
            ignoreOneWayPlatforms = false;
            slopeAnglePrev = slopeAngle;
            slopeAngle = 0f;
        }

        public bool IsColliding()
        {
            return above || below || left || right;
        }
    }

    [SerializeField, Range(0.001f, 0.3f)]
    private float skinWidth = 0.015f;

    [SerializeField, Range(2, 30)]
    private int horizontalRayCount = 4;

    [SerializeField, Range(2, 30)]
    private int verticalRayCount = 4;

    [SerializeField]
    private LayerMask collisionMask;

    [SerializeField]
    private LayerMask oneWayPlatformsMask;

    [SerializeField, Range(0f, 90f)]
    private float maxAscentAngle = 65f;

    [SerializeField, Range(0f, 90f)]
    private float maxDescentAngle = 65f;

    private float horizontalRaySpacing;
    private float verticalRaySpacing;
    private Color rayColor = Color.yellow;

    private BoxCollider2D boxCollider;
    private CharacterRaycastOrigins raycastOrigins;
    private CharacterCollisionState collisionState;
    private LayerMask verticalCollisionMask;

    public CharacterCollisionState CollisionState
    {
        get { return collisionState; }
    }


    void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();
    }


    void Start()
    {
        // set spacing between raycasts
        SetRaycastSpacing();

        // oneWayPlatforms do not collide horizontally, so vertical collisions need a unique mask to check for them
        verticalCollisionMask = collisionMask | oneWayPlatformsMask;

        // by default, character is initiated facing right
        collisionState.horizontalDir = 1;
    }


    /// <summary>
    /// Sets the spacing between individual raycasts fired in the same direction.
    /// Raycasts are fired from a small distance within the box collider, so as to avoid issues
    /// with other objects we're touching directly. The distance is defined by skinWidth.
    /// </summary>
    private void SetRaycastSpacing()
    {
        Bounds insetBounds = boxCollider.bounds;
        insetBounds.Expand(skinWidth * -2);

        // safety check, guarantee at least two rays in each direction
        horizontalRayCount = Mathf.Clamp(horizontalRayCount, 2, int.MaxValue);
        verticalRayCount = Mathf.Clamp(verticalRayCount, 2, int.MaxValue);

        // spacing between rays
        horizontalRaySpacing = insetBounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = insetBounds.size.x / (verticalRayCount - 1);
    }
    

    /// <summary>
    /// Updates the points from which raycasts are fired based on the inset collider's corners
    /// in world coordinates. Should be called every frame.
    /// </summary>
    private void UpdateRaycastOrigins()
    {
        Bounds insetBounds = boxCollider.bounds;
        insetBounds.Expand(skinWidth * -2);

        raycastOrigins.topLeft = new Vector2(insetBounds.min.x, insetBounds.max.y);
        raycastOrigins.topRight = new Vector2(insetBounds.max.x, insetBounds.max.y);
        raycastOrigins.bottomLeft = new Vector2(insetBounds.min.x, insetBounds.min.y);
        raycastOrigins.bottomRight = new Vector2(insetBounds.max.x, insetBounds.min.y);
    }


    /// <summary>
    /// Moves the character according to a given velocity. Checks for collisions in the horizontal
    /// and vertical axis before attempting to move the character to the new position. 
    /// </summary>
    /// <param name="ignoreOneWayPlatforms">
    /// Should be set to true in order for the character to drop down a platform.
    /// </param>
    public void Move(Vector3 velocity, bool ignoreOneWayPlatforms = false)
    {
        // set new points to fire raycasts from
        UpdateRaycastOrigins();

        // reset collision state
        collisionState.Reset();
        collisionState.initialVelocity = velocity;
        collisionState.ignoreOneWayPlatforms = ignoreOneWayPlatforms;

        // if we're moving horizontally, update character direction
        if (velocity.x != 0f)
        {
            collisionState.horizontalDir = (int)Mathf.Sign(velocity.x);
        }

        // only check for descending slopes when moving in -y
        if (velocity.y < 0f)
        {
            DescendSlope(ref velocity);
        }

        // for static collisions (eg: sliding against a wall), we'll always check horizonally
        HorizontalCollisions(ref velocity);

        // only check vertically if we're moving in y
        if (velocity.y != 0f)
        {
            VerticalCollisions(ref velocity);
        }

        // finally, update our position according to the new velocity
        transform.Translate(velocity);
    }

    /// <summary>
    /// Checks for collisions in the horizontal axis and adjusts velocity as needed. This function is also
    /// responsible for the detection of ascending slopes, as they will first be caught as obstacles in 
    /// the horizontal axis.
    /// </summary>
    private void HorizontalCollisions(ref Vector3 velocity)
    {
        float rayDir = collisionState.horizontalDir;
        float rayLength = Mathf.Abs(velocity.x) + skinWidth;
        Vector2 rayOrigin = (rayDir == -1f) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;

        // for static collisions where velocity is near zero (or actually zero), we'll manually 
        // set the rayLength so that it detects nearby objects. 
        // eg: detecting a wall when directly touching it and proceeding to jump with no horizontal movement
        if (Mathf.Abs(velocity.x) < skinWidth)
        {
            rayLength = 2 * skinWidth;
        }

        // fire raycasts and adjust velocity whenever an object is hit
        for (int i = 0; i < horizontalRayCount; i++)
        {
            Vector2 rayPos = rayOrigin + Vector2.up * (horizontalRaySpacing * i);
            RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.right * rayDir, rayLength, collisionMask);

            Debug.DrawRay(rayPos, Vector2.right * rayDir * rayLength, rayColor);

            if (hit)
            {
                float surfaceAngle = Vector2.Angle(hit.normal, Vector2.up);

                // only check for ascending slopes on the bottom ray
                if (i == 0 && surfaceAngle <= maxAscentAngle)
                {
                    AscendSlope(ref velocity, rayDir, surfaceAngle, hit.distance);
                }

                // when meeting an obstacle that is not a valid slope, we need to clamp velocity
                if (!collisionState.ascendingSlope || surfaceAngle > maxAscentAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * rayDir;
                    rayLength = hit.distance;

                    // when ascending a slope and meeting an object in the horizontal axis
                    if (collisionState.ascendingSlope)
                    {
                        velocity.y = Mathf.Tan(collisionState.slopeAngle * Mathf.Deg2Rad * Mathf.Abs(velocity.x));
                    }

                    collisionState.left = rayDir == -1f;
                    collisionState.right = rayDir == 1f;
                }
            }
        }
    }


    /// <summary>
    /// Checks for collisions in the vertical axis and adjusts velocity as needed. 
    /// </summary>
    private void VerticalCollisions(ref Vector3 velocity)
    {
        float rayDir = Mathf.Sign(velocity.y);
        float rayLength = Mathf.Abs(velocity.y) + skinWidth;
        Vector2 rayOrigin = (rayDir == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;

        // fire raycasts and adjust velocity whenever an object is hit
        for (int i = 0; i < verticalRayCount; i++)
        {
            Vector2 rayPos = rayOrigin + Vector2.right * (verticalRaySpacing * i + velocity.x);
            RaycastHit2D hit = Physics2D.Raycast(rayPos, Vector2.up * rayDir, rayLength, verticalCollisionMask);

            Debug.DrawRay(rayPos, Vector2.up * rayDir * rayLength, rayColor);

            if (hit)
            {
                // if colliding with a oneWayPlatform under certain conditions, we can ignore it
                if (SkipOneWayPlatform(rayDir, hit))
                {
                    continue;
                }

                // reset falling-through state if the obstacle should not be ignored
                collisionState.throughPlatform = hit.collider;
                collisionState.fallingThroughPlatform = false;

                velocity.y = (hit.distance - skinWidth) * rayDir;
                rayLength = hit.distance;

                // when ascending a slope and meeting an object in the vertical axis
                if (collisionState.ascendingSlope)
                {
                    velocity.x = velocity.y / Mathf.Tan(collisionState.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(velocity.x);
                }

                collisionState.below = rayDir == -1f;
                collisionState.above = rayDir == 1f;
            }
        }

        // when the velocity set to ascend a given slope is too large, we might move into (inside) 
        // a slope with a steeper angle. as a safety check, we'll fire another raycast and, if necessary,
        // re-adjust the velocity to properly climb the new slope
        if (collisionState.ascendingSlope)
        {
            rayLength = Mathf.Abs(velocity.x) + skinWidth;
            rayDir = Mathf.Sign(velocity.x); // horizontal this time, not vertical

            rayOrigin = ((rayDir == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * velocity.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * rayDir, rayLength, collisionMask);

            if (hit)
            {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                if (slopeAngle != collisionState.slopeAngle)
                {
                    velocity.x = (hit.distance - skinWidth) * rayDir;
                    collisionState.slopeAngle = slopeAngle;
                }
            }
        }
    }

    /// <summary>
    ///  Distributes overall horizontal velocity in both horizontal and vertical axis,
    ///  so as to make up for the slope.
    /// </summary>
    private void AscendSlope(ref Vector3 velocity, float dir, float surfaceAngle, float surfaceDistance)
    {
        // if descending a slope and meeting an ascending one in the direction we're going,
        // update state and reset velocity (as descending slopes are checked for and velocity is adjusted
        // in a previous step)
        if (collisionState.descendingSlope)
        {
            collisionState.descendingSlope = false;
            velocity = collisionState.initialVelocity;
        }

        float slopeDistance = 0f;
        float moveDistance = Mathf.Abs(velocity.x);
        float verticalAscendingVelocity = Mathf.Sin(surfaceAngle * Mathf.Deg2Rad) * moveDistance;

        // to ensure we don't start ascending before actually reaching a new slope, 
        // we'll temporary remove the excess velocity, attempt to ascend, and then re-add the excess
        if (surfaceAngle != collisionState.slopeAnglePrev)
        {
            slopeDistance = surfaceDistance - skinWidth;
            velocity.x -= slopeDistance * dir;
        }

        // only adjust velocity if not jumping
        if (velocity.y <= verticalAscendingVelocity)
        {
            velocity.y = verticalAscendingVelocity;
            velocity.x = Mathf.Cos(surfaceAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);

            collisionState.below = true;
            collisionState.ascendingSlope = true;
            collisionState.slopeAngle = surfaceAngle;
        }

        // after ascending, we can re-add our excess
        velocity.x += slopeDistance * dir; 
    }


    /// <summary>
    ///  Distributes overall horizontal velocity in both horizontal and vertical axis,
    ///  so as to make up for the slope.
    /// </summary>
    private void DescendSlope(ref Vector3 velocity)
    {
        float rayDir = Mathf.Sign(velocity.x);
        Vector2 rayOrigin = (rayDir == -1 ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

        // fire a single ray, vertically, from the corner associated with the current horizontal velocity
        if (hit)
        {
            float surfaceAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (surfaceAngle != 0f && surfaceAngle <= maxDescentAngle)
            {
                if (Mathf.Sign(hit.normal.x) == rayDir)
                {
                    if (hit.distance - skinWidth <= Mathf.Tan(surfaceAngle * Mathf.Deg2Rad) * Mathf.Abs(velocity.x))
                    {
                        float moveDistance = Mathf.Abs(velocity.x);
                        float descendVelocity = Mathf.Sin(surfaceAngle * Mathf.Deg2Rad) * moveDistance;

                        velocity.x = Mathf.Cos(surfaceAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(velocity.x);
                        velocity.y -= descendVelocity;

                        collisionState.below = true;
                        collisionState.descendingSlope = true;
                        collisionState.slopeAngle = surfaceAngle;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks whether a given obstacle is a oneWayPlatform and should be ignored.
    /// </summary>
    private bool SkipOneWayPlatform(float dir, RaycastHit2D hit)
    {
        int layer = hit.collider.gameObject.layer;

        // first, check whether the obstacle's layer is included in the mask
        if (oneWayPlatformsMask == (oneWayPlatformsMask | 1 << layer))
        {
            // ignore when vertical velocity is positive (eg, going up, jumping)
            if (dir == 1f || hit.distance == 0f)
            {
                return true;
            }

            // ignore while we're falling through a specific platform
            if (collisionState.fallingThroughPlatform && collisionState.throughPlatform == hit.collider)
            {
                return true;
            }

            // ignore during this frame, set falling state, and save the platform for future checks
            if (collisionState.ignoreOneWayPlatforms)
            {
                collisionState.fallingThroughPlatform = true;
                collisionState.throughPlatform = hit.collider;
                return true;
            }
        }

        return false;
    }
}