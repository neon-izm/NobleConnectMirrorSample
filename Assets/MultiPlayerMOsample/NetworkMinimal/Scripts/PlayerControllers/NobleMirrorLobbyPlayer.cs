﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

namespace NobleMirrorSample
{
    /// <summary>This is almost an exact copy of Unity's LobbyPlayer but with a few changes so that it works with NobleNetworkLobbyManager</summary>
    [DisallowMultipleComponent]
    public class NobleMirrorLobbyPlayer : NetworkBehaviour
    {
        public bool ShowLobbyGUI = true;

        [SyncVar(hook = nameof(ReadyStateChanged))]
        public bool ReadyToBegin;

        [SyncVar] public int Index;

        #region Unity Callbacks

        /// <summary>
        /// Do not use Start - Override OnStartrHost / OnStartClient instead!
        /// </summary>
        public void Start()
        {
            if (NetworkManager.singleton as NobleMirrorLobbyManagerMinimal)
                OnClientEnterLobby();
            else
                Debug.LogError(
                    "LobbyPlayer could not find a NetworkLobbyManager. The LobbyPlayer requires a NetworkLobbyManager object to function. Make sure that there is one in the scene.");
        }

        #endregion

        #region Commands

        [Command]
        public void CmdChangeReadyState(bool ReadyState)
        {
            ReadyToBegin = ReadyState;
            NobleMirrorLobbyManagerMinimal lobby = NetworkManager.singleton as NobleMirrorLobbyManagerMinimal;
            lobby?.ReadyStatusChanged();
        }

        #endregion

        #region SyncVar Hooks

        void ReadyStateChanged(bool NewReadyState)
        {
            OnClientReady(ReadyToBegin);
        }

        #endregion

        #region Lobby Client Virtuals

        public virtual void OnClientEnterLobby()
        {
        }

        public virtual void OnClientExitLobby()
        {
        }

        public virtual void OnClientReady(bool readyState)
        {
        }

        #endregion

        #region Optional UI

        public virtual void OnGUI()
        {
            if (!ShowLobbyGUI)
                return;

            NobleMirrorLobbyManagerMinimal lobby = NetworkManager.singleton as NobleMirrorLobbyManagerMinimal;
            if (lobby)
            {
                if (!lobby.showLobbyGUI)
                    return;

                if (SceneManager.GetActiveScene().name != lobby.LobbyScene)
                    return;

                GUILayout.BeginArea(new Rect(20f + (Index * 100), 200f, 90f, 130f));

                GUILayout.Label($"Player [{Index + 1}]");

                if (ReadyToBegin)
                    GUILayout.Label("Ready");
                else
                    GUILayout.Label("Not Ready");

                if (((isServer && Index > 0) || isServerOnly) && GUILayout.Button("REMOVE"))
                {
                    // This button only shows on the Host for all players other than the Host
                    // Host and Players can't remove themselves (stop the client instead)
                    // Host can kick a Player this way.
                    GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
                }

                GUILayout.EndArea();

                if (NetworkClient.active && isLocalPlayer)
                {
                    GUILayout.BeginArea(new Rect(20f, 300f, 120f, 20f));

                    if (ReadyToBegin)
                    {
                        if (GUILayout.Button("Cancel"))
                            CmdChangeReadyState(false);
                    }
                    else
                    {
                        if (GUILayout.Button("Ready"))
                            CmdChangeReadyState(true);
                    }

                    GUILayout.EndArea();
                }
            }
        }

        #endregion
    }
}