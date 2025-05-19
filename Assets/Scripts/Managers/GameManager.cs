using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Core;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
namespace MinipollGame
{
    // מנהל המשחק הראשי - אחראי על כל צדדי המשחק ברמה גבוהה
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; } // Singleton
        
        [Header("Game Settings")]
        [SerializeField] private bool _startPaused = false;
        [SerializeField] private float _gameSpeed = 1.0f;
        [SerializeField] private float _dayNightCycleDuration = 300f; // 5 דקות ליממה
        
        [Header("Minipoll Settings")]
        [SerializeField] private GameObject _minipollPrefab;
        [SerializeField] private int _initialMinipollCount = 5;
        [SerializeField] private int _maxMinipollCount = 20;
        [SerializeField] private Transform[] _spawnPoints;
        
        [Header("World Management")]
        [SerializeField] private WorldManager _worldManager;
        [SerializeField] private bool _dynamicEnvironment = true;
        
        [Header("UI References")]
        [SerializeField] private GameObject _gameUI;
        [SerializeField] private GameObject _pauseMenu;
        [SerializeField] private GameObject _loadingScreen;
        
        // מצב המשחק
        private bool _isGamePaused = false;
        private TimeOfDay _currentTimeOfDay;
        private int _dayNumber = 1;
        private float _timeOfDayNormalized = 0f; // 0-1 representing progress through the day
        private List<MinipollBrain> _activeMinipolls = new List<MinipollBrain>();
        
        // מידע סטטיסטי
        private int _totalMinipollsCreated = 0;
        private int _totalInteractionsPerformed = 0;
        private Dictionary<EmotionType, int> _emotionCounts = new Dictionary<EmotionType, int>();
        
        // אירועים
        public event System.Action<TimeOfDay> OnTimeOfDayChanged;
        public event System.Action<bool> OnGamePaused;
        public event System.Action<MinipollBrain> OnMinipollCreated;
        public event System.Action<MinipollBrain> OnMinipollRemoved;
        
        // Properties
        public bool IsGamePaused => _isGamePaused;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public int DayNumber => _dayNumber;
        public float GameSpeed => _gameSpeed;
        public List<MinipollBrain> ActiveMinipolls => _activeMinipolls;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // איתחול
            InitializeGame();
        }
        
        private void Start()
        {
            // התחלת ניהול זמן
            StartCoroutine(TimeManager());
            
            // אם המשחק מתחיל במצב מושהה
            if (_startPaused)
            {
                PauseGame();
            }
            
            // יצירת מיניפולים התחלתיים
            for (int i = 0; i < _initialMinipollCount; i++)
            {
                CreateMinipoll();
            }
            
            // איתחול מנהל העולם
            if (_worldManager != null)
            {
                _worldManager.Initialize();
            }
            
            // רישום לאירועים
            EventSystem.OnMinipollInteraction += HandleMinipollInteraction;
            EventSystem.OnMinipollEmotionChanged += HandleMinipollEmotionChanged;
        }
        
        private void OnDestroy()
        {
            // ביטול הרשמה לאירועים
            EventSystem.OnMinipollInteraction -= HandleMinipollInteraction;
            EventSystem.OnMinipollEmotionChanged -= HandleMinipollEmotionChanged;
        }
        
        private void Update()
        {
       if (Keyboard.current.escapeKey.wasPressedThisFrame)
{
    TogglePause();
}

// Increase game speed
if (Keyboard.current.equalsKey.wasPressedThisFrame    // "="  (shift + "=" == "+")
    || (Keyboard.current.numpadPlusKey != null        // במקלדת עם NumPad
        && Keyboard.current.numpadPlusKey.wasPressedThisFrame))
{
    SetGameSpeed(_gameSpeed + 0.25f);
}
        }
        #region Game Management
        
        // איתחול המשחק
        private void InitializeGame()
        {
            // איפוס מצב המשחק
            _isGamePaused = false;
            _dayNumber = 1;
            _timeOfDayNormalized = 0.3f; // התחלה בבוקר
            
            // איתחול סטטיסטיקות
            _totalMinipollsCreated = 0;
            _totalInteractionsPerformed = 0;
            _emotionCounts.Clear();
            
            // איפוס זמן
            InitializeTimeOfDay();
            
            // איתחול ממשק משתמש
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
            
            if (_gameUI != null)
            {
                _gameUI.SetActive(true);
            }
            
            if (_pauseMenu != null)
            {
                _pauseMenu.SetActive(false);
            }
            
            // הגדרת מהירות
            SetGameSpeed(1.0f);
        }
        
        // איתחול שעון המשחק
        private void InitializeTimeOfDay()
        {
            // קביעת מצב זמן התחלתי
            _currentTimeOfDay = new TimeOfDay
            {
                CurrentPhase = CalculateTimePhase(_timeOfDayNormalized),
                NormalizedTime = _timeOfDayNormalized,
                DayNumber = _dayNumber
            };
            
            // הודעה על שינוי זמן
            OnTimeOfDayChanged?.Invoke(_currentTimeOfDay);
            EventSystem.TriggerTimeOfDayChange(_currentTimeOfDay);
        }
        
        // קורוטינה לניהול הזמן
        private IEnumerator TimeManager()
        {
            while (true)
            {
                if (!_isGamePaused)
                {
                    // חישוב קידום זמן
                    float timeIncrement = (Time.deltaTime / _dayNightCycleDuration) * _gameSpeed;
                    _timeOfDayNormalized += timeIncrement;
                    
                    // מעבר ליום הבא
                    if (_timeOfDayNormalized >= 1.0f)
                    {
                        _timeOfDayNormalized = 0f;
                        _dayNumber++;
                        
                        // טריגר אירוע יום חדש אם צריך
                        ProcessNewDay();
                    }
                    
                    // עדכון זמן משחק
                    TimeOfDay.DayPhase currentPhase = CalculateTimePhase(_timeOfDayNormalized);
                    
                    // בדיקה אם היה שינוי בשלב היום
                    if (currentPhase != _currentTimeOfDay.CurrentPhase)
                    {
                        _currentTimeOfDay = new TimeOfDay
                        {
                            CurrentPhase = currentPhase,
                            NormalizedTime = _timeOfDayNormalized,
                            DayNumber = _dayNumber
                        };
                        
                        // הפעלת אירוע שינוי זמן
                        OnTimeOfDayChanged?.Invoke(_currentTimeOfDay);
                        EventSystem.TriggerTimeOfDayChange(_currentTimeOfDay);
                    }
                }
                
                yield return null;
            }
        }
        
        // חישוב שלב היום לפי זמן נורמלי
        private TimeOfDay.DayPhase CalculateTimePhase(float normalizedTime)
        {
            if (normalizedTime < 0.125f)
                return TimeOfDay.DayPhase.Dawn;
            else if (normalizedTime < 0.3f)
                return TimeOfDay.DayPhase.Morning;
            else if (normalizedTime < 0.45f)
                return TimeOfDay.DayPhase.Noon;
            else if (normalizedTime < 0.6f)
                return TimeOfDay.DayPhase.Afternoon;
            else if (normalizedTime < 0.75f)
                return TimeOfDay.DayPhase.Evening;
            else if (normalizedTime < 0.9f)
                return TimeOfDay.DayPhase.Night;
            else
                return TimeOfDay.DayPhase.Midnight;
        }
        
        // עיבוד יום חדש
        private void ProcessNewDay()
        {
            Debug.Log($"New day started: Day {_dayNumber}");
            
            // הוספת מיניפול חדש מדי כמה ימים
            if (_dayNumber % 3 == 0 && _activeMinipolls.Count < _maxMinipollCount)
            {
                CreateMinipoll();
            }
            
            // עדכון עולם דינמי
            if (_dynamicEnvironment && _worldManager != null)
            {
                _worldManager.UpdateWorldForNewDay(_dayNumber);
            }
        }
        
        // השהיית משחק
        public void PauseGame()
        {
            if (!_isGamePaused)
            {
                _isGamePaused = true;
                Time.timeScale = 0f;
                
                if (_pauseMenu != null)
                {
                    _pauseMenu.SetActive(true);
                }
                
                OnGamePaused?.Invoke(true);
            }
        }
        
        // המשך משחק
        public void ResumeGame()
        {
            if (_isGamePaused)
            {
                _isGamePaused = false;
                Time.timeScale = _gameSpeed;
                
                if (_pauseMenu != null)
                {
                    _pauseMenu.SetActive(false);
                }
                
                OnGamePaused?.Invoke(false);
            }
        }
        
        // החלפת מצב משחק
        public void TogglePause()
        {
            if (_isGamePaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
        
        // הגדרת מהירות משחק
        public void SetGameSpeed(float speed)
        {
            _gameSpeed = Mathf.Clamp(speed, 0.25f, 3f);
            
            if (!_isGamePaused)
            {
                Time.timeScale = _gameSpeed;
            }
        }
        
        // טעינת סצנה חדשה
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        // טעינת סצנה אסינכרונית
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            // הצגת מסך טעינה
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(true);
            }
            
            // איפוס מהירות
            Time.timeScale = 1f;
            
            // טעינת סצנה
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            
            while (!asyncLoad.isDone)
            {
                // עדכון מדד טעינה אם צריך
                yield return null;
            }
            
            // סיום טעינה
            if (_loadingScreen != null)
            {
                _loadingScreen.SetActive(false);
            }
            
            // איתחול מחדש של המשחק
            InitializeGame();
        }
        
        // יציאה מהמשחק
        public void QuitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
        
        #region Minipoll Management
        
        // יצירת מיניפול חדש
      public MinipollBrain CreateMinipoll()
{
    // 1. pick spawn position via WorldManager
    Vector3 spawnPosition;

    // try to get a random spawn point from WorldManager
    Transform spawnPoint = _worldManager != null
        ? _worldManager.GetRandomMinipollSpawnPoint()
        : null;

    if (spawnPoint != null)
    {
        spawnPosition = spawnPoint.position;
    }
    else
    {
        // fallback: small random square around origin
        spawnPosition = new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f));
    }

    // 2. instantiate the minipoll
    GameObject minipollObj = Instantiate(_minipollPrefab, spawnPosition, Quaternion.identity);
    MinipollBrain brain = minipollObj.GetComponent<MinipollBrain>();

    if (brain != null)
    {
        // give a unique name
        minipollObj.name = $"Minipoll_{_totalMinipollsCreated++}";

        // track in active list
        _activeMinipolls.Add(brain);

        // fire creation event
        OnMinipollCreated?.Invoke(brain);
    }

    return brain;
}
        
        // הסרת מיניפול
        public void RemoveMinipoll(MinipollBrain minipoll)
        {
            if (minipoll != null && _activeMinipolls.Contains(minipoll))
            {
                // הסרה מהרשימה
                _activeMinipolls.Remove(minipoll);
                
                // הפעלת אירוע
                OnMinipollRemoved?.Invoke(minipoll);
                
                // השמדת האובייקט
                Destroy(minipoll.gameObject);
            }
        }
        
        // קבלת מיניפול אקראי
        public MinipollBrain GetRandomMinipoll()
        {
            if (_activeMinipolls.Count > 0)
            {
                return _activeMinipolls[Random.Range(0, _activeMinipolls.Count)];
            }
            
            return null;
        }
        
        // קבלת מיניפול הכי קרוב לנקודה
        public MinipollBrain GetClosestMinipoll(Vector3 position)
        {
            MinipollBrain closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var minipoll in _activeMinipolls)
            {
                float distance = Vector3.Distance(position, minipoll.transform.position);
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = minipoll;
                }
            }
            
            return closest;
        }
        
        #endregion
        
        #region Event Handlers
        
        // טיפול באינטראקציות מיניפול
        private void HandleMinipollInteraction(MinipollBrain minipoll, IInteractable target, InteractionResult result)
        {
            _totalInteractionsPerformed++;
        }
        
        // טיפול בשינויי רגשות
        private void HandleMinipollEmotionChanged(MinipollBrain minipoll, EmotionType emotion, float intensity)
        {
            // עדכון סטטיסטיקות רגשות
            if (!_emotionCounts.ContainsKey(emotion))
            {
                _emotionCounts[emotion] = 0;
            }
            
            _emotionCounts[emotion]++;
        }
        
        #endregion
        
        #region Debug Methods
        
        // שיטות לבדיקת המערכת
        
        // הדפסת מצב משחק
        public void PrintGameState()
        {
            Debug.Log($"=== Game State ===");
            Debug.Log($"Day: {_dayNumber}, Time: {_currentTimeOfDay.CurrentPhase} ({_timeOfDayNormalized:F2})");
            Debug.Log($"Active Minipolls: {_activeMinipolls.Count}");
            Debug.Log($"Total Interactions: {_totalInteractionsPerformed}");
            Debug.Log($"Game Speed: {_gameSpeed}x");
            Debug.Log($"Game Paused: {_isGamePaused}");
            
            // הדפסת מצב רגשות
            Debug.Log("Emotion Statistics:");
            foreach (var emotion in _emotionCounts)
            {
                Debug.Log($"- {emotion.Key}: {emotion.Value}");
            }
        }
        
        // בדיקת כל המיניפולים
        public void InspectAllMinipolls()
        {
            Debug.Log($"=== Inspecting {_activeMinipolls.Count} Minipolls ===");
            
            foreach (var minipoll in _activeMinipolls)
            {
                Debug.Log($"Minipoll: {minipoll.gameObject.name}");
                Debug.Log($"- Position: {minipoll.transform.position}");
                Debug.Log($"- Dominant Emotion: {minipoll.EmotionalState.DominantEmotion}");
                Debug.Log($"- Movement State: {minipoll.MovementController.MovementState}");
                Debug.Log($"- Life State: {minipoll.CurrentLifeState}");
                Debug.Log($"- Last Decision: {minipoll.LastDecisionType}");
                Debug.Log("-----");
            }
        }
        
        #endregion
    }
}