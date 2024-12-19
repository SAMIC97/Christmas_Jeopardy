using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the question display, timer, and answer validation logic.
/// </summary>
public class QuestionManager : MonoBehaviour
{
    [Header("UI Elements")]
    public Text questionText;                 // UI Text to display the question
    public List<Button> answerButtons;       // List of buttons for answer choices
    [SerializeField] private Slider timerSlider; // Slider to visually represent the timer

    [Header("Settings")]
    [SerializeField] private int defaultTime = 20; // Default time for questions if points are invalid

    [Header("Timer Variables")]
    private float timeRemaining;             // Remaining time for the current question
    private bool isTimerActive = false;      // Tracks if the timer is running
    private bool isTimedOut = false;         // Tracks if the timer has reached zero

    [Header("Stealing Phase Variables")]
    private bool isStealPhase = false;       // Tracks if the game is in the stealing phase

    [Header("Question Data")]
    private QuestionData questionData;       // Holds all loaded questions from JSON
    private Question currentQuestion;        // The question currently being displayed
    private QuestionData originalQuestionData; // Backup of original questions

    private UIManager uiManager;             // Reference to UIManager for managing panels and feedback

    // Singleton Instance
    public static QuestionManager Instance { get; private set; }

    #region Unity Callbacks
    private void Awake()
    {
        // Ensure only one instance of the QuestionManager exists
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate instances
        }
    }
    private void Start()
    {
        LoadQuestions();
        uiManager = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        if (isTimerActive)
        {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isTimerActive = false;
                OnTimeUp(); // Handle time up logic
            }
            UpdateTimerDisplay(); // Update the timer slider
        }
    }
    #endregion

    #region Question Logic
    /// <summary>
    /// Loads questions from a JSON file located in the Resources folder.
    /// </summary>
    private void LoadQuestions()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("questions");
        questionData = JsonUtility.FromJson<QuestionData>(jsonText.text);

        // Clone the original data for reset purposes
        originalQuestionData = JsonUtility.FromJson<QuestionData>(jsonText.text);
    }

    /// <summary>
    /// Handles category button clicks to display the appropriate question.
    /// </summary>
    /// <param name="selectedButton">The button that was clicked.</param>
    public void OnCategoryButtonClicked(Button selectedButton)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.categoryClickSFX);

        // Parse the button's name to determine the category and points
        string[] buttonInfo = selectedButton.name.Split('_');
        string categoryName = buttonInfo[0];
        int points = int.Parse(buttonInfo[1]);

        // Find the category and randomly select a question
        foreach (Category category in questionData.categories)
        {
            if (category.name == categoryName)
            {
                var questionsForPoints = category.questions.FindAll(q => q.points == points);

                if (questionsForPoints != null && questionsForPoints.Count > 0)
                {
                    int randomIndex = Random.Range(0, questionsForPoints.Count);
                    currentQuestion = questionsForPoints[randomIndex];

                    // Remove the question from the pool to avoid repetition
                    category.questions.Remove(currentQuestion);

                    // Display the question and disable the button
                    DisplayQuestion();
                    selectedButton.interactable = false;
                    return;
                }
                else
                {
                    Debug.LogWarning($"No available questions for category {categoryName} and points {points}.");
                }
            }
        }
        /*
        foreach (Category category in questionData.categories)
        {
            if (category.name == categoryName)
            {
                foreach (Question question in category.questions)
                {
                    if (question.points == points)
                    {
                        currentQuestion = question;
                        DisplayQuestion();
                        selectedButton.interactable = false; // Disable the button after selection
                        return;
                    }
                }
            }
        }*/
    }

    /// <summary>
    /// Displays the current question and sets up answer buttons.
    /// </summary>
    private void DisplayQuestion()
    {
        questionText.text = currentQuestion.question;

        // Enable and populate answer buttons
        EnableAnswerButtons();
        int numberOfChoices = currentQuestion.choices.Count;

        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (i < numberOfChoices)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = currentQuestion.choices[i];

                string selectedAnswer = currentQuestion.choices[i];
                Button selectedAnswerButton = answerButtons[i];
                int points = currentQuestion.points;

                // Assign button click logic
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => CheckAnswer(selectedAnswer, selectedAnswerButton, points));
                answerButtons[i].GetComponent<Image>().color = Color.white; // Reset button color
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        uiManager.returnButton.gameObject.SetActive(false); // Hide the Return button initially
        uiManager.ShowQuestionPanel(); // Display the Question Panel
        StartTimer(); // Start the question timer
    }

    /// <summary>
    /// Method to restore the original question data.
    /// </summary>
    public void ResetQuestions()
    {
        questionData = JsonUtility.FromJson<QuestionData>(JsonUtility.ToJson(originalQuestionData));
    }
    #endregion

    #region Timer Logic
    /// <summary>
    /// Starts the timer for the current question.
    /// </summary>
    public void StartTimer()
    {
        timeRemaining = isStealPhase ? 5f : GetMaxTimeForCurrentQuestion();
        isTimerActive = true;
        isTimedOut = false;

        if (timerSlider != null)
        {
            timerSlider.value = 1f; // Reset slider
        }

        AudioManager.Instance.StartTickingSound(); // Start ticking sound
    }

    /// <summary>
    /// Updates the timer slider based on the remaining time.
    /// </summary>
    private void UpdateTimerDisplay()
    {
        if (timerSlider != null)
        {
            timerSlider.value = timeRemaining / GetMaxTimeForCurrentQuestion();
        }
    }

    /// <summary>
    /// Handles logic when the timer runs out.
    /// </summary>
    private void OnTimeUp()
    {
        DisableAnswerButtons();
        AudioManager.Instance.StopTickingSound();
        isTimedOut = true;

        if (currentQuestion.hasBeenStolen)
        {
            CheckAnswer("", null, currentQuestion.points); // Process timeout as incorrect answer
            uiManager.ShowTimeoutMessage();
        }
        else
        {
            StartCoroutine(HandleTimeoutWithFlash());
        }
    }

    /// <summary>
    /// Gets the maximum time allowed for the current question based on its point value.
    /// </summary>
    /// <returns>Max time in seconds.</returns>
    private float GetMaxTimeForCurrentQuestion()
    {
        return currentQuestion.points switch
        {
            200 => 10f,
            400 => 20f,
            600 => 30f,
            800 => 40f,
            1000 => 50f,
            _ => defaultTime, // Default if points are invalid
        };
    }
    #endregion

    #region Answer Checking
    /// <summary>
    /// Validates the player's answer and updates scores or initiates stealing logic.
    /// </summary>
    void CheckAnswer(string playerAnswer, Button selectedButton, int questionPoints)
    {
        // Timer and UI adjustments
        AudioManager.Instance.StopTickingSound();
        isTimerActive = false;
        UpdateTimerDisplay();

        // Determine if the answer is correct
        bool isCorrect = false;

        // Check if the question was answered within the time limit
        if (!isTimedOut)
        {
            DisableAnswerButtons(); // Disable answer buttons after the selection

            // Compare player's answer to the correct answer (case insensitive)
            isCorrect = playerAnswer.Equals(currentQuestion.correctAnswer, System.StringComparison.OrdinalIgnoreCase);
        }

        // Handle correct answer scenario
        if (isCorrect)
        {
            HandleCorrectAnswer(selectedButton, questionPoints);
        }
        else
        {
            HandleIncorrectAnswer(selectedButton, questionPoints);
        }

        // Reset timeout flag after processing the answer
        isTimedOut = false;
    }
    private void HandleCorrectAnswer(Button selectedButton, int questionPoints)
    {
        // Play the correct answer sound and change button color to green
        if (selectedButton != null)
        {
            AudioManager.Instance.PlaySFX(AudioManager.Instance.correctAnswerSFX);
            selectedButton.GetComponent<Image>().color = Color.green;
        }

        int teamIndex = GetTeamIndexForCorrectAnswer();

        // Update the score for the team
        GameManager.Instance.UpdateScore(teamIndex, questionPoints);
        
        // End the turn
        GameManager.Instance.EndTurn();

        // Display the return button after answering
        uiManager.returnButton.gameObject.SetActive(true);

        // Handle if the question has been stolen
        if (currentQuestion.hasBeenStolen)
        {
            currentQuestion.hasBeenStolen = false;
            isStealPhase = false; // Exit the steal phase
        }
        else
        {
            GameManager.Instance.UpdateCoins(teamIndex);
        }
    }
    private int GetTeamIndexForCorrectAnswer()
    {
        int teamIndex;

        if (currentQuestion.hasBeenStolen)
        {
            // If the question was stolen, use the stealing team's index
            teamIndex = GameManager.Instance.currentStealTeamIndex;
            Debug.Log("currentStealTeamIndex: " + teamIndex);
        }
        else
        {
            // Otherwise, use the current team index
            teamIndex = GameManager.Instance.currentTeamIndex;
            Debug.Log("currentTeamIndex: " + teamIndex);
        }

        return teamIndex;
    }
    private void HandleIncorrectAnswer(Button selectedButton, int questionPoints)
    {
        // Handle incorrect answer by checking if the question was stolen
        if (selectedButton != null)
        {
            if (currentQuestion.hasBeenStolen)
            {
                // If the question was stolen, show grey button color, highlight the correct answer, and end turn
                selectedButton.GetComponent<Image>().color = Color.grey;
                uiManager.HighlightCorrectAnswer(answerButtons, currentQuestion);
                currentQuestion.hasBeenStolen = false;
                isStealPhase = false;

                // End the turn and show the return button
                uiManager.returnButton.gameObject.SetActive(true);
                GameManager.Instance.EndTurn();
            }
            else
            {
                // If not stolen, attempt to steal the question
                GameManager.Instance.AttemptStealQuestion(questionPoints);
            }

            // Play wrong answer sound
            AudioManager.Instance.PlaySFX(AudioManager.Instance.wrongAnswerSFX);
        }
        else
        {
            // If no button was selected (timed out or something else), handle accordingly
            if (currentQuestion.hasBeenStolen)
            {
                Debug.Log("Timed out second time and null button");
                uiManager.HighlightCorrectAnswer(answerButtons, currentQuestion);  // Highlight the correct answer
                currentQuestion.hasBeenStolen = false;
                isStealPhase = false; // Exit the steal phase
                uiManager.returnButton.gameObject.SetActive(true);  // Show the Return button after answering
                GameManager.Instance.EndTurn();
            }
            else
            {
                GameManager.Instance.AttemptStealQuestion(questionPoints);
            }
        }
    }
    #endregion

    #region Helper Methods
    public void DisableAnswerButtons()
    {
        foreach (Button button in answerButtons)
        {
            button.interactable = false;
        }
    }

    public void EnableAnswerButtons()
    {
        foreach (Button button in answerButtons)
        {
            button.interactable = true;
        }
    }
    private IEnumerator HandleTimeoutWithFlash()
    {
        // Show flash effect (e.g., 0.5 seconds duration)
        yield return StartCoroutine(uiManager.FlashEffect());

        // Add a delay before showing the steal panel
        yield return new WaitForSeconds(0.5f);

        // Show the steal panel
        GameManager.Instance.AttemptStealQuestion(currentQuestion.points);
    }
    #endregion

    #region Stealing Logic
    public void ResetQuestionForStealing()
    {
        Debug.Log("Resetting question for stealing team.");

        currentQuestion.hasBeenStolen = true;
        isStealPhase = true; // Set steal phase active

        // Display the current question again
        questionText.text = currentQuestion.question;

        // Re-enable answer buttons for the stealing attempt
        EnableAnswerButtons();

        // Ensure the stealing team gets to interact with the question
        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (i < currentQuestion.choices.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = currentQuestion.choices[i];

                string selectedAnswer = currentQuestion.choices[i];
                Button selectedAnswerButton = answerButtons[i];
                int points = currentQuestion.points;

                // Clear previous listeners and assign the new stealing logic
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() =>
                    CheckAnswer(selectedAnswer, selectedAnswerButton, points));
            }
        }
        // Reset timer for the stealing team
        StartTimer();
    }
    #endregion
}
