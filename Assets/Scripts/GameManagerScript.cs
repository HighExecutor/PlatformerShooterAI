using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.Text;

public class GameManagerScript : MonoBehaviour
{
    public static GameManagerScript Instance;
    
    [SerializeField] public Transform[] teamSpawnPoints;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject botPrefab;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform mainCamera;
    [SerializeField] private CinemachineVirtualCamera cinemachine;
    [SerializeField] private PlayerStatsUI playerStatsUI;
    [SerializeField] private bool createPlayer;
    [SerializeField] private int teamSize = 1;
    private InputAction resetAction;
    private EnvironmentParameters m_ResetParams;
    List<CharacterStatus> team0Players = new List<CharacterStatus>();
    List<CharacterStatus> team1Players = new List<CharacterStatus>();
    int[] teamScores = new int[2];
    [SerializeField] private GameStatsUI gameStatsUI;
    [SerializeField] private int roundTime = 0;
    
    private void Awake()
    {
        Instance = this;
        resetAction = inputActions.actionMaps[1].actions[0];
    }
    
    void Start()
    {
        InitializeCharacters();
        m_ResetParams = Academy.Instance.EnvironmentParameters;
        Time.timeScale = (int)m_ResetParams.GetWithDefault("time_scale", Time.timeScale);
    }

    void Update()
    {
        if (roundTime > 0 && Time.timeSinceLevelLoad > roundTime)
        {
            ResetScene();
        }
        gameStatsUI.UpdateScore(teamScores[0], teamScores[1]);
        gameStatsUI.UpdateTime(Time.timeSinceLevelLoad);
    }

    private void InitializeCharacters()
    {
        for (int i = 0; i < teamSpawnPoints.Length; i++) // teams
        {
            for (int j = 0; j < teamSize; j++) // players in team
            {
                
                if (i == 0 && j == 0 && createPlayer)
                {
                    GameObject character = Instantiate(playerPrefab, teamSpawnPoints[i].position, Quaternion.identity);
                    character.name = $"Player_{i}_{j}";
                    character.GetComponent<CharacterStatus>().TeamId = i;
                    cinemachine.Follow = character.transform;
                    cinemachine.LookAt = character.transform;
                    character.GetComponent<CharacterStatus>().SetPlayerStatsUi(playerStatsUI);
                    team0Players.Add(character.GetComponent<CharacterStatus>());
                    character.GetComponent<CharacterStatus>().SetGameManager(this);
                }
                else
                {
                    botPrefab.GetComponent<BehaviorParameters>().TeamId = i;
                    botPrefab.GetComponent<CharacterStatus>().TeamId = i;
                    GameObject character = Instantiate(botPrefab, teamSpawnPoints[i].position, Quaternion.identity);
                    character.name = $"Player_{i}_{j}";
                    if (i == 0)
                    {
                        team0Players.Add(character.GetComponent<CharacterStatus>());
                    }
                    else
                    {
                        team1Players.Add(character.GetComponent<CharacterStatus>());
                    }
                    character.GetComponent<CharacterStatus>().SetGameManager(this);
                    if (i == 0 && j == 0)
                    {
                        character.GetComponent<CharacterStatus>().SetPlayerStatsUi(playerStatsUI);
                    }
                }
            }
        }
    }
    
    private void OnEnable()
    {
        resetAction.performed += OnResetScene;
        resetAction.Enable();
    }

    private void OnDisable()
    {
        resetAction.performed -= OnResetScene;
        resetAction.Disable();
    }
    
    private void OnResetScene(InputAction.CallbackContext ctx)
    {
        // Reload the currently active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ResetScene()
    {
        foreach (CharacterStatus character in team0Players)
        {
            AgentScript agent = character.GetComponent<AgentScript>();
            if (agent != null)
            {
                agent.EndEpisode();
            }
        }
        foreach (CharacterStatus character in team1Players)
        {
            AgentScript agent = character.GetComponent<AgentScript>();
            if (agent != null)
            {
                agent.EndEpisode();
            }
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void RegisterDeath(CharacterStatus player)
    {
        teamScores[player.TeamId]++;
    }
}
