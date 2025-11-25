using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Run Settings")]
    [Tooltip("Forward speed along +Z (world)")]
    public float runSpeed = 8f;

    [Tooltip("Gravity acceleration (negative)")]
    public float gravity = -30f;

    [Tooltip("Desired jump height in meters")]
    public float jumpHeight = 1.8f;

    [Header("Lane Settings")]
    [Tooltip("Number of lanes (prefer odd numbers like 3, 5)")]
    public int laneCount = 3;

    [Tooltip("Width between lane centers in meters")]
    public float laneWidth = 2.5f;

    [Tooltip("How quickly we lerp sideways to target lane")]
    public float laneChangeSpeed = 14f;

    [Header("Slide/Crouch")]
    [Tooltip("Time in seconds the slide lasts")]
    public float slideDuration = 0.7f;

    [Tooltip("CharacterController height while sliding")]
    public float slideHeight = 0.375f;
    [Tooltip("CharacterController radius while sliding")]
    public float slideRadius = 0.3f;

    [Tooltip("Cooldown after sliding before another slide")]
    public float slideCooldown = 0.1f;

    [Header("Grounding")]
    [Tooltip("Extra downward force when grounded to keep you stuck to ground")]
    public float stickToGroundForce = -3f;

    [Header("Input")]
    [Tooltip("Minimum swipe distance in pixels to register")]
    public float swipeThreshold = 60f;

    [Tooltip("Max time in seconds for a quick swipe")]
    public float swipeMaxTime = 0.6f;

    [Header("Optional Animation")]
    public Animator animator; // set if you have animations
    public string animJumpTrigger = "Jump";
    public string animSlideTrigger = "Slide";
    public string animLaneChangeTrigger = "Lane"; // optional
    [Tooltip("Animator bool updated every frame with controller.isGrounded; leave empty to ignore")] public string animGroundedBool = "Grounded";
    [Tooltip("Animator bool set true on slide start and false on slide end; leave empty to ignore")] public string animSlidingBool = "Sliding";
    [Header("Debug")]
    public bool debugLogs = true;

    private CharacterController controller;
    private float verticalVelocity; // Y velocity for gravity/jump

    private int currentLaneIndex = 0; // 0=center; negatives left, positives right
    private int minLaneIndex;
    private int maxLaneIndex;
    private float _startZ; // initial Z position for distance tracking

    public float DistanceRun => transform.position.z - _startZ;

    // Slide state
    private bool isSliding = false;
    private float slideTimer = 1f;
    private float slideCooldownTimer = 0f;
    private float originalHeight;
    private float originalRadius;
    private Vector3 originalCenter;

    // Swipe detection
    private bool trackingSwipe = false;
    private Vector2 startPos;
    private double startTime;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponent<Animator>();
        // Enforce standard standing height and align capsule bottom to ground
        controller.center = new Vector3(controller.center.x, controller.height * 0.5f, controller.center.z);
        originalHeight = controller.height;
        originalRadius = controller.radius;
        originalCenter = controller.center;
        _startZ = transform.position.z;

        // compute lane bounds (centered around 0)
        int halfSpan = (laneCount - 1) / 2; // for 3 lanes => 1
        minLaneIndex = -halfSpan;
        maxLaneIndex = halfSpan;
        currentLaneIndex = Mathf.Clamp(currentLaneIndex, minLaneIndex, maxLaneIndex);
    }

    private void OnValidate()
    {
        // Auto-cache Animator in editor if not set
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleSwipeInput();
        HandleKeyboardFallbacks();
        HandleMovement(Time.deltaTime);
    }

    private void HandleSwipeInput()
    {
        // Touch input
        if (Touchscreen.current != null && Application.isFocused)
        {
            var touch = Touchscreen.current.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                trackingSwipe = true;
                startPos = touch.position.ReadValue();
                startTime = Time.timeAsDouble;
            }
            if (touch.press.wasReleasedThisFrame && trackingSwipe)
            {
                var endPos = touch.position.ReadValue();
                var dt = (float)(Time.timeAsDouble - startTime);
                EvaluateSwipe(startPos, endPos, dt);
                trackingSwipe = false;
            }
        }

        // Mouse drag as swipe (for editor/desktop)
        if (Mouse.current != null && Application.isFocused)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                trackingSwipe = true;
                startPos = Mouse.current.position.ReadValue();
                startTime = Time.timeAsDouble;
            }
            if (Mouse.current.leftButton.wasReleasedThisFrame && trackingSwipe)
            {
                var endPos = Mouse.current.position.ReadValue();
                var dt = (float)(Time.timeAsDouble - startTime);
                EvaluateSwipe(startPos, endPos, dt);
                trackingSwipe = false;
            }
        }
    }

    private void EvaluateSwipe(Vector2 start, Vector2 end, float deltaTime)
    {
        var delta = end - start;
        if (deltaTime > swipeMaxTime) return;
        if (delta.magnitude < swipeThreshold) return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0f) ChangeLane(+1);
            else ChangeLane(-1);
        }
        else
        {
            if (delta.y > 0f) TryJump();
            else TrySlide();
        }
    }

    private void HandleKeyboardFallbacks()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame)
                ChangeLane(-1);
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame)
                ChangeLane(+1);
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame)
                TryJump();
            if (Keyboard.current.leftCtrlKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame)
                TrySlide();
        }
    }

    private void ChangeLane(int dir)
    {
        int target = Mathf.Clamp(currentLaneIndex + dir, minLaneIndex, maxLaneIndex);
        if (target != currentLaneIndex)
        {
            currentLaneIndex = target;
            if (animator && !string.IsNullOrEmpty(animLaneChangeTrigger)) animator.SetTrigger(animLaneChangeTrigger);
        }
    }

    private void TryJump()
    {
        if (controller.isGrounded && !isSliding)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (debugLogs) Debug.Log($"[PlayerMovement] Jump v0={verticalVelocity:F2}, grounded={controller.isGrounded}");
            if (animator && !string.IsNullOrEmpty(animJumpTrigger)) animator.SetTrigger(animJumpTrigger);
        }
    }

    private void TrySlide()
    {
        if (!isSliding && slideCooldownTimer <= 0f)
        {
            StartSlide();
        }
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        slideCooldownTimer = slideDuration + slideCooldown;
        // Adjust controller height/center
        controller.height = slideHeight;
        controller.center = new Vector3(originalCenter.x, slideHeight, originalCenter.z);
        controller.radius = slideRadius;
        if (debugLogs) Debug.Log($"[PlayerMovement] Slide start, duration={slideDuration:F2}s, height={slideHeight:F2}");
        if (animator && !string.IsNullOrEmpty(animSlideTrigger)) animator.SetTrigger(animSlideTrigger);
        if (animator && !string.IsNullOrEmpty(animSlidingBool)) animator.SetBool(animSlidingBool, true);
    }

    private void EndSlide()
    {
        isSliding = false;
        controller.height = originalHeight;
        controller.center = originalCenter;
        controller.radius = originalRadius;

        if (debugLogs) Debug.Log("[PlayerMovement] Slide end");
        if (animator && !string.IsNullOrEmpty(animSlidingBool)) animator.SetBool(animSlidingBool, false);
    }

    private void HandleMovement(float dt)
    {
        // Compute target lane X position relative to starting X (assume forward along +Z)
        float targetX = currentLaneIndex * laneWidth;
        var pos = transform.position;
        float newX = Mathf.MoveTowards(pos.x, targetX, laneChangeSpeed * dt);

        // Forward constant motion
        float forward = runSpeed;

        // Gravity
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = stickToGroundForce; // small downward to keep grounded
        }
        verticalVelocity += gravity * dt;

        // Slide timers
        if (isSliding)
        {
            slideTimer -= dt;
            if (slideTimer <= 0f)
            {
                EndSlide();
            }
        }
        if (slideCooldownTimer > 0f) slideCooldownTimer -= dt;

        // Compose motion vector in world space
        Vector3 move = new Vector3(newX - pos.x, 0f, 0f); // lateral correction this frame
        move += Vector3.forward * forward * dt;
        move.y = 0f;

        // Apply lateral + forward as horizontal component via SimpleMove-like behavior
        controller.Move(move);

        // Apply vertical separately
        controller.Move(new Vector3(0f, verticalVelocity * dt, 0f));

        // Animator bool maintenance (prevents sticky states when Animator expects bools)
        if (animator)
        {
            if (!string.IsNullOrEmpty(animGroundedBool)) animator.SetBool(animGroundedBool, controller.isGrounded);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Draw lane centers ahead of player for visualization
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        int halfSpan = (laneCount - 1) / 2;
        for (int i = -halfSpan; i <= halfSpan; i++)
        {
            Vector3 p = transform.position;
            p.x = i * laneWidth;
            Gizmos.DrawLine(p + Vector3.forward * -1f, p + Vector3.forward * 4f);
        }
    }
}
