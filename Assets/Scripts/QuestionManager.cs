using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class QuestionManager : MonoBehaviour
{
    public Text questionText;             // Text for displaying the question
    public List<Button> answerButtons;    // Answer choice buttons (multiple choice)

    private QuestionData questionData;    // Loaded JSON data
    private Question currentQuestion;     // The current question being displayed
    private UIManager uiManager;

    [SerializeField] private int defaultTime = 20; // Default time in case the points are invalid or unspecified
    [SerializeField] private Slider timerSlider;

    private float timeRemaining; // Tracks the remaining time for the question
    private bool isTimerActive = false; // To control timer state
    private bool isTimedOut = false;

    void Start()
    {
        LoadQuestions();
        uiManager = FindObjectOfType<UIManager>();
    }

    void Update()
    {
        if (isTimerActive)
        {
            timeRemaining -= Time.deltaTime; // Decrement time
            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                isTimerActive = false;

                OnTimeUp(); // Handle time up
            }
            UpdateTimerDisplay(); // Update the timer display
        }
    }

    // Loads question from JSON file
    void LoadQuestions()
    {
        TextAsset jsonText = Resources.Load<TextAsset>("questions");  // Load the JSON file
        questionData = JsonUtility.FromJson<QuestionData>(jsonText.text);
    }

    // This function is called when a category button is clicked
    public void OnCategoryButtonClicked(Button selectedButton)
    {
        AudioManager.Instance.PlaySFX(AudioManager.Instance.categoryClickSFX);

        string[] buttonInfo = selectedButton.name.Split('_');
        string categoryName = buttonInfo[0];
        int points = int.Parse(buttonInfo[1]);

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

                        // Disable the button after selection
                        selectedButton.interactable = false;
                        return;
                    }
                }
            }
        }
    }

    void DisplayQuestion()
    {
        // Display the question text
        questionText.text = currentQuestion.question;

        // Enable all buttons before displaying a new question
        EnableAnswerButtons();

        // Ensure we don't go beyond the available choices for this question
        int numberOfChoices = currentQuestion.choices.Count;

        // Populate answer buttons
        for (int i = 0; i < answerButtons.Count; i++)
        {
            if (i < currentQuestion.choices.Count)
            {
                answerButtons[i].gameObject.SetActive(true);
                answerButtons[i].GetComponentInChildren<Text>().text = currentQuestion.choices[i];

                string selectedAnswer = currentQuestion.choices[i];
                Button selectedAnswerButton = answerButtons[i];
                int points = currentQuestion.points;
                answerButtons[i].onClick.RemoveAllListeners();
                answerButtons[i].onClick.AddListener(() => CheckAnswer(selectedAnswer, selectedAnswerButton, points));
                answerButtons[i].GetComponent<Image>().color = Color.white;  // Reset button color
            }
            else
            {
                answerButtons[i].gameObject.SetActive(false);
            }
        }

        // Hide the Return button initially
        uiManager.returnButton.gameObject.SetActive(false);

        // Show the Question Panel
        uiManager.ShowQuestionPanel();

        // Start the timer for this question
        StartTimer(currentQuestion.points);
    }

    void CheckAnswer(string playerAnswer, Button selectedButton, int questionPoints) 
    {
        // Stop the timer and ticking sound
        AudioManager.Instance.StopTickingSound();
        isTimerActive = false;
        timeRemaining = 0; // Optional: Reset timer to 0 for clarity
        UpdateTimerDisplay(); // Ensure the UI reflects the stopped timer

        bool isCorrect = false;

        if (!isTimedOut) // If the answer was not timed out, check the player's answer
        {
            DisableAnswerButtons();

            isCorrect = playerAnswer.Equals(currentQuestion.correctAnswer, System.StringComparison.OrdinalIgnoreCase);
        }

        // Change color based on correctness
        if (isCorrect)
        {
            if (selectedButton != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.correctAnswerSFX);

                selectedButton.GetComponent<Image>().color = Color.green;  // Correct answer
            }
            GameManager.Instance.UpdateScore(questionPoints);
        }
        else
        {
            if (selectedButton != null)
            {
                AudioManager.Instance.PlaySFX(AudioManager.Instance.wrongAnswerSFX);

                selectedButton.GetComponent<Image>().color = Color.grey;     // Incorrect answer
            }
            uiManager.HighlightCorrectAnswer(answerButtons, currentQuestion);  // Highlight the correct answer
        }

        // Reset timeout flag after the answer has been processed
        isTimedOut = false;

        GameManager.Instance.EndTurn();

        uiManager.returnButton.gameObject.SetActive(true);  // Show the Return button after answering
    }

    public void StartTimer(int questionPoints)
    {
        timeRemaining = GetMaxTimeForCurrentQuestion(); // Get time based on question points
        isTimerActive = true; // Activate the timer
        isTimedOut = false;   // Reset the timeout flag

        if (timerSlider != null)
        {
            timerSlider.value = 1f; // Start with a full progress bar
        }

        //AudioManager.Instance.PlaySFX(AudioManager.Instance.timeTickingFX);
        AudioManager.Instance.StartTickingSound();
    }

    private float GetMaxTimeForCurrentQuestion()
    {
        switch (currentQuestion.points)
        {
            case 200: return 20f;
            case 400: return 30f;
            case 600: return 40f;
            case 800: return 50f;
            case 1000: return 60f;
            default: return defaultTime; // Use default time if points are invalid
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerSlider != null)
        {
            // Update the slider value based on the remaining time
            timerSlider.value = timeRemaining / GetMaxTimeForCurrentQuestion();
        }
    }

    void OnTimeUp()
    {
        Debug.Log("Time's up!");

        // Disable all answer buttons
        DisableAnswerButtons();

        // Stop ticking sound
        AudioManager.Instance.StopTickingSound();

        // Set the timeout flag to true
        isTimedOut = true;

        // Call CheckAnswer with no player answer (empty or null)
        CheckAnswer("", null, currentQuestion.points); // Passing dummy values

        uiManager.ShowTimeoutMessage();
    }

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
}
