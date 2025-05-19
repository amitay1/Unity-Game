using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MinipollGame.Core;

namespace MinipollGame
{
    // מערכת צרכים למיניפול - מנהלת צרכים בסיסיים כמו רעב, אנרגיה וכו'
    public class MinipollNeedsSystem : MonoBehaviour
    {
        [Header("Need Settings")]
        [SerializeField] private List<NeedDefinition> _needDefinitions = new List<NeedDefinition>();
        [SerializeField] private float _needUpdateInterval = 2f;
        
        [Header("Status Effects")]
        [SerializeField] private float _lowNeedThreshold = 0.3f;
        [SerializeField] private float _criticalNeedThreshold = 0.1f;
        [SerializeField] private float _highNeedThreshold = 0.8f;
        [SerializeField] private float _emotionalImpactStrength = 1f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private bool _logNeedChanges = true;
        
        // התייחסויות למערכות
        private MinipollEmotionalState _emotionalState;
        private MinipollBrain _brain;
        
        // מצב נוכחי של הצרכים
        private Dictionary<NeedType, float> _currentNeeds = new Dictionary<NeedType, float>();
        private Dictionary<NeedType, float> _needDecayRates = new Dictionary<NeedType, float>();
        
        // זמן עדכון אחרון
        private float _lastUpdateTime;
        
        // אירועים
        public event System.Action<NeedType, float> OnNeedChanged;
        public event System.Action<NeedType> OnNeedCritical;
        public event System.Action<NeedType> OnNeedSatisfied;
        
        private void Awake()
        {
            _emotionalState = GetComponent<MinipollEmotionalState>();
            _brain = GetComponent<MinipollBrain>();
            
            // איתחול צרכים
            InitializeNeeds();
        }
        
        private void Start()
        {
            _lastUpdateTime = Time.time;
            
            // רישום לאירועים
            EventSystem.OnTimeOfDayChanged += HandleTimeOfDayChanged;
        }
        
        private void OnDestroy()
        {
            EventSystem.OnTimeOfDayChanged -= HandleTimeOfDayChanged;
        }
        
        private void Update()
        {
            // עדכון צרכים לפי אינטרוול זמן
            if (Time.time - _lastUpdateTime >= _needUpdateInterval)
            {
                UpdateNeeds();
                _lastUpdateTime = Time.time;
            }
        }
        
        // איתחול צרכים
        private void InitializeNeeds()
        {
            // איתחול מהגדרות
            foreach (var definition in _needDefinitions)
            {
                // איתחול מצב נוכחי
                _currentNeeds[definition.type] = definition.initialValue;
                
                // איתחול קצב דעיכה
                _needDecayRates[definition.type] = definition.decayRate;
            }
            
            // הוספת צרכים בסיסיים אם לא הוגדרו
            if (!_currentNeeds.ContainsKey(NeedType.Energy))
            {
                _currentNeeds[NeedType.Energy] = 1.0f;
                _needDecayRates[NeedType.Energy] = 0.01f;
            }
            
            if (!_currentNeeds.ContainsKey(NeedType.Hunger))
            {
                _currentNeeds[NeedType.Hunger] = 1.0f;
                _needDecayRates[NeedType.Hunger] = 0.005f;
            }
            
            if (!_currentNeeds.ContainsKey(NeedType.Social))
            {
                _currentNeeds[NeedType.Social] = 1.0f;
                _needDecayRates[NeedType.Social] = 0.003f;
            }
        }
        
        // עדכון צרכים
        private void UpdateNeeds()
        {
            // עדכון רק אם המיניפול במצב ער
            if (_brain != null && _brain.CurrentLifeState != MinipollLifeState.Awake)
            {
                return;
            }
            
            foreach (var needType in _currentNeeds.Keys.ToList())
            {
                // קבלת קצב דעיכה
                float decayRate = _needDecayRates.ContainsKey(needType) ? _needDecayRates[needType] : 0.01f;
                
                // שמירת הערך הקודם
                float previousValue = _currentNeeds[needType];
                
                // עדכון ערך חדש (דעיכה)
                float newValue = Mathf.Max(0f, previousValue - (decayRate * _needUpdateInterval));
                _currentNeeds[needType] = newValue;
                
                // בדיקה אם הערך השתנה משמעותית
                if (Mathf.Abs(previousValue - newValue) > 0.01f)
                {
                    // הפעלת אירוע
                    OnNeedChanged?.Invoke(needType, newValue);
                    
                    // עדכון לוג
                    if (_logNeedChanges)
                    {
                        Debug.Log($"Minipoll {gameObject.name} {needType}: {previousValue:F2} -> {newValue:F2}");
                    }
                    
                    // בדיקת מצבים קריטיים
                    if (newValue <= _criticalNeedThreshold && previousValue > _criticalNeedThreshold)
                    {
                        // הפעלת אירוע מצב קריטי
                        OnNeedCritical?.Invoke(needType);
                        
                        // השפעה רגשית
                        ApplyEmotionalEffects(needType, true);
                    }
                    else if (newValue <= _lowNeedThreshold && previousValue > _lowNeedThreshold)
                    {
                        // השפעה רגשית בעוצמה נמוכה יותר
                        ApplyEmotionalEffects(needType, false);
                    }
                }
            }
        }
        
        // השפעות רגשיות של צרכים
        private void ApplyEmotionalEffects(NeedType needType, bool isCritical)
        {
            if (_emotionalState == null)
                return;
            
            float intensity = isCritical ? 0.4f * _emotionalImpactStrength : 0.2f * _emotionalImpactStrength;
            
            switch (needType)
            {
                case NeedType.Energy:
                    _emotionalState.ModifyEmotion(EmotionType.Tired, intensity);
                    break;
                
                case NeedType.Hunger:
                    _emotionalState.ModifyEmotion(EmotionType.Sad, intensity * 0.5f);
                    // שילוב עם רעב יכול גם להשפיע קצת על עייפות
                    _emotionalState.ModifyEmotion(EmotionType.Tired, intensity * 0.3f);
                    break;
                
                case NeedType.Social:
                    _emotionalState.ModifyEmotion(EmotionType.Sad, intensity);
                    break;
                
                case NeedType.Fun:
                    _emotionalState.ModifyEmotion(EmotionType.Sad, intensity * 0.4f);
                    _emotionalState.ModifyEmotion(EmotionType.Curious, -intensity * 0.3f);
                    break;
                
                case NeedType.Hygiene:
                    _emotionalState.ModifyEmotion(EmotionType.Sad, intensity * 0.2f);
                    break;
            }
        }
        
        // מילוי צורך
        public void FillNeed(NeedType needType, float amount)
        {
            if (!_currentNeeds.ContainsKey(needType))
            {
                // הוספת צורך חדש אם לא קיים
                _currentNeeds[needType] = 0f;
                _needDecayRates[needType] = 0.01f; // קצב דעיכה ברירת מחדל
            }
            
            // שמירת הערך הקודם
            float previousValue = _currentNeeds[needType];
            
            // עדכון הערך החדש
            float newValue = Mathf.Min(1f, previousValue + amount);
            _currentNeeds[needType] = newValue;
            
            // הפעלת אירוע
            OnNeedChanged?.Invoke(needType, newValue);
            
            // בדיקה אם הצורך סופק במלואו
            if (newValue >= _highNeedThreshold && previousValue < _highNeedThreshold)
            {
                OnNeedSatisfied?.Invoke(needType);
                
                // השפעה רגשית חיובית
                ApplyPositiveEmotionalEffect(needType);
            }
            
            // עדכון לוג
            if (_logNeedChanges)
            {
                Debug.Log($"Minipoll {gameObject.name} {needType} filled: {previousValue:F2} -> {newValue:F2}");
            }
        }
        
        // השפעה רגשית חיובית ממילוי צורך
        private void ApplyPositiveEmotionalEffect(NeedType needType)
        {
            if (_emotionalState == null)
                return;
            
            float intensity = 0.3f * _emotionalImpactStrength;
            
            switch (needType)
            {
                case NeedType.Energy:
                    _emotionalState.ModifyEmotion(EmotionType.Tired, -intensity);
                    _emotionalState.ModifyEmotion(EmotionType.Happy, intensity * 0.5f);
                    break;
                
                case NeedType.Hunger:
                    _emotionalState.ModifyEmotion(EmotionType.Happy, intensity);
                    break;
                
                case NeedType.Social:
                    _emotionalState.ModifyEmotion(EmotionType.Happy, intensity);
                    _emotionalState.ModifyEmotion(EmotionType.Curious, intensity * 0.3f);
                    break;
                
                case NeedType.Fun:
                    _emotionalState.ModifyEmotion(EmotionType.Happy, intensity * 1.2f);
                    _emotionalState.ModifyEmotion(EmotionType.Curious, intensity * 0.5f);
                    break;
                
                case NeedType.Hygiene:
                    _emotionalState.ModifyEmotion(EmotionType.Happy, intensity * 0.5f);
                    break;
            }
        }
        
        // טיפול בשינוי זמן
        private void HandleTimeOfDayChanged(TimeOfDay timeOfDay)
        {
            // צרכים שונים משתנים בהתאם לזמן היום
            switch (timeOfDay.CurrentPhase)
            {
                case TimeOfDay.DayPhase.Dawn:
                    // בוקר - אנרגיה חדשה
                    if (_brain.CurrentLifeState == MinipollLifeState.Asleep)
                    {
                        FillNeed(NeedType.Energy, 0.4f);
                    }
                    break;
                
                case TimeOfDay.DayPhase.Evening:
                    // ערב - עייפות גוברת
                    _needDecayRates[NeedType.Energy] = GetBaseDecayRate(NeedType.Energy) * 1.5f;
                    break;
                
                case TimeOfDay.DayPhase.Night:
                    // לילה - רעב גובר
                    _needDecayRates[NeedType.Hunger] = GetBaseDecayRate(NeedType.Hunger) * 1.2f;
                    _needDecayRates[NeedType.Energy] = GetBaseDecayRate(NeedType.Energy) * 2f;
                    break;
                
                default:
                    // החזרת קצבי דעיכה נורמליים
                    ResetDecayRates();
                    break;
            }
        }
        
        // החזרת קצב דעיכה בסיסי
        private float GetBaseDecayRate(NeedType needType)
        {
            foreach (var definition in _needDefinitions)
            {
                if (definition.type == needType)
                {
                    return definition.decayRate;
                }
            }
            
            // ערכי ברירת מחדל
            switch (needType)
            {
                case NeedType.Energy:
                    return 0.01f;
                case NeedType.Hunger:
                    return 0.005f;
                case NeedType.Social:
                    return 0.003f;
                case NeedType.Fun:
                    return 0.002f;
                case NeedType.Hygiene:
                    return 0.001f;
                default:
                    return 0.005f;
            }
        }
        
        // איפוס קצבי דעיכה
        private void ResetDecayRates()
        {
            foreach (var definition in _needDefinitions)
            {
                _needDecayRates[definition.type] = definition.decayRate;
            }
        }
        
        // קבלת ערך צורך
        public float GetNeedValue(NeedType needType)
        {
            if (_currentNeeds.ContainsKey(needType))
            {
                return _currentNeeds[needType];
            }
            
            return 1f; // ברירת מחדל - 100%
        }
        
        // בדיקה אם צורך במצב קריטי
        public bool IsNeedCritical(NeedType needType)
        {
            return GetNeedValue(needType) <= _criticalNeedThreshold;
        }
        
        // בדיקה אם צורך נמוך
        public bool IsNeedLow(NeedType needType)
        {
            return GetNeedValue(needType) <= _lowNeedThreshold;
        }
        
        // קבלת הצורך הנמוך ביותר
        public NeedType GetLowestNeed()
        {
            NeedType lowestNeed = NeedType.Energy;
            float lowestValue = 1f;
            
            foreach (var pair in _currentNeeds)
            {
                if (pair.Value < lowestValue)
                {
                    lowestValue = pair.Value;
                    lowestNeed = pair.Key;
                }
            }
            
            return lowestNeed;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showDebugInfo || !Application.isPlaying)
                return;
            
            // הצגת מצב צרכים מעל המיניפול
            float yOffset = 1.5f;
            float barWidth = 0.5f;
            float barHeight = 0.05f;
            float spacing = 0.1f;
            
            foreach (var pair in _currentNeeds)
            {
                Color barColor;
                
                switch (pair.Key)
                {
                    case NeedType.Energy:
                        barColor = Color.yellow;
                        break;
                    case NeedType.Hunger:
                        barColor = Color.green;
                        break;
                    case NeedType.Social:
                        barColor = Color.cyan;
                        break;
                    case NeedType.Fun:
                        barColor = Color.magenta;
                        break;
                    case NeedType.Hygiene:
                        barColor = Color.blue;
                        break;
                    default:
                        barColor = Color.white;
                        break;
                }
                
                Vector3 barCenter = transform.position + Vector3.up * yOffset;
                Vector3 barSize = new Vector3(barWidth * pair.Value, barHeight, 0.01f);
                
                Gizmos.color = barColor;
                Gizmos.DrawCube(barCenter, barSize);
                
                // גבול מסביב
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(transform.position + Vector3.up * yOffset, new Vector3(barWidth, barHeight, 0.01f));
                
                yOffset += spacing + barHeight;
            }
        }
    }
    
    // סוגי צרכים
    public enum NeedType
    {
        Energy,
        Hunger,
        Social,
        Fun,
        Hygiene
    }
    
    // הגדרת צורך
    [System.Serializable]
    public class NeedDefinition
    {
        public NeedType type;
        [Range(0f, 1f)]
        public float initialValue = 1f;
        [Range(0.001f, 0.1f)]
        public float decayRate = 0.01f;
        [Tooltip("אילו רגשות מושפעים כאשר הצורך נמוך")]
        public EmotionType linkedEmotion = EmotionType.Sad;
    }
}