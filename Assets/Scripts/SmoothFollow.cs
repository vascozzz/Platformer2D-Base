using UnityEngine;
using System.Collections;

/// <summary>
/// Smooth camera follow based on a focus area. Camera only moves when the target escapes its focus area.
/// Could be updated to follow a simple Transform, but we would need to introduce an extra parameter for the target's size. 
/// </summary>
public class SmoothFollow : MonoBehaviour
{
    [SerializeField] private Collider2D target;
    [SerializeField] private float lookAhead;
    [SerializeField] private float hSmoothDelay;
    [SerializeField] private float vOffset;
    [SerializeField] private float vSmoohTime;
    [SerializeField] private Vector2 focusAreaSize;

    private Color areaColor = new Color(1f, 0f, 0f, 0f);

    private FocusArea focusArea;
    private float camOffset;
    private float currentLookAhead;
    private float targetLookAhead;
    private float lookAheadDir; 
    private float lookAheadSmoothing;

    struct FocusArea
    {
        public Vector2 velocity;
        public Vector2 center;
        public float left;
        public float right;
        public float top;
        public float bottom;

        public FocusArea(Bounds targetBounds, Vector2 size)
        {
            // left and right in x coords
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;

            // top and bottom in y coords
            // should be noted that bottom is always collider bottom, regardless of size
            bottom = targetBounds.min.y; 
            top = targetBounds.min.y + size.y;

            // set new center
            center = new Vector2((left + right) / 2, (top + bottom) / 2);

            // reset velocity
            velocity = Vector2.zero;
        }

        public void Update(Bounds targetBounds)
        {
            float hShift = 0f;
            float vShift = 0f;

            // check if target has moved outside focus area
            if (targetBounds.min.x < left)
            {
                hShift = targetBounds.min.x - left;
            }
            else if (targetBounds.max.x > right)
            {
                hShift = targetBounds.max.x - right;
            }

            if (targetBounds.min.y < bottom)
            {
                vShift = targetBounds.min.y - bottom;
            }
            else if (targetBounds.max.y > top)
            {
                vShift = targetBounds.max.y - top;
            }

            // update focus area
            left += hShift;
            right += hShift;
            bottom += vShift;
            top += vShift;

            center = new Vector2((left + right) / 2, (top + bottom) / 2);

            // set current velocity
            velocity = new Vector2(hShift, vShift);
        }
    }

    void Start()
    {
        // save initial offset, as it should be maintained
        camOffset = transform.position.z;

        // initiate focus area
        focusArea = new FocusArea(target.bounds, focusAreaSize);
    }

    void LateUpdate()
    {
        // update focus area position
        focusArea.Update(target.bounds);

        // get lookahead based on the current velocity
        if (focusArea.velocity.x != 0f)
        {
            lookAheadDir = Mathf.Sign(focusArea.velocity.x);
        }

        targetLookAhead = lookAheadDir * lookAhead;
        currentLookAhead = Mathf.SmoothDamp(currentLookAhead, targetLookAhead, ref lookAheadSmoothing, hSmoothDelay);

        // update focus area position by lookahead and vertical offset
        Vector2 focusPosition = focusArea.center + Vector2.up * vOffset;
        focusPosition += Vector2.right * currentLookAhead;

        // update camera position
        transform.position = (Vector3)focusPosition + Vector3.forward * camOffset;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = areaColor;
        Gizmos.DrawCube(focusArea.center, focusAreaSize);
    }
}