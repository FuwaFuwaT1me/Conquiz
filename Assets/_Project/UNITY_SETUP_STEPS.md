# Unity Editor Setup Steps

Complete these steps in Unity Editor to finish the project setup.

## Step 1: Install Required Packages

1. **TextMeshPro**
   - Window → TextMeshPro → Import TMP Essential Resources
   - Click "Import" in the popup

2. **Universal RP** (if not already installed)
   - Window → Package Manager
   - Unity Registry → search "Universal RP"
   - Install if needed

## Step 2: Create Scenes

Create three scenes in `Assets/_Project/Scenes/`:

1. **BootScene**
   - File → New Scene → Basic (Built-In)
   - File → Save As → `Assets/_Project/Scenes/BootScene`
   - Delete Main Camera and Directional Light (optional for Boot)
   - Add: Empty GameObject named "GameBootstrap"

2. **MenuScene**
   - File → New Scene → Basic (Built-In)
   - File → Save As → `Assets/_Project/Scenes/MenuScene`
   - Keep Main Camera
   - Add UI Canvas (see Canvas Setup below)

3. **GameScene**
   - File → New Scene → Basic (Built-In)
   - File → Save As → `Assets/_Project/Scenes/GameScene`
   - Keep Main Camera
   - Add UI Canvas (see Canvas Setup below)

4. **Add to Build Settings**
   - File → Build Settings
   - Drag all three scenes from Project window into "Scenes in Build"
   - Order: BootScene (0), MenuScene (1), GameScene (2)

## Step 3: Project Settings

### Platform Settings
1. File → Build Settings
   - Select **Android** (or iOS)
   - Click "Switch Platform" (wait for reimport)

2. Player Settings button (bottom left of Build Settings)
   - **Resolution and Presentation**
     - Default Orientation: **Auto Rotation**
     - Check: Portrait, Portrait Upside Down, Landscape Left, Landscape Right
   - **Other Settings**
     - API Compatibility Level: **.NET Standard 2.1**
   - **Company Name**: Your name
   - **Product Name**: Conquiz

### Graphics Settings
1. Edit → Project Settings → Graphics
   - Scriptable Render Pipeline Settings: Assign **UniversalRenderPipelineAsset**
     (Should be in Assets by default for 2D URP projects)

2. Edit → Project Settings → Quality
   - For each quality level, assign the URP asset in "Render Pipeline Asset"

## Step 4: Canvas Setup (for MenuScene and GameScene)

### For MenuScene:
1. Right-click in Hierarchy → UI → Canvas
   - Rename to "MenuCanvas"
2. Select MenuCanvas:
   - **Canvas** component:
     - Render Mode: **Screen Space - Camera**
     - Render Camera: Drag **Main Camera** here
   - **Canvas Scaler** component:
     - UI Scale Mode: **Scale With Screen Size**
     - Reference Resolution: X=**1080**, Y=**1920**
     - Screen Match Mode: **Match Width Or Height**
     - Match: **0.5**
3. Right-click MenuCanvas → UI → Panel
   - Rename to "SafeAreaPanel"
   - Add Component → search "Safe Area" → attach SafeArea script
4. Set SafeAreaPanel:
   - Anchors: Stretch/Stretch (full screen)
   - Left/Right/Top/Bottom: 0
5. Place all future UI as children of SafeAreaPanel

### For GameScene:
Repeat the exact same steps as MenuScene, but name canvas "GameCanvas"

## Step 5: Verification

1. **Check folder structure** in Project window:
   - Assets/_Project/ should have: Scenes, Scripts (with subfolders), ScriptableObjects, Prefabs, Data, Art, UI

2. **Test scenes**:
   - Open each scene, press Play
   - No errors in Console

3. **Test UI scaling**:
   - Open MenuScene or GameScene
   - Game view → Aspect dropdown
   - Try: Free Aspect, 16:9, 19.5:9, 21:9
   - SafeAreaPanel should adapt

4. **Test SafeArea script**:
   - Add a TextMeshPro Text as child of SafeAreaPanel
   - Set text to "SAFE AREA TEST"
   - Anchor to top-left corner
   - Game view → try different aspect ratios
   - Text should stay within safe bounds

## You're Done!

Project is now ready for Conquiz game development.

### Quick Start Next Steps:
- Implement BootScene bootstrapper (GameManager, SceneLoader)
- Create MenuScene UI (Play button)
- Start Map system in GameScene
