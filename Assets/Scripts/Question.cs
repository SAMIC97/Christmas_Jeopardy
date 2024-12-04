using System.Collections.Generic;

[System.Serializable]
public class Question
{
    public string question;         // Question to be asked
    public List<string> choices;    // Holds multiple choices
    public string correctAnswer;    // The correct answer for validation
    public int points;              //Value of the question
    public bool hasBeenStolen = false; //Attempt to be steal question
}

[System.Serializable]
public class Category
{
    public string name;                 // Category Name
    public List<Question> questions;    //  List of the questions
}

[System.Serializable]
public class QuestionData
{
    public List<Category> categories;
}