using UnityEngine;
using Spine.Unity;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviourPun
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
    private float lastMoveInput = 0f;
    private float moveInput = 0f;

    void Awake()
    {
        if (skeletonAnimation == null)
            skeletonAnimation = GetComponent<SkeletonAnimation>();

        if (skeletonAnimation != null)
        {
            skeletonAnimation.Initialize(true);
        }
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        if (skeletonAnimation != null) skeletonAnimation.Skeleton.ScaleX = 1;

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        // 1. Nur den eigenen Charakter steuern
        if (!photonView.IsMine) return;

        // 2. Nicht reagieren, wenn wir noch nicht voll gejoined sind
        if (!PhotonNetwork.InRoom) return;

        // Block-Input (S-Taste halten)
        if (Input.GetKey(KeyCode.S) && isGrounded)
        {
            if (!isBlocking)
            {
                isBlocking = true;
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "block bottom", false);
            }
        }
        else if (isBlocking)
        {
            isBlocking = false;
        }

        // Keine Bewegung/Angriffe, wenn am Blocken oder Angreifen
        if (isAttacking || isBlocking)
        {
            moveInput = 0f;
            return;
        }

        // Tekken Angriffe
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab single", 0.25f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab double", 0.3f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick high", 0.35f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick low", 0.3f);
            return;
        }

        // WASD Input
        moveInput = 0f;
        if (Input.GetKey(KeyCode.D)) moveInput = 1f;
        else if (Input.GetKey(KeyCode.A)) moveInput = -0.5f; // Rückwärts langsamer

        if (isGrounded) lastMoveInput = moveInput;

        // WASD Sprung (W)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump A", false);
            isGrounded = false;
        }

        // Animations-Logik
        UpdateMovementAnimations();
    }

    void FixedUpdate()
    {
        // Physics-Bewegung nur auf dem eigenen Client ausführen
        if (!photonView.IsMine) return;

        if (!isAttacking && !isBlocking)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }
    }

    private void UpdateMovementAnimations()
    {
        if (isAttacking || isBlocking) return;

        if (isGrounded)
        {
            if (moveInput > 0)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "run", true);
            else if (moveInput < 0)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "walk normal", true);
            else
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "idle active", true);
        }
        else if (rb != null)
        {
            if (lastMoveInput < 0)
            {
                if (rb.velocity.y > 0.1f) photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump C", true);
                else if (rb.velocity.y < -0.1f) photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump B", true);
            }
            else
            {
                if (rb.velocity.y > 0.1f) photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump B", true);
                else if (rb.velocity.y < -0.1f) photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump C", true);
            }
        }
    }

    // --- RPCs ---

    [PunRPC]
    void RPC_PlaySpineAnimation(string animName, bool loop)
    {
        PlaySpineAnimation(animName, loop);
    }

    [PunRPC]
    void RPC_PlayAttackAnimation(string animName, float duration)
    {
        StartCoroutine(AttackRoutine(animName, duration));
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

        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, fullPath, loop);
            currentAnimation = fullPath;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;

        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.name.ToLower().Contains("floor") || collision.gameObject.name.ToLower().Contains("boden"))
        {
            isGrounded = true;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;

        if (collision.gameObject.CompareTag(groundTag) || collision.gameObject.name.ToLower().Contains("floor") || collision.gameObject.name.ToLower().Contains("boden"))
        {
            if (rb != null && rb.velocity.y > 0.1f)
            {
                isGrounded = false;
            }
        }
    }
}