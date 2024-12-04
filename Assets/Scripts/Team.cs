using UnityEngine;

[System.Serializable]
public class Team
{
    public string teamName;
    public int score = 0;
    public int coins = 3; // Start with 5 coins
    public bool isTurn = false;  // To keep track of the current turn
    public GameObject scorePanel;  // Reference to the UI panel for this team

    public bool SpendCoins(int cost)
    {
        if (coins >= cost)
        {
            coins -= cost;
            return true;
        }
        return false;
    }

    public void GainCoin()
    {
        if (coins < 5) coins++;
    }
}