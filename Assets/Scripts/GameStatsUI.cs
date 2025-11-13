using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameStatsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI team0ScoreText;
    [SerializeField] private TextMeshProUGUI team1ScoreText;
    [SerializeField] private TextMeshProUGUI timeText;

    public void UpdateScore(int t0, int t1)
    {
        team0ScoreText.text = t0.ToString();
        team1ScoreText.text = t1.ToString();
    }

    public void UpdateTime(float time)
    {
        int seconds = (int)time;
        int minutes = seconds / 60;
        seconds -= minutes * 60;
        timeText.text = $"{minutes}:";
        if (seconds < 10)
        {
            timeText.text += $"0{seconds}";
        }
        else
        {
            timeText.text += $"{seconds}";
        }

        ;
    }
}
