using UnityEngine;
using Spine.Unity;

[RequireComponent(typeof(SkeletonAnimation))]
[RequireComponent(typeof(Rigidbody2D))] // Stellt sicher, dass ein Rigidbody da ist
public class SpineCharacterController : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;
    private Rigidbody2D rb;

    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 6f;
    public float jumpForce = 10f; // Etwas erhöht, um die stärkere Schwerkraft auszugleichen
    public float gravityScale = 2.5f; // Höherer Wert = Schnellere Anziehungskraft / Fällt schneller

    [Header("Animation Names")]
    [SpineAnimation] public string idleAnim = "1_/idle";
    [SpineAnimation] public string walkAnim = "1_/walk normal";
    [SpineAnimation] public string runAnim = "1_/run";
    [SpineAnimation] public string runStopAnim = "1_/run stop";

    [Header("Jump Sequence")]
    [SpineAnimation] public string jumpA = "1_/jump A";
    [SpineAnimation] public string jumpB = "1_/jump B";
    [SpineAnimation] public string jumpC = "1_/jump C";

    private string currentAnimation;
    private bool isJumping = false;
    private bool wasRunning = false;

    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        rb = GetComponent<Rigidbody2D>();

        // Stellt die Anziehungskraft für den Rigidbody ein
        rb.gravityScale = gravityScale;

        SetAnimation(idleAnim, true);
    }

    void Update()
    {
        // Falls du den Wert im Inspector während des Spiels anpasst:
        rb.gravityScale = gravityScale;

        // 1. Input abfragen
        float moveInput = Input.GetAxisRaw("Horizontal");
        bool isMoving = moveInput != 0;
        bool isRunning = isMoving && Input.GetKey(KeyCode.LeftShift);
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

        // 2. Physische Bewegung (Rigidbody)
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        rb.velocity = new Vector2(moveInput * currentSpeed, rb.velocity.y);

        // 3. Charakter in die richtige Richtung drehen (Flip)
        if (moveInput > 0)
        {
            skeletonAnimation.skeleton.ScaleX = 1;
        }
        else if (moveInput < 0)
        {
            skeletonAnimation.skeleton.ScaleX = -1;
        }

        // 4. Sprung-Logik (Physisch + Animation)
        if (jumpPressed && !isJumping)
        {
            isJumping = true;
            wasRunning = false;

            // Physischer Sprung nach oben
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);

            // Sprung-Animationen anreihen (A -> B -> C)
            skeletonAnimation.AnimationState.SetAnimation(0, jumpA, false);
            skeletonAnimation.AnimationState.AddAnimation(0, jumpB, false, 0f);
            skeletonAnimation.AnimationState.AddAnimation(0, jumpC, true, 0f);

            currentAnimation = "jumping";
        }

        // 5. Boden-Animationen (nur wenn wir nicht springen)
        else if (!isJumping)
        {
            if (isMoving)
            {
                if (isRunning)
                {
                    SetAnimation(runAnim, true);
                    wasRunning = true;
                }
                else
                {
                    SetAnimation(walkAnim, true);
                    wasRunning = false;
                }
            }
            else // Keine Bewegungstasten gedrückt
            {
                if (wasRunning)
                {
                    skeletonAnimation.AnimationState.SetAnimation(0, runStopAnim, false);
                    skeletonAnimation.AnimationState.AddAnimation(0, idleAnim, true, 0f);

                    wasRunning = false;
                    currentAnimation = runStopAnim;
                }
                else
                {
                    var currentTrack = skeletonAnimation.AnimationState.GetCurrent(0);
                    if (currentTrack != null && currentTrack.Animation.Name == runStopAnim)
                    {
                        currentAnimation = idleAnim;
                    }
                    else
                    {
                        SetAnimation(idleAnim, true);
                    }
                }
            }
        }
    }

    private void SetAnimation(string animName, bool loop)
    {
        if (animName == currentAnimation) return;

        skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
        currentAnimation = animName;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Sobald der Charakter den Boden (Tag "Floor") berührt, wird der Sprung beendet
        if (collision.gameObject.CompareTag("Floor"))
        {
            if (isJumping)
            {
                isJumping = false;
                SetAnimation(idleAnim, true);
            }
        }
    }
}