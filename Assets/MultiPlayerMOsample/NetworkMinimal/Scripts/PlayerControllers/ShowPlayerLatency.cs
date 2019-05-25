using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace NobleMirrorSample.Player
{
    /// <summary>
    /// RTTの値を表示する
    /// </summary>
    public class ShowPlayerLatency : NetworkBehaviour
    {
        private double latency;
        [SerializeField] private TMPro.TextMeshPro latencyText;
        
        // Update is called once per frame
        void Update()
        {
            ShowLatency();
        }

        void ShowLatency()
        {
            if (isLocalPlayer)
            {
                //HOSTは0になり、そうでないクライアントは0.03などの秒数が取得できます
                latency = NetworkTime.rtt;
                latencyText.text = "RTT :" + $"{latency:0.######}";
            }
        }
    }
}