using UnityEngine;
using Spine.Unity;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    public SkeletonAnimation skeletonAnimation;
    private Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed = 15f;
    public float jumpForce = 25f;
    public string animationFolder = "1_";

    [Header("Ground Check Settings")]
    public string groundTag = "Floor";
    private bool isGrounded = true;

    private string currentAnimation = "";
    private bool isAttacking = false;
    private bool isBlocking = false;
    private float lastMoveInput = 0f; // Merkt sich die Richtung vor dem Sprung

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (skeletonAnimation != null) skeletonAnimation.Skeleton.ScaleX = 1;

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        // 1. Block-Input (S-Taste halten)
        if (Input.GetKey(KeyCode.S) && isGrounded)
        {
            isBlocking = true;
            PlaySpineAnimation("block bottom", false);
        }
        else
        {
            isBlocking = false;
        }

        // Wenn wir angreifen oder blocken, bewegen wir uns nicht
        if (isAttacking || isBlocking) return;

        // 2. Tekken Angriffe (Pfeiltasten)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            StartCoroutine(AttackRoutine("jab single", 0.25f));
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StartCoroutine(AttackRoutine("jab double", 0.3f));
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            StartCoroutine(AttackRoutine("kick high", 0.35f));
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            StartCoroutine(AttackRoutine("kick low", 0.3f));
            return;
        }

        // 3. WASD Laufen
        float moveInput = 0f;
        float currentSpeed = moveSpeed;

        if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f;
            currentSpeed = moveSpeed;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = -1f;
            currentSpeed = moveSpeed * 0.5f;
        }

        // Richtung merken, solange wir am Boden sind
        if (isGrounded)
        {
            lastMoveInput = moveInput;
        }

        transform.Translate(new Vector3(moveInput * currentSpeed * Time.deltaTime, 0, 0));

        if (skeletonAnimation != null) skeletonAnimation.Skeleton.ScaleX = 1;

        // 4. WASD Sprung (W = Springen)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            PlaySpineAnimation("jump A", false);
            isGrounded = false;
        }

        // 5. Animations-Logik für Movement am Boden & in der Luft
        if (isGrounded)
        {
            if (moveInput > 0)
            {
                PlaySpineAnimation("run", true);
            }
            else if (moveInput < 0)
            {
                PlaySpineAnimation("walk normal", true);
            }
            else
            {
                PlaySpineAnimation("idle active", true);
            }
        }
        else
        {
            // Luft-Logik mit vertauschten Animationen beim Rückwärtsspringen
            if (rb != null)
            {
                if (lastMoveInput < 0) // RÜCKWÄRTS-SPRUNG (Animationen umgedreht! 🥀)
                {
                    if (rb.velocity.y > 0.1f)
                    {
                        PlaySpineAnimation("jump C", true); // C zuerst beim Hochfliegen
                    }
                    else if (rb.velocity.y < -0.1f)
                    {
                        PlaySpineAnimation("jump B", true); // B als zweites beim Runterfallen
                    }
                }
                else // VORWÄRTS- oder VERTIKAL-SPRUNG (Normal)
                {
                    if (rb.velocity.y > 0.1f)
                    {
                        PlaySpineAnimation("jump B", true); // B zuerst
                    }
                    else if (rb.velocity.y < -0.1f)
                    {
                        PlaySpineAnimation("jump C", true); // C als zweites
                    }
                }
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.name.ToLower().Contains("floor") || collision.gameObject.name.ToLower().Contains("boden"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.name.ToLower().Contains("floor") || collision.gameObject.name.ToLower().Contains("boden"))
        {
            if (rb != null && rb.velocity.y > 0.1f)
            {
                isGrounded = false;
            }
        }
    }

    System.Collections.IEnumerator AttackRoutine(string animName, float duration)
    {
        isAttacking = true;
        PlaySpineAnimation(animName, false);
        yield return new WaitForSeconds(duration);
        isAttacking = false;
    }

    void PlaySpineAnimation(string animName, bool loop)
    {
        string fullPath = animationFolder + "/" + animName;

        if (currentAnimation == fullPath) return;

        skeletonAnimation.AnimationState.SetAnimation(0, fullPath, loop);
        currentAnimation = fullPath;
    }
}