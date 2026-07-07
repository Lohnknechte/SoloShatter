using UnityEngine;
using Spine.Unity;
using Photon.Pun;

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

	void Awake()
	{
		// Spine direkt beim Aufwachen initialisieren, um Timing-Fehler mit Photon zu verhindern
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
		// 1. Sicherheit: Nur den eigenen Charakter steuern
		if (!photonView.IsMine) return;

		// 2. Sicherheit: Blockiert Eingaben, wenn wir noch nicht im Photon-Raum verbunden sind
		if (!PhotonNetwork.InRoom) return;

		// Block-Input (S-Taste halten)
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

		// Tekken Angriffe (Pfeiltasten) -> Werden als RPC an alle gesendet
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

		// WASD Laufen
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

		// WASD Sprung (W = Springen)
		if (Input.GetKeyDown(KeyCode.W) && isGrounded && rb != null)
		{
			rb.velocity = new Vector2(rb.velocity.x, 0);
			rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
			PlaySpineAnimation("jump A", false);
			isGrounded = false;
		}

		// Animations-Logik für Movement am Boden & in der Luft
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
				if (lastMoveInput < 0) // RÜCKWÄRTS-SPRUNG
				{
					if (rb.velocity.y > 0.1f) PlaySpineAnimation("jump C", true);
					else if (rb.velocity.y < -0.1f) PlaySpineAnimation("jump B", true);
				}
				else // VORWÄRTS- oder VERTIKAL-SPRUNG
				{
					if (rb.velocity.y > 0.1f) PlaySpineAnimation("jump B", true);
					else if (rb.velocity.y < -0.1f) PlaySpineAnimation("jump C", true);
				}
			}
		}
	}

	// --- NETZWERK METHODEN (RPCs) ---

	[PunRPC]
	void RPC_PlayAttackAnimation(string animName, float duration)
	{
		// Startet die Angriffs-Coroutine auf allen PCs gleichzeitig
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