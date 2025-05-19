using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MinipollGame.Core;

namespace MinipollGame
{
    // מערכת למידה למיניפול
    public class MinipollLearningSystem : MonoBehaviour
    {
        [Header("Learning Settings")]
        [SerializeField] private float _learningRate = 0.1f;
        [SerializeField] private float _forgettingRate = 0.01f;
        [SerializeField] private int _maxMemories = 50;
        [SerializeField] private bool _useLearningCurve = true;
        [SerializeField] private AnimationCurve _learningCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Memory Persistence")]
        [SerializeField] private float _memoryUpdateInterval = 5f;
        [SerializeField] private bool _memorizeImportantEvents = true;

        [Header("Debug")]
        [SerializeField] private bool _logLearning = true;

        // מאגר זיכרונות וחוויות
        private Dictionary<string, ExperienceData> _memories = new Dictionary<string, ExperienceData>();

        // אירועים
        public event System.Action<string, bool, float> OnExperienceLearned;
        public event System.Action<string, float> OnExperienceRecalled;

        // זיכרונות אחרונים שנצפו
        private List<string> _recentExperiences = new List<string>();
        private float _lastMemoryUpdateTime;

        // שדות פומביים
        public float LearningRate
        {
            get => _learningRate;
            set => _learningRate = Mathf.Clamp(value, 0.01f, 1f);
        }

        public float ForgettingRate
        {
            get => _forgettingRate;
            set => _forgettingRate = Mathf.Clamp(value, 0.001f, 0.1f);
        }

        public int MaxMemories
        {
            get => _maxMemories;
            set => _maxMemories = Mathf.Max(10, value);
        }

        private void Start()
        {
            _lastMemoryUpdateTime = Time.time;
            InvokeRepeating("UpdateMemories", _memoryUpdateInterval, _memoryUpdateInterval);
        }

        // רישום חוויה חדשה
        public void RecordExperience(string experienceID, bool positiveOutcome, float intensity = 1.0f)
        {
            if (string.IsNullOrEmpty(experienceID))
                return;

            // התאמת עוצמת הלמידה
            float adjustedIntensity = intensity;
            if (_useLearningCurve)
            {
                adjustedIntensity = _learningCurve.Evaluate(intensity);
            }

            ExperienceData memory;

            // בדיקה אם החוויה קיימת
            if (_memories.TryGetValue(experienceID, out memory))
            {
                // עדכון זיכרון קיים
                memory.encounters++;

                if (positiveOutcome)
                {
                    memory.positiveOutcomeWeight += _learningRate * adjustedIntensity;
                }
                else
                {
                    memory.negativeOutcomeWeight += _learningRate * adjustedIntensity;
                }

                // עדכון משמעותיות הזיכרון
                memory.significance = CalculateSignificance(memory);

                _memories[experienceID] = memory;
            }
            else
            {
                // יצירת זיכרון חדש
                memory = new ExperienceData
                {
                    experienceID = experienceID,
                    positiveOutcomeWeight = positiveOutcome ? _learningRate * adjustedIntensity : 0,
                    negativeOutcomeWeight = positiveOutcome ? 0 : _learningRate * adjustedIntensity,
                    encounters = 1,
                    lastEncounterTime = Time.time,
                    significance = 0.1f * adjustedIntensity // משמעותיות התחלתית
                };

                _memories.Add(experienceID, memory);

                // בדיקה אם יש יותר מדי זיכרונות
                EnsureMemoryLimit();
            }

            // הוספה לרשימת חוויות אחרונות
            _recentExperiences.Add(experienceID);
            if (_recentExperiences.Count > 10)
            {
                _recentExperiences.RemoveAt(0);
            }

            if (_logLearning)
            {
                UnityEngine.Debug.Log($"Minipoll {gameObject.name} learned from experience {experienceID}: " +
                          $"Positive={memory.positiveOutcomeWeight:F2}, Negative={memory.negativeOutcomeWeight:F2}, " +
                          $"Significance={memory.significance:F2}");
            }

            // הפעלת אירוע למידה
            OnExperienceLearned?.Invoke(experienceID, positiveOutcome, adjustedIntensity);
        }

        // הערכת חוויה קודמת
        public float EvaluateExperience(string experienceID)
        {
            if (string.IsNullOrEmpty(experienceID))
                return 0;

            ExperienceData memory;

            if (_memories.TryGetValue(experienceID, out memory))
            {
                // עדכון זמן השימוש האחרון
                memory.lastRecallTime = Time.time;
                _memories[experienceID] = memory;

                // חישוב ציון הערכה בטווח -1 עד 1
                float evaluationScore = (memory.positiveOutcomeWeight - memory.negativeOutcomeWeight);

                // התאמה לפי מספר המפגשים
                float experienceFactor = Mathf.Min(1.0f, memory.encounters / 5.0f);
                evaluationScore *= experienceFactor;

                // הגבלה לטווח -1 עד 1
                evaluationScore = Mathf.Clamp(evaluationScore, -1f, 1f);

                // הפעלת אירוע שליפת זיכרון
                OnExperienceRecalled?.Invoke(experienceID, evaluationScore);

                return evaluationScore;
            }

            return 0; // אם אין זיכרון, מחזיר ערך ניטרלי
        }

        // פונקציה להאם זיכרון קיים
        public bool HasExperience(string experienceID)
        {
            return _memories.ContainsKey(experienceID);
        }

        // פונקציה למציאת זיכרון דומה
        public string FindSimilarExperience(string partialExperienceID)
        {
            if (string.IsNullOrEmpty(partialExperienceID))
                return string.Empty;

            // חיפוש זיכרונות שמכילים את המחרוזת החלקית
            var matchingExperiences = _memories.Keys
                .Where(k => k.Contains(partialExperienceID))
                .ToList();

            if (matchingExperiences.Count > 0)
            {
                // החזרת הזיכרון המשמעותי ביותר
                return matchingExperiences
                    .OrderByDescending(k => _memories[k].significance)
                    .First();
            }

            return string.Empty; // אם אין התאמה
        }

        // פונקציה למציאת זיכרון משמעותי ביותר
        public string GetMostSignificantExperience()
        {
            if (_memories.Count == 0)
                return string.Empty;

            return _memories
                .OrderByDescending(m => m.Value.significance)
                .First().Key;
        }

        // פונקציה למציאת זיכרון חיובי או שלילי משמעותי
        public string GetMostSignificantExperience(bool positive)
        {
            if (_memories.Count == 0)
                return string.Empty;

            if (positive)
            {
                var filtered = _memories
                    .Where(m => m.Value.positiveOutcomeWeight > m.Value.negativeOutcomeWeight);

                if (!filtered.Any())
                    return string.Empty;

                return filtered
                    .OrderByDescending(m => m.Value.significance)
                    .First().Key;
            }
            else
            {
                var filtered = _memories
                    .Where(m => m.Value.negativeOutcomeWeight > m.Value.positiveOutcomeWeight);

                if (!filtered.Any())
                    return string.Empty;

                return filtered
                    .OrderByDescending(m => m.Value.significance)
                    .First().Key;
            }
        }

        // עדכון תקופתי של זיכרונות
        private void UpdateMemories()
        {
            float deltaTime = Time.time - _lastMemoryUpdateTime;
            _lastMemoryUpdateTime = Time.time;

            List<string> keysToRemove = new List<string>();

            foreach (var entry in _memories)
            {
                ExperienceData memory = entry.Value;

                // דעיכת משקלי זיכרון לאורך זמן
                if (!_recentExperiences.Contains(entry.Key)) // לא דועכים זיכרונות שנחוו לאחרונה
                {
                    memory.positiveOutcomeWeight = Mathf.Max(0, memory.positiveOutcomeWeight - _forgettingRate * deltaTime);
                    memory.negativeOutcomeWeight = Mathf.Max(0, memory.negativeOutcomeWeight - _forgettingRate * deltaTime);

                    // עדכון משמעותיות
                    memory.significance = CalculateSignificance(memory);

                    // אם הזיכרון אינו משמעותי עוד, שוקל הסרה
                    if (memory.significance < 0.05f && !_memorizeImportantEvents)
                    {
                        keysToRemove.Add(entry.Key);
                    }
                    else
                    {
                        _memories[entry.Key] = memory;
                    }
                }
            }

            // הסרת זיכרונות שאינם משמעותיים
            foreach (var key in keysToRemove)
            {
                _memories.Remove(key);
            }

            // ניקוי רשימת החוויות האחרונות
            _recentExperiences.Clear();
        }

        // הבטחת מגבלת זיכרונות
        private void EnsureMemoryLimit()
        {
            if (_memories.Count <= _maxMemories)
                return;

            // מיון זיכרונות לפי משמעותיות
            var sortedMemories = _memories
                .OrderBy(m => m.Value.significance)
                .ToList();

            // הסרת הזיכרונות הכי פחות משמעותיים
            int removeCount = _memories.Count - _maxMemories;
            for (int i = 0; i < removeCount; i++)
            {
                _memories.Remove(sortedMemories[i].Key);
            }
        }

        // חישוב משמעותיות זיכרון
        private float CalculateSignificance(ExperienceData memory)
        {
            // משמעותיות מבוססת על:
            // 1. עוצמת התגובה הרגשית (חיובית או שלילית)
            // 2. מספר המפגשים
            // 3. זמן שחלף מאז המפגש האחרון

            float emotionalImpact = Mathf.Abs(memory.positiveOutcomeWeight - memory.negativeOutcomeWeight);
            float encounterFactor = Mathf.Log(Mathf.Max(1, memory.encounters)) / Mathf.Log(10); // logscale

            // חישוב גורם דעיכה לפי זמן
            float timeDecay = 1.0f;
            if (memory.lastEncounterTime > 0)
            {
                float timeSinceLastEncounter = Time.time - memory.lastEncounterTime;
                timeDecay = Mathf.Exp(-timeSinceLastEncounter / 300f); // דעיכה אקספוננציאלית לאורך זמן
            }

            // חישוב משמעותיות כוללת
            float significance = emotionalImpact * 0.6f + encounterFactor * 0.3f + timeDecay * 0.1f;

            return Mathf.Clamp01(significance);
        }

        // פונקציה לשיתוף זיכרון עם מיניפול אחר
        public void ShareExperience(MinipollLearningSystem otherLearningSystem, string experienceID, float trustFactor = 0.7f)
        {
            if (otherLearningSystem == null || string.IsNullOrEmpty(experienceID))
                return;

            ExperienceData memory;

            if (_memories.TryGetValue(experienceID, out memory))
            {
                bool isPositive = memory.positiveOutcomeWeight > memory.negativeOutcomeWeight;
                float intensity = Mathf.Abs(memory.positiveOutcomeWeight - memory.negativeOutcomeWeight) * trustFactor;

                // שיתוף עם המיניפול האחר
                otherLearningSystem.RecordExperience(experienceID, isPositive, intensity * 0.7f);
            }
        }
    }

    // מחלקת עזר לנתוני זיכרון
    [System.Serializable]
    public class ExperienceData
    {
        public string experienceID;
        public float positiveOutcomeWeight;
        public float negativeOutcomeWeight;
        public int encounters;
        public float lastEncounterTime;
        public float lastRecallTime;
        public float significance; // 0-1 משמעותיות כוללת של הזיכרון
    }
}