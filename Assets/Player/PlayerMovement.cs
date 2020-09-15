using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using UnityEngine;

public class PlayerMovement : PortalTraveler {
    public CharacterController controller;

    [Header("Movement Settings")]
    public float speed = 12f;
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;

    [SerializeField]
    Vector3 velocity;
    bool isGrounded;

    [Header("Jump Settings")]
    public float gravity = -9.81f;
    public float jumpHeight = 3.0f;
    int jumpCount = 0;
    public int maxJumpCount = 2;

    [Header("Wallrun Settings")]
    public float maxTiltAngle = 30.0f;
    [SerializeField]
    private bool isWallRunning;
    private Vector3 lastRunNormal;

    private CharacterController characterController;

    private void Start() {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update() {
        if (isGrounded) {
            jumpCount = 0;

            // Set a slight down motion to make sure player settles on the ground
            // (Size of ground detection sphere may leave us slightly off the ground at unexpected heights)
            // Feels hacky, see about replacing
            if (velocity.y < 0) {
                velocity.y = -2f;
            }
        }

        HandleMovement();

        // After movement applied, check if we're grounded
        isGrounded = IsGrounded();
    }

    private void HandleMovement() {
        // Handle wallrunning
        RunnableWall runnable = CheckRunnableWall();
        isWallRunning = runnable.GetRunnableSide() != RunnableSide.None && !isGrounded;

        if (!isWallRunning) {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;
            controller.Move(move * speed * Time.deltaTime);
        } else {
            jumpCount = 0;
            Vector3 runDir = runnable.GetSurfaceDirection();

            // The surface direction we get is parallel to the surface, but may be in teh wrong direction.
            // Ensure it's facing the same direction as the player. If not, flip it
            float dirCheck = Vector3.Dot(transform.forward, runDir);
            if (dirCheck < 0) {
                runDir *= -1;
            }

            controller.Move(runDir * speed * Time.deltaTime);
        }

        if (Input.GetButtonDown("Jump") && (isGrounded || jumpCount < maxJumpCount || isWallRunning)) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
            jumpCount++;

            if (isWallRunning) {
                isWallRunning = false;
                velocity += runnable.GetJumpAwayDirection() * 10f;
            }
        }

        // Take gravity in to account
        // Have to multiply by Time.deltaTime twice as acceleration is m/s^2
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Decelerate horizontal velocity
        velocity.x = Mathf.Lerp(velocity.x, 0.0f, 0.5f);
        velocity.z = Mathf.Lerp(velocity.z, 0.0f, 0.5f);
    }

    private RunnableWall CheckRunnableWall() {
        Ray leftRay = new Ray(transform.position, -transform.right);
        Ray rightRay = new Ray(transform.position, transform.right);

        RaycastHit leftHit;
        RaycastHit rightHit;

        float leftDistance = Mathf.Infinity;
        float rightDistance = Mathf.Infinity;

        // Collect info on walls to either side of the player
        // Dot product of the hit normal and "up" being 0 indicates the surface hit is vertical, and likely a wall
        // May expand upon later to allow slightly angled walls
        if (Physics.Raycast(leftRay, out leftHit)) {
            if (Vector3.Dot(leftHit.normal, Vector3.up) == 0f) {
                leftDistance = leftHit.distance;
            }
        }

        if (Physics.Raycast(rightRay, out rightHit)) {
            if (Vector3.Dot(rightHit.normal, Vector3.up) == 0f) {
                rightDistance = rightHit.distance;
            }
        }

        // Pick whichever wall is closer
        if (leftDistance <= 1.5f || rightDistance <= 1.5f) {
            if (leftDistance < rightDistance) {
                return new RunnableWall(RunnableSide.Left, Vector3.Cross(leftHit.normal, Vector3.up), leftHit.normal);
            } else {
                return new RunnableWall(RunnableSide.Right, Vector3.Cross(rightHit.normal, Vector3.up), rightHit.normal);
            }
        }
        // If neither wall is within wallrun distance, return None
        return new RunnableWall(RunnableSide.None);
    }

    private bool IsGrounded() {
        float extraHeight = 0.1f;
        return Physics.Raycast(characterController.bounds.center, Vector3.down, characterController.bounds.extents.y + extraHeight);
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot) {
        transform.position = pos;
        Vector3 eulerRot = rot.eulerAngles;
        float smoothYaw = transform.rotation.eulerAngles.y;
        float delta = Mathf.DeltaAngle(smoothYaw, eulerRot.y);
        transform.eulerAngles = Vector3.up * (smoothYaw + delta);
        velocity = toPortal.TransformVector(fromPortal.InverseTransformVector(velocity));
        Physics.SyncTransforms();
    }

    enum RunnableSide {
        Left,
        Right,
        None
    }

    private class RunnableWall {
        RunnableSide runnableSide;
        Vector3 surfaceDir;
        Vector3 jumpAwayDir;

        public RunnableWall(RunnableSide runnableSide) : this(runnableSide, Vector3.zero, Vector3.zero) { }

        public RunnableWall(RunnableSide runnableSide, Vector3 surfaceDir, Vector3 jumpAwayDir) {
            this.runnableSide = runnableSide;
            this.surfaceDir = surfaceDir;
            this.jumpAwayDir = jumpAwayDir;
        }

        public RunnableSide GetRunnableSide() {
            return runnableSide;
        }

        public Vector3 GetSurfaceDirection() {
            return surfaceDir;
        }

        public Vector3 GetJumpAwayDirection() {
            return jumpAwayDir;
        }
    }
}
