using UnityEngine;
using Spine.Unity;
using Photon.Pun;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviourPun, IPunObservable
{
    [Header("Components")]
    public SkeletonAnimation skeletonAnimation;
    private Rigidbody2D rb;

    [Header("Movement")]
    public float moveSpeed = 15f;
    public float jumpForce = 35f;
    public string animationFolder = "1_";

    [Header("Combat & Health")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    [Header("Ground Check Settings")]
    public string groundTag = "Floor";
    private bool isGrounded = true;

    private string currentAnimation = "";
    private bool isAttacking = false;
    private bool isBlocking = false;
    private float moveInput = 0f;
    private bool isDead = false;

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

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        if (isDead) return;

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

        // Attacks mit neuen Ranges (Jab: 6.5m, Kick High: 9.5m)
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Debug.Log("💥 [ATTACK] Jab Single!");
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab single", 0.25f, 15f, 6.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Debug.Log("💥 [ATTACK] Jab Double!");
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab double", 0.3f, 25f, 7.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            Debug.Log("💥 [ATTACK] Kick High!");
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick high", 0.35f, 35f, 9.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            Debug.Log("💥 [ATTACK] Kick Low!");
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick low", 0.3f, 20f, 7.0f);
            return;
        }

        // Steuerung
        moveInput = 0f;

        if (Input.GetKey(KeyCode.D))
        {
            moveInput = 1f;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            moveInput = -0.5f;
        }

        // Jump (W)
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
        if (!photonView.IsMine || isDead) return;

        if (!isAttacking && !isBlocking)
        {
            float directionSign = (skeletonAnimation != null && skeletonAnimation.Skeleton.ScaleX > 0) ? 1f : -1f;
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

                Collider2D myCol = GetComponent<Collider2D>();
                Collider2D oppCol = p.GetComponent<Collider2D>();
                if (myCol != null && oppCol != null)
                {
                    Physics2D.IgnoreCollision(myCol, oppCol, true);
                }
                break;
            }
        }
    }

    private void HandleFacingDirection()
    {
        if (opponent == null || skeletonAnimation == null) return;

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

        if (isGrounded)
        {
            if (moveInput > 0)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "run", true);
            else if (moveInput < 0)
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "walk normal", true);
            else
                photonView.RPC("RPC_PlaySpineAnimation", RpcTarget.All, "idle active", true);
        }
    }

    private void CheckHit(float damage, float range)
    {
        if (opponent == null) return;

        float dist = Vector2.Distance(transform.position, opponent.position);
        if (dist <= range)
        {
            Debug.Log($"🎯 [HIT SUCCESS] Distanz: {dist:F2}m / Range: {range}m");
            PlayerController target = opponent.GetComponent<PlayerController>();
            if (target != null)
            {
                target.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
            }
        }
        else
        {
            Debug.Log($"❌ [HIT MISSED] Zu weit weg! Distanz: {dist:F2}m / Range: {range}m");
        }
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage *= 0.2f;
            Debug.Log("🛡️ [BLOCK] Schaden geblockt!");
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Debug.Log($"🩸 [DAMAGE] Rest-HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            PlaySpineAnimation("knockdown", false);

            // Winner UI aufrufen
            GameManager gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                string winnerText = photonView.IsMine ? "GEGNER GEWINNT!" : "DU HAST GEWONNEN!";
                gm.photonView.RPC("RPC_ShowEndScreen", RpcTarget.All, winnerText);
            }
        }
    }

    [PunRPC]
    void RPC_PlaySpineAnimation(string animName, bool loop)
    {
        PlaySpineAnimation(animName, loop);
    }

    [PunRPC]
    void RPC_PlayAttackAnimation(string animName, float duration, float damage, float range)
    {
        StartCoroutine(AttackRoutine(animName, duration, damage, range));
    }

    System.Collections.IEnumerator AttackRoutine(string animName, float duration, float damage, float range)
    {
        isAttacking = true;
        PlaySpineAnimation(animName, false);

        if (photonView.IsMine)
        {
            CheckHit(damage, range);
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {}

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