using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace NobleMirrorSample
{
    /// <summary>
    /// 剣の挙動を示す
    /// </summary>
    public class SwordController : NetworkBehaviour
    {
        //与えられるダメージ量
        private int damageAmmount = 10;

        private bool canDealDamage = false;

        public bool CanDealDamage
        {
            get => canDealDamage;
            set => canDealDamage = value;
        }

        public override void OnStartServer()
        {
        }

        // サーバ側で当たり判定をつけています。
        [ServerCallback]
        void OnTriggerEnter(Collider co)
        {
            if (canDealDamage == false)
            {
                Debug.Log("OnTriggerEnter 剣は無効状態、攻撃中に当ててね");
                return;
            }
            Debug.Log("ヒットしました！");
            //対象がHPを持っていたらDealDamageを呼ぶ
            var damageApplyer = co.gameObject.GetComponent<IDamageable>();
            damageApplyer?.DealDamage(damageAmmount);
        }
        
        
    }
}