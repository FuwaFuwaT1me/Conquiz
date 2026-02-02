# Детальное описание скриптов

## 📜 Список всех скриптов

| Скрипт | Назначение | Аналог в Android |
|--------|------------|------------------|
| QuizSceneSetup.cs | Entry point, создание UI | Application + LayoutInflater |
| QuizSessionController.cs | Логика игровой сессии | ViewModel + UseCase |
| QuizUIController.cs | Управление UI | Fragment UI logic |
| HumanAnswerProvider.cs | Ввод от игрока | InputListener |
| BotAnswerProvider.cs | ИИ бота | Bot/AI logic |
| SessionDebugPanel.cs | Debug UI | Debug drawer |
| PlayerStatusBadge.cs | Статус игрока | Custom View |
| DebugBotSettings.cs | Настройки бота | SharedPreferences |
| McqQuestionData.cs | Модель MCQ вопроса | Data class |
| NumericQuestionData.cs | Модель Numeric вопроса | Data class |
| QuizAnswerResult.cs | Результат ответа | Data class |
| SessionResult.cs | Результат сессии | Data class |
| IAnswerProvider.cs | Интерфейс провайдера | Interface |

---

## 1️⃣ QuizSceneSetup.cs

**Путь:** `Assets/_Project/Scripts/UI/QuizSceneSetup.cs`

### Что это?
Entry point приложения. Создаёт весь UI программно при запуске игры.

### Почему UI создаётся в коде, а не в редакторе?
1. **Гибкость** - можно легко менять layout
2. **Версионирование** - код легче diff'ить в git
3. **Переиспользование** - можно создавать UI динамически

### Ключевые методы:

```csharp
// Entry point - вызывается Unity автоматически
private void Start()
{
    LoadQuestionsIfNeeded();  // Загрузка вопросов
    CreateQuizCanvas();       // Создание Quiz UI
    CreateDebugCanvas();      // Создание Debug Panel
    CreateManagers();         // Создание controllers
    WireReferences();         // Связывание через reflection
}

// Update вызывается каждый кадр
private void Update()
{
    // Глобальная обработка F1
    SessionDebugPanel.CheckGlobalToggle();
}
```

### Как добавить новый UI элемент:

```csharp
// 1. Создай метод для элемента:
private void CreateMyElement(Transform parent)
{
    var go = new GameObject("MyElement");
    go.transform.SetParent(parent, false);
    
    // Добавь компоненты
    var image = go.AddComponent<Image>();
    image.color = Color.blue;
    
    var rect = go.GetComponent<RectTransform>();
    rect.sizeDelta = new Vector2(200, 100);
}

// 2. Вызови в CreateQuizPanelUI():
private void CreateQuizPanelUI(Transform canvasTransform)
{
    // ... существующий код ...
    CreateMyElement(quizPanelGo.transform);
}
```

### Паттерн создания UI:

```csharp
// Базовый паттерн для любого UI элемента:

// 1. Создай GameObject
var go = new GameObject("Name");
go.transform.SetParent(parent, false);  // false = сохранить local transform

// 2. Добавь RectTransform (для UI)
var rect = go.AddComponent<RectTransform>();
// или просто GetComponent если уже есть

// 3. Настрой размеры/позицию
rect.anchorMin = Vector2.zero;      // Левый нижний угол
rect.anchorMax = Vector2.one;       // Правый верхний
rect.offsetMin = Vector2.zero;      // Отступ от min anchor
rect.offsetMax = Vector2.zero;      // Отступ от max anchor

// 4. Добавь визуальные компоненты
var image = go.AddComponent<Image>();
var text = go.AddComponent<TextMeshProUGUI>();
var button = go.AddComponent<Button>();
```

---

## 2️⃣ QuizSessionController.cs

**Путь:** `Assets/_Project/Scripts/Quiz/QuizSessionController.cs`

### Что это?
"Мозг" игры. Управляет логикой сессии: раунды, таймеры, победители.

### Ключевые поля:

```csharp
[SerializeField] private QuizUIController quizUI;        // UI controller
[SerializeField] private McqQuestionData[] mcqQuestions; // Вопросы MCQ
[SerializeField] private NumericQuestionData[] numericQuestions; // Numeric

private IAnswerProvider player1;  // Первый игрок (human/bot)
private IAnswerProvider player2;  // Второй игрок (bot)
```

### Как работает сессия:

```csharp
public void StartSession(IAnswerProvider p1, IAnswerProvider p2)
{
    player1 = p1;
    player2 = p2;
    StartCoroutine(RunSessionCoroutine());  // Запуск game loop
}

private IEnumerator RunSessionCoroutine()
{
    // 1. Выбрать случайный MCQ вопрос
    var mcqQuestion = mcqQuestions[Random.Range(0, mcqQuestions.Length)];
    
    // 2. Показать вопрос
    quizUI.ShowMcqQuestion(mcqQuestion);
    
    // 3. Ждать ответы от обоих игроков
    var task1 = player1.ProvideAnswerAsync(...);
    var task2 = player2.ProvideAnswerAsync(...);
    
    // 4. Ждать пока оба ответят или timeout
    yield return WaitForBothAnswers(task1, task2);
    
    // 5. Оценить раунд
    var result = EvaluateMcqRound();
    
    // 6. Если ничья - второй раунд
    if (result == RoundResult.Tie)
    {
        yield return RunNumericRound();
    }
    
    // 7. Показать финальный результат
    yield return ShowFinalResult();
}
```

### Что такое IEnumerator/Coroutine?

```csharp
// Coroutine - это способ делать асинхронные операции в Unity
// Похоже на suspend функции в Kotlin, но с другим синтаксисом

IEnumerator MyCoroutine()
{
    Debug.Log("Start");
    
    yield return null;                    // Ждать 1 кадр
    yield return new WaitForSeconds(2);   // Ждать 2 секунды
    yield return new WaitUntil(() => isReady); // Ждать условие
    yield return StartCoroutine(Other()); // Ждать другую coroutine
    
    Debug.Log("End");
}

// Запуск:
StartCoroutine(MyCoroutine());

// Остановка:
StopCoroutine(myCoroutine);
StopAllCoroutines();
```

---

## 3️⃣ QuizUIController.cs

**Путь:** `Assets/_Project/Scripts/UI/QuizUIController.cs`

### Что это?
Управляет всем UI квиза: показ вопросов, таймер, результаты.

### События (Events):

```csharp
// Подписка на события (как Callback в Android)
public event Action<int, float> OnMcqAnswerSubmitted;    // (index, time)
public event Action<float, float> OnNumericAnswerSubmitted; // (value, time)
public event Action OnTimerExpired;
public event Action OnContinuePressed;

// Как подписаться:
quizUI.OnMcqAnswerSubmitted += HandleMcqAnswer;

void HandleMcqAnswer(int index, float responseTime)
{
    Debug.Log($"Player chose {index} in {responseTime}s");
}

// Отписка (важно при уничтожении объекта!):
quizUI.OnMcqAnswerSubmitted -= HandleMcqAnswer;
```

### Основные методы:

```csharp
// Показать MCQ вопрос
public void ShowMcqQuestion(McqQuestionData question, float timeLimit = 0f)
{
    // Показать панель
    ShowPanel(quizPanel, true);
    ShowPanel(mcqPanel, true);
    
    // Установить текст
    questionText.text = question.QuestionText;
    categoryText.text = question.Category;
    
    // Установить варианты ответов
    for (int i = 0; i < mcqButtons.Length; i++)
    {
        mcqButtonTexts[i].text = $"{(char)('A' + i)}) {question.Choices[i]}";
    }
    
    // Запустить таймер
    StartTimer(timeLimit > 0 ? timeLimit : defaultTimeLimit);
}

// Показать результаты MCQ
public void ShowMcqResult(...)
{
    // Подсветить правильный ответ зелёным
    // Показать кто что выбрал
    // Обновить статусы игроков
}

// Скрыть квиз
public void HideQuiz()
{
    ShowPanel(quizPanel, false);
}

// Обновить статус игрока
public void MarkPlayerAnswered()
{
    playerBadgeLeft.SetState(BadgeState.Answered);
}

public void MarkOpponentAnswered()
{
    playerBadgeRight.SetState(BadgeState.Answered);
}
```

### Как работает таймер:

```csharp
private void Update()
{
    if (isTimerRunning)
    {
        timeRemaining -= Time.deltaTime;  // Уменьшаем каждый кадр
        UpdateTimerDisplay();
        
        if (timeRemaining <= 0)
        {
            isTimerRunning = false;
            OnTimerExpired?.Invoke();
        }
    }
}

private void UpdateTimerDisplay()
{
    // Обновить текст
    timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
    
    // Обновить заполнение кольца
    float normalized = timeRemaining / timeLimit;
    timerFillImage.fillAmount = normalized;
    
    // Менять цвет при малом времени
    if (normalized < 0.25f)
        timerFillImage.color = Color.red;
    else if (normalized < 0.5f)
        timerFillImage.color = Color.yellow;
    else
        timerFillImage.color = Color.blue;
}
```

---

## 4️⃣ HumanAnswerProvider.cs

**Путь:** `Assets/_Project/Scripts/Quiz/HumanAnswerProvider.cs`

### Что это?
Получает ответы от человека через UI.

### Как работает:

```csharp
public class HumanAnswerProvider : MonoBehaviour, IAnswerProvider
{
    private TaskCompletionSource<QuizAnswerResult> answerTcs;
    
    public Task<QuizAnswerResult> ProvideAnswerAsync(object question, float timeout)
    {
        // Создаём "promise" для ответа
        answerTcs = new TaskCompletionSource<QuizAnswerResult>();
        
        // Подписываемся на UI события
        quizUI.OnMcqAnswerSubmitted += OnMcqAnswer;
        quizUI.OnNumericAnswerSubmitted += OnNumericAnswer;
        quizUI.OnTimerExpired += OnTimeout;
        
        return answerTcs.Task;  // Возвращаем Task который завершится когда игрок ответит
    }
    
    private void OnMcqAnswer(int index, float time)
    {
        // Игрок кликнул кнопку
        var result = new QuizAnswerResult
        {
            AnswerIndex = index,
            ResponseTime = time
        };
        
        answerTcs.SetResult(result);  // Завершаем Task
    }
}
```

### Аналог в Kotlin:

```kotlin
// Это похоже на:
suspend fun provideAnswer(): QuizAnswerResult {
    return suspendCoroutine { continuation ->
        button.setOnClickListener {
            continuation.resume(QuizAnswerResult(index, time))
        }
    }
}
```

---

## 5️⃣ BotAnswerProvider.cs

**Путь:** `Assets/_Project/Scripts/Bots/BotAnswerProvider.cs`

### Что это?
Генерирует ответы для бота. Поддерживает Auto (всегда правильно) и Manual (из Debug Panel).

### Режимы работы:

```csharp
public enum BotAnswerMode
{
    Automatic,  // Бот всегда отвечает правильно
    Manual      // Ответ задаётся через Debug Panel
}
```

### Как работает:

```csharp
public async Task<QuizAnswerResult> ProvideAnswerAsync(object question, float timeout)
{
    // 1. Симулируем "думание" бота
    float thinkTime = Random.Range(0.5f, 2.0f);
    await Task.Delay((int)(thinkTime * 1000));
    
    // 2. Определяем ответ в зависимости от режима
    int answerIndex;
    
    if (DebugBotSettings.Instance.Mode == BotAnswerMode.Automatic)
    {
        // Автоматический режим - всегда правильный ответ
        var mcq = question as McqQuestionData;
        answerIndex = mcq.CorrectIndex;
    }
    else
    {
        // Manual режим - ждём ответ из Debug Panel
        answerIndex = await WaitForManualAnswer();
    }
    
    return new QuizAnswerResult
    {
        AnswerIndex = answerIndex,
        ResponseTime = thinkTime
    };
}
```

### Как изменить логику бота:

```csharp
// Добавить случайность (иногда ошибается):
if (DebugBotSettings.Instance.Mode == BotAnswerMode.Automatic)
{
    var mcq = question as McqQuestionData;
    
    // 80% шанс правильного ответа
    if (Random.value < 0.8f)
    {
        answerIndex = mcq.CorrectIndex;
    }
    else
    {
        // Случайный неправильный ответ
        do
        {
            answerIndex = Random.Range(0, 4);
        }
        while (answerIndex == mcq.CorrectIndex);
    }
}
```

---

## 6️⃣ SessionDebugPanel.cs

**Путь:** `Assets/_Project/Scripts/UI/SessionDebugPanel.cs`

### Что это?
Debug панель слева. Позволяет запускать игру и управлять ботом.

### Ключевые элементы:

```csharp
// Кнопки запуска
private Button humanVsBotButton;   // Human vs Bot
private Button botVsBotButton;     // Bot vs Bot

// Переключатель режима бота
private Button autoModeButton;
private Button manualModeButton;

// Статус
private TextMeshProUGUI statusText;
```

### Как добавить новую кнопку:

```csharp
private void SetupButtons()
{
    // ... существующие кнопки ...
    
    // Добавить разделитель
    CreateSeparator(debugPanel.transform);
    
    // Добавить свою кнопку
    var myButton = CreateButton(
        debugPanel.transform, 
        "My Feature",           // Текст
        new Color(0.8f, 0.4f, 0.1f)  // Цвет
    );
    
    myButton.onClick.AddListener(() => {
        Debug.Log("My feature activated!");
        MyFeatureMethod();
    });
}

private void MyFeatureMethod()
{
    // Твоя логика
}
```

### Как работает F1 toggle:

```csharp
// Проблема: когда panel.SetActive(false), Update() не вызывается
// Решение: глобальный static метод + CanvasGroup вместо SetActive

public static void CheckGlobalToggle()
{
    if (activeInstance != null && Input.GetKeyDown(KeyCode.F1))
    {
        activeInstance.TogglePanel();
    }
}

private void TogglePanel()
{
    panelVisible = !panelVisible;
    
    // Используем CanvasGroup для скрытия без отключения GameObject
    var cg = debugPanel.GetComponent<CanvasGroup>();
    cg.alpha = panelVisible ? 1f : 0f;
    cg.interactable = panelVisible;
    cg.blocksRaycasts = panelVisible;
}
```

---

## 7️⃣ PlayerStatusBadge.cs

**Путь:** `Assets/_Project/Scripts/UI/PlayerStatusBadge.cs`

### Что это?
Иконка игрока с именем и статусом (Thinking.../Answered!).

### Состояния:

```csharp
public enum BadgeState
{
    Hidden,        // Скрыт
    Thinking,      // "Thinking..." (ожидание ответа)
    Answered,      // "Answered!" (игрок ответил)
    TimedOut,      // "Timed Out" (время вышло)
    ResultCorrect, // "Correct! ✓"
    ResultWrong    // "Wrong ✗"
}
```

### Использование:

```csharp
// Показать что игрок думает
playerBadge.SetState(BadgeState.Thinking);

// Показать что игрок ответил
playerBadge.SetState(BadgeState.Answered);

// Показать результат
playerBadge.SetState(BadgeState.ResultCorrect);
```

---

## 8️⃣ Data классы (ScriptableObjects)

### McqQuestionData.cs

```csharp
[CreateAssetMenu(fileName = "MCQ_Question", menuName = "Quiz/MCQ Question")]
public class McqQuestionData : ScriptableObject
{
    [SerializeField] private string questionText;   // Текст вопроса
    [SerializeField] private string[] choices;      // 4 варианта
    [SerializeField] private int correctIndex;      // Индекс правильного (0-3)
    [SerializeField] private string category;       // Категория
    [SerializeField] private float timeLimit = 15f; // Время (опционально)
    
    // Публичные свойства для чтения
    public string QuestionText => questionText;
    public string[] Choices => choices;
    public int CorrectIndex => correctIndex;
    public string Category => category;
}
```

### NumericQuestionData.cs

```csharp
[CreateAssetMenu(fileName = "Numeric_Question", menuName = "Quiz/Numeric Question")]
public class NumericQuestionData : ScriptableObject
{
    [SerializeField] private string questionText;
    [SerializeField] private float correctValue;     // Правильный ответ
    [SerializeField] private string unit;            // Единица (км, кг, ...)
    [SerializeField] private float allowedRangeMin;  // Минимальное значение
    [SerializeField] private float allowedRangeMax;  // Максимальное значение
    [SerializeField] private int decimalPlaces = 1;  // Знаков после запятой
}
```

### QuizAnswerResult.cs

```csharp
public class QuizAnswerResult
{
    public int AnswerIndex { get; set; }      // Индекс выбранного ответа (MCQ)
    public float NumericValue { get; set; }   // Числовой ответ (Numeric)
    public float ResponseTime { get; set; }   // Время ответа в секундах
    public bool IsTimeout { get; set; }       // Был ли timeout
}
```

### SessionResult.cs

```csharp
public class SessionResult
{
    public IAnswerProvider Winner { get; set; }   // Победитель
    public IAnswerProvider Loser { get; set; }    // Проигравший
    public SessionDecision Decision { get; set; } // Как определён победитель
    public int RoundsPlayed { get; set; }         // Сколько раундов сыграно
}

public enum SessionDecision
{
    McqCorrectness,    // По правильности MCQ
    NumericCloseness,  // По близости к ответу (Numeric)
    ResponseTime,      // По скорости ответа (tie-break)
    Draw               // Ничья
}
```

---

## 9️⃣ IAnswerProvider.cs - Интерфейс

```csharp
public interface IAnswerProvider
{
    /// <summary>
    /// Получить ответ на вопрос
    /// </summary>
    /// <param name="question">Вопрос (McqQuestionData или NumericQuestionData)</param>
    /// <param name="timeout">Максимальное время ответа</param>
    /// <returns>Результат ответа</returns>
    Task<QuizAnswerResult> ProvideAnswerAsync(object question, float timeout);
    
    /// <summary>
    /// ID игрока (0 = human, 1 = bot)
    /// </summary>
    int PlayerId { get; }
    
    /// <summary>
    /// Имя игрока для отображения
    /// </summary>
    string DisplayName { get; }
}
```

### Реализации:
- `HumanAnswerProvider` - получает ответы от UI
- `BotAnswerProvider` - генерирует ответы автоматически/вручную

