using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace NobleMirrorSample
{
    /// <summary>
    /// 敵の挙動を書くよ
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyController : NetworkBehaviour, IDamageable
    {
        /// <summary>
        /// デフォルト挙動として、最低限ネットワークTransformで同期しつつ、適当にパトロールするようにした
        /// </summary>
        private NavMeshAgent _agent;

        // ヒットポイント
        [SyncVar(hook = nameof(ChangeHP))] public int hp = 10;

        [SerializeField] private TMPro.TMP_Text _infoText;

        public Action OnFinishedLife;

        // Start is called before the first frame     update
        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            // autoBraking を無効にすると、目標地点の間を継続的に移動します
            //(つまり、エージェントは目標地点に近づいても
            // 速度をおとしません)
            _agent.autoBraking = false;

            if (_infoText == null)
            {
                _infoText = GetComponentInChildren<TMPro.TMP_Text>();
            }

            ChangeHP(hp);

            GotoNextPoint();
            //サーバじゃないなら（クライアント側の人は）ナビメッシュ移動しないで良い。サーバ側がナビメッシュで移動して、ネットワークトランスフォームで動くから
            if (isServer == false)
            {
                _agent.enabled = false;
            }
        }

        void ChangeHP(int p)
        {
            _infoText.text = "hp:" + p;
        }

        void GotoNextPoint()
        {
            // エージェントが現在設定された目標地点に行くように設定します
            _agent.destination = new Vector3(Random.Range(-5f, 5f), 1, Random.Range(-5f, 5f));
        }

        // Update is called once per frame
        void Update()
        {
            if (isServer == false)
            {
                return;
            }

            // エージェントが現目標地点に近づいてきたら、
            // 次の目標地点を選択します
            if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                GotoNextPoint();
        }
        
        
        /// <summary>
        /// 自キャラの弾や剣が当たった時の挙動
        /// </summary>
        public void DealDamage(int damage)
        {
            hp -= damage;
            if (hp <= 0)
            {
                //サーバでだけ呼ばれる
                OnFinishedLife?.Invoke();
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}