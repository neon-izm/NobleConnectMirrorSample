using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace NobleMirrorSample
{
    public class NobleMirrorGameEndManager : NetworkBehaviour
    {
        [SerializeField] private Canvas selectGameEndCanvas;
        // Start is called before the first frame update
        void Start()
        {
            if (!isServer)
            {
                selectGameEndCanvas.enabled = false;
            }
            else
            {
                selectGameEndCanvas.enabled = true;
            }
            
        }

        /// <summary>
        /// ロビーに戻る
        /// </summary>
        public void ReturnToLobby()
        {
            //NetworkServer.SetAllClientsNotReady();
            NobleMirrorLobbyManagerMinimal.singleton.ServerChangeScene("MinimalLobby");
        }

        
        /// <summary>
        /// 再戦する
        /// </summary>
        public void ReturnToGame()
        {
            //NetworkServer.SetAllClientsNotReady();
            NobleMirrorLobbyManagerMinimal.singleton.ServerChangeScene("MinimalGamePlay");
        }
        
        //サーバ以外は無視して良い
        [ServerCallback]
        // Update is called once per frame
        void Update()
        {
        }
    }
}