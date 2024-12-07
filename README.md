# **Christmas Jeopardy Game**

🎄 A festive trivia game inspired by the classic *Jeopardy*! Designed for players of all ages (15–82 years old), this game brings family and friends together during the holiday season with categories and questions that celebrate Mexican and American culture.

## **Features**
- 🎅 **Customizable Teams/Players**: Choose the number of players or teams before starting the game.
- ✨ **Dynamic Gameplay**: Automatically updates the current team's turn, with a visual UI indicator.
- ❓ **Holiday-Themed Questions**: Categories include Mexican traditions, American culture, and a special *"¿Who said it? Christmas Version"* for Christmas-related quotes from movies and series.
- ✅ **Answer Validation**: Highlights correct and incorrect answers with visual cues.
- 🕒 **Timer**: Keeps the game fast-paced with a countdown timer for answering questions.
- 🔄 **Steal Phase**: Teams can attempt to steal points for incorrectly answered questions.
- 🎶 **Sound Effects**: Feedback for correct and incorrect answers with custom festive sound effects.
- 🖥️ **Platform**: Built as a computer application using Unity for deployment.

---

## **Getting Started**

### Prerequisites
- [Unity](https://unity.com/) (Version 2021.3 or later recommended)
- A system capable of running Unity Editor
- Git installed for cloning the repository

### Installation
1. Clone this repository to your local machine:
   ```bash
   git clone https://github.com/your-username/christmas-jeopardy.git
2. Clone this repository to your local machine:
   - Launch Unity Hub.
   - Click on Open Project and select the cloned folder.
3. Build and run the project:
   - Navigate to File > Build Settings.
   - Select your platform (PC, Mac, or Linux).
   - Click Build and Run.

---

## **Gameplay Instructions**

1. Setup:
   - Input the number of players or teams at the start.
    
2. Gameplay:
   - Teams take turns selecting categories and answering questions.
   - Earn points for correct answers.
   - Incorrect answers open the "Steal Phase" for other teams.
    
3. Categories:
   - Christmas Movies
   - Christmas Carols and more
   - History of Chritmas
   - Christmas in Books
   - Christmas in the World
   - ¿Who said it? Chritmas Version
    
4. Endgame:
   - The team with the most points at the end wins the game.
   - Celebrate with your family and friends!

---

## **Project Structure**
- Assets/: Contains game scripts, UI elements, sounds, and graphics.
- Scripts/:
    - GameManager.cs: Handles game logic, scoring, and turn management.
    - UIManager.cs: Manages UI interactions and updates.
    - AudioManager.cs: Controls sound effects.
    - QuestionManager.cs: Loads and manages questions and answers.
- Prefabs/: Reusable game components (e.g., buttons, player indicators).
- Scenes/: Unity scenes for the game menu and main game board.

---

## **Technologies Used**
- Unity: Game engine used for development.
- C#: Programming language for scripting game logic.
- Adobe Photoshop: For designing UI and visual assets.

---

## **License**
This project is licensed under the MIT License. See the LICENSE file for details.
