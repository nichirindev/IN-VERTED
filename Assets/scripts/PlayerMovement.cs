using UnityEngine;

/// <summary>
/// Physics-based first-person player controller with sliding, counter-movement,
/// and air control. Uses Rigidbody forces for all movement.
///
/// Required: Rigidbody (useGravity=true, interpolation=Interpolate), CapsuleCollider.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    public Transform playerCam;
    public Transform orientation;
    public LayerMask whatIsGround;

    [Header("State")]
    public bool grounded;
    public bool dead;
    public bool secondJump = true;
    public int jumpsLeft = 1;
    public int maxJumps = 1;

    [Header("Movement")]
    private float moveSpeed = 1500f;
    private float maxSpeed = 22f;
    private float slideForce = 800f;
    private float slideCounterMovement = 0.12f;
    private float counterMovement = 0.14f;
    private float threshold = 0.01f;
    private float maxSlopeAngle = 35f;
    private float jumpForce = 13f;
    private int jumpCounterResetTime = 10;
    private readonly Vector3 crouchScale = new(1f, 0.5f, 1f);

    private Rigidbody rb;
    private Collider playerCollider;
    private Vector3 playerScale;
    private float playerHeight;
    private float x, y;
    private float fallSpeed;
    private Vector3 lastMoveSpeed;
    private Vector3 normalVector;
    private bool onRamp;
    private bool jumping;
    private bool sliding;
    private bool crouching;
    private bool cancellingGrounded;
    private bool readyToJump = true;
    private float delay = 5f;
    private int groundCancel;
    private int readyToCounterX;
    private int readyToCounterY;
    private int resetJumpCounter;

    public static PlayerMovement Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        rb = GetComponent<Rigidbody>();
        playerHeight = GetComponent<CapsuleCollider>().bounds.size.y;
    }

    private void Start()
    {
        playerScale = transform.localScale;
        playerCollider = GetComponent<Collider>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (dead) return;
        fallSpeed = rb.velocity.y;
        lastMoveSpeed = rb.velocity.XZVector();
    }

    public void SetInput(Vector2 dir, bool crouching, bool jumping)
    {
        x = dir.x;
        y = dir.y;
        this.crouching = crouching;
        this.jumping = jumping;
    }

    private void CheckInput()
    {
        if (crouching && !sliding) StartCrouch();
        if (!crouching && sliding) StopCrouch();
    }

    public void Movement(float x, float y)
    {
        UpdateCollisionChecks();
        this.x = x;
        this.y = y;
        if (dead) return;

        CheckInput();

        if (!grounded)
            rb.AddForce(Vector3.down * 2f);

        Vector2 vel = FindVelRelativeToLook();
        CounterMovement(x, y, vel);
        RampMovement(vel);

        if (readyToJump && jumping)
            Jump();

        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(Vector3.down * 60f);
        }
        else
        {
            float inputX = x;
            float inputY = y;

            if (x > 0 && vel.x > maxSpeed) inputX = 0f;
            if (x < 0 && vel.x < -maxSpeed) inputX = 0f;
            if (y > 0 && vel.y > maxSpeed) inputY = 0f;
            if (y < 0 && vel.y < -maxSpeed) inputY = 0f;

            float airX = 1f;
            float airY = 1f;

            if (!grounded)
            {
                airX = 0.6f;
                airY = 0.6f;
                if (IsHoldingAgainstVerticalVel(vel))
                {
                    float f = Mathf.Abs(vel.y * 0.025f);
                    if (f < 0.5f) f = 0.5f;
                    airY = Mathf.Abs(f);
                }
            }
            if (grounded && crouching) airY = 0f;

            float tiny = 0.01f;
            rb.AddForce(orientation.forward * inputY * moveSpeed * 0.02f * airY);
            rb.AddForce(orientation.right * inputX * moveSpeed * 0.02f * airX);

            if (!grounded)
            {
                if (inputX != 0)
                    rb.AddForce(-orientation.forward * vel.y * moveSpeed * 0.02f * tiny);
                if (inputY != 0)
                    rb.AddForce(-orientation.right * vel.x * moveSpeed * 0.02f * tiny);
            }

            if (!readyToJump)
            {
                resetJumpCounter++;
                if (resetJumpCounter >= jumpCounterResetTime) readyToJump = true;
            }
        }
    }

    public void StartCrouch()
    {
        if (sliding) return;
        sliding = true;
        transform.localScale = crouchScale;
        float crouchOffset = playerHeight * (1f - crouchScale.y) * 0.5f;
        transform.position = new Vector3(transform.position.x, transform.position.y - crouchOffset, transform.position.z);
        if (rb.velocity.magnitude > 0.5f && grounded)
        {
            rb.AddForce(orientation.forward * slideForce);
        }
    }

    public void StopCrouch()
    {
        sliding = false;
        float crouchOffset = playerHeight * (1f - crouchScale.y) * 0.5f;
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + crouchOffset, transform.position.z);
    }

    public void Jump()
    {
        if (!grounded && jumpsLeft <= 0) return;
        if (!readyToJump || !secondJump) return;

        readyToJump = false;
        jumpsLeft--;
        resetJumpCounter = 0;
        secondJump = false;

        rb.AddForce(Vector2.up * jumpForce * 1.5f, ForceMode.Impulse);
        rb.AddForce(normalVector * jumpForce * 0.5f, ForceMode.Impulse);

        if (rb.velocity.y < 0.5f || rb.velocity.y > 0f)
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }

    private void CounterMovement(float x, float y, Vector2 mag)
    {
        if (!grounded || jumping) return;

        if (crouching)
        {
            rb.AddForce(moveSpeed * 0.02f * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        if (Mathf.Abs(mag.x) > threshold && Mathf.Abs(x) < 0.05f && readyToCounterX > 1)
            rb.AddForce(moveSpeed * orientation.right * 0.02f * -mag.x * counterMovement);
        if (Mathf.Abs(mag.y) > threshold && Mathf.Abs(y) < 0.05f && readyToCounterY > 1)
            rb.AddForce(moveSpeed * orientation.forward * 0.02f * -mag.y * counterMovement);

        if (IsHoldingAgainstHorizontalVel(mag))
            rb.AddForce(moveSpeed * orientation.right * 0.02f * -mag.x * counterMovement * 2f);
        if (IsHoldingAgainstVerticalVel(mag))
            rb.AddForce(moveSpeed * orientation.forward * 0.02f * -mag.y * counterMovement * 2f);

        float flatSpeed = Mathf.Sqrt(rb.velocity.x * rb.velocity.x + rb.velocity.z * rb.velocity.z);
        if (flatSpeed > maxSpeed)
        {
            float yVel = rb.velocity.y;
            Vector3 capped = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(capped.x, yVel, capped.z);
        }

        readyToCounterX = Mathf.Abs(x) < 0.05f ? readyToCounterX + 1 : 0;
        readyToCounterY = Mathf.Abs(y) < 0.05f ? readyToCounterY + 1 : 0;
    }

    private bool IsHoldingAgainstHorizontalVel(Vector2 vel)
    {
        if (vel.x < -threshold && x > 0) return true;
        if (vel.x > threshold && x < 0) return true;
        return false;
    }

    private bool IsHoldingAgainstVerticalVel(Vector2 vel)
    {
        if (vel.y < -threshold && y > 0) return true;
        if (vel.y > threshold && y < 0) return true;
        return false;
    }

    private void RampMovement(Vector2 mag)
    {
        if (grounded && onRamp && !crouching && !jumping
            && Mathf.Abs(x) < 0.05f && Mathf.Abs(y) < 0.05f)
        {
            rb.useGravity = false;
            if (rb.velocity.y > 0)
                rb.velocity = new Vector3(rb.velocity.x, 0f, 0f);
            else if (rb.velocity.y <= 0 && mag.magnitude < 1f)
                rb.velocity = Vector3.zero;
        }
        else
        {
            rb.useGravity = true;
        }
    }

    public Vector2 FindVelRelativeToLook()
    {
        float angle = Mathf.DeltaAngle(orientation.eulerAngles.y, Mathf.Atan2(rb.velocity.x, rb.velocity.z) * Mathf.Rad2Deg);
        float num = 90f - angle;
        float mag = new Vector2(rb.velocity.x, rb.velocity.z).magnitude;
        float y = mag * Mathf.Cos(angle * Mathf.Deg2Rad);
        return new Vector2(mag * Mathf.Cos(num * Mathf.Deg2Rad), y);
    }

    private bool IsFloor(Vector3 v) => Vector3.Angle(Vector3.up, v) < maxSlopeAngle;

    private void OnCollisionEnter(Collision other)
    {
        int layer = other.gameObject.layer;
        Vector3 normal = other.contacts[0].normal;
        if ((whatIsGround & (1 << layer)) == 0) return;

        if (IsFloor(normal))
        {
            jumpsLeft = maxJumps;
            secondJump = true;
            MoveCamera.Instance?.BobOnce(new Vector3(0f, fallSpeed, 0f));
        }
    }

    private void OnCollisionStay(Collision other)
    {
        if ((whatIsGround & (1 << other.gameObject.layer)) == 0) return;

        for (int i = 0; i < other.contactCount; i++)
        {
            Vector3 normal = other.contacts[i].normal;
            if (IsFloor(normal))
            {
                onRamp = Vector3.Angle(Vector3.up, normal) > 1f;
                grounded = true;
                normalVector = normal;
                cancellingGrounded = false;
                groundCancel = 0;
            }
        }
    }

    private void UpdateCollisionChecks()
    {
        if (!cancellingGrounded)
        {
            cancellingGrounded = true;
        }
        else
        {
            groundCancel++;
            if (groundCancel > delay)
                grounded = false;
        }
    }

    public Vector3 GetVelocity() => rb.velocity;
    public float GetFallSpeed() => rb.velocity.y;
    public Collider GetPlayerCollider() => playerCollider;
    public Transform GetPlayerCamTransform() => playerCam;
    public Rigidbody GetRb() => rb;
    public bool IsCrouching() => crouching;
    public bool IsDead() => dead;
}
