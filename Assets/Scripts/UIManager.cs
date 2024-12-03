using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject homePanel;           // Initial Game Panel
    public GameObject teamSetupPanel;       // Panel to input number of teams
    public GameObject gameBoardPanel;       // Panel with game board
    public GameObject instructionsPanel;    // Panel with Instructions
    public GameObject WinningPanel;         // Reference to the Winning Panel UI
    public GameObject QuestionPanel;       // Reference to the Question Panel

    public Button returnButton;            // Button to return to the Game Board
    public Button startButton;              // Start button in initial game panel
    public Button confirmTeamsButton;       // Button to submit number teams in teamSetUpPanel
    public Text winningText;                // Text to display the winning team

    public List<Team> teams = new List<Team>();

    [SerializeField] private Transform panelBoard; // Reference to the "Panel Board" object
    [SerializeField] private GameObject timedOutPanel; // Reference to the Panel with child Image for the flash
    [SerializeField] private float flashDuration = 0.5f; // Duration of the flash effect
    [SerializeField] private AnimationCurve flashCurve = AnimationCurve.Linear(0, 1, 1, 0); // Effect for a smooth flash transition

    private bool victory = false;
    // Reference to GameManager to call methods on team confirmation
    private GameManager gameManager;

    private void Start()
    {
        // Initialize GameManager reference
        gameManager = GameManager.Instance;

        homePanel.SetActive(true);
        teamSetupPanel.SetActive(false);
        gameBoardPanel.SetActive(false);
        returnButton.gameObject.SetActive(false);  // Hide the Return button initially

        startButton.onClick.AddListener(ShowTeamSetupPanel);
        confirmTeamsButton.onClick.AddListener(gameManager.OnConfirmTeams);
    }
    //When user press Instructions Button
    public void ShowInstructions()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        instructionsPanel.SetActive(true);
    }

    //When Exit Game Button is pressed
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

    public void ReturnHomeMenu(GameObject panelToDeactivated)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        if(victory)
        {
            ResetGame();
        }
        else
        {
            panelToDeactivated.SetActive(false);
            homePanel.SetActive(true);
        }
    }

    public void ShowWinningPanel()
    {
        int highestScore = 0;
        List<string> winningTeamNames = new List<string>(); // List to store names of teams with the highest score

        // Loop through all teams to find the highest score and the teams with that score
        foreach (Team team in teams)
        {
            if (team.score > highestScore)
            {
                highestScore = team.score;
                winningTeamNames.Clear(); // Clear the list as there's a new highest score
                winningTeamNames.Add(team.teamName); // Add the new leading team
            }
            else if (team.score == highestScore)
            {
                winningTeamNames.Add(team.teamName); // Add to the list for a tie
            }
        }

        // Play winner music
        AudioManager.Instance.PlayMusic(AudioManager.Instance.winnerMusic);

        // Display the winning message
        if (winningTeamNames.Count == 1)
        {
            // Single winner
            winningText.text = $"¡Felicidades, {winningTeamNames[0]}! Ganaron con {highestScore} puntos!";
        }
        else
        {
            // Tie scenario
            string teams = string.Join(", ", winningTeamNames); // Combine team names into a single string
            winningText.text = $"¡Es un empate entre {teams}! Cada uno obtuvo {highestScore} puntos!";
        }

        // Show the Winning Panel and hide the Game Board Panel
        WinningPanel.SetActive(true);
        gameBoardPanel.SetActive(false);

        victory = true;
    }

    public void ReturnToGameBoard()
    {
        ShowGameBoard();
        GameManager.Instance.answeredQuestions++;
        GameManager.Instance.CheckForGameEnd();
    }

    // Function to show the Game Board panel and hide the Question panel
    void ShowGameBoard()
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.buttonClickSFX);

        gameBoardPanel.SetActive(true);
        QuestionPanel.SetActive(false);
    }

    // Function to show the Question panel and hide the Game Board panel
    public void ShowQuestionPanel()
    {
        gameBoardPanel.SetActive(false);
        QuestionPanel.SetActive(true);
    }

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
        //Debug.Log("TIMED OUT!!! INCORRECT ANSWER");

        // Start the flash effect
        StartCoroutine(FlashEffect());
    }

    private IEnumerator FlashEffect()
    {
        timedOutPanel.SetActive(true);

        Image flashImage = timedOutPanel.transform.Find("Red Flash Image").GetComponent<Image>();

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

    public void ResetGame()
    {
        // Clear the list of teams
        teams.Clear();

        // Reset team input field (assuming you use UIManager for handling this)
        ResetTeamInputField();

        ResetCategoryButtons(panelBoard);

        // Hide the Winning Panel and return to the main menu
        WinningPanel.SetActive(false);
        gameBoardPanel.SetActive(false);
        homePanel.SetActive(true);

        AudioManager.Instance.PlayMusic(AudioManager.Instance.mainMenuMusic);

        // Reset any other UI elements or game variables
        //HideTimeoutMessage();
        victory = false;
    }

    void ResetTeamInputField()
    {
        GameManager.Instance.teamInputField.text = ""; // Clear the input field
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
}
