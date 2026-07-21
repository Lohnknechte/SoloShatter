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

    // Gegner-Referenz
    private Transform opponent;

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

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        // Immer nach dem Gegner suchen
        FindOpponent();

        // Ausrichtung zum Gegner (Flip)
        HandleFacingDirection();

        // Nur den eigenen Charakter steuern
        if (!photonView.IsMine) return;
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

        // Steuerung relativ zum Gegner:
        // D = Auf den Gegner zu (Vorwärts), A = Vom Gegner weg (Rückwärts)
        moveInput = 0f;

        bool isLeftOfOpponent = opponent == null || transform.position.x < opponent.position.x;

        if (Input.GetKey(KeyCode.D))
        {
            moveInput = isLeftOfOpponent ? 1f : -1f; // Vorwärts
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = isLeftOfOpponent ? -0.5f : 0.5f; // Rückwärts
        }

        if (isGrounded) lastMoveInput = moveInput;

        // WASD Sprung (W)
        if (Input.GetKeyDown(KeyCode.W) && isGrounded && rb != null)
        {
            rb.velocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "jump A", false);
            isGrounded = false;
        }

        UpdateMovementAnimations();
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (!isAttacking && !isBlocking)
        {
            rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);
        }
    }

    private void FindOpponent()
    {
        if (opponent != null) return;

        PlayerController[] allPlayers = FindObjectsOfType<PlayerController>();
        foreach (var p in allPlayers)
        {
            if (p != this)
            {
                opponent = p.transform;
                break;
            }
        }
    }

    private void HandleFacingDirection()
    {
        if (opponent == null || skeletonAnimation == null) return;

        // Dreht den ScaleX von Spine um, sodass man immer zum Gegner schaut
        if (transform.position.x < opponent.position.x)
        {
            skeletonAnimation.Skeleton.ScaleX = 1;
        }
        else
        {
            skeletonAnimation.Skeleton.ScaleX = -1;
        }
    }

    private void UpdateMovementAnimations()
    {
        if (isAttacking || isBlocking) return;

        bool isMovingForward = (skeletonAnimation.Skeleton.ScaleX > 0 && moveInput > 0) || (skeletonAnimation.Skeleton.ScaleX < 0 && moveInput < 0);
        bool isMovingBackward = (skeletonAnimation.Skeleton.ScaleX > 0 && moveInput < 0) || (skeletonAnimation.Skeleton.ScaleX < 0 && moveInput > 0);

        if (isGrounded)
        {
            if (isMovingForward)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "run", true);
            else if (isMovingBackward)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "walk normal", true);
            else
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "idle active", true);
        }
        else if (rb != null)
        {
            if (isMovingBackward)
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