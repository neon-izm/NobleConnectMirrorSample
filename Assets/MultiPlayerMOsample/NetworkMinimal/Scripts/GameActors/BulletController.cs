using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace NobleMirrorSample
{
    /// <summary>
    /// 自機が撃つ弾の挙動
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BulletController : NetworkBehaviour
    {
        public float destroyAfter = 7;
        public Rigidbody rigidBody;
        public float force = 1000;

        private int damageAmmount = 1;
        
        public override void OnStartServer()
        {
            Invoke(nameof(DestroySelf), destroyAfter);
        }

        // set velocity for server and client. this way we don't have to sync the
        // position, because both the server and the client simulate it.
        void Start()
        {
            if (rigidBody == null)
            {
                rigidBody = GetComponent<Rigidbody>();
            }
            
            rigidBody.AddForce(transform.forward * force);
        }

        // destroy for everyone on the server
        [Server]
        void DestroySelf()
        {
            NetworkServer.Destroy(gameObject);
        }

        // サーバ側で当たり判定をつけて、Enemyに当たったら0.1秒後に消えます
        [ServerCallback]
        void OnCollisionEnter(Collision co)
        {
            Debug.Log("ヒットしました！");
            //対象がHPを持っていたらDealDamageを呼ぶ
            var damageApplyer = co.gameObject.GetComponent<IDamageable>();
            damageApplyer?.DealDamage(damageAmmount);

            Invoke(nameof(DestroySelf),0.1f);
        }

    }
}