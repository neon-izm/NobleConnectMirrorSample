using System.Collections;
using System.Collections.Generic; // Queueのために必要
using UnityEngine;
using System.Text; // StringBuilderのために必要

public class LogDisplay : MonoBehaviour
{
    // ログを何個まで保持するか
    [SerializeField] int m_MaxLogCount = 30;

    // 表示領域
    [SerializeField] Rect m_Area = new Rect(220, 0, 400, 400);

    // ログの文字列を入れておくためのQueue
    Queue<string> m_LogMessages = new Queue<string>();

    // ログの文字列を結合するのに使う
    StringBuilder m_StringBuilder = new StringBuilder();
 
    void Start()
    {
        // Application.logMessageReceivedに関数を登録しておくと、
        // ログが出力される際に呼んでくれる
        Application.logMessageReceived += LogReceived;
    }

    // ログが出力される際に呼んでもらう関数
    void LogReceived(string text, string stackTrace, LogType type)
    {
        // ログをQueueに追加
        m_LogMessages.Enqueue(text);

        // ログの個数が上限を超えていたら、最古のものを削除する
        while(m_LogMessages.Count > m_MaxLogCount)
        {
            m_LogMessages.Dequeue();
        }
    }

    void OnGUI()
    {
        // StringBuilderの内容をリセット
        m_StringBuilder.Length = 0;

        // ログの文字列を結合する（1個ごとに末尾に改行を追加）
        foreach (string s in m_LogMessages)
        {
            m_StringBuilder.Append(s).Append(System.Environment.NewLine);
        }

        // 画面に表示
        GUI.Label(m_Area, m_StringBuilder.ToString());
    }
}