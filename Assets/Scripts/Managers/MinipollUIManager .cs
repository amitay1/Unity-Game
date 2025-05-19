using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MinipollGame.Core;
using UnityEngine.InputSystem;
namespace MinipollGame
{
    // מנהל ממשק משתמש - אחראי על הצגת מידע על המיניפולים וממשק המשחק
    public class MinipollUIManager : MonoBehaviour
    {
        [Header("Main UI")]
        [SerializeField] private GameObject _mainHUD;
        [SerializeField] private TMP_Text _timeDisplay;
        [SerializeField] private TMP_Text _dayDisplay;
        [SerializeField] private Image _weatherIcon;
        [SerializeField] private TMP_Text _minipollCountText;
        [SerializeField] private Slider _gameSpeedSlider;
        [SerializeField] private Camera _mainCamera;
        
        [Header("Minipoll Info")]
        [SerializeField] private GameObject _minipollInfoPanel;
        [SerializeField] private TMP_Text _selectedMinipollName;
        [SerializeField] private Image _emotionIcon;
        [SerializeField] private Slider _emotionIntensitySlider;
        [SerializeField] private TMP_Text _emotionText;
        [SerializeField] private Image _minipollPortrait;
        [SerializeField] private GameObject _relationshipItemPrefab;
        [SerializeField] private Transform _relationshipsContainer;
        [SerializeField] private GameObject _experienceItemPrefab;
        [SerializeField] private Transform _experiencesContainer;
        
        [Header("Icons")]
        [SerializeField] private Sprite[] _emotionIcons;
        [SerializeField] private Sprite[] _weatherIcons;
        [SerializeField] private Sprite _defaultMinipollSprite;
        [SerializeField] private Color[] _emotionColors;
        
        [Header("World Objects")]
        [SerializeField] private GameObject _selectionCirclePrefab;
        [SerializeField] private GameObject _emotionBubblePrefab;
        
        // התייחסויות
        private GameManager _gameManager;
        private MinipollBrain _selectedMinipoll;
        private GameObject _selectionCircle;
        private Dictionary<MinipollBrain, GameObject> _emotionBubbles = new Dictionary<MinipollBrain, GameObject>();
        
        // נתוני ממשק
        private bool _isInfoPanelVisible = false;
        private float _uiUpdateInterval = 0.5f;
        private float _lastUIUpdateTime;
        
        private void Start()
        {
            // מציאת מנהל המשחק
            _gameManager = GameManager.Instance;
            
            if (_gameManager == null)
            {
                Debug.LogError("GameManager not found!");
                return;
            }
            
            // מציאת המצלמה הראשית
            _mainCamera = Camera.main;
            
            // רישום לאירועים
            _gameManager.OnTimeOfDayChanged += UpdateTimeDisplay;
            _gameManager.OnGamePaused += HandleGamePaused;
            _gameManager.OnMinipollCreated += HandleMinipollCreated;
            _gameManager.OnMinipollRemoved += HandleMinipollRemoved;
            
            EventSystem.OnMinipollEmotionChanged += HandleMinipollEmotionChanged;
            
            // איתחול ממשק
            InitializeUI();
            
            // הסתרת פאנל מידע מיניפול
            ShowMinipollInfoPanel(false);
        }
        
        private void Awake()
{
    if (_mainCamera == null)
        _mainCamera = Camera.main;
}
        private void OnDestroy()
        {
            // ביטול הרשמה לאירועים
            if (_gameManager != null)
            {
                _gameManager.OnTimeOfDayChanged -= UpdateTimeDisplay;
                _gameManager.OnGamePaused -= HandleGamePaused;
                _gameManager.OnMinipollCreated -= HandleMinipollCreated;
                _gameManager.OnMinipollRemoved -= HandleMinipollRemoved;
            }
            
            EventSystem.OnMinipollEmotionChanged -= HandleMinipollEmotionChanged;
            
            // ניקוי בועות רגש
            ClearAllEmotionBubbles();
        }
        
        private void Update()
        {
            // עדכון ממשק בהתאם לאינטרוול הנבחר
            if (Time.time - _lastUIUpdateTime > _uiUpdateInterval)
            {
                UpdateUI();
                _lastUIUpdateTime = Time.time;
            }
            
            // בדיקה לבחירת מיניפול
            CheckForMinipollSelection();
            
            // עדכון מיקום של בועות רגש וחפצים ויזואליים
            UpdateWorldUIElements();
        }
        
        // איתחול ממשק משתמש
        private void InitializeUI()
        {
            // הגדרת Slider למהירות משחק
            if (_gameSpeedSlider != null)
            {
                _gameSpeedSlider.minValue = 0.25f;
                _gameSpeedSlider.maxValue = 3f;
                _gameSpeedSlider.value = 1f;
                _gameSpeedSlider.onValueChanged.AddListener(HandleGameSpeedChanged);
            }
            
            // איתחול טקסטים
            UpdateTimeDisplay(_gameManager.CurrentTimeOfDay);
            UpdateMinipollCountText();
        }
        
        // עדכון ממשק
        private void UpdateUI()
        {
            // עדכון מספר מיניפולים
            UpdateMinipollCountText();
            
            // עדכון אייקון מזג אוויר
            UpdateWeatherIcon();
            
            // עדכון מידע על מיניפול נבחר
            if (_isInfoPanelVisible && _selectedMinipoll != null)
            {
                UpdateSelectedMinipollInfo();
            }
        }
        
        // עדכון תצוגת זמן
        private void UpdateTimeDisplay(TimeOfDay timeOfDay)
        {
            if (_timeDisplay != null)
            {
                _timeDisplay.text = timeOfDay.CurrentPhase.ToString();
            }
            
            if (_dayDisplay != null)
            {
                _dayDisplay.text = $"Day {timeOfDay.DayNumber}";
            }
        }
        
        // עדכון טקסט מספר מיניפולים
        private void UpdateMinipollCountText()
        {
            if (_minipollCountText != null && _gameManager != null)
            {
                _minipollCountText.text = $"Minipolls: {_gameManager.ActiveMinipolls.Count}";
            }
        }
        
        // עדכון אייקון מזג אוויר
        private void UpdateWeatherIcon()
        {
            // אם אין אייקון או מנהל עולם, מסיימים כאן
            if (_weatherIcon == null || _weatherIcons == null || _weatherIcons.Length == 0)
            {
                return;
            }
            
            // מציאת מנהל עולם
            WorldManager worldManager = FindFirstObjectByType<WorldManager>();
            
            if (worldManager != null)
            {
                // קבלת מזג אוויר נוכחי
                WeatherType currentWeather = worldManager.GetCurrentWeather();
                
                // עדכון אייקון מתאים
                int iconIndex = (int)currentWeather;
                if (iconIndex < _weatherIcons.Length)
                {
                    _weatherIcon.sprite = _weatherIcons[iconIndex];
                    _weatherIcon.enabled = true;
                }
            }
        }
        
        // הצגת/הסתרת פאנל מידע מיניפול
        private void ShowMinipollInfoPanel(bool show)
        {
            if (_minipollInfoPanel != null)
            {
                _minipollInfoPanel.SetActive(show);
                _isInfoPanelVisible = show;
            }
        }
        
        // עדכון מידע על מיניפול נבחר
        private void UpdateSelectedMinipollInfo()
        {
            if (_selectedMinipoll == null || !_isInfoPanelVisible)
                return;
            
            // עדכון שם
            if (_selectedMinipollName != null)
            {
                _selectedMinipollName.text = _selectedMinipoll.gameObject.name;
            }
            
            // עדכון מידע רגשי
            if (_selectedMinipoll.EmotionalState != null)
            {
                EmotionType dominantEmotion = _selectedMinipoll.EmotionalState.DominantEmotion;
                float intensity = _selectedMinipoll.EmotionalState.GetEmotionIntensity(dominantEmotion);
                
                // רגש
                if (_emotionText != null)
                {
                    _emotionText.text = dominantEmotion.ToString();
                }
                
                // עוצמת רגש
                if (_emotionIntensitySlider != null)
                {
                    _emotionIntensitySlider.value = intensity;
                }
                
                // אייקון רגש
                if (_emotionIcon != null && _emotionIcons != null && _emotionIcons.Length > 0)
                {
                    int emotionIndex = (int)dominantEmotion;
                    if (emotionIndex < _emotionIcons.Length)
                    {
                        _emotionIcon.sprite = _emotionIcons[emotionIndex];
                    }
                    
                    // צבע אייקון לפי רגש
                    if (_emotionColors != null && _emotionColors.Length > 0 && emotionIndex < _emotionColors.Length)
                    {
                        _emotionIcon.color = _emotionColors[emotionIndex];
                    }
                }
            }
            
            // עדכון יחסים
            UpdateRelationshipsList();
            
            // עדכון רשימת חוויות
            UpdateExperiencesList();
        }
        
        // עדכון רשימת יחסים
        private void UpdateRelationshipsList()
        {
            if (_relationshipsContainer == null || _relationshipItemPrefab == null || _selectedMinipoll == null)
                return;
            
            // ניקוי אובייקטים קיימים
            foreach (Transform child in _relationshipsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // אם אין מערכת יחסים, יוצאים
            if (_selectedMinipoll.SocialRelations == null)
                return;
            
            // מציאת חברים
            List<MinipollBrain> friends = _selectedMinipoll.SocialRelations.GetFriends();
            
            // יצירת פריט עבור כל חבר
            foreach (var friend in friends)
            {
                GameObject item = Instantiate(_relationshipItemPrefab, _relationshipsContainer);
                
                // הגדרת מידע
                TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = friend.gameObject.name;
                }
                
                // הגדרת אירוע לחיצה
                Button selectButton = item.GetComponentInChildren<Button>();
                if (selectButton != null)
                {
                    // שמירת התייחסות למיניפול
                    MinipollBrain reference = friend;
                    selectButton.onClick.AddListener(() => SelectMinipoll(reference));
                }
            }
            
            // אם אין חברים, הצגת "אין חברים"
            if (friends.Count == 0)
            {
                GameObject item = Instantiate(_relationshipItemPrefab, _relationshipsContainer);
                
                TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = "No friends yet";
                }
                
                Button selectButton = item.GetComponentInChildren<Button>();
                if (selectButton != null)
                {
                    selectButton.interactable = false;
                }
            }
        }
        
        // עדכון רשימת חוויות
        private void UpdateExperiencesList()
        {
            if (_experiencesContainer == null || _experienceItemPrefab == null || _selectedMinipoll == null)
                return;
            
            // ניקוי אובייקטים קיימים
            foreach (Transform child in _experiencesContainer)
            {
                Destroy(child.gameObject);
            }
            
            // אם אין מערכת למידה, יוצאים
            if (_selectedMinipoll.LearningSystem == null)
                return;
            
            // קבלת חוויות (בגרסה זו יש להשלים את איסוף החוויות)
            string significantPositive = _selectedMinipoll.LearningSystem.GetMostSignificantExperience(true);
            string significantNegative = _selectedMinipoll.LearningSystem.GetMostSignificantExperience(false);
            
            // יצירת פריט עבור חוויות חיוביות
            if (!string.IsNullOrEmpty(significantPositive))
            {
                GameObject item = Instantiate(_experienceItemPrefab, _experiencesContainer);
                
                TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = $"+ {significantPositive}";
                    nameText.color = Color.green;
                }
            }
            
            // יצירת פריט עבור חוויות שליליות
            if (!string.IsNullOrEmpty(significantNegative))
            {
                GameObject item = Instantiate(_experienceItemPrefab, _experiencesContainer);
                
                TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = $"- {significantNegative}";
                    nameText.color = Color.red;
                }
            }
            
            // אם אין חוויות, הצגת "אין חוויות"
            if (string.IsNullOrEmpty(significantPositive) && string.IsNullOrEmpty(significantNegative))
            {
                GameObject item = Instantiate(_experienceItemPrefab, _experiencesContainer);
                
                TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
                if (nameText != null)
                {
                    nameText.text = "No significant experiences yet";
                }
            }
        }
        
        // בדיקת בחירת מיניפול על ידי לחיצה
      private void CheckForMinipollSelection()
{
       if (_mainCamera == null || Mouse.current == null)
        return;
    // בדיקה ללחיצת עכבר (Input System)
    if (Mouse.current.leftButton.wasPressedThisFrame)
    {
        Vector2 screenPos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // בדיקה אם נבחר מיניפול
            MinipollBrain hitMinipoll = hit.collider.GetComponent<MinipollBrain>();

            if (hitMinipoll != null)
            {
                SelectMinipoll(hitMinipoll);
            }
            else
            {
                // ביטול בחירה אם נלחץ במקום אחר
                DeselectMinipoll();
            }
        }
        else
        {
            // ביטול בחירה אם נלחץ במקום ריק
            DeselectMinipoll();
        }
    }
}
        
        // בחירת מיניפול
        public void SelectMinipoll(MinipollBrain minipoll)
        {
            // שמירת המיניפול הנבחר
            _selectedMinipoll = minipoll;
            
            // הצגת פאנל מידע
            ShowMinipollInfoPanel(true);
            
            // עדכון מידע
            UpdateSelectedMinipollInfo();
            
            // יצירת עיגול בחירה אם לא קיים
            if (_selectionCircle == null && _selectionCirclePrefab != null)
            {
                _selectionCircle = Instantiate(_selectionCirclePrefab, minipoll.transform.position, Quaternion.identity);
            }
            else if (_selectionCircle != null)
            {
                _selectionCircle.transform.position = minipoll.transform.position;
                _selectionCircle.SetActive(true);
            }
        }
        
        // ביטול בחירת מיניפול
        public void DeselectMinipoll()
        {
            _selectedMinipoll = null;
            ShowMinipollInfoPanel(false);
            
            // הסתרת עיגול בחירה
            if (_selectionCircle != null)
            {
                _selectionCircle.SetActive(false);
            }
        }
        
        // עדכון אלמנטים ויזואליים בעולם
        private void UpdateWorldUIElements()
        {
            // עדכון עיגול בחירה
            if (_selectionCircle != null && _selectionCircle.activeSelf && _selectedMinipoll != null)
            {
                _selectionCircle.transform.position = new Vector3(
                    _selectedMinipoll.transform.position.x,
                    _selectedMinipoll.transform.position.y + 0.1f,
                    _selectedMinipoll.transform.position.z
                );
            }
            
            // עדכון בועות רגש
            foreach (var bubble in _emotionBubbles)
            {
                if (bubble.Key != null && bubble.Value != null)
                {
                    // עדכון מיקום
                    Vector3 bubblePosition = bubble.Key.transform.position + Vector3.up * 1.2f;
                    bubble.Value.transform.position = bubblePosition;
                    
                    // סיבוב לכיוון המצלמה
                    bubble.Value.transform.LookAt(bubble.Value.transform.position + _mainCamera.transform.forward);
                }
            }
        }
        
        // עדכון בועת רגש למיניפול
        private void UpdateEmotionBubble(MinipollBrain minipoll, EmotionType emotion, float intensity)
        {
            // אם עוצמת הרגש נמוכה, לא מציגים בועה
            if (intensity < 0.5f)
            {
                // הסרת בועה קיימת אם יש
                if (_emotionBubbles.ContainsKey(minipoll) && _emotionBubbles[minipoll] != null)
                {
                    Destroy(_emotionBubbles[minipoll]);
                    _emotionBubbles.Remove(minipoll);
                }
                
                return;
            }
            
            // אם אין אייקונים או פריפאב, יוצאים
            if (_emotionBubblePrefab == null || _emotionIcons == null || _emotionIcons.Length == 0)
            {
                return;
            }
            
            // אם כבר יש בועה, עדכון שלה
            if (_emotionBubbles.ContainsKey(minipoll) && _emotionBubbles[minipoll] != null)
            {
                // עדכון אייקון
                Image bubbleImage = _emotionBubbles[minipoll].GetComponentInChildren<Image>();
                
                if (bubbleImage != null)
                {
                    int emotionIndex = (int)emotion;
                    
                    if (emotionIndex < _emotionIcons.Length)
                    {
                        bubbleImage.sprite = _emotionIcons[emotionIndex];
                    }
                    
                    // עדכון גודל לפי עוצמה
                    float scale = 0.5f + (intensity * 0.5f);
                    _emotionBubbles[minipoll].transform.localScale = new Vector3(scale, scale, scale);
                }
            }
            else
            {
                // יצירת בועה חדשה
                Vector3 bubblePosition = minipoll.transform.position + Vector3.up * 1.2f;
                GameObject bubbleObj = Instantiate(_emotionBubblePrefab, bubblePosition, Quaternion.identity);
                
                // עדכון אייקון
                Image bubbleImage = bubbleObj.GetComponentInChildren<Image>();
                
                if (bubbleImage != null)
                {
                    int emotionIndex = (int)emotion;
                    
                    if (emotionIndex < _emotionIcons.Length)
                    {
                        bubbleImage.sprite = _emotionIcons[emotionIndex];
                    }
                    
                    // עדכון גודל לפי עוצמה
                    float scale = 0.5f + (intensity * 0.5f);
                    bubbleObj.transform.localScale = new Vector3(scale, scale, scale);
                }
                
                // הוספה למילון
                _emotionBubbles[minipoll] = bubbleObj;
            }
        }
        
        // ניקוי כל בועות הרגש
        private void ClearAllEmotionBubbles()
        {
            foreach (var bubble in _emotionBubbles.Values)
            {
                if (bubble != null)
                {
                    Destroy(bubble);
                }
            }
            
            _emotionBubbles.Clear();
        }
        
        #region Event Handlers
        
        // טיפול באירוע השהיית משחק
        private void HandleGamePaused(bool isPaused)
        {
            // עדכון UI בהתאם
            if (_mainHUD != null)
            {
                // אפשר להשתמש בזה להסתיר חלקים מסוימים של ה-UI במצב השהייה
            }
        }
        
        // טיפול באירוע יצירת מיניפול
        private void HandleMinipollCreated(MinipollBrain minipoll)
        {
            UpdateMinipollCountText();
        }
        
        // טיפול באירוע הסרת מיניפול
        private void HandleMinipollRemoved(MinipollBrain minipoll)
        {
            // עדכון מספר מיניפולים
            UpdateMinipollCountText();
            
            // אם זה המיניפול הנבחר, ביטול בחירה
            if (_selectedMinipoll == minipoll)
            {
                DeselectMinipoll();
            }
            
            // הסרת בועת רגש אם יש
            if (_emotionBubbles.ContainsKey(minipoll))
            {
                if (_emotionBubbles[minipoll] != null)
                {
                    Destroy(_emotionBubbles[minipoll]);
                }
                
                _emotionBubbles.Remove(minipoll);
            }
        }
        
        // טיפול באירוע שינוי רגש
        private void HandleMinipollEmotionChanged(MinipollBrain minipoll, EmotionType emotion, float intensity)
        {
            // עדכון בועת רגש
            UpdateEmotionBubble(minipoll, emotion, intensity);
            
            // אם זה המיניפול הנבחר, עדכון מידע
            if (_selectedMinipoll == minipoll && _isInfoPanelVisible)
            {
                UpdateSelectedMinipollInfo();
            }
        }
        
        // טיפול בשינוי מהירות משחק
        private void HandleGameSpeedChanged(float value)
        {
            if (_gameManager != null)
            {
                _gameManager.SetGameSpeed(value);
            }
        }
        
        #endregion
        
        #region Public UI Methods
        
        // כפתור להפעלת/השהיית משחק
        public void TogglePauseButton()
        {
            if (_gameManager != null)
            {
                _gameManager.TogglePause();
            }
        }
        
        // יציאה ממשחק
        public void QuitGameButton()
        {
            if (_gameManager != null)
            {
                _gameManager.QuitGame();
            }
        }
        
        // סגירת פאנל מידע מיניפול
        public void CloseInfoPanelButton()
        {
            DeselectMinipoll();
        }
        
        // הפעלת פקודה על המיניפול הנבחר
        public void CommandSelectedMinipoll(string commandName)
        {
            if (_selectedMinipoll == null)
                return;
            
            switch (commandName)
            {
                case "Stop":
                    _selectedMinipoll.MovementController.StopMoving();
                    break;
                
                case "Wander":
                    _selectedMinipoll.MovementController.WanderRandomly();
                    break;
                
                case "Rest":
                    _selectedMinipoll.SetLifeState(MinipollLifeState.Asleep);
                    break;
                
                case "Wake":
                    _selectedMinipoll.SetLifeState(MinipollLifeState.Awake);
                    break;
                
                case "Interact":
                    _selectedMinipoll.WorldInteraction.TryInteractWithNearbyObject();
                    break;
                
                case "Socialize":
                    _selectedMinipoll.SocialRelations.TryInteractWithNearbyMinipoll();
                    break;
            }
        }
        
        #endregion
    }
}