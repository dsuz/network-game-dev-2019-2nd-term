using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;   // UnityWebRequest を使うために追加した
using UnityEngine.UI;

/// <summary>
/// ランキングシステムを管理するクラス
/// </summary>
public class RankingManager : MonoBehaviour
{
    /// <summary>Web API の URL</summary>
    [SerializeField] string m_url = "http://localhost:1337/ranking";
    /// <summary>ランキングを表示する Text</summary>
    [SerializeField] Text m_rankingText;
    /// <summary>名前を入力するフィールド</summary>
    [SerializeField] InputField m_nameInput;
    /// <summary>名前の登録を行うためのオブジェクトが配置されたパネル</summary>
    [SerializeField] RectTransform m_entryPanel;
    /// <summary>ランキング情報の配列</summary>
    RankInfo[] m_ranking;
    /// <summary>今回のスコア</summary>
    int m_score;

    void Start()
    {
        
    }

    /// <summary>
    /// ランキングシステムを閉じる
    /// </summary>
    public void CloseRanking()
    {
        if (!m_entryPanel.gameObject.activeSelf)    // エントリーが表示されている間は閉じさせない
        {
            Destroy(this.gameObject);
        }
    }

    /// <summary>
    /// ランキングを取得する
    /// ランキングをサーバーから取ってきて、ランクインしてたら名前を入力する画面を表示する
    /// </summary>
    /// <param name="score">今回のスコア。0 の場合はランキング入力画面は出ない</param>
    public void GetRanking(int score)
    {
        m_score = score;
        StartCoroutine(GetRankingImpl(score));
    }

    /// <summary>
    /// ランキングを取得する
    /// ランキングをサーバーから取ってきて、ランクインしてたら名前を入力する画面を表示する
    /// </summary>
    /// <param name="score">今回のスコア。0 の場合はランキング入力画面は出ない</param>
    IEnumerator GetRankingImpl(int score)
    {
        // 指定の URL に GET リクエストをする
        UnityWebRequest req = UnityWebRequest.Get(m_url);
        yield return req.SendWebRequest();  // 非同期で待つ

        if (req.isHttpError || req.isNetworkError)  // エラーだったらその内容を表示する
        {
            Debug.LogError(req.error);
        }
        else
        {
            // エラーなく応答が受け取れたら、応答の JSON を逆シリアライズしてランキングを作り、表示する
            string json = "{ \"Items\": " + req.downloadHandler.text + "}";
            Debug.Log("Ranking Data(JSON):\r\n" + json);
            m_ranking = JsonHelper.FromJson<RankInfo>(json);    // 逆シリアライズする
            Debug.Log("Ranking Data Length:\r\n" + m_ranking.Length.ToString());
            MakeRankingText();

            // ランキングの一番下より点数が大きい場合は
            if (m_ranking.Length == 0 || m_ranking[m_ranking.Length - 1].score < score || (m_ranking.Length < 10 && score > 0)) // ランキング登録がない場合は 0 点でも強制的に登録する。ランキング登録が 10 個以下の時も登録する。
            {
                m_entryPanel.gameObject.SetActive(true);    // エントリーパネルを表示する
            }
        }
    }

    /// <summary>
    /// ランキング情報の配列から、ランキング情報のテキストを作って表示する
    /// </summary>
    void MakeRankingText()
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < m_ranking.Length; i++)
        {
            builder.Append((i + 1).ToString());
            builder.Append(" : ");
            builder.Append(m_ranking[i].name);
            builder.AppendLine(m_ranking[i].score.ToString());
        }
        Debug.Log("Ranking Text:\r\n" + builder.ToString());
        m_rankingText.text = builder.ToString();
    }

    /// <summary>
    /// 今回のスコアを登録する。ランキングシステムを呼び出したら、まずこの関数を呼び出す。
    /// これを行うと、ランキングの取得や表示が始まる
    /// </summary>
    /// <param name="score"></param>
    public void SetScoreOfCurrentPlay(int score)
    {
        GetRanking(score);
    }

    /// <summary>
    /// ハイスコアの名前登録を行う
    /// </summary>
    public void Entry()
    {
        StartCoroutine(EntryImpl());
    }

    /// <summary>
    /// ハイスコアの名前登録を行う
    /// </summary>
    IEnumerator EntryImpl()
    {
        // POST するデータを作る
        WWWForm form = new WWWForm();
        form.AddField("name", m_nameInput.text);
        form.AddField("score", m_score.ToString());
        
        // 指定の URL に POST リクエストを送る
        UnityWebRequest req = UnityWebRequest.Post(m_url, form);
        yield return req.SendWebRequest();

        if (req.isHttpError || req.isNetworkError)  // エラーだったらその内容を表示する
        {
            Debug.LogError(req.error);
        }
        else
        {
            // 正常終了したら、エントリー画面を消してランキングをリロードする
            Debug.Log(req.downloadHandler.text);
            m_entryPanel.gameObject.SetActive(false);   // エントリー画面を消す
            GetRanking(0);  // ランキングをリロードする
        }
    }

    /// <summary>
    /// JSON で配列を扱うテスト
    /// </summary>
    public void Test()
    {
        RankInfo[] rank =
        {
            new RankInfo("a", 5), new RankInfo("b", 8),
        };

        string json = JsonHelper.ToJson<RankInfo>(rank);
        Debug.Log(json);
        rank = JsonHelper.FromJson<RankInfo>(json);
    }
}

/// <summary>
/// ランキング情報（一人分）
/// </summary>
[Serializable]
public class RankInfo
{
    public string name;
    public int score;

    public RankInfo(string name, int score)
    {
        this.name = name;
        this.score = score;
    }
}