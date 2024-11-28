using UnityEngine;

[System.Serializable]
public class Team
{
    public string teamName;
    public int score = 0;
    public bool isTurn = false;  // To keep track of the current turn
    public GameObject scorePanel;  // Reference to the UI panel for this team
}