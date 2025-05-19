using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Core;
using MinipollGame.ScriptableObjects;

namespace MinipollGame
{
    // מרכז השליטה של המיניפול - המוח שלו
    [RequireComponent(typeof(MinipollEmotionalState))]
    [RequireComponent(typeof(MinipollLearningSystem))]
    [RequireComponent(typeof(MinipollMovementController))]
    [RequireComponent(typeof(MinipollSocialRelations))]
    [RequireComponent(typeof(MinipollWorldInteraction))]
    [RequireComponent(typeof(MinipollVisualController))]
    [RequireComponent(typeof(MinipollBlinkController))]
    public class MinipollBrain : MonoBehaviour
    {
        [Header("Minipoll Data")]
        [SerializeField] private MinipollData minipollData;
        
        [Header("Behavior Settings")]
        [SerializeField] private float decisionInterval = 2f;
        [SerializeField] private float autonomyLevel = 1f; // 0 = fully controlled, 1 = fully autonomous
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool logDecisions = true;
        
        // מערכות מקושרות
        private MinipollEmotionalState _emotionalState;
        private MinipollLearningSystem _learningSystem;
        private MinipollMovementController _movementController;
        private MinipollSocialRelations _socialRelations;
        private MinipollWorldInteraction _worldInteraction;
        private MinipollVisualController _visualController;
        private MinipollBlinkController _blinkController;
        
        // מצב נוכחי
        private MinipollLifeState _currentLifeState = MinipollLifeState.Awake;
        private DecisionType _lastDecisionType;
        private float _lastDecisionTime;
        private bool _isInitialized = false;
        
        // תור לפעולות
        private Queue<System.Action> _actionQueue = new Queue<System.Action>();
        
        // Properties פומביים
        public MinipollEmotionalState EmotionalState => _emotionalState;
        public MinipollLearningSystem LearningSystem => _learningSystem;
        public MinipollMovementController MovementController => _movementController;
        public MinipollSocialRelations SocialRelations => _socialRelations;
        public MinipollWorldInteraction WorldInteraction => _worldInteraction;
        public MinipollVisualController VisualController => _visualController;
        public MinipollBlinkController BlinkController => _blinkController;
        
        public MinipollLifeState CurrentLifeState => _currentLifeState;
        public DecisionType LastDecisionType => _lastDecisionType;
        public MinipollData Data => minipollData;
        
        private void Awake()
        {
            // איתור כל המערכות הדרושות
            _emotionalState = GetComponent<MinipollEmotionalState>();
            _learningSystem = GetComponent<MinipollLearningSystem>();
            _movementController = GetComponent<MinipollMovementController>();
            _socialRelations = GetComponent<MinipollSocialRelations>();
            _worldInteraction = GetComponent<MinipollWorldInteraction>();
            _visualController = GetComponent<MinipollVisualController>();
            _blinkController = GetComponent<MinipollBlinkController>();
        }
        
        private void Start()
        {
            // החלת הנתונים מהסקריפטאבל אובייקט
            if (minipollData != null)
            {
                minipollData.ApplyToMinipoll(gameObject);
            }
            
            // רישום לאירועים
            _emotionalState.OnEmotionChanged += HandleEmotionChanged;
            _worldInteraction.OnInteractionCompleted += HandleInteractionCompleted;
            
            // התחלת מערכת קבלת החלטות
            StartCoroutine(DecisionLoop());
            
            _isInitialized = true;
            
            // הודעה על אתחול
            if (logDecisions)
            {
                Debug.Log($"Minipoll {gameObject.name} initialized successfully.");
            }
            
            // התחלת אנימציית מצמוץ
            _blinkController.StartBlinking();
        }
        
        private void OnDestroy()
        {
            // ביטול הרשמה לאירועים
            if (_emotionalState != null)
            {
                _emotionalState.OnEmotionChanged -= HandleEmotionChanged;
            }
            
            if (_worldInteraction != null)
            {
                _worldInteraction.OnInteractionCompleted -= HandleInteractionCompleted;
            }
        }
        
        private void Update()
        {
            // עיבוד תור הפעולות
            ProcessActionQueue();
        }
        
        // לולאת קבלת החלטות - מופעלת בפרקי זמן קבועים
        private IEnumerator DecisionLoop()
        {
            while (true)
            {
                // רק אם המיניפול במצב ער
                if (_currentLifeState == MinipollLifeState.Awake)
                {
                    MakeDecision();
                }
                
                // המתנה לפרק הזמן הבא
                yield return new WaitForSeconds(decisionInterval);
            }
        }
        
        // קבלת החלטה אוטונומית
        private void MakeDecision()
        {
            // אם רמת האוטונומיה נמוכה, יש סיכוי שלא יקבל החלטה
            if (Random.value > autonomyLevel)
            {
                return;
            }
            
            // רשימת אפשרויות החלטה
            List<DecisionOption> options = new List<DecisionOption>();
            
            // אם המיניפול עייף, הוא מעדיף לנוח
            if (_emotionalState.GetEmotionIntensity(EmotionType.Tired) > 0.7f)
            {
                options.Add(new DecisionOption { type = DecisionType.Rest, weight = 3f });
            }
            else
            {
                options.Add(new DecisionOption { type = DecisionType.Rest, weight = 0.5f });
            }
            
            // אם המיניפול סקרן, הוא מעדיף לחקור
            float curiosityLevel = _emotionalState.GetEmotionIntensity(EmotionType.Curious);
            options.Add(new DecisionOption { type = DecisionType.Explore, weight = 0.5f + curiosityLevel });
            
            // בדיקת אובייקטים קרובים לאינטראקציה
            if (_worldInteraction.HasNearbyInteractables())
            {
                float interactWeight = 1f;
                
                // אם המיניפול מפחד, הוא חושש מאינטראקציות
                if (_emotionalState.DominantEmotion == EmotionType.Scared)
                {
                    interactWeight *= 0.3f;
                }
                
                options.Add(new DecisionOption { type = DecisionType.Interaction, weight = interactWeight });
            }
            
            // אם יש מיניפולים קרובים ואנחנו חברותיים
            if (_socialRelations.HasNearbyMinipolls())
            {
                float socialWeight = minipollData.socializingTendency;
                
                // אם המיניפול שמח, הוא מעדיף לחברת
                if (_emotionalState.DominantEmotion == EmotionType.Happy)
                {
                    socialWeight *= 1.5f;
                }
                
                options.Add(new DecisionOption { type = DecisionType.SocialBehavior, weight = socialWeight });
            }
            
            // הוספת אפשרות תנועה אקראית
            options.Add(new DecisionOption { type = DecisionType.Movement, weight = 1f });
            
            // בחירת ההחלטה
            DecisionType decision = ChooseDecision(options);
            _lastDecisionType = decision;
            _lastDecisionTime = Time.time;
            
            // ביצוע ההחלטה
            ExecuteDecision(decision);
            
            // רישום החלטה
            if (logDecisions)
            {
                Debug.Log($"Minipoll {gameObject.name} decided to {decision}");
            }
        }
        
        // בחירת החלטה על פי משקלים
        private DecisionType ChooseDecision(List<DecisionOption> options)
        {
            float totalWeight = 0f;
            foreach (var option in options)
            {
                totalWeight += option.weight;
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            foreach (var option in options)
            {
                currentWeight += option.weight;
                if (randomValue <= currentWeight)
                {
                    return option.type;
                }
            }
            
            // ברירת מחדל אם משהו השתבש
            return DecisionType.Movement;
        }
        
        // ביצוע ההחלטה
        private void ExecuteDecision(DecisionType decision)
        {
            switch (decision)
            {
                case DecisionType.Movement:
                    _actionQueue.Enqueue(() => _movementController.WanderRandomly());
                    break;
                
                case DecisionType.Interaction:
                    _actionQueue.Enqueue(() => _worldInteraction.TryInteractWithNearbyObject());
                    break;
                
                case DecisionType.SocialBehavior:
                    _actionQueue.Enqueue(() => _socialRelations.TryInteractWithNearbyMinipoll());
                    break;
                
                case DecisionType.Rest:
                    _actionQueue.Enqueue(() => Rest());
                    break;
                
                case DecisionType.Explore:
                    _actionQueue.Enqueue(() => _movementController.ExploreNewArea());
                    break;
            }
        }
        
        // עיבוד תור פעולות
        private void ProcessActionQueue()
        {
            if (_actionQueue.Count > 0)
            {
                var action = _actionQueue.Dequeue();
                action.Invoke();
            }
        }
        
        // הידלות רגשות
        private void HandleEmotionChanged(EmotionType emotion, float intensity)
        {
            // עדכון אנימציית המיניפול בהתאם לרגש
            _visualController.UpdateEmotionVisuals(emotion, intensity);
            
            // הפעלת אירוע גלובלי
            EventSystem.TriggerEmotionChange(this, emotion, intensity);
        }
        
        // טיפול בהשלמת אינטראקציה
        private void HandleInteractionCompleted(IInteractable target, InteractionResult result)
        {
            // עדכון רגשות
            _emotionalState.ModifyEmotion(result.PrimaryEmotion, result.EmotionalImpact * (result.Success ? 1 : -1));
            
            // עדכון מערכת הלמידה
            _learningSystem.RecordExperience(result.ExperienceID, result.Success, result.EmotionalImpact);
            
            // הפעלת אירוע גלובלי
            EventSystem.TriggerInteraction(this, target, result);
        }
        
        // מנוחה
        private void Rest()
        {
            // עצירת תנועה
            _movementController.StopMoving();
            
            // הפחתת רמת עייפות
            _emotionalState.ModifyEmotion(EmotionType.Tired, -0.3f);
            
            // אנימציית מנוחה
            _visualController.PlayAnimation("Rest");
            
            if (logDecisions)
            {
                Debug.Log($"Minipoll {gameObject.name} is resting");
            }
        }
        
        // פונקציות חיצוניות להפעלה ידנית
        
        // שליטה ישירה במיניפול
        public void SetDirectMovementTarget(Vector3 target)
        {
            _movementController.SetDestination(target);
        }
        
        // הכרחת אינטראקציה עם אובייקט
        public void ForceInteraction(IInteractable target)
        {
            _worldInteraction.InteractWith(target);
        }
        
        // שינוי מצב חיים (ער/ישן/חורף)
        public void SetLifeState(MinipollLifeState newState)
        {
            if (_currentLifeState != newState)
            {
                _currentLifeState = newState;
                
                switch (newState)
                {
                    case MinipollLifeState.Asleep:
                        _movementController.StopMoving();
                        _blinkController.StopBlinking();
                        _visualController.PlayAnimation("Sleep");
                        break;
                    
                    case MinipollLifeState.Awake:
                        _blinkController.StartBlinking();
                        _visualController.PlayAnimation("WakeUp");
                        break;
                    
                    case MinipollLifeState.Hibernating:
                        _movementController.StopMoving();
                        _blinkController.StopBlinking();
                        _visualController.PlayAnimation("Hibernate");
                        break;
                }
                
                // הפעלת אירוע גלובלי
                EventSystem.TriggerStateChange(this, newState);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebugInfo || !_isInitialized)
                return;
            
            // הצגת מצב רגשי נוכחי
            if (_emotionalState != null)
            {
                EmotionType dominantEmotion = _emotionalState.DominantEmotion;
                float intensity = _emotionalState.GetEmotionIntensity(dominantEmotion);
                
                // צבע לפי רגש
                Color emotionColor = Color.gray;
                switch (dominantEmotion)
                {
                    case EmotionType.Happy: emotionColor = Color.green; break;
                    case EmotionType.Sad: emotionColor = Color.blue; break;
                    case EmotionType.Angry: emotionColor = Color.red; break;
                    case EmotionType.Scared: emotionColor = Color.yellow; break;
                    case EmotionType.Curious: emotionColor = Color.cyan; break;
                }
                
                Gizmos.color = emotionColor;
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.3f * intensity);
            }
            
            // הצגת יעד תנועה נוכחי
            if (_movementController != null && _movementController.HasDestination)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _movementController.CurrentDestination);
                Gizmos.DrawWireSphere(_movementController.CurrentDestination, 0.2f);
            }
        }
    }
    
    // מחלקת עזר להחלטות
    [System.Serializable]
    public class DecisionOption
    {
        public DecisionType type;
        public float weight;
    }
}