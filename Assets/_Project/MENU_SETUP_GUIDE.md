# Menu System Setup Guide

## Goal
Create a working main menu that loads the game scene, and a game HUD that displays player info and turn controls.

## Files Created
- [Scripts/Core/SceneLoader.cs](Scripts/Core/SceneLoader.cs) - Scene navigation utility
- [Scripts/UI/MainMenuController.cs](Scripts/UI/MainMenuController.cs) - Main menu logic
- [Scripts/UI/GameHUD.cs](Scripts/UI/GameHUD.cs) - In-game HUD logic

---

## Unity Editor Setup Steps

### A. BootScene Setup (SceneLoader)

1. Open **BootScene**
2. Create Empty GameObject:
   - Name: "SceneLoader"
   - Add Component → SceneLoader script
3. This SceneLoader will persist across all scenes (DontDestroyOnLoad)

---

### B. MenuScene Setup

1. Open **MenuScene**
2. In the **SafeAreaPanel** (under MenuCanvas):

   **Create Title Text:**
   - Right-click SafeAreaPanel → UI → Text - TextMeshPro
   - Name: "TitleText"
   - Text: "CONQUIZ"
   - Font Size: 72
   - Alignment: Center
   - Anchors: Top-Center
   - Pos Y: -150

   **Create Start Button:**
   - Right-click SafeAreaPanel → UI → Button - TextMeshPro
   - Name: "StartVsBotButton"
   - Position: Center (0, -50, 0)
   - Size: Width 300, Height 80
   - Button text (child): "Start vs Bot"
   - Font Size: 36

3. **Add MainMenuController:**
   - Create Empty GameObject under SafeAreaPanel
   - Name: "MainMenuController"
   - Add Component → MainMenuController script
   - In Inspector:
     - Start Vs Bot Button: Drag **StartVsBotButton** here
     - Game Scene Name: "GameScene" (should be default)

---

### C. GameScene Setup

1. Open **GameScene**
2. In the **SafeAreaPanel** (under GameCanvas):

   **Create Player Name Panel (Top):**
   - Right-click SafeAreaPanel → UI → Panel
   - Name: "PlayerInfoPanel"
   - Anchors: Top-Stretch
   - Height: 80
   - Pos Y: 0
   - Color: Semi-transparent (e.g., R:0, G:0, B:0, A:150)

   **Create Player Name Text:**
   - Right-click PlayerInfoPanel → UI → Text - TextMeshPro
   - Name: "PlayerNameText"
   - Text: "Player 1"
   - Font Size: 32
   - Alignment: Center
   - Anchors: Stretch-Stretch
   - Color: White

   **Create End Turn Button:**
   - Right-click SafeAreaPanel → UI → Button - TextMeshPro
   - Name: "EndTurnButton"
   - Anchors: Bottom-Right
   - Pos X: -160, Pos Y: 80
   - Size: Width 280, Height 70
   - Button text (child): "End Turn"
   - Font Size: 32

3. **Add GameHUD Controller:**
   - Create Empty GameObject under SafeAreaPanel
   - Name: "GameHUD"
   - Add Component → GameHUD script
   - In Inspector:
     - Player Name Text: Drag **PlayerNameText** here
     - End Turn Button: Drag **EndTurnButton** here
     - Default Player Name: "Player 1" (should be default)

---

## Test Checklist

### Test 1: Scene Loading
- [ ] Open **MenuScene** and press Play
- [ ] Click "Start vs Bot" button
- [ ] GameScene should load successfully
- [ ] Check Console for "Starting game vs Bot..." message
- [ ] No errors in Console

### Test 2: Game HUD
- [ ] In **GameScene**, press Play
- [ ] "Player 1" should display at top of screen
- [ ] Click "End Turn" button
- [ ] Player name should toggle to "Bot Player"
- [ ] Click "End Turn" again
- [ ] Player name should toggle back to "Player 1"
- [ ] Check Console for turn messages

### Test 3: SafeArea Adaptation
- [ ] In either scene, change Game view aspect ratio
- [ ] Try: 16:9, 19.5:9, Free Aspect
- [ ] UI should stay within safe bounds
- [ ] Buttons should remain accessible

### Test 4: Scene Persistence
- [ ] Play BootScene first
- [ ] SceneLoader should persist when loading MenuScene
- [ ] SceneLoader should persist when loading GameScene
- [ ] Only one SceneLoader instance should exist (check Hierarchy)

---

## Optional Enhancements (Do Later)

- Add fade transitions between scenes
- Add "Back to Menu" button in GameScene
- Add settings menu
- Add sound effects for button clicks
- Replace demo player toggle with real game manager integration

---

## Integration Notes

When you implement the game manager:
- Remove `DemoTogglePlayer()` from GameHUD
- Call `GameHUD.SetPlayerName()` when turn changes
- Hook `OnEndTurnClicked()` to your turn system
- SceneLoader is ready to use from any game manager

---

## Done!

You now have:
- Working main menu → game scene flow
- Basic HUD showing player info
- Turn control button ready for integration
- Reusable scene loading system
