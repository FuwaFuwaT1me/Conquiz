# Conquiz - Документация для Android разработчика

## 🎯 Быстрый старт

Ты Android разработчик, который привык к Activity/Fragment/ViewModel. Вот как это соотносится с Unity:

| Android | Unity | Описание |
|---------|-------|----------|
| Activity/Fragment | Scene | "Экран" приложения |
| View/ViewGroup | GameObject | Любой объект на сцене |
| XML Layout | Hierarchy + Inspector | Визуальное расположение элементов |
| ViewModel | MonoBehaviour | Логика компонента |
| RecyclerView.Adapter | - | В Unity нет аналога, UI создаётся вручную |
| Drawable/Resource | Asset | Картинки, звуки, шрифты |
| SharedPreferences | PlayerPrefs | Простое хранение данных |

---

## ❓ Почему сцена пустая, а UI появляется при запуске?

### Короткий ответ:
UI создаётся **программно в коде** (как если бы ты создавал все View в коде Android вместо XML).

### Подробное объяснение:

В проекте используется **runtime UI creation** - весь интерфейс создаётся скриптом `QuizSceneSetup.cs` когда ты нажимаешь Play.

```
Сцена (Scene) содержит:
├── Main Camera          ← Камера (как viewport)
├── EventSystem          ← Обработчик ввода (touch/click)
├── ---MANAGERS---       ← Просто разделитель
└── QuizManager          ← GameObject с компонентом QuizSceneSetup
    └── Start() создаёт весь UI при запуске!
```

**Почему так сделано?**
- Весь UI создаётся динамически для гибкости
- Меньше зависимостей от ручной настройки в Inspector
- Легче поддерживать в коде

**Как это работает:**
```csharp
// QuizSceneSetup.cs
private void Start()  // Start() вызывается Unity автоматически при запуске
{
    LoadQuestionsIfNeeded();
    CreateQuizCanvas();      // Создаёт Canvas с QuizUIController
    CreateDebugCanvas();     // Создаёт Debug Panel
    CreateManagers();        // Создаёт провайдеры ответов
    WireReferences();        // Связывает всё вместе
}
```

### Аналогия с Android:
```kotlin
// Как если бы в Android ты делал:
class MainActivity : AppCompatActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        // Вместо setContentView(R.layout.activity_main)
        // Ты создаёшь всё в коде:
        val rootView = LinearLayout(this)
        val button = Button(this)
        rootView.addView(button)
        setContentView(rootView)
    }
}
```

---

## 📁 Структура проекта

```
Assets/_Project/
├── Scripts/
│   ├── Quiz/                    ← Логика квиза
│   │   ├── QuizSessionController.cs   ← "ViewModel" для сессии
│   │   ├── IAnswerProvider.cs         ← Интерфейс для ответов
│   │   ├── HumanAnswerProvider.cs     ← Провайдер для человека
│   │   ├── McqQuestionData.cs         ← Модель вопроса MCQ
│   │   ├── NumericQuestionData.cs     ← Модель вопроса Numeric
│   │   ├── QuizAnswerResult.cs        ← Результат ответа
│   │   └── SessionResult.cs           ← Результат сессии
│   │
│   ├── Bots/                    ← Логика ботов
│   │   ├── BotAnswerProvider.cs       ← Провайдер для бота
│   │   └── DebugBotSettings.cs        ← Настройки бота (Singleton)
│   │
│   └── UI/                      ← UI компоненты
│       ├── QuizSceneSetup.cs          ← Создаёт весь UI (Entry Point!)
│       ├── QuizUIController.cs        ← Управляет отображением квиза
│       ├── SessionDebugPanel.cs       ← Debug панель слева
│       └── PlayerStatusBadge.cs       ← Иконка игрока (YOU/OPPONENT)
│
├── ScriptableObjects/
│   └── Questions/               ← Данные вопросов (как JSON)
│
├── Scenes/
│   └── NewQuizScene.unity       ← Главная сцена
│
└── Documentation/               ← Ты тут!
```

---

## 🔄 Поток данных (Data Flow)

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  QuizSceneSetup │────▶│ QuizSessionCtrl  │────▶│ QuizUIController│
│  (создаёт UI)   │     │ (логика игры)    │     │ (показывает UI) │
└─────────────────┘     └──────────────────┘     └─────────────────┘
                               │
                    ┌──────────┴──────────┐
                    ▼                     ▼
         ┌──────────────────┐   ┌──────────────────┐
         │ HumanAnswerProv  │   │ BotAnswerProvider│
         │ (ввод человека)  │   │ (ИИ/Debug бот)   │
         └──────────────────┘   └──────────────────┘
```

### Порядок вызовов при старте:

1. **Unity вызывает `Start()`** на `QuizSceneSetup`
2. `QuizSceneSetup` создаёт все UI элементы программно
3. `QuizSceneSetup` создаёт `QuizSessionController`, `HumanAnswerProvider`, `BotAnswerProvider`
4. Всё связывается через reflection (как Dependency Injection)
5. `SessionDebugPanel` ждёт нажатия "Human vs Bot"
6. При нажатии запускается `QuizSessionController.StartSession()`

---

## 🎮 Основные скрипты

### 1. QuizSceneSetup.cs - Entry Point (как Application в Android)
**Путь:** `Assets/_Project/Scripts/UI/QuizSceneSetup.cs`

**Что делает:**
- Создаёт весь UI при запуске
- Связывает все компоненты между собой

**Ключевые методы:**
```csharp
Start()                    // Точка входа, вызывается Unity
CreateQuizCanvas()         // Создаёт Canvas для квиза
CreateQuizPanelUI()        // Создаёт панель с вопросами
CreateHeaderSection()      // Создаёт шапку (игроки + таймер)
CreateMcqPanel()           // Создаёт кнопки ответов
CreateDebugCanvas()        // Создаёт Debug Panel
WireReferences()           // Связывает всё вместе
```

**Как добавить новый UI элемент:**
```csharp
// В методе CreateQuizPanelUI() добавь:
var myButton = CreateButton(parent, "My Button Text", Color.blue);
myButton.onClick.AddListener(() => {
    Debug.Log("Кнопка нажата!");
});
```

---

### 2. QuizSessionController.cs - ViewModel/Presenter
**Путь:** `Assets/_Project/Scripts/Quiz/QuizSessionController.cs`

**Что делает:**
- Управляет логикой игровой сессии
- Определяет победителя
- Координирует раунды (MCQ → Numeric)

**Аналог в Android:** ViewModel + UseCase

**Ключевые методы:**
```csharp
StartSession(player1, player2)     // Запуск сессии
RunSessionCoroutine()              // Основной game loop (как while в thread)
EvaluateMcqRound()                 // Оценка MCQ раунда
EvaluateNumericRound()             // Оценка Numeric раунда
DetermineWinner()                  // Определение победителя
```

**Что такое Coroutine?**
```csharp
// Coroutine - это как suspend функция в Kotlin
// Позволяет делать паузы без блокировки UI

IEnumerator Example() {
    Debug.Log("Начало");
    yield return new WaitForSeconds(2f);  // Пауза 2 секунды
    Debug.Log("Прошло 2 секунды");
}

// Запуск:
StartCoroutine(Example());
```

---

### 3. QuizUIController.cs - View Layer
**Путь:** `Assets/_Project/Scripts/UI/QuizUIController.cs`

**Что делает:**
- Показывает/скрывает UI элементы
- Обрабатывает нажатия кнопок
- Управляет таймером
- Показывает результаты

**Аналог в Android:** Fragment/Activity UI logic

**Ключевые методы:**
```csharp
ShowMcqQuestion(question)          // Показать MCQ вопрос
ShowNumericQuestion(question)      // Показать Numeric вопрос
ShowMcqResult(...)                 // Показать результат MCQ
ShowNumericResult(...)             // Показать результат Numeric
HideQuiz()                         // Скрыть всё
MarkPlayerAnswered()               // Обновить статус игрока
```

**События (как Callback/Listener):**
```csharp
// Подписка на события:
quizUI.OnMcqAnswerSubmitted += (index, time) => {
    Debug.Log($"Игрок выбрал ответ {index} за {time} сек");
};

quizUI.OnNumericAnswerSubmitted += (value, time) => {
    Debug.Log($"Игрок ввёл {value} за {time} сек");
};

quizUI.OnTimerExpired += () => {
    Debug.Log("Время вышло!");
};
```

---

### 4. HumanAnswerProvider.cs - User Input
**Путь:** `Assets/_Project/Scripts/Quiz/HumanAnswerProvider.cs`

**Что делает:**
- Получает ответы от человека через UI
- Преобразует клики в данные

**Как добавить свою обработку:**
```csharp
// В ProvideAnswerAsync() можно добавить логику:
public async Task<QuizAnswerResult> ProvideAnswerAsync(...)
{
    // Твоя кастомная логика перед ответом
    Debug.Log("Ожидаем ответ от игрока...");
    
    // Ждём ответ
    var result = await WaitForAnswer();
    
    // Твоя логика после ответа
    Debug.Log($"Игрок ответил: {result}");
    
    return result;
}
```

---

### 5. BotAnswerProvider.cs - AI/Bot Logic
**Путь:** `Assets/_Project/Scripts/Bots/BotAnswerProvider.cs`

**Что делает:**
- Генерирует ответы бота
- Поддерживает режимы: Automatic (всегда правильно) и Manual (из Debug Panel)

**Как изменить логику бота:**
```csharp
// Сейчас бот всегда отвечает правильно в Auto режиме
// Чтобы добавить случайность:

private int GetMcqAnswer(McqQuestionData question)
{
    if (mode == BotAnswerMode.Automatic)
    {
        // Добавляем 20% шанс ошибки:
        if (Random.value < 0.2f)
        {
            // Выбираем случайный неправильный ответ
            int wrong;
            do { wrong = Random.Range(0, 4); } 
            while (wrong == question.CorrectIndex);
            return wrong;
        }
        return question.CorrectIndex;
    }
    // ... manual mode
}
```

---

### 6. SessionDebugPanel.cs - Debug UI
**Путь:** `Assets/_Project/Scripts/UI/SessionDebugPanel.cs`

**Что делает:**
- Показывает Debug панель слева
- Кнопки "Human vs Bot", "Bot vs Bot"
- Переключатель Auto/Manual для бота
- Управление ответами бота в Manual режиме

**Как добавить свою кнопку:**
```csharp
// В методе SetupButtons() добавь:
var myButton = CreateButton(parent, "My Debug Button", Color.magenta);
myButton.onClick.AddListener(() => {
    Debug.Log("Debug button clicked!");
    // Твоя логика
});
```

---

## 📦 ScriptableObject - Хранение данных

### Что это?
ScriptableObject - это как JSON файл, но типизированный. Хранит данные вне кода.

### McqQuestionData.cs
```csharp
[CreateAssetMenu(fileName = "MCQ_Question", menuName = "Quiz/MCQ Question")]
public class McqQuestionData : ScriptableObject
{
    [SerializeField] private string questionText;
    [SerializeField] private string[] choices;      // 4 варианта
    [SerializeField] private int correctIndex;      // Индекс правильного
    [SerializeField] private string category;
}
```

### Как создать новый вопрос:
1. В Unity: **Right Click** в папке `Assets/_Project/ScriptableObjects/Questions/`
2. Выбери **Create → Quiz → MCQ Question**
3. Заполни поля в Inspector

### NumericQuestionData.cs
```csharp
[CreateAssetMenu(fileName = "Numeric_Question", menuName = "Quiz/Numeric Question")]
public class NumericQuestionData : ScriptableObject
{
    [SerializeField] private string questionText;
    [SerializeField] private float correctValue;    // Правильный ответ
    [SerializeField] private string unit;           // Единица измерения
    [SerializeField] private float allowedRangeMin;
    [SerializeField] private float allowedRangeMax;
}
```

---

## 🐛 Debugging (Отладка)

### Console (как Logcat)
- **Window → General → Console** (или Ctrl+Shift+C)
- `Debug.Log("сообщение")` - обычный лог
- `Debug.LogWarning("warning")` - жёлтый warning
- `Debug.LogError("error")` - красная ошибка

### Inspector (как Layout Inspector)
- Выбери GameObject в Hierarchy
- Справа в Inspector увидишь все компоненты и их значения
- Можно менять значения **в реальном времени** во время Play

### Breakpoints
1. Открой скрипт в VS Code/Rider
2. Поставь breakpoint (F9)
3. В Unity: **Edit → Preferences → External Tools** - выбери свой IDE
4. Нажми Play в Unity
5. В IDE: **Attach to Unity**

### Полезные Debug методы:
```csharp
// Пауза игры
Debug.Break();

// Логирование с контекстом (кликни на лог - выделится объект)
Debug.Log("Message", gameObject);

// Рисование линий в Scene view
Debug.DrawLine(start, end, Color.red, duration);

// Проверка условий (как assert)
Debug.Assert(value != null, "Value should not be null!");
```

---

## ➕ Как добавить новую функциональность

### Добавить новый тип вопроса:

1. **Создай ScriptableObject:**
```csharp
// Assets/_Project/Scripts/Quiz/TrueFalseQuestionData.cs
[CreateAssetMenu(fileName = "TF_Question", menuName = "Quiz/True-False Question")]
public class TrueFalseQuestionData : ScriptableObject
{
    [SerializeField] private string questionText;
    [SerializeField] private bool correctAnswer;
}
```

2. **Добавь UI в QuizSceneSetup.cs:**
```csharp
private void CreateTrueFalsePanel(Transform parent)
{
    var panel = new GameObject("TrueFalsePanel");
    panel.transform.SetParent(parent, false);
    
    var trueBtn = CreateButton(panel.transform, "TRUE", Color.green);
    var falseBtn = CreateButton(panel.transform, "FALSE", Color.red);
    
    // ... настройка layout
}
```

3. **Добавь логику в QuizUIController.cs:**
```csharp
public void ShowTrueFalseQuestion(TrueFalseQuestionData question)
{
    // Показать панель
    // Настроить текст
    // Запустить таймер
}
```

4. **Обработай в QuizSessionController.cs:**
```csharp
private IEnumerator RunTrueFalseRound(TrueFalseQuestionData question)
{
    quizUI.ShowTrueFalseQuestion(question);
    
    // Ждём ответы
    yield return WaitForBothAnswers();
    
    // Оцениваем
    EvaluateTrueFalseRound();
}
```

---

### Добавить новый режим игры:

1. **В SessionDebugPanel.cs добавь кнопку:**
```csharp
var tournamentBtn = CreateButton(parent, "Tournament Mode", Color.cyan);
tournamentBtn.onClick.AddListener(StartTournament);

private void StartTournament()
{
    // Твоя логика турнира
    Debug.Log("Starting tournament!");
}
```

2. **В QuizSessionController.cs добавь метод:**
```csharp
public void StartTournament(List<IAnswerProvider> players)
{
    // Логика турнира с несколькими игроками
}
```

---

## 🔧 Частые проблемы

### "NullReferenceException"
**Причина:** Объект не найден или не создан
**Решение:** Добавь проверку:
```csharp
if (myObject != null)
{
    myObject.DoSomething();
}
// Или:
myObject?.DoSomething();
```

### "UI не отображается"
**Проверь:**
1. Canvas есть на сцене?
2. Canvas.sortingOrder правильный?
3. Объект активен? (`gameObject.SetActive(true)`)
4. RectTransform настроен? (размеры, anchors)

### "Кнопка не реагирует"
**Проверь:**
1. Button компонент добавлен?
2. `button.interactable = true`?
3. GraphicRaycaster есть на Canvas?
4. EventSystem есть на сцене?
5. Нет ли другого UI элемента поверх?

### "Скрипт не выполняется"
**Проверь:**
1. Скрипт добавлен на GameObject?
2. GameObject активен?
3. Нет ошибок компиляции?

---

## 📚 Полезные ссылки

- [Unity Manual](https://docs.unity3d.com/Manual/)
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/)
- [Unity Learn](https://learn.unity.com/)

---

## 📝 Глоссарий Unity терминов

| Термин | Аналог в Android | Описание |
|--------|------------------|----------|
| Scene | Activity | Отдельный "экран" |
| GameObject | View | Любой объект |
| Component | - | Поведение/данные объекта |
| MonoBehaviour | ViewModel | Базовый класс для скриптов |
| Transform | LayoutParams | Позиция, поворот, масштаб |
| RectTransform | View bounds | Transform для UI элементов |
| Canvas | ViewGroup root | Контейнер для UI |
| Prefab | XML layout | Шаблон объекта |
| Inspector | Properties panel | Редактор свойств |
| Hierarchy | View tree | Дерево объектов на сцене |
| Project | Resources | Файлы проекта |
| Play Mode | Run | Запуск игры |
| Edit Mode | Design | Редактирование |

