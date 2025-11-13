using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SharedData", menuName = "Game/SharedData")]
public class SharedDataScriptableObject : ScriptableObject
{
    [SerializeField] private Color[] teamColors;

    public Color GetTeamColor(int teamId)
    {
        return teamColors[teamId];
    }
}
