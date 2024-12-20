using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    //===============================
    // VARIABLES
    //===============================

    // Panels
    public GameObject homePanel;           // Initial Game Panel
    public GameObject teamSetupPanel;      // Panel to input number of teams
    public GameObject gameBoardPanel;      // Panel with game board
    public GameObject instructionsPanel;   // Panel with Instructions
    public GameObject winningPanel;        // Panel to display the winning team
    public GameObject questionPanel;       // Panel to display questions
    public GameObject buttonAnswerPanel;   // Panel with buttons for answers
    public GameObject stealPanel;          // Panel for steal logic
    public GameObject stealMessagePanel;   // Panel for steal messages
    public GameObject timedOutPanel;       // Panel for timeout messages

    // UI Elements
    public InputField teamInputField;      // Input field for number of teams
    public Button returnButton;            // Button to return to the Game Board
    public Button startButton;             // Start button in initial game panel
    public Button confirmTeamsButton;      // Button to confirm number of teams
    public Text winningText;               // Text for the winning message
    public Text placeholderText;           // Placeholder text for the InputField
    public Text stealMessageText;          // Text inside the StealPanel

    // Effects
    public ParticleSystem snowEffect;      // Snow particle effect

    // Other Variables
    public List<Team> teams = new List<Team>();
    public bool teamHasStolen = false;     // Flag to track if a team has stolen
    private bool victory = false;          // Victory flag

    // Serialized Variables
    [SerializeField] private int maxTeams = 10; // Maximum number of teams
    [SerializeField] private Transform panelBoard; // Reference to the "Panel Board"
    [SerializeField] private float flashDuration = 0.5f; // Duration of the flash effect
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.Linear(0, 1, 1, 0); // Flash effect curve

    // Game Manager Reference
    private GameManager gameManager;

    //===============================
    // UNITY METHODS
    //===============================

    private void Start()
    {
        // Initialize GameManager reference
        gameManager = GameManager.Instance;

        // Set initial UI states
        ToggleSnowEffect(true);
        homePanel.SetActive(true);
        teamSetupPanel.SetActive(false);
        gameBoardPanel.SetActive(false);
        returnButton.gameObject.SetActive(false);

        // Subscribe to events
        teamInputField.onValueChanged.AddListener(ValidateInput);
        startButton.onClick.AddListener(ShowTeamSetupPanel);
        confirmTeamsButton.onClick.AddListener(OnConfirmTeams);
    }

    //===============================
    // PANEL MANAGEMENT
    //===============================
    public void ShowInstructions()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        instructionsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        Application.Quit();
    }

    public void ShowTeamSetupPanel()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        homePanel.SetActive(false);
        teamSetupPanel.SetActive(true);
    }

    public void ReturnHomeMenu(GameObject panelToDeactivate)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        if (victory)
        {
            ResetGame();
        }
        else
        {
            panelToDeactivate.SetActive(false);
            homePanel.SetActive(true);
        }
    }

    public void ShowWinningPanel()
    {
        // Calculate the winner(s)
        int highestScore = 0;
        List<string> winningTeamNames = new List<string>();

        foreach (Team team in teams)
        {
            if (team.score > highestScore)
            {
                highestScore = team.score;
                winningTeamNames.Clear();
                winningTeamNames.Add(team.teamName);
            }
            else if (team.score == highestScore)
            {
                winningTeamNames.Add(team.teamName);
            }
        }

        // Display the winning message
        AudioManager.Instance.PlayMusic(AudioManager.Instance.winnerMusic);
        if (winningTeamNames.Count == 1)
        {
            winningText.text = $"¡Felicidades, {winningTeamNames[0]}! Ganaron con {highestScore} puntos!";
        }
        else
        {
            string teams = string.Join(", ", winningTeamNames);
            winningText.text = $"¡Es un empate entre {teams}! Cada uno obtuvo {highestScore} puntos!";
        }

        winningPanel.SetActive(true);
        gameBoardPanel.SetActive(false);
        victory = true;
    }

    public void ReturnToGameBoard()
    {
        ShowGameBoard();
        GameManager.Instance.answeredQuestions++;
        GameManager.Instance.CheckForGameEnd();
    }

    void ShowGameBoard()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        gameBoardPanel.SetActive(true);
        questionPanel.SetActive(false);
    }

    public void ShowQuestionPanel()
    {
        gameBoardPanel.SetActive(false);
        questionPanel.SetActive(true);
    }

    //===============================
    // TEAM MANAGEMENT
    //===============================

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

    private IEnumerator ResetPlaceholderAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        placeholderText.text = "Ingrese número de equipos";
    }

    public void OnConfirmTeams()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        int teamCount;
        if (int.TryParse(teamInputField.text, out teamCount) && teamCount > 0)
        {
            gameManager.CreateScorePanels(teamCount);

            ToggleSnowEffect(false);
            teamSetupPanel.SetActive(false);
            gameBoardPanel.SetActive(true);

            AudioManager.Instance.PlayMusic(AudioManager.Instance.backgroundMusic);

            // Set the first team as the one to start
            teams[gameManager.currentTeamIndex].isTurn = true;
            gameManager.UpdateTurnDisplay();
        }
        else
        {
            Debug.LogError("Invalid team count entered.");
        }
    }

    public void ResetGame()
    {

        // Reset questions for replay
        QuestionManager.Instance.ResetQuestions();

        // Clear the list of teams
        teams.Clear();

        // Reset team input field (assuming you use UIManager for handling this)
        ResetTeamInputField();

        ResetCategoryButtons(panelBoard);

        // Hide the Winning Panel and return to the main menu
        winningPanel.SetActive(false);
        gameBoardPanel.SetActive(false);
        homePanel.SetActive(true);
        ToggleSnowEffect(true);

        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);

        // Reset any other UI elements or game variables
        victory = false;
    }

    void ResetTeamInputField()
    {
        teamInputField.text = ""; // Clear the input field
        foreach (Transform child in GameManager.Instance.teamsPanelParent)
        {
            Destroy(child.gameObject); // Remove all instantiated team UI elements
        }
    }

    void ResetCategoryButtons(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // If the child has a Button component, reset it
            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true; // Reset button to be interactable
            }

            // Recursively check the child's children
            if (child.childCount > 0)
            {
                ResetCategoryButtons(child);
            }
        }
    }

    public void ToggleSnowEffect(bool isActive)
    {
        if (snowEffect == null)
        {
            Debug.LogError("SnowEffect is not assigned in UIManager!");
            return;
        }

        if (isActive)
        {
            if (!snowEffect.isPlaying)
                snowEffect.Play();
        }
        else
        {
            if (snowEffect.isPlaying)
                snowEffect.Stop();
        }
    }

    //===============================
    // ANSWER UI MANAGMENT
    //===============================
    public void HighlightCorrectAnswer(List<Button> answerButtons, Question currentQuestion)
    {
        // Log to check how many answer buttons there are and the correct answer
        Debug.Log("Highlighting correct answer...");
        
        foreach (Button button in answerButtons)
        {
            // Log the button name/text for debugging
            Debug.Log($"Checking button: {button.GetComponentInChildren<Text>().text}");

            if (button.GetComponentInChildren<Text>().text == currentQuestion.correctAnswer)
            {
                button.GetComponent<Image>().color = Color.green;
                break;
            }
        }
    }

    public void ShowTimeoutMessage()
    {
        // Start the flash effect
        StartCoroutine(FlashEffect());
    }

    public IEnumerator FlashEffect()
    {
        timedOutPanel.SetActive(true);

        Image flashImage = timedOutPanel.transform.Find("Flash Image").GetComponent<Image>();

        // Flash twice
        int flashes = 2; // Number of flashes
        for (int i = 0; i < flashes; i++)
        {
            // Ensure the image is fully visible at the start of each flash
            flashImage.color = new Color(flashImage.color.r, flashImage.color.g, flashImage.color.b, 1);
            AudioManager.Instance.PlaySFX(AudioManager.Instance.wrongAnswerSFX);

            // Gradually fade out the image
            float elapsedTime = 0;
            while (elapsedTime < flashDuration)
            {
                elapsedTime += Time.deltaTime;

                // Use flashCurve to evaluate alpha if available, otherwise use linear interpolation
                float alpha = flashCurve != null
                    ? flashCurve.Evaluate(elapsedTime / flashDuration)
                    : Mathf.Lerp(1, 0, elapsedTime / flashDuration);

                flashImage.color = new Color(flashImage.color.r, flashImage.color.g, flashImage.color.b, alpha);
                yield return null;
            }

            // Ensure the image is fully transparent at the end of each flash
            flashImage.color = new Color(flashImage.color.r, flashImage.color.g, flashImage.color.b, 0);

            // Optionally, add a short delay between flashes
            yield return new WaitForSeconds(0.2f);
        }

        timedOutPanel.SetActive(false);
    }

    //===============================
    // STEAL POINTS LOGIC
    //===============================
    public void OnYesButtonClicked()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        teamHasStolen = true; // Mark as stolen
        Debug.Log($"Team {teams[GameManager.Instance.currentStealTeamIndex].teamName} decided to steal!");
        stealPanel.SetActive(false); // Close the panel
        GameManager.Instance.HandleSteal(GameManager.Instance.currentStealTeamIndex, GameManager.Instance.questionValue);
        buttonAnswerPanel.SetActive(true);
    }

    public void OnNoButtonClicked()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);
        Debug.Log($"Team {teams[GameManager.Instance.currentStealTeamIndex].teamName} declined to steal.");
        stealPanel.SetActive(false); // Close the panel
    }
}
