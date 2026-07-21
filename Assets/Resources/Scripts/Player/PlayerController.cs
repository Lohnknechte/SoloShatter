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
    public float jumpForce = 35f; // Etwas höherer Sprung
    public string animationFolder = "1_";

    [Header("Combat & Health System")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public Transform attackPoint; // Leeres GameObject vor der Faust/Fuß
    public float attackRange = 1.5f;
    public LayerMask opponentLayer;

    [Header("Ground Check Settings")]
    public string groundTag = "Floor";
    private bool isGrounded = true;

    private string currentAnimation = "";
    private bool isAttacking = false;
    private bool isBlocking = false;
    private float lastMoveInput = 0f;
    private float moveInput = 0f;

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
        currentHealth = maxHealth;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        // Deaktiviert Kollision zwischen Playern, damit man drüber springen kann
        Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        FindOpponent();
        HandleFacingDirection();

        if (!photonView.IsMine || !PhotonNetwork.InRoom) return;

        // Blocken (S-Taste)
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

        // Tekken Angriffe (Left, Right, Up, Down) -> Schaden & Anims
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab single", 0.25f, 10f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab double", 0.3f, 15f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick high", 0.35f, 20f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick low", 0.3f, 12f);
            return;
        }

        // FIXED MOVEMENT: D = Immer Vorwärts (auf Gegner zu), A = Immer Rückwärts (vom Gegner weg)
        moveInput = 0f;

        if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f; // Vorwärts
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = -0.5f; // Rückwärts
        }

        if (isGrounded) lastMoveInput = moveInput;

        // W = Sprung
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
            // Berechnet echte Welt-Richtung basierend auf dem Blick zum Gegner
            float directionSign = skeletonAnimation.Skeleton.ScaleX > 0 ? 1f : -1f;
            rb.velocity = new Vector2(moveInput * moveSpeed * directionSign, rb.velocity.y);
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

        if (transform.position.x < opponent.position.x)
        {
            skeletonAnimation.Skeleton.ScaleX = 1; // Blick nach rechts
        }
        else
        {
            skeletonAnimation.Skeleton.ScaleX = -1; // Blick nach links
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
            if (moveInput < 0)
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

    // --- DAMAGE & HIT SYSTEM ---

    public void TakeDamage(float damage)
    {
        if (!photonView.IsMine) return;

        if (isBlocking)
        {
            damage *= 0.2f; // Block reduziert Schaden um 80%
        }

        currentHealth -= damage;
        Debug.Log(photonView.Owner.NickName + " HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "knockdown", false);
            // Hier später KO-Logik einbauen
        }
    }

    private void CheckHit(float damage)
    {
        if (opponent == null) return;

        // Prüft Distanz zum Gegner für Hits
        float distance = Vector2.Distance(transform.position, opponent.position);
        if (distance <= attackRange)
        {
            PlayerController target = opponent.GetComponent<PlayerController>();
            if (target != null)
            {
                target.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
            }
        }
    }

    // --- RPCs ---

    [PunRPC]
    void RPC_TakeDamage(float damage)
    {
        TakeDamage(damage);
    }

    [PunRPC]
    void RPC_PlaySpineAnimation(string animName, bool loop)
    {
        PlaySpineAnimation(animName, loop);
    }

    [PunRPC]
    void RPC_PlayAttackAnimation(string animName, float duration, float damage)
    {
        StartCoroutine(AttackRoutine(animName, duration, damage));
    }

    System.Collections.IEnumerator AttackRoutine(string animName, float duration, float damage)
    {
        isAttacking = true;
        PlaySpineAnimation(animName, false);

        if (photonView.IsMine)
        {
            CheckHit(damage);
        }

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