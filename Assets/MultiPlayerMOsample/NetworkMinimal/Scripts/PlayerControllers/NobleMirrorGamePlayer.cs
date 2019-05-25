using System;
using NobleMirrorSample;
using UnityEngine;
using Mirror;
using NobleMirrorSample.Player;

namespace NobleMirrorSample
{
    /// <summary>
    /// プレイヤーキャラです
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(RayCastPickUp))]
    public class NobleMirrorGamePlayer : NetworkBehaviour
    {
        [SerializeField] private TMPro.TMP_Text scoreText;

        CharacterController characterController;

        public float moveSpeed = 300f;
        public float turnSpeedAccel = 30f;
        public float turnSpeedDecel = 30f;
        public float maxTurnSpeed = 100f;

        [SyncVar] public int Index;

        [SyncVar(hook = nameof(SetScore))] public uint score;

        [SyncVar(hook = nameof(SetColor))] public Color playerColor = Color.black;

        public GameObject projectilePrefab;
        public Transform projectileMount;

        [SerializeField] private Animator animator;

        [SerializeField] private RayCastPickUp _pickUp;


        public override void OnStartLocalPlayer()
        {
            Debug.Log("OnStartLocalPlayer!");
            base.OnStartLocalPlayer();
            characterController = GetComponent<CharacterController>();


            // Turn off main camera because GamePlayer prefab has its own camera
            GetComponentInChildren<Camera>().enabled = true;
            Camera.main.enabled = false;
        }

        void SetScore(uint newScore)
        {
            if (scoreText != null)
            {
                scoreText.text = "score:" + newScore.ToString();
            }
        }

        void SetColor(Color color)
        {
            GetComponent<Renderer>().material.color = color;
        }

        float horizontal = 0f;
        float vertical = 0f;
        float turn = 0f;

        private void Start()
        {
            //これをいれておかないと途中参加時にスコア表示が最初更新されない
            SetScore(score);

            //インスペクタ上でAnimatorの指定が無かったら取得する
            //これをOnStartLocalPlayerにすると、自分以外のPlayerでは呼ばれないので注意
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            //他の端末からも参照するものはStartで取得する
            _pickUp = GetComponent<RayCastPickUp>();
        }

        void Update()
        {
            if (!isLocalPlayer) return;

            horizontal = Input.GetAxis("Horizontal");
            vertical = Input.GetAxis("Vertical");

            if (Input.GetKey(KeyCode.Q) && (turn > -maxTurnSpeed))
                turn -= turnSpeedAccel;
            else if (Input.GetKey(KeyCode.E) && (turn < maxTurnSpeed))
                turn += turnSpeedAccel;
            else
            {
                if (turn > turnSpeedDecel)
                    turn -= turnSpeedDecel;
                else if (turn < -turnSpeedDecel)
                    turn += turnSpeedDecel;
                else
                    turn = 0f;
            }

            // shoot
            if (Input.GetKeyDown(KeyCode.Space))
            {
                CmdFire();
            }

            // shoot
            if (Input.GetKeyDown(KeyCode.K))
            {
                if (_pickUp?.pickedItemGameObject != null)
                {
                    CmdSwordSlash();
                }
            }
        }

        void FixedUpdate()
        {
            if (!isLocalPlayer || characterController == null) return;

            transform.Rotate(0f, turn * Time.fixedDeltaTime, 0f);

            Vector3 direction = Vector3.ClampMagnitude(new Vector3(horizontal, 0f, vertical), 1f) * moveSpeed;
            direction = transform.TransformDirection(direction);
            characterController.SimpleMove(direction * Time.fixedDeltaTime);
        }

        GameObject controllerColliderHitObject;
        private static readonly int Sword = Animator.StringToHash("Sword");

        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // If player and prize objects are on their own layer(s) with correct
            // collision matrix, we wouldn't have to validate the hit.gameobject.
            // Since this is just an example, project settings aren't included so we check the name.

            controllerColliderHitObject = hit.gameObject;

            if (isLocalPlayer && controllerColliderHitObject.name.StartsWith("Coin"))
            {
                if (LogFilter.logDebug)
                    Debug.LogFormat("OnControllerColliderHit {0}[{1}] with {2}[{3}]", name, netId,
                        controllerColliderHitObject.name,
                        controllerColliderHitObject.GetComponent<NetworkIdentity>().netId);

                // Disable the prize gameobject so it doesn't impede player movement
                // It's going to be destroyed in a few frames and we don't want to spam CmdClaimPrize.
                // OnControllerColliderHit will fire many times as the player slides against the object.
                controllerColliderHitObject.SetActive(false);

                CmdClaimPrize(controllerColliderHitObject);
            }
        }

        [Command]
        void CmdSwordSlash()
        {
            RpcSwordSlash();
        }

        [ClientRpc]
        void RpcSwordSlash()
        {
            Debug.Log(netId + " がここで剣を振る:" + gameObject.name);
            SlashSword();
        }

        /// <summary>
        /// 同期して動く処理は、そのまま普通の関数として書いて良い。
        /// </summary>
        void SlashSword()
        {
            _pickUp.pickedItemGameObject.GetComponent<SwordController>().CanDealDamage = true;
            //ここでソードに対してエフェクトを追加したりしても良い
            animator.SetTrigger(Sword);
            Invoke(nameof(DisableSwordDamagable), 2f);
        }

        void DisableSwordDamagable()
        {
            _pickUp.pickedItemGameObject.GetComponent<SwordController>().CanDealDamage = false;
        }


        [Command]
        void CmdFire()
        {
            GameObject projectile = Instantiate(projectilePrefab, projectileMount.position, transform.rotation);
            NetworkServer.Spawn(projectile);
            RpcOnFire();
        }

        // this is called on the tank that fired for all observers
        [ClientRpc]
        void RpcOnFire()
        {
            //ここで発射アニメーションを呼んだりする
            //Debug.Log("ここで発射アニメーションをする:" + gameObject.name);
            //animator.SetTrigger("Shoot");
        }


        [Command]
        public void CmdClaimPrize(GameObject hitObject)
        {
            // Null check is required, otherwise close timing of multiple claims could throw a null ref.
            hitObject?.GetComponent<Prize>().ClaimPrize(gameObject);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(10f + (Index * 110), 10f, 100f, 25f), score.ToString().PadLeft(10));
        }
    }
}