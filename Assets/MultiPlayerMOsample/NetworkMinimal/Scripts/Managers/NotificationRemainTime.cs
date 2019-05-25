using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NobleMirrorSample.UI
{
    /// <summary>
    /// GameManagerで受け取ったスコア情報を表示する部分、各クライアントごとに同期する必要は無いのでMonoBehaviourを継承しています
    /// </summary>
    [RequireComponent(typeof(NobleMirrorGameManager))]
    public class NotificationRemainTime : MonoBehaviour
    {
        private NobleMirrorGameManager gameManager;

        [SerializeField] private TMPro.TMP_Text _text;

        [SerializeField] private TMPro.TMP_Text _score;

        // Start is called before the first frame update
        void Start()
        {
            gameManager = GetComponent<NobleMirrorGameManager>();
            gameManager.OnRemainTimeChange += OnRemainTimeChange;
            gameManager.OnKillScoreUpdate+= OnKillScoreUpdate;
        }

        private void OnKillScoreUpdate(int obj)
        {
            Debug.Log("スコア評更新します:"+obj);
            _score.text = "Kill Score:" + (int)obj;
        }

        private void OnRemainTimeChange(float obj)
        {
            _text.text = "remain:" + (int)obj;
        }

    }
}