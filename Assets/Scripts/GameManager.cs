using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GameObject teamsPanelPrefab;     // Prefab for team display w/image and text
    public InputField teamInputField;       // Input field in teamSetupPanel
    public Transform teamsPanelParent;      // Panel to display the number of teams in Gameboard
    //public Sprite[] teamIcons;              //Icons to use for team setup
    public Text coinCountText; // Reference to the coin count text

    public int answeredQuestions = 0;       // Track the number of answered questions
       
    private int totalQuestions = 30;        // Set this to the total number of questions
    private int currentTeamIndex = 0;       // To keep track of the team's turn
    private UIManager uiManager;
    public static GameManager Instance { get; private set; }

    [SerializeField] private int maxTeams = 10; // Limit the Number of Teams allowed
    [SerializeField] private Text placeholderText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep this instance alive across scenes (optional)
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        uiManager = FindObjectOfType<UIManager>();
        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);
        teamInputField.onValueChanged.AddListener(ValidateInput); // Subscribe to the InputField's value change event
    }

    private IEnumerator ResetPlaceholderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        placeholderText.text = "Ingrese número de equipos";
    }

    private void ValidateInput(string input)
    {
        // Remove non-numeric characters
        string numericInput = "";
        foreach (char c in input)
        {
            if (char.IsDigit(c)) numericInput += c;
        }

        // Limit the number of teams to maxTeams
        if (int.TryParse(numericInput, out int numTeams))
        {
            if (numTeams > maxTeams)
            {
                // Clear the input and show placeholder message
                numericInput = "";
                placeholderText.text = "Se permiten máximo 10 equipos.";
                StartCoroutine(ResetPlaceholderAfterDelay(2f)); // Reset after 2 seconds
            }
            else
            {
                // Reset placeholder to default if input is valid
                placeholderText.text = "Ingrese número de equipos";
            }
        }

        // Update the InputField text
        teamInputField.text = numericInput;
    }

    public void OnConfirmTeams()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        int teamCount;
        if (int.TryParse(teamInputField.text, out teamCount) && teamCount > 0)
        {
            CreateScorePanels(teamCount);

            uiManager.teamSetupPanel.SetActive(false);
            uiManager.gameBoardPanel.SetActive(true);

            AudioManager.Instance.PlayMusic(AudioManager.Instance.backgroundMusic);

            // Set the first team as the one to start
            uiManager.teams[currentTeamIndex].isTurn = true;
            UpdateTurnDisplay();
        }
        else
        {
            Debug.LogError("Invalid team count entered.");
        }
    }

    void CreateScorePanels(int teamCount)
    {
        foreach (Transform child in teamsPanelParent)
        {
            Destroy(child.gameObject);
        }

        uiManager.teams.Clear();
        List<int> usedIcons = new List<int>();

        for (int i = 0; i < teamCount; i++)
        {
            // Create a new team
            Team newTeam = new Team { teamName = "Team " + (i + 1) };
            uiManager.teams.Add(newTeam);

            // Instantiate the score panel
            GameObject scorePanel = Instantiate(teamsPanelPrefab, teamsPanelParent);

            // Set the team name in the score panel
            Transform teamPanel = teamsPanelParent.GetChild(i); // Get the current team's panel
            Text scoreText = teamPanel.Find("Panel Scores/Text Score").GetComponent<Text>(); // Navigate the hierarchy
            scoreText.text = newTeam.teamName + ": 0";

            // Set the coin in the coins panel
            Text coinText = teamPanel.Find("Panel Coins/Text Coins").GetComponent<Text>(); // Navigate the hierarchy
            coinText.text = newTeam.coins.ToString();

            /*
            // Assign a random icon
            Transform iconPanel = scorePanel.transform.Find("Panel Icon/Image Icon");
            if (iconPanel != null)
            {
                Image iconImage = iconPanel.GetComponent<Image>();
                int randomIndex;
                do
                {
                    randomIndex = Random.Range(0, teamIcons.Length);
                } while (usedIcons.Contains(randomIndex)); // Ensure unique icons
                usedIcons.Add(randomIndex);
                iconImage.sprite = teamIcons[randomIndex];
            }
            else
            {
                Debug.LogError("Icon not found in prefab! Check your hierarchy.");
            }*/

            newTeam.scorePanel = scorePanel;  // Store the score panel in the team
        }
    }

    void UpdateTurnDisplay()
    {
        Color activeColor = new Color(0f, 1f, 0f, 0.5f);  // Green with 50% transparency
        Color inactiveColor = new Color(1f, 1f, 1f, 0f);   // White with full opacity

        // Highlight the current team's panel
        for (int i = 0; i < uiManager.teams.Count; i++)
        {
            if (i == currentTeamIndex)
            {
                // Change color to indicate this team's turn
                uiManager.teams[i].scorePanel.GetComponent<Image>().color = activeColor;
            }
            else
            {
                // Reset color for other teams
                uiManager.teams[i].scorePanel.GetComponent<Image>().color = inactiveColor;
            }
        }

        Debug.Log(uiManager.teams[currentTeamIndex].teamName + "'s turn to play!");
        Debug.Log(uiManager.teams[currentTeamIndex].coins + "'s coins");
    }

    public void UpdateScore(int points)
    {
        // Add points to the current team's score
        uiManager.teams[currentTeamIndex].score += points;

        // Update score display in UI
        Transform teamPanel = teamsPanelParent.GetChild(currentTeamIndex); // Get the current team's panel
        Text scoreText = teamPanel.Find("Panel Scores/Text Score").GetComponent<Text>(); // Navigate the hierarchy
        scoreText.text = uiManager.teams[currentTeamIndex].teamName + ": " + uiManager.teams[currentTeamIndex].score;
    }

    public void UpdateCoins()
    {
        // Award 1 coin if the answering team is the current team
        if (uiManager.teams[currentTeamIndex].coins < 5)
        {
            uiManager.teams[currentTeamIndex].GainCoin();
            UpdateTeamCoins(currentTeamIndex);
        }

        Debug.Log("Coins Total: " + uiManager.teams[currentTeamIndex].coins);
    }

    public void UpdateTeamCoins(int teamIndex)
    {
        int coins = uiManager.teams[teamIndex].coins;
        UpdateCoinCount(coins);
    }

    // Method to update the displayed coin count
    public void UpdateCoinCount(int coins)
    {
        Transform teamPanel = teamsPanelParent.GetChild(currentTeamIndex); // Get the current team's panel
        Text coinCountText = teamPanel.Find("Panel Coins/Text Coins").GetComponent<Text>(); // Navigate the hierarchy
        coinCountText.text = coins.ToString();
    }

    public void AttemptSteal(Team stealingTeam, int questionPoints)
    {
        int stealCost = questionPoints / 200; // Determine cost based on points (200 = 1 coin, etc.)

        // Check if the stealing team has enough coins
        if (!stealingTeam.SpendCoins(stealCost))
        {
            Debug.Log($"{stealingTeam.teamName} doesn't have enough coins to steal.");
            return;
        }

        Debug.Log($"{stealingTeam.teamName} is attempting to steal!");

        // Show the question to the stealing team and set up for their response
        //ShowQuestionToStealingTeam(stealingTeam);
    }

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

    //Checks if game it's over
    public void CheckForGameEnd()
    {
        if (answeredQuestions >= totalQuestions)
        {
            uiManager.ShowWinningPanel();
            totalQuestions = 3;
            answeredQuestions = 0;
        }
    }
}