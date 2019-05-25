using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace NobleMirrorSample
{
    /// <summary>
    /// Mirrorを使ったロビーシーンの最小構成
    /// このクラスをコピペしたり拡張して使ってください
    /// </summary>
    public class NobleMirrorLobbyManagerMinimal : NobleConnect.Mirror.NobleNetworkManager
    {
        /// <summary>
        /// Acceptを押してないロビー上のプレーヤー一蘭
        /// </summary>
        [System.Serializable]
        public struct PendingPlayer
        {
            public NetworkConnection conn;
            public GameObject lobbyPlayer;
        }

        [Header("Lobby Settings")]
        // configuration
        [SerializeField]
        bool m_ShowLobbyGUI = true;

        [SerializeField] int m_MaxPlayers = 4;
        [SerializeField] int m_MinPlayers = 1;
        [SerializeField] NobleMirrorLobbyPlayer m_LobbyPlayerPrefab;


        [Scene] [SerializeField] public string LobbyScene = "";
        [Scene] [SerializeField] public string PlayScene = "";


        // runtime data
        public List<PendingPlayer> pendingPlayers = new List<PendingPlayer>();
        public List<NobleMirrorLobbyPlayer> lobbySlots = new List<NobleMirrorLobbyPlayer>();

        public bool allPlayersReady = false;


        #region 使わないかも

        static IntegerMessage s_SceneLoadedMessage = new IntegerMessage();

        // properties
        public bool showLobbyGUI
        {
            get { return m_ShowLobbyGUI; }
            set { m_ShowLobbyGUI = value; }
        }

        public int maxPlayers
        {
            get { return m_MaxPlayers; }
            set { m_MaxPlayers = value; }
        }

        public int minPlayers
        {
            get { return m_MinPlayers; }
            set { m_MinPlayers = value; }
        }

        public NobleMirrorLobbyPlayer lobbyPlayerPrefab
        {
            get { return m_LobbyPlayerPrefab; }
            set { m_LobbyPlayerPrefab = value; }
        }

        #endregion

        public override void OnValidate()
        {
            // always >= 0
            maxConnections = Mathf.Max(maxConnections, 0);

            // always <= maxConnections
            minPlayers = Mathf.Min(minPlayers, maxConnections);

            // always >= 0
            minPlayers = Mathf.Max(minPlayers, 0);

            if (m_LobbyPlayerPrefab != null)
            {
                var uv = m_LobbyPlayerPrefab.GetComponent<NetworkIdentity>();
                if (uv == null)
                {
                    m_LobbyPlayerPrefab = null;
                    Debug.LogWarning("LobbyPlayer prefab must have a NetworkIdentity component.");
                }
            }

            base.OnValidate();
        }

        internal void ReadyStatusChanged()
        {
            int CurrentPlayers = 0;
            int ReadyPlayers = 0;

            foreach (var item in lobbySlots)
            {
                if (item != null)
                {
                    CurrentPlayers++;
                    if (item.ReadyToBegin)
                        ReadyPlayers++;
                }
            }

            if (CurrentPlayers == ReadyPlayers)
                CheckReadyToBegin();
            else
                allPlayersReady = false;
        }

        public override void OnServerReady(NetworkConnection conn)
        {
            if (LogFilter.logDebug)
            {
                Debug.Log("NetworkLobbyManager OnServerReady");
            }

            base.OnServerReady(conn);

            if (conn != null && conn.playerController != null)
            {
                GameObject lobbyPlayer = conn?.playerController?.gameObject;

                // if null or not a lobby player, dont replace it
                if (lobbyPlayer?.GetComponent<NobleMirrorLobbyPlayer>() != null)
                {
                    SceneLoadedForPlayer(conn, lobbyPlayer);
                }
            }
        }

        /// <summary>
        /// ここでゲームシーンのキャラ生成が走る！
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="lobbyPlayer"></param>
        void SceneLoadedForPlayer(NetworkConnection conn, GameObject lobbyPlayerGameObject)
        {
            if (LogFilter.logDebug)
            {
                Debug.LogFormat("NetworkLobby SceneLoadedForPlayer scene: {0} {1}", SceneManager.GetActiveScene().name,
                    conn);
            }

            if (SceneManager.GetActiveScene().name == LobbyScene)
            {
                // cant be ready in lobby, add to ready list
                PendingPlayer pending;
                pending.conn = conn;
                pending.lobbyPlayer = lobbyPlayerGameObject;
                pendingPlayers.Add(pending);
                return;
            }

            //これを読む
            GameObject gamePlayer = OnLobbyServerCreateGamePlayer(conn);
            if (gamePlayer == null)
            {
                // get start position from base class
                Transform startPos = GetStartPosition();

                if (startPos != null)
                {
                    gamePlayer = (GameObject) Instantiate(playerPrefab, startPos.position, startPos.rotation);
                }
                else
                {
                    gamePlayer = (GameObject) Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                }

                gamePlayer.name = playerPrefab.name;
                Debug.Log("gamePlayerが無かったので生成を試みます:" + gamePlayer.name + " prefabは:" + playerPrefab.name);
            }
            else
            {
                Debug.Log("gamePlayerが既に生成済み:" + gamePlayer.name);
            }

            if (!OnLobbyServerSceneLoadedForPlayer(lobbyPlayerGameObject, gamePlayer))
                return;

            // replace lobby player with game player
            NetworkServer.ReplacePlayerForConnection(conn, gamePlayer);
        }

        public void CheckReadyToBegin()
        {
            if (SceneManager.GetActiveScene().name != LobbyScene) return;

            if (minPlayers > 0 && NetworkServer.connections.Count(conn =>
                    conn.Value != null && conn.Value.playerController.gameObject.GetComponent<NobleMirrorLobbyPlayer>()
                        .ReadyToBegin) < minPlayers)
            {
                allPlayersReady = false;
                return;
            }

            pendingPlayers.Clear();
            allPlayersReady = true;
            OnLobbyServerPlayersReady();
        }

        public void ServerReturnToLobby()
        {
            if (!NetworkServer.active)
            {
                Debug.Log("ServerReturnToLobby called on client");
                return;
            }

            ServerChangeScene(LobbyScene);
        }

        void CallOnClientEnterLobby()
        {
            OnLobbyClientEnter();
            foreach (var player in lobbySlots)
                player?.OnClientEnterLobby();
        }

        void CallOnClientExitLobby()
        {
            OnLobbyClientExit();
            foreach (var player in lobbySlots)
                player?.OnClientExitLobby();
        }

        // これがNobleConnectを使う上で最も重要な関数
        // HOST側でNobleConnectのリレーサーバIP,PORTを受け取ったらここが呼ばれる
        // OnServerPrepared is called when the host is listening and has received 
        // their HostEndPoint from the NobleConnect service.
        // Use this HostEndPoint on the client in order to connect to the host.
        // Typically you would use a matchmaking system to pass the HostEndPoint to the client.
        // Look at the Match Up Example for one way to do it. Match Up comes free with any paid plan. 
        public override void OnServerPrepared(string hostAddress, ushort hostPort)
        {
            // Get your HostEndPoint here. 
            Debug.Log("Noble Connect Server Prepared! Hosting at: " + hostAddress + ":" + hostPort);
            //for debug purpose
            PlayerPrefs.SetString("PORT", hostPort.ToString());
        }

        #region server handlers

        // ------------------------ server handlers ------------------------

        public override void OnServerConnect(NetworkConnection conn)
        {
            // numPlayers returns the player count not including this one, so not ok to be equal
            if (numPlayers >= maxPlayers)
            {
                if (LogFilter.logWarn)
                {
                    Debug.LogWarning("NetworkLobbyManager can't accept new connection [" + conn +
                                     "], too many players connected.");
                }

                conn.Disconnect();
                return;
            }

            // cannot join game in progress
            string loadedSceneName = SceneManager.GetSceneAt(0).name;
            if (loadedSceneName != LobbyScene)
            {
                if (LogFilter.logWarn)
                {
                    Debug.LogWarning("NetworkLobbyManager can't accept new connection [" + conn +
                                     "], not in lobby and game already in progress.");
                }

                conn.Disconnect();
                return;
            }

            base.OnServerConnect(conn);

            OnLobbyServerConnect(conn);
        }

        public override void OnServerDisconnect(NetworkConnection conn)
        {
            if (conn.playerController != null)
            {
                var player = conn.playerController.GetComponent<NobleMirrorLobbyPlayer>();

                if (player != null)
                    lobbySlots.Remove(player);
            }

            allPlayersReady = false;

            foreach (var player in lobbySlots)
            {
                if (player != null)
                    player.GetComponent<NobleMirrorLobbyPlayer>().ReadyToBegin = false;
            }

            if (SceneManager.GetActiveScene().name == LobbyScene)
            {
                RecalculateLobbyPlayerIndices();
            }

            base.OnServerDisconnect(conn);
            OnLobbyServerDisconnect(conn);
        }

        public override void OnServerAddPlayer(NetworkConnection conn, AddPlayerMessage extraMessage)
        {
            if (SceneManager.GetActiveScene().name != LobbyScene) return;

            if (lobbySlots.Count == maxConnections) return;

            allPlayersReady = false;

            if (LogFilter.Debug)
                Debug.LogFormat("NetworkLobbyManager.OnServerAddPlayer playerPrefab:{0}", lobbyPlayerPrefab.name);

            GameObject newLobbyGameObject = OnLobbyServerCreateLobbyPlayer(conn);
            if (newLobbyGameObject == null)
                newLobbyGameObject =
                    (GameObject) Instantiate(lobbyPlayerPrefab.gameObject, Vector3.zero, Quaternion.identity);

            var newLobbyPlayer = newLobbyGameObject.GetComponent<NobleMirrorLobbyPlayer>();

            lobbySlots.Add(newLobbyPlayer);

            RecalculateLobbyPlayerIndices();

            NetworkServer.AddPlayerForConnection(conn, newLobbyGameObject);
        }

        void RecalculateLobbyPlayerIndices()
        {
            if (lobbySlots.Count > 0)
            {
                for (int i = 0; i < lobbySlots.Count; i++)
                {
                    lobbySlots[i].Index = i;
                }
            }
        }


        /// <summary>
        /// ロビーから移行したときはsceneNameにGameSceneが入る
        /// 
        /// </summary>
        /// <param name="sceneName"></param>
        public override void ServerChangeScene(string sceneName)
        {
            NetworkServer.SetAllClientsNotReady();
            
            Debug.Log("NobleMirrorLobbyManagerMinimalでServerChangeSceneが呼ばれました:" + sceneName);
            if (LogFilter.logDebug)
            {
                Debug.Log("ServerChangeScene");
            }

            //ロビーから移行したときは、ここは==ではない(sceneNameはGamePlayなので)
            if (sceneName == LobbyScene)
            {
                foreach (var lobbyPlayer in lobbySlots)
                {
                    if (lobbyPlayer == null) continue;

                    // find the game-player object for this connection, and destroy it
                    NetworkIdentity identity = lobbyPlayer.GetComponent<NetworkIdentity>();

                    NetworkIdentity playerController = identity.connectionToClient.playerController;
                    NetworkServer.Destroy(playerController.gameObject);

                    if (NetworkServer.active)
                    {
                        // re-add the lobby object
                        lobbyPlayer.GetComponent<NobleMirrorLobbyPlayer>().ReadyToBegin = false;
                        NetworkServer.ReplacePlayerForConnection(identity.connectionToClient, lobbyPlayer.gameObject);
                    }
                }
            }
            else if (sceneName == PlayScene)
            {
                if (dontDestroyOnLoad)
                {
                    //ここにロビープレイヤー一覧が入っているはず
                    foreach (var lobbyPlayer in lobbySlots)
                    {
                        if (lobbyPlayer != null)
                        {
                            lobbyPlayer.transform.SetParent(null);
                            DontDestroyOnLoad(lobbyPlayer);
                        }
                    }
                }

                foreach (var lobbyPlayer in lobbySlots)
                {
                    NetworkIdentity identity = lobbyPlayer.GetComponent<NetworkIdentity>();

                    if (NetworkServer.active)
                    {
                        Debug.Log(" player 再生成");
                        // re-add the lobby object
                        lobbyPlayer.GetComponent<NobleMirrorLobbyPlayer>().ReadyToBegin = false;
                        NetworkServer.ReplacePlayerForConnection(identity.connectionToClient, lobbyPlayer.gameObject);
                    }
                }
            }
            
            else
            {
                Debug.Log("ゲームシーンでもロビーシーンでも無い場所に行きます lobbySlotsの中身は:"+lobbySlots.Count+"個");
                foreach (var lobbyPlayer in lobbySlots)
                {
                   
                    // find the game-player object for this connection, and destroy it
                    NetworkIdentity identity = lobbyPlayer.GetComponent<NetworkIdentity>();

                    NetworkIdentity playerController = identity.connectionToClient.playerController;
                    if (playerController == null)
                    {
                        Debug.Log("player controllerがnull");
                    }
                    else
                    {
                        NetworkServer.Destroy(playerController.gameObject);

                    }

                    if (NetworkServer.active)
                    {
                        Debug.Log(" player 再生成");
                        // re-add the lobby object
                        lobbyPlayer.GetComponent<NobleMirrorLobbyPlayer>().ReadyToBegin = false;
                        NetworkServer.ReplacePlayerForConnection(identity.connectionToClient, lobbyPlayer.gameObject);
                    }
                }
            }
            

            base.ServerChangeScene(sceneName);
        }

        public override void OnServerSceneChanged(string sceneName)
        {
            if (sceneName != LobbyScene)
            {
                // call SceneLoadedForPlayer on any players that become ready while we were loading the scene.
                foreach (PendingPlayer pending in pendingPlayers)
                    SceneLoadedForPlayer(pending.conn, pending.lobbyPlayer);

                pendingPlayers.Clear();
            }

            OnLobbyServerSceneChanged(sceneName);
        }


        void OnServerReturnToLobbyMessage(NetworkMessage netMsg)
        {
            if (LogFilter.logDebug)
            {
                Debug.Log("NetworkLobbyManager OnServerReturnToLobbyMessage");
            }

            ServerReturnToLobby();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            if (string.IsNullOrEmpty(LobbyScene))
            {
                Debug.LogError(
                    "NetworkLobbyManager LobbyScene is empty. Set the LobbyScene in the inspector for the NetworkLobbyMangaer");
                return;
            }

            if (string.IsNullOrEmpty(PlayScene))
            {
                Debug.LogError(
                    "NetworkLobbyManager PlayScene is empty. Set the PlayScene in the inspector for the NetworkLobbyMangaer");
                return;
            }

            OnLobbyStartServer();
        }

        public override void OnStopServer()
        {
            lobbySlots.Clear();
            base.OnStopServer();
        }

        public override void OnStartHost()
        {
            base.OnStartHost();
            OnLobbyStartHost();
        }

        public override void OnStopHost()
        {
            base.OnStopHost();
            OnLobbyStopHost();
        }

        // ------------------------ client handlers ------------------------

        #endregion

        #region client handlers

        public override void OnStartClient()
        {
            if (lobbyPlayerPrefab == null || lobbyPlayerPrefab.gameObject == null)
                Debug.LogError(
                    "NetworkLobbyManager no LobbyPlayer prefab is registered. Please add a LobbyPlayer prefab.");
            else
                ClientScene.RegisterPrefab(lobbyPlayerPrefab.gameObject);

            if (playerPrefab == null)
                Debug.LogError(
                    "NetworkLobbyManager no GamePlayer prefab is registered. Please add a GamePlayer prefab.");
            else
                ClientScene.RegisterPrefab(playerPrefab);

            OnLobbyStartClient();
        }

        public override void OnClientConnect(NetworkConnection conn)
        {
            OnLobbyClientConnect(conn);
            CallOnClientEnterLobby();
            base.OnClientConnect(conn);
        }

        public override void OnClientDisconnect(NetworkConnection conn)
        {
            OnLobbyClientDisconnect(conn);
            base.OnClientDisconnect(conn);
        }

        public override void OnStopClient()
        {
            OnLobbyStopClient();
            CallOnClientExitLobby();
            if (!string.IsNullOrEmpty(offlineScene))
            {
                // Move the LobbyManager from the virtual DontDestroyOnLoad scene to the Game scene.
                // This let's it be destroyed when client changes to the Offline scene.
                SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
            }
        }

        public override void OnClientChangeScene(string newSceneName)
        {
            if (LogFilter.Debug)
            {
                Debug.LogFormat("OnClientChangeScene from {0} to {1}", SceneManager.GetActiveScene().name,
                    newSceneName);
            }

            if (SceneManager.GetActiveScene().name == LobbyScene && newSceneName == PlayScene && dontDestroyOnLoad &&
                NetworkClient.isConnected)
            {
                GameObject lobbyPlayer = NetworkClient.connection?.playerController?.gameObject;
                if (lobbyPlayer != null)
                {
                    lobbyPlayer.transform.SetParent(null);
                    DontDestroyOnLoad(lobbyPlayer);
                }
                else
                    Debug.LogWarningFormat("OnClientChangeScene: lobbyPlayerGameObject is null");
            }
            else if (LogFilter.Debug)
                Debug.LogFormat("OnClientChangeScene {0} {1}", dontDestroyOnLoad, NetworkClient.isConnected);
        }

        public override void OnClientSceneChanged(NetworkConnection conn)
        {
            if (LogFilter.Debug)
            {
                Debug.Log("OnClientSceneChanged");
            }

            if (SceneManager.GetActiveScene().name == LobbyScene)
            {
                if (NetworkClient.isConnected)
                    CallOnClientEnterLobby();
            }
            else
            {
                CallOnClientExitLobby();
            }

            base.OnClientSceneChanged(conn);
            OnLobbyClientSceneChanged(conn);
        }

        #endregion

        // ------------------------ lobby server virtuals ------------------------

        public virtual void OnLobbyStartHost()
        {
        }

        public virtual void OnLobbyStopHost()
        {
        }

        public virtual void OnLobbyStartServer()
        {
        }

        public virtual void OnLobbyServerConnect(NetworkConnection conn)
        {
        }

        public virtual void OnLobbyServerDisconnect(NetworkConnection conn)
        {
        }

        public virtual void OnLobbyServerSceneChanged(string sceneName)
        {
        }

        public virtual GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn)
        {
            return null;
        }

        public virtual GameObject OnLobbyServerCreateGamePlayer(NetworkConnection conn)
        {
            //ここでプレイヤー作る？
            Debug.Log("OnLobbyServerCreateGamePlaye called");
            return null;
        }

        // for users to apply settings from their lobby player object to their in-game player object
        public virtual bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            return true;
        }

        public virtual void OnLobbyServerPlayersReady()
        {
            Debug.Log("OnLobbyServerPlayersReadyが呼ばれた！");
            if (showLobbyGUI)
            {
                showStartButton = true;
            }
            else
            {
                // all players are readyToBegin, start the game
                ServerChangeScene(PlayScene);
            }
        }

        // ------------------------ lobby client virtuals ------------------------

        public virtual void OnLobbyClientEnter()
        {
        }

        public virtual void OnLobbyClientExit()
        {
        }

        public virtual void OnLobbyClientConnect(NetworkConnection conn)
        {
        }

        public virtual void OnLobbyClientDisconnect(NetworkConnection conn)
        {
        }

        public virtual void OnLobbyStartClient()
        {
        }

        public virtual void OnLobbyStopClient()
        {
        }

        public virtual void OnLobbyClientSceneChanged(NetworkConnection conn)
        {
        }

        // for users to handle adding a player failed on the server
        public virtual void OnLobbyClientAddPlayerFailed()
        {
        }


        // ------------------------ optional UI ------------------------

        [SerializeField]
        bool showStartButton;

        public void OnGUI()
        {
            if (!showLobbyGUI)
                return;


            string loadedSceneName = SceneManager.GetSceneAt(0).name;
            if (loadedSceneName != LobbyScene)
                return;

            Rect backgroundRec = new Rect(0, 180, 500, 150);
            GUI.Box(backgroundRec, "Players:");
            if (allPlayersReady && showStartButton && GUI.Button(new Rect(150, 300, 120, 20), "START GAME"))
            {
                // set to false to hide it in the game scene
                showStartButton = false;

                ServerChangeScene(PlayScene);
            }
        }
    }
}