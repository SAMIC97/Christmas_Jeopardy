using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject teamsPanelPrefab;     // Prefab for team display (e.g., scores, coins)
    public Transform teamsPanelParent;      // Parent panel to hold team score displays
    public Text coinCountText;              // Reference to the coin count text
    //public Sprite[] teamIcons;            //In case of using icons to use for team setup

    [Header("Game Settings")]
    public int totalQuestions = 30;         // Total number of questions for the game
    public int questionValue;               // Current question value

    [Header("Team Management")]
    public int currentTeamIndex = 0;        // Index of the current team's turn
    public int currentStealTeamIndex;       // Index of the team attempting to steal
    private Queue<int> stealQueue;          // Queue to manage stealing order

    [Header("Track Progress")]
    public int answeredQuestions = 0;       // Number of questions answered

    // Singleton instance
    public bool noCoins = false;
    public static GameManager Instance { get; private set; }

    private UIManager uiManager;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance alive across scenes
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);
    }

    #region Team Setup and UI Updates

    /// <summary>
    /// Creates the team score panels and initializes their data.
    /// </summary>
    /// <param name="teamCount">Number of teams in the game.</param>
    public void CreateScorePanels(int teamCount)
    {
        // Clear existing panels
        foreach (Transform child in teamsPanelParent)
        {
            Destroy(child.gameObject);
        }

        // Clear team list
        uiManager.teams.Clear();

        for (int i = 0; i < teamCount; i++)
        {
            // Create a new team
            Team newTeam = new Team { teamName = "Equipo " + (i + 1) };
            uiManager.teams.Add(newTeam);

            // Instantiate the score panel
            GameObject scorePanel = Instantiate(teamsPanelPrefab, teamsPanelParent);

            // Set the team name and initial score
            Transform teamPanel = teamsPanelParent.GetChild(i);
            Text scoreText = teamPanel.Find("Panel Scores/Text Score").GetComponent<Text>();
            scoreText.text = $"{newTeam.teamName}: 0";

            // Set the initial coin count
            Text coinText = teamPanel.Find("Panel Coins/Text Coins").GetComponent<Text>();
            coinText.text = newTeam.coins.ToString();

            // Store the score panel in the team
            newTeam.scorePanel = scorePanel;
        }
    }

    /// <summary>
    /// Updates the UI to highlight the active team's turn.
    /// </summary>
    public void UpdateTurnDisplay()
    {
        Color activeColor = new Color(0f, 1f, 0f, 0.5f);  // Semi-transparent green
        Color inactiveColor = new Color(1f, 1f, 1f, 0f);   // Fully transparent white

        // Highlight the current team's panel
        for (int i = 0; i < uiManager.teams.Count; i++)
        {
            uiManager.teams[i].scorePanel.GetComponent<Image>().color =
                i == currentTeamIndex ? activeColor : inactiveColor;
        }

        Debug.Log($"{uiManager.teams[currentTeamIndex].teamName}'s turn to play!");
        Debug.Log($"{uiManager.teams[currentTeamIndex].coins} coins available.");
    }

    /// <summary>
    /// Updates the score of a specified team.
    /// </summary>
    /// <param name="teamIndex">Index of the team.</param>
    /// <param name="points">Points to add to the team's score.</param>
    public void UpdateScore(int teamIndex, int points)
    {
        uiManager.teams[teamIndex].score += points;

        // Update score display in UI
        Transform teamPanel = teamsPanelParent.GetChild(teamIndex);
        Text scoreText = teamPanel.Find("Panel Scores/Text Score").GetComponent<Text>();
        scoreText.text = $"{uiManager.teams[teamIndex].teamName}: {uiManager.teams[teamIndex].score}";
    }

    /// <summary>
    /// Updates the coin count for a specified team.
    /// </summary>
    /// <param name="teamIndex">Index of the team.</param>
    public void UpdateCoins(int teamIndex)
    {
        if (uiManager.teams[teamIndex].coins < 5)
        {
            uiManager.teams[teamIndex].GainCoin();
            UpdateCoinCount(teamIndex, uiManager.teams[teamIndex].coins);
        }

        Debug.Log($"Total Coins: {uiManager.teams[teamIndex].coins}");
    }

    /// <summary>
    /// Updates the displayed coin count for a team.
    /// </summary>
    /// <param name="teamIndex">Index of the team.</param>
    /// <param name="coins">New coin count.</param>
    public void UpdateCoinCount(int teamIndex, int coins)
    {
        Transform teamPanel = teamsPanelParent.GetChild(teamIndex);
        Text coinCountText = teamPanel.Find("Panel Coins/Text Coins").GetComponent<Text>();
        coinCountText.text = coins.ToString();
        Debug.Log($"Update Coins of: {uiManager.teams[teamIndex].coins} has {coins}");
    }

    #endregion

    #region Turn Management

    /// <summary>
    /// Ends the current team's turn and moves to the next team.
    /// </summary>
    //End turn of current team and proceed with next one
    public void EndTurn()
    {
        // End the current team's turn
        uiManager.teams[currentTeamIndex].isTurn = false;

        // Move to the next team
        currentTeamIndex = (currentTeamIndex + 1) % uiManager.teams.Count;
        uiManager.teams[currentTeamIndex].isTurn = true;

        // Update UI to reflect the new team's turn
        UpdateTurnDisplay();
    }

    /// <summary>
    /// Checks if the game has ended by verifying answered questions.
    /// </summary>
    public void CheckForGameEnd()
    {
        if (answeredQuestions == totalQuestions)
        {
            uiManager.ShowWinningPanel();
            answeredQuestions = 0;
        }
    }
    #endregion

    #region Steal Logic

    /// <summary>
    /// Initiates the steal phase for eligible teams.
    /// </summary>
    /// <param name="questionValue">Value of the question to be stolen.</param>
    public void AttemptStealQuestion(int questionValue)
    {
        stealQueue = new Queue<int>();
        int totalTeams = uiManager.teams.Count;

        // Check eligibility for stealing
        for (int i = 0; i < totalTeams; i++)
        {
            int teamIndex = (currentTeamIndex + 1 + i) % totalTeams;
            if (teamIndex != currentTeamIndex && uiManager.teams[teamIndex].coins >= GetCoinCost(questionValue))
            {
                stealQueue.Enqueue(teamIndex);
            }
        }

        if (stealQueue.Count == 0)
        {
            Debug.Log("No teams can afford to steal.");
            noCoins = true;
            EndStealPhase();
            return;
        }

        StartCoroutine(DelayBeforeSteal(stealQueue, questionValue));
    }

    private IEnumerator DelayBeforeSteal(Queue<int> eligibleTeams, int questionValue)
    {
        //Play Grinch laugh with steal message image
        AudioManager.Instance.PlaySFX(AudioManager.Instance.grichLaughSFX);

        // Show a transition panel during the delay
        uiManager.stealMessagePanel.SetActive(true);

        yield return new WaitForSeconds(3.0f); // Wait for 3 seconds

        // Hide the transition panel before showing the steal panel
        uiManager.stealMessagePanel.SetActive(false);

        // Start processing steal attempts
        StartCoroutine(ProcessStealAttempts(eligibleTeams, questionValue));
    }

    private IEnumerator ProcessStealAttempts(Queue<int> eligibleTeams, int questionValue)
    {
        stealQueue = new Queue<int>(eligibleTeams);
        this.questionValue = questionValue;

        while (stealQueue.Count > 0)
        {
            currentStealTeamIndex = stealQueue.Dequeue();

            // Update panel message
            uiManager.stealMessageText.text = $"Equipo {uiManager.teams[currentStealTeamIndex].teamName}. ¿Quieres robar esta pregunta por {GetCoinCost(questionValue)} moneda(s)?";

            // Show the steal panel and wait for input
            uiManager.stealPanel.SetActive(true);

            //Hide question buttons to avoid a false answer
            uiManager.buttonAnswerPanel.SetActive(false);

            yield return new WaitUntil(() => uiManager.stealPanel.activeSelf == false); // Wait until the panel is closed

            // If a team steals, stop further attempts
            if (uiManager.teamHasStolen)
            {
                uiManager.teamHasStolen = false; // Reset flag for future steals
                yield break;
            }
        }

        Debug.Log("No team chose to steal.");
        EndStealPhase(); // No team chose to steal, end the phase
    }

    /// <summary>
    /// Handles stealing logic when a team decides to steal.
    /// </summary>
    public void HandleSteal(int teamIndex, int questionValue)
    {
        int coinCost = GetCoinCost(questionValue);
        uiManager.teams[teamIndex].coins -= coinCost;
        UpdateCoinCount(teamIndex, uiManager.teams[teamIndex].coins);

        Debug.Log($"Team {uiManager.teams[teamIndex].teamName} stole the question!");
        QuestionManager.Instance.ResetQuestionForStealing();
    }

    private int GetCoinCost(int questionValue)
    {
        return questionValue / 200; // 1 coin per 200 points (e.g., 200 -> 1, 400 -> 2, ...)
    }

    // Ends the steal phase
    private void EndStealPhase()
    {
        Debug.Log("Steal phase ended. Continuing game.");
        uiManager.stealPanel.SetActive(false); // Ensure panel is hidden
        uiManager.returnButton.gameObject.SetActive(true);  // Show the Return button after answering
        uiManager.buttonAnswerPanel.SetActive(true);
        EndTurn(); // Continue to the next turn
    }
    #endregion
}