using UnityEngine;
using Spine.Unity;
using Photon.Pun;
using TMPro;

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

    [Header("UI System")]
    public TMP_Text winTextUI;

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

        // Deaktiviert Trigger-Sensitivität für glattes Drüberspringen
        Collider2D myCol = GetComponent<Collider2D>();
        if (myCol != null) myCol.isTrigger = false;

        PlaySpineAnimation("idle active", true);
    }

    void Update()
    {
        if (isDead) return;

        FindOpponent();
        HandleFacingDirection();

        if (!photonView.IsMine || !PhotonNetwork.InRoom) return;

        // Blocken
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

        // Attacks
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab single", 0.25f, 15f, 6.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "jab double", 0.3f, 25f, 7.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            photonView.RPC("RPC_PlayAttackAnimation", RpcTarget.All, "kick high", 0.35f, 35f, 9.5f);
            return;
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
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

        // Jump
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

                // Kollision zwischen beiden Playern KOMPLETT ignorieren (Verhindert Teleport/Jitter-Bugs)
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
            PlayerController target = opponent.GetComponent<PlayerController>();
            if (target != null)
            {
                target.photonView.RPC("RPC_TakeDamage", RpcTarget.All, damage);
            }
        }
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage *= 0.2f;
        }

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        if (currentHealth <= 0 && !isDead)
        {
            // RPC auf allen Clients ausführen, damit der Verlierer überall duckt
            photonView.RPC("RPC_Die", RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Die()
    {
        isDead = true;
        if (rb != null) rb.velocity = Vector2.zero;

        // Ducken-Animation EINMALIG ohne Loop abspielen
        PlaySpineAnimation("block bottom", false);

        // Text auf beiden Bildschirmen einschalten
        ShowWinText();
    }

    void ShowWinText()
    {
        // Sucht den Text in der Scene (auch wenn er inaktiv ist)
        TMP_Text[] texts = Resources.FindObjectsOfTypeAll<TMP_Text>();
        foreach (var t in texts)
        {
            if (t.gameObject.name.Contains("WinnerText") || t.gameObject.CompareTag("WinText"))
            {
                winTextUI = t;
                break;
            }
        }

        if (winTextUI != null)
        {
            winTextUI.gameObject.SetActive(true);
            if (winTextUI.transform.parent != null)
            {
                winTextUI.transform.parent.gameObject.SetActive(true);
            }

            if (photonView.IsMine)
            {
                winTextUI.text = "ROUND OVER\nGEGNER GEWINNT!";
            }
            else
            {
                winTextUI.text = "ROUND OVER\nDU HAST GEWONNEN!";
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