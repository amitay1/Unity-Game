using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MinipollGame.Core;
using MinipollGame.Utils;

namespace MinipollGame
{
    // מערכת רגשות למיניפול
    public class MinipollEmotionalState : MonoBehaviour
    {
        [Header("Emotion Settings")]
        [SerializeField] private EmotionType _dominantEmotion = EmotionType.Neutral;
        [SerializeField] private float _emotionChangeThreshold = 0.1f; // שינוי רגש דומיננטי רק כשערך חדש עובר את הקודם בערך זה
        
        [Header("Decay Settings")]
        [SerializeField] private float _baseDecayMultiplier = 1.0f;
        [SerializeField] private float _decayUpdateInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool _logEmotionChanges = true;
        
        // התפלגות רגשות נוכחית
        private Dictionary<EmotionType, EmotionData> _emotions = new Dictionary<EmotionType, EmotionData>();
        
        // אירועים
        public event System.Action<EmotionType, float> OnEmotionChanged;
        public event System.Action<EmotionType, EmotionType> OnDominantEmotionChanged;
        
        // Properties
        public EmotionType DominantEmotion => _dominantEmotion;
        
        private float _lastDecayTime;
        
        private void Awake()
        {
            // איתחול רגשות בסיסיים עם ערכים ברירת מחדל
            InitializeEmotions();
        }
        
        private void Start()
        {
            _lastDecayTime = Time.time;
            InvokeRepeating("DecayEmotions", 0f, _decayUpdateInterval);
        }
        
        private void InitializeEmotions()
        {
            // איתחול רגשות בסיסיים
            foreach (EmotionType emotionType in System.Enum.GetValues(typeof(EmotionType)))
            {
                _emotions[emotionType] = new EmotionData
                {
                    type = emotionType,
                    currentValue = (emotionType == EmotionType.Neutral) ? 0.5f : 0.0f,
                    baseValue = (emotionType == EmotionType.Neutral) ? 0.5f : 0.0f,
                    decayRate = 0.01f,
                    responseMultiplier = 1.0f
                };
            }
            
            // קביעת רגש התחלתי
            _dominantEmotion = EmotionType.Neutral;
        }
        
        // ניהול דעיכת רגשות לאורך זמן
        private void DecayEmotions()
        {
            float deltaTime = Time.time - _lastDecayTime;
            _lastDecayTime = Time.time;
            
            bool recalculateDominant = false;
            
            foreach (var emotion in _emotions.Values)
            {
                // דעיכה חזרה לערך הבסיסי
                if (emotion.type != EmotionType.Neutral)
                {
                    float originalValue = emotion.currentValue;
                    float decayAmount = emotion.decayRate * deltaTime * _baseDecayMultiplier;
                    
                    if (emotion.currentValue > emotion.baseValue)
                    {
                        emotion.currentValue = Mathf.Max(emotion.baseValue, emotion.currentValue - decayAmount);
                    }
                    else if (emotion.currentValue < emotion.baseValue)
                    {
                        emotion.currentValue = Mathf.Min(emotion.baseValue, emotion.currentValue + decayAmount);
                    }
                    
                    // אם היה שינוי משמעותי, נעדכן את הרגש הדומיננטי
                    if (Mathf.Abs(originalValue - emotion.currentValue) > 0.01f)
                    {
                        recalculateDominant = true;
                    }
                }
            }
            
            if (recalculateDominant)
            {
                RecalculateDominantEmotion();
            }
        }
        
        // שינוי עוצמת רגש
        public void ModifyEmotion(EmotionType emotionType, float delta)
        {
            if (!_emotions.ContainsKey(emotionType))
            {
                Debug.LogWarning($"Emotion type {emotionType} not found in emotions dictionary.");
                return;
            }
            
            EmotionData emotion = _emotions[emotionType];
            float oldValue = emotion.currentValue;
            
            // התאמת השינוי על פי מכפיל תגובה
            delta *= emotion.responseMultiplier;
            
            // עדכון הערך עם מגבלות
            emotion.currentValue = Mathf.Clamp01(emotion.currentValue + delta);
            
            // בדיקה אם היה שינוי משמעותי
            if (Mathf.Abs(oldValue - emotion.currentValue) >= 0.01f)
            {
                if (_logEmotionChanges)
                {
                    Debug.Log($"Minipoll {gameObject.name} emotion {emotionType} changed from {oldValue:F2} to {emotion.currentValue:F2}");
                }
                
                // הפעלת אירוע שינוי רגש
                OnEmotionChanged?.Invoke(emotionType, emotion.currentValue);
                
                // בדיקה אם יש לעדכן את הרגש הדומיננטי
                if (emotion.currentValue > GetEmotionIntensity(_dominantEmotion) + _emotionChangeThreshold ||
                    emotionType == _dominantEmotion)
                {
                    RecalculateDominantEmotion();
                }
            }
        }
        
        // חישוב מחדש של הרגש הדומיננטי
        private void RecalculateDominantEmotion()
        {
            EmotionType oldDominant = _dominantEmotion;
            
            // מציאת הרגש החזק ביותר (מלבד ניטרלי)
            var strongestEmotion = _emotions
                .Where(e => e.Key != EmotionType.Neutral)
                .OrderByDescending(e => e.Value.currentValue)
                .FirstOrDefault();
            
            // אם יש רגש חזק מספיק
            if (strongestEmotion.Value != null && strongestEmotion.Value.currentValue > 0.2f)
            {
                _dominantEmotion = strongestEmotion.Key;
            }
            else
            {
                _dominantEmotion = EmotionType.Neutral;
            }
            
            // אם הרגש הדומיננטי השתנה
            if (oldDominant != _dominantEmotion)
            {
                if (_logEmotionChanges)
                {
                    Debug.Log($"Minipoll {gameObject.name} dominant emotion changed from {oldDominant} to {_dominantEmotion}");
                }
                
                // הפעלת אירוע שינוי רגש דומיננטי
                OnDominantEmotionChanged?.Invoke(oldDominant, _dominantEmotion);
            }
        }
        
        // קבלת עוצמת רגש
        public float GetEmotionIntensity(EmotionType emotionType)
        {
            if (_emotions.ContainsKey(emotionType))
            {
                return _emotions[emotionType].currentValue;
            }
            return 0f;
        }
        
        // הגדרת נטייה רגשית (לשימוש בהתאמת סוגי מיניפול שונים)
        public void SetEmotionalTendency(EmotionType emotionType, float baseValue, float responseMultiplier, float decayRate)
        {
            if (!_emotions.ContainsKey(emotionType))
            {
                _emotions[emotionType] = new EmotionData
                {
                    type = emotionType,
                    currentValue = baseValue,
                    baseValue = baseValue,
                    decayRate = decayRate,
                    responseMultiplier = responseMultiplier
                };
            }
            else
            {
                var emotion = _emotions[emotionType];
                emotion.baseValue = baseValue;
                emotion.currentValue = Mathf.Lerp(emotion.currentValue, baseValue, 0.5f); // החלפה הדרגתית
                emotion.decayRate = decayRate;
                emotion.responseMultiplier = responseMultiplier;
            }
            
            // חישוב מחדש של הרגש הדומיננטי
            RecalculateDominantEmotion();
        }
        
        // שילוב של שני רגשות ליצירת רגש שלישי
        public void BlendEmotions(EmotionType firstEmotion, EmotionType secondEmotion, float blendRatio = 0.5f)
        {
            float firstIntensity = GetEmotionIntensity(firstEmotion);
            float secondIntensity = GetEmotionIntensity(secondEmotion);
            
            // רק אם שני הרגשות מספיק חזקים
            if (firstIntensity > 0.3f && secondIntensity > 0.3f)
            {
                // קביעת הרגש המשולב
                EmotionType blendedEmotion = MinipollUtils.BlendEmotions(firstEmotion, secondEmotion, blendRatio);
                
                // חישוב העוצמה המשולבת
                float blendedIntensity = (firstIntensity * blendRatio) + (secondIntensity * (1 - blendRatio));
                
                // עדכון הרגש המשולב
                if (blendedEmotion != firstEmotion && blendedEmotion != secondEmotion)
                {
                    ModifyEmotion(blendedEmotion, blendedIntensity * 0.5f);
                    
                    // הפחתה קלה של הרגשות המקוריים
                    ModifyEmotion(firstEmotion, -0.1f);
                    ModifyEmotion(secondEmotion, -0.1f);
                }
            }
        }
        
        // קבלת העוצמה הכוללת של כל הרגשות
        public float GetTotalEmotionalIntensity()
        {
            float total = 0f;
            foreach (var emotion in _emotions.Values)
            {
                if (emotion.type != EmotionType.Neutral)
                {
                    total += emotion.currentValue;
                }
            }
            return total;
        }
    }
    
    // מחלקת עזר למידע על רגש
    [System.Serializable]
    public class EmotionData
    {
        public EmotionType type;
        public float currentValue; // 0-1 range
        public float baseValue; // default value when no stimulus
        public float decayRate; // how fast it returns to base value
        public float responseMultiplier; // how strongly it responds to stimuli
    }
}