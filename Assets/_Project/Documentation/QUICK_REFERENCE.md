# Quick Reference - Частые задачи

## 🔧 Как сделать...

### Добавить новую кнопку на Debug Panel

**Файл:** `Assets/_Project/Scripts/UI/SessionDebugPanel.cs`

```csharp
// Найди метод SetupButtons() и добавь:

private void SetupButtons()
{
    // ... существующие кнопки ...
    
    // Твоя новая кнопка:
    var myButton = CreateButton(debugPanel.transform, "My Button", new Color(0.8f, 0.2f, 0.5f));
    myButton.onClick.AddListener(MyButtonAction);
}

private void MyButtonAction()
{
    Debug.Log("My button clicked!");
    // Твоя логика
}
```

---

### Добавить новый вопрос через код

**Файл:** `Assets/_Project/Scripts/UI/QuizSceneSetup.cs`

```csharp
// Найди метод CreateDefaultMcqQuestions() и добавь:

private McqQuestionData[] CreateDefaultMcqQuestions()
{
    var questions = new McqQuestionData[4]; // Увеличь размер массива
    
    // ... существующие вопросы ...
    
    // Твой новый вопрос:
    questions[3] = ScriptableObject.CreateInstance<McqQuestionData>();
    SetMcqQuestion(questions[3], 
        "Какой язык используется в Android разработке?",
        new[] { "Kotlin", "Swift", "C#", "Ruby" },
        0,  // Правильный ответ - индекс 0 (Kotlin)
        "Programming"
    );
    
    return questions;
}
```

---

### Изменить время таймера

**Файл:** `Assets/_Project/Scripts/UI/QuizUIController.cs`

```csharp
// Найди поле:
[SerializeField] private float defaultTimeLimit = 15f;

// Измени на нужное значение:
[SerializeField] private float defaultTimeLimit = 30f; // 30 секунд
```

---

### Изменить размер/положение Quiz окна

**Файл:** `Assets/_Project/Scripts/UI/QuizSceneSetup.cs`

```csharp
// Найди метод CreateQuizPanelUI() и измени anchors:

private void CreateQuizPanelUI(Transform canvasTransform)
{
    // ...
    
    // Сейчас: 60% ширины, 70% высоты, по центру
    quizPanelRect.anchorMin = new Vector2(0.20f, 0.15f);
    quizPanelRect.anchorMax = new Vector2(0.80f, 0.85f);
    
    // Сделать больше (80% ширины, 90% высоты):
    quizPanelRect.anchorMin = new Vector2(0.10f, 0.05f);
    quizPanelRect.anchorMax = new Vector2(0.90f, 0.95f);
    
    // Сделать меньше (40% ширины, 50% высоты):
    quizPanelRect.anchorMin = new Vector2(0.30f, 0.25f);
    quizPanelRect.anchorMax = new Vector2(0.70f, 0.75f);
}
```

---

### Изменить цвет кнопок ответов

**Файл:** `Assets/_Project/Scripts/UI/QuizSceneSetup.cs`

```csharp
// Найди метод CreateMcqButton():

private Button CreateMcqButton(Transform parent, string name, string text)
{
    // ...
    
    // Измени цвета:
    var colors = button.colors;
    colors.normalColor = new Color(0.15f, 0.18f, 0.26f);     // Обычный
    colors.highlightedColor = new Color(0.22f, 0.28f, 0.40f); // При наведении
    colors.pressedColor = new Color(0.12f, 0.15f, 0.22f);     // При нажатии
    button.colors = colors;
}
```

---

### Добавить звук при нажатии кнопки

```csharp
// 1. Добавь AudioSource на объект
var audioSource = gameObject.AddComponent<AudioSource>();

// 2. Загрузи звук (положи .wav/.mp3 в Assets/Audio/)
var clickSound = Resources.Load<AudioClip>("Audio/click");

// 3. Играй при клике
button.onClick.AddListener(() => {
    audioSource.PlayOneShot(clickSound);
});
```

---

### Показать popup/alert

```csharp
// Создай простой popup:

private void ShowPopup(string message)
{
    var popup = new GameObject("Popup");
    popup.transform.SetParent(canvas.transform, false);
    
    var rect = popup.AddComponent<RectTransform>();
    rect.anchorMin = new Vector2(0.3f, 0.4f);
    rect.anchorMax = new Vector2(0.7f, 0.6f);
    
    var bg = popup.AddComponent<Image>();
    bg.color = new Color(0, 0, 0, 0.9f);
    
    var textGo = new GameObject("Text");
    textGo.transform.SetParent(popup.transform, false);
    var text = textGo.AddComponent<TextMeshProUGUI>();
    text.text = message;
    text.alignment = TextAlignmentOptions.Center;
    
    // Закрыть через 3 секунды
    Destroy(popup, 3f);
}
```

---

### Сохранить данные (как SharedPreferences)

```csharp
// Сохранить:
PlayerPrefs.SetInt("highScore", 100);
PlayerPrefs.SetString("playerName", "John");
PlayerPrefs.SetFloat("volume", 0.8f);
PlayerPrefs.Save();

// Загрузить:
int score = PlayerPrefs.GetInt("highScore", 0);  // 0 = default
string name = PlayerPrefs.GetString("playerName", "Player");
float volume = PlayerPrefs.GetFloat("volume", 1f);

// Удалить:
PlayerPrefs.DeleteKey("highScore");
PlayerPrefs.DeleteAll();  // Удалить всё
```

---

### Сделать HTTP запрос

```csharp
using UnityEngine.Networking;
using System.Collections;

IEnumerator FetchData(string url)
{
    using (UnityWebRequest request = UnityWebRequest.Get(url))
    {
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string json = request.downloadHandler.text;
            Debug.Log("Response: " + json);
            
            // Parse JSON:
            var data = JsonUtility.FromJson<MyDataClass>(json);
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }
    }
}

// Вызов:
StartCoroutine(FetchData("https://api.example.com/data"));
```

---

### Добавить анимацию появления

```csharp
using System.Collections;

IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
{
    canvasGroup.alpha = 0f;
    float elapsed = 0f;
    
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        canvasGroup.alpha = elapsed / duration;
        yield return null;  // Ждём следующий кадр
    }
    
    canvasGroup.alpha = 1f;
}

// Использование:
var cg = panel.AddComponent<CanvasGroup>();
StartCoroutine(FadeIn(cg, 0.5f));  // Появление за 0.5 сек
```

---

### Реагировать на клавиши

```csharp
void Update()
{
    // Проверка нажатия каждый кадр
    if (Input.GetKeyDown(KeyCode.Space))
    {
        Debug.Log("Space pressed!");
    }
    
    if (Input.GetKeyDown(KeyCode.Escape))
    {
        Debug.Log("Escape pressed!");
    }
    
    // Числовые клавиши 1-4 для ответов
    if (Input.GetKeyDown(KeyCode.Alpha1)) SelectAnswer(0);
    if (Input.GetKeyDown(KeyCode.Alpha2)) SelectAnswer(1);
    if (Input.GetKeyDown(KeyCode.Alpha3)) SelectAnswer(2);
    if (Input.GetKeyDown(KeyCode.Alpha4)) SelectAnswer(3);
}
```

---

## 🐞 Debug команды

```csharp
// Обычный лог
Debug.Log("Message");

// Warning (жёлтый)
Debug.LogWarning("Warning!");

// Error (красный)
Debug.LogError("Error!");

// С объектом (кликни на лог → выделится объект)
Debug.Log("Object info", gameObject);

// Форматированный лог
Debug.Log($"Player score: {score}, Level: {level}");

// Условный лог
Debug.Assert(health > 0, "Health should be positive!");

// Пауза игры
Debug.Break();

// Время выполнения
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
// ... код ...
stopwatch.Stop();
Debug.Log($"Execution time: {stopwatch.ElapsedMilliseconds}ms");
```

---

## 📁 Важные пути в проекте

```
Assets/_Project/
├── Scripts/
│   ├── Quiz/
│   │   └── QuizSessionController.cs  ← Логика сессии
│   ├── Bots/
│   │   └── BotAnswerProvider.cs      ← Логика бота
│   └── UI/
│       ├── QuizSceneSetup.cs         ← ENTRY POINT (создание UI)
│       ├── QuizUIController.cs       ← Управление UI
│       └── SessionDebugPanel.cs      ← Debug панель
│
├── ScriptableObjects/Questions/      ← Данные вопросов
├── Scenes/NewQuizScene.unity         ← Главная сцена
└── Documentation/                    ← Документация (ты тут!)
```

---

## ⌨️ Горячие клавиши Unity

| Клавиша | Действие |
|---------|----------|
| Ctrl+P | Play/Stop |
| Ctrl+Shift+P | Pause |
| Ctrl+S | Save Scene |
| Ctrl+Z | Undo |
| Ctrl+D | Duplicate |
| Delete | Delete selected |
| F | Focus on selected |
| Ctrl+Shift+C | Console |
| Ctrl+1 | Scene view |
| Ctrl+2 | Game view |
| Ctrl+3 | Inspector |
| Ctrl+4 | Hierarchy |
| Ctrl+5 | Project |

---

## 🔍 Поиск в проекте

**В Unity:**
- Ctrl+Shift+F - глобальный поиск
- В Hierarchy: введи имя в поиск
- В Project: введи имя или тип (t:Script)

**В коде (VS Code/Rider):**
- Ctrl+Shift+F - поиск по всем файлам
- Ctrl+G - перейти к строке
- Ctrl+Click - перейти к определению

