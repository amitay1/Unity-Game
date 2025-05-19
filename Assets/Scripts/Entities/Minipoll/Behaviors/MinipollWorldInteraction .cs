// תוכן שצריך להיכנס לקובץ MinipollWorldInteraction.cs הקיים
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MinipollGame.Core;
using MinipollGame.ScriptableObjects;

namespace MinipollGame
{
    // מערכת אינטראקציה עם אובייקטים בעולם
    public class MinipollWorldInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float _interactionRange = 2f;
        [SerializeField] private LayerMask _interactableLayer;
        [SerializeField] private float _interactionCooldown = 1.5f;
        [SerializeField] private int _maxInteractionAttempts = 3;
        
        [Header("Interest Settings")]
        [SerializeField] private float _interestMultiplier = 1.2f;
        [SerializeField] private float _maxInterestDistance = 8f;
        
        [Header("Debug")]
        [SerializeField] private bool _logInteractions = true;
        
        // התייחסויות למערכות
        private MinipollEmotionalState _emotionalState;
        private MinipollLearningSystem _learningSystem;
        private MinipollMovementController _movementController;
        
        // נתוני אינטראקציה
        private float _lastInteractionTime;
        private int _consecutiveFailedInteractions = 0;
        private IInteractable _currentTarget;
        
        // רשימת אובייקטים מעניינים בסביבה
        private List<InterestPoint> _interestPoints = new List<InterestPoint>();
        
        // אירועים
        public event System.Action<IInteractable> OnInteractionStarted;
        public event System.Action<IInteractable, InteractionResult> OnInteractionCompleted;
        
        private void Awake()
        {
            _emotionalState = GetComponent<MinipollEmotionalState>();
            _learningSystem = GetComponent<MinipollLearningSystem>();
            _movementController = GetComponent<MinipollMovementController>();
        }
        
        private void Start()
        {
            _lastInteractionTime = Time.time;
            
            // רישום לאירועי תנועה
            if (_movementController != null)
            {
                _movementController.OnDestinationReached += HandleDestinationReached;
            }
        }
        
        private void OnDestroy()
        {
            if (_movementController != null)
            {
                _movementController.OnDestinationReached -= HandleDestinationReached;
            }
        }
        
        private void Update()
        {
            // עדכון נקודות עניין
            UpdateInterestPoints();
        }
        
        // בדיקה אם יש אובייקטים להתעסקות בקרבת מקום
        public bool HasNearbyInteractables()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _interactionRange, _interactableLayer);
            
            foreach (var collider in colliders)
            {
                IInteractable interactable = collider.GetComponent<IInteractable>();
                
                if (interactable != null && interactable.CanInteract(GetComponent<MinipollBrain>()))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // ניסיון אינטראקציה עם אובייקט קרוב
        public bool TryInteractWithNearbyObject()
        {
            // בדיקת קולדאון
            if (Time.time - _lastInteractionTime < _interactionCooldown)
            {
                return false;
            }
            
            // מציאת אובייקטים בטווח
            Collider[] colliders = Physics.OverlapSphere(transform.position, _interactionRange, _interactableLayer);
            
            List<IInteractable> nearbyInteractables = new List<IInteractable>();
            
            foreach (var collider in colliders)
            {
                IInteractable interactable = collider.GetComponent<IInteractable>();
                
                if (interactable != null && interactable.CanInteract(GetComponent<MinipollBrain>()))
                {
                    nearbyInteractables.Add(interactable);
                }
            }
            
            // אם אין אובייקטים להתעסקות בטווח
            if (nearbyInteractables.Count == 0)
            {
                // אם יש נקודות עניין, ננסה ללכת לשם
                if (_interestPoints.Count > 0 && _consecutiveFailedInteractions < _maxInteractionAttempts)
                {
                    MoveToMostInterestingPoint();
                    _consecutiveFailedInteractions++;
                    return true; // מחזירים הצלחה עבור עצם הניסיון
                }
                
                return false;
            }
            
            // מיון אובייקטים לפי עניין וניסיון קודם
            nearbyInteractables = nearbyInteractables
                .OrderByDescending(i => CalculateInterestLevel(i))
                .ToList();
            
            // אינטראקציה עם האובייקט המעניין ביותר
            IInteractable targetInteractable = nearbyInteractables[0];
            
            return InteractWith(targetInteractable);
        }
        
        // אינטראקציה ישירה עם אובייקט ספציפי
        public bool InteractWith(IInteractable target)
        {
            if (target == null)
                return false;
            
            _currentTarget = target;
            
            // בדיקה אם המיניפול קרוב מספיק
            float distance = Vector3.Distance(transform.position, target.transform.position);
            
            if (distance > _interactionRange)
            {
                // תנועה לעבר האובייקט אם רחוק מדי
                if (_movementController != null)
                {
                    _movementController.SetDestination(target.transform.position);
                }
                return true; // הצלחנו להתחיל תהליך (תנועה לקראת אינטראקציה)
            }
            
            // הפעלת אירוע תחילת אינטראקציה
            OnInteractionStarted?.Invoke(target);
            
            // ביצוע האינטראקציה
            InteractionResult result = target.Interact(GetComponent<MinipollBrain>());
            
            // עדכון זמן אינטראקציה אחרון
            _lastInteractionTime = Time.time;
            
            // אם ההתעסקות הצליחה, מאפסים את מונה הכישלונות
            if (result.Success)
            {
                _consecutiveFailedInteractions = 0;
            }
            else
            {
                _consecutiveFailedInteractions++;
            }
            
            // הפעלת אירוע סיום אינטראקציה
            OnInteractionCompleted?.Invoke(target, result);
            
            // רישום לוג
            if (_logInteractions)
            {
                Debug.Log($"Minipoll {gameObject.name} interaction with {target.InteractionID}: {(result.Success ? "Success" : "Failure")}");
            }
            
            return result.Success;
        }
        
        // חישוב רמת העניין באובייקט
        private float CalculateInterestLevel(IInteractable interactable)
        {
            float interestLevel = 1.0f; // ערך בסיסי
            
            // התאמה לפי ניסיון קודם
            if (_learningSystem != null)
            {
                float experienceValue = _learningSystem.EvaluateExperience(interactable.InteractionID);
                
                // חוויות חיוביות מעניינות יותר (עד נקודה מסוימת)
                if (experienceValue > 0)
                {
                    // אם זו חוויה מאוד חיובית אבל חזרה על עצמה הרבה פעמים, היא פחות מעניינת
                    if (_learningSystem.HasExperience(interactable.InteractionID) && experienceValue > 0.8f)
                    {
                        interestLevel += experienceValue * 0.5f;
                    }
                    else
                    {
                        // Using interest multiplier here
                        interestLevel += experienceValue * _interestMultiplier;
                    }
                }
                // חוויות שליליות מעניינות פחות, אבל עדיין יש קצת עניין
                else if (experienceValue < 0)
                {
                    // אם זו חוויה מאוד שלילית, היא מעניינת פחות
                    if (experienceValue < -0.5f)
                    {
                        interestLevel -= Mathf.Abs(experienceValue) * 0.8f;
                    }
                    else
                    {
                        interestLevel -= Mathf.Abs(experienceValue) * 0.3f;
                    }
                }
            }
            
            // התאמה לפי מצב רגשי
            if (_emotionalState != null)
            {
                EmotionType dominantEmotion = _emotionalState.DominantEmotion;
                float intensity = _emotionalState.GetEmotionIntensity(dominantEmotion);
                
                switch (dominantEmotion)
                {
                    case EmotionType.Curious:
                        // סקרנות מגבירה עניין
                        interestLevel *= 1 + (intensity * 0.5f);
                        break;
                    
                    case EmotionType.Scared:
                        // פחד מפחית עניין
                        interestLevel *= 1 - (intensity * 0.7f);
                        break;
                    
                    case EmotionType.Tired:
                        // עייפות מפחיתה עניין
                        interestLevel *= 1 - (intensity * 0.5f);
                        break;
                }
            }
            
            // הגבלת טווח
            return Mathf.Clamp(interestLevel, 0.1f, 2.0f);
        }
        
        // עדכון נקודות עניין בסביבה
        private void UpdateInterestPoints()
        {
            // עדכון קיים
            for (int i = _interestPoints.Count - 1; i >= 0; i--)
            {
                InterestPoint point = _interestPoints[i];
                
                // הסרת נקודות ישנות או רחוקות מדי
                if (Time.time - point.discoveryTime > 30f || 
                    Vector3.Distance(transform.position, point.position) > _maxInterestDistance)
                {
                    _interestPoints.RemoveAt(i);
                }
            }
            
            // מגביל את כמות נקודות העניין
            if (_interestPoints.Count > 10)
            {
                _interestPoints = _interestPoints
                    .OrderByDescending(p => p.interestLevel)
                    .Take(10)
                    .ToList();
            }
        }
        
        // הוספת נקודת עניין חדשה
        public void AddInterestPoint(Vector3 position, float interestLevel = 1.0f)
        {
            // בדיקה אם הנקודה כבר קיימת
            if (_interestPoints.Any(p => Vector3.Distance(p.position, position) < 1.0f))
            {
                return;
            }
            
            InterestPoint newPoint = new InterestPoint
            {
                position = position,
                interestLevel = interestLevel,
                discoveryTime = Time.time
            };
            
            _interestPoints.Add(newPoint);
        }
        
        // תנועה לנקודת העניין המעניינת ביותר
        private void MoveToMostInterestingPoint()
        {
            if (_interestPoints.Count == 0 || _movementController == null)
                return;
            
            // מיון נקודות לפי עניין ומרחק
            var sortedPoints = _interestPoints
                .OrderByDescending(p => 
                    p.interestLevel / (1 + Vector3.Distance(transform.position, p.position) * 0.1f))
                .ToList();
            
            if (sortedPoints.Count > 0)
            {
                // תנועה לנקודה המעניינת ביותר
                _movementController.SetDestination(sortedPoints[0].position);
                
                if (_logInteractions)
                {
                    Debug.Log($"Minipoll {gameObject.name} moving to interest point at {sortedPoints[0].position}");
                }
            }
        }
        
        // טיפול בהגעה ליעד תנועה
        private void HandleDestinationReached()
        {
            // אם הגענו ליעד ויש מטרת אינטראקציה ממתינה
            if (_currentTarget != null)
            {
                // בדיקת מרחק שוב
                float distance = Vector3.Distance(transform.position, _currentTarget.transform.position);
                
                if (distance <= _interactionRange)
                {
                    // עכשיו אפשר לבצע את האינטראקציה
                    InteractWith(_currentTarget);
                    _currentTarget = null;
                }
            }
        }
        
        // חיפוש אובייקטים מעניינים בסביבה
        public void ScanSurroundings()
        {
            // חיפוש אובייקטים בטווח רחב יותר
            Collider[] colliders = Physics.OverlapSphere(transform.position, _maxInterestDistance, _interactableLayer);
            
            foreach (var collider in colliders)
            {
                IInteractable interactable = collider.GetComponent<IInteractable>();
                
                if (interactable != null)
                {
                    float interestLevel = CalculateInterestLevel(interactable);
                    AddInterestPoint(interactable.transform.position, interestLevel);
                }
            }
        }
    }
    
    // מחלקת עזר לנקודות עניין
    [System.Serializable]
    public class InterestPoint
    {
        public Vector3 position;
        public float interestLevel;
        public float discoveryTime;
    }
}