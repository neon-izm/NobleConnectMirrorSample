using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NobleMirrorSample
{
    /// <summary>
    /// ロビーシーンへの遷移用
    /// </summary>
    public class StartSceneChange : MonoBehaviour
    {
        public void GoToLobbyScene()
        {
            SceneManager.LoadScene("MinimalLobby");
        }
    }
}