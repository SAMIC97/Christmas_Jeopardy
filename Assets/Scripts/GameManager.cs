using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public GameObject teamsPanelPrefab;     // Prefab for team display w/image and text
    public InputField teamInputField;       // Input field in teamSetupPanel
    public Transform teamsPanelParent;      // Panel to display the number of teams in Gameboard
    public Sprite[] teamIcons;              //Icons to use for team setup

    public int answeredQuestions = 0;       // Track the number of answered questions
       
    private int totalQuestions = 3;        // Set this to the total number of questions
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
            Text scoreText = scorePanel.GetComponentInChildren<Text>();
            scoreText.text = newTeam.teamName + ": 0";

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
            }

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
    }

    public void UpdateScore(int points)
    {
        // Add points to the current team's score
        uiManager.teams[currentTeamIndex].score += points;

        // Update score display in UI
        Text scoreText = teamsPanelParent.GetChild(currentTeamIndex).GetComponentInChildren<Text>();
        scoreText.text = uiManager.teams[currentTeamIndex].teamName + ": " + uiManager.teams[currentTeamIndex].score;
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