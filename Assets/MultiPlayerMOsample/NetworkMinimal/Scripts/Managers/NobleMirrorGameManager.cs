using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.Examples.NetworkLobby;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace NobleMirrorSample
{
    /// <summary>
    /// CommandとかRpcはここを見てね
    /// http://motoyama.hateblo.jp/entry/unet-05
    /// 基本的にクライアントからCommandでサーバに送って、サーバ側がRpcでみんなに配る感じ
    /// </summary>
    public class NobleMirrorGameManager : NetworkBehaviour
    {
        [SerializeField] private GameObject prizePrefab;

        [SerializeField] private GameObject enemyPrefab;

        [SerializeField] private float spawnTimer = 0f;

        //残り何秒、みたいなやつ、ゲームのプレイ時間を秒で指定して下さい
        [SyncVar(hook = nameof(OnChangeRemainTime))]
        float gameTimeEnd = 90f;//

        //敵のキルスコア、参加者全員の合計を持っています
        [SyncVar(hook = nameof(OnChangeKillScore))]
        public int enemyKillScore = 0;


        public Action<float> OnRemainTimeChange;

        public Action<int> OnKillScoreUpdate;


        [SerializeField] private List<EnemyController> _enemys = new List<EnemyController>();

        /// <summary>
        /// ロビーに戻る
        /// </summary>
        public void ReturnToLobby()
        {
            //NetworkManager.Shutdown();
            
            SceneManager.LoadScene("MinimalLobby");
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        [Command]
        public void CmdSpawnPrize()
        {
            Debug.Log("prizeがspawn");
            var newPrize = Instantiate(prizePrefab.gameObject, new Vector3(Random.Range(-3f, 3f), 1, Random.Range(-3f, 3f)),
                Quaternion.identity);

            NetworkServer.Spawn(newPrize);
            newPrize.GetComponent<Prize>().prizeColor = new Color(Random.Range(0f, 1f), 0f, Random.Range(0f, 1f));

        }

        [Command]
        public void CmdSpawnEnemy()
        {
            Debug.Log("enemyがspawn");
            var newEnemy = Instantiate(enemyPrefab.gameObject,
                new Vector3(Random.Range(-3f, 3f), 1, Random.Range(-3f, 3f)),
                Quaternion.identity);

            //サーバから生み出した敵にはアクションにイベントを登録
            var cont = newEnemy.GetComponent<EnemyController>();
            cont.OnFinishedLife += OnEnemyDestoyed;
            _enemys.Add(cont);

            NetworkServer.Spawn(newEnemy);
        }

        private void OnEnemyDestoyed()
        {
            Debug.Log("サーバ側で敵が死んだのをゲームマネージャが感知！");
            if (isServer == false)
            {
                Debug.LogError("なんでこの部分がクライアントでも呼ばれるの？！？！？！");
            }

            enemyKillScore++;
        }

        /// <summary>
        /// サーバ側でだけ呼ばれるはず
        /// </summary>
        /// <param name="remainTime"></param>
        void OnChangeRemainTime(float remainTime)
        {
            OnRemainTimeChange?.Invoke(remainTime);
        }

        /// <summary>
        /// サーバ側でだけ呼ばれるはず
        /// </summary>
        /// <param name="killScore"></param>
        void OnChangeKillScore(int killScore)
        {
            OnKillScoreUpdate?.Invoke(killScore);
        }

        private bool gameEnding = false;

        // Update is called once per frame
        void Update()
        {
            if (!isServer)
            {
                return;
            }

            gameTimeEnd -= Time.deltaTime;
            //ゲーム終了後にリザルト&再戦を選ぶシーンに遷移する
            //連続2回呼ばれる可能性があるので、一度だけ呼ぶ
            if (gameTimeEnd < 0 && gameEnding == false)
            {
                NobleMirrorLobbyManagerMinimal.singleton.ServerChangeScene("MinimalGameEnd");
                gameEnding = true;
            }

            spawnTimer += Time.deltaTime;
            if (spawnTimer > 3f)
            {
                CmdSpawnEnemy();
                //ここでprizeを発生させる
                CmdSpawnPrize();
                spawnTimer -= 3f;
            }
        }
    }
}