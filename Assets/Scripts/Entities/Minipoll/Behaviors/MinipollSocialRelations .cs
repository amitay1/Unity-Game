using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using MinipollGame.Core;

namespace MinipollGame
{
    // מערכת יחסים חברתיים בין מיניפולים
    public class MinipollSocialRelations : MonoBehaviour
    {
        [Header("Social Settings")]
        [SerializeField] private float _socialRadius = 5f;
        [SerializeField] private float _interactionProbability = 0.3f;
        [SerializeField] private float _socialUpdateInterval = 2f;
        [SerializeField] private float _maxRelationshipsCount = 20;
        [SerializeField] private LayerMask _minipollLayerMask;
        
        [Header("Relationship Development")]
        [SerializeField] private float _baseRelationshipChangeRate = 0.05f;
        [SerializeField] private float _interactionImpactMultiplier = 1.5f;
        [SerializeField] private float _sharedExperienceBonus = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool _logSocialInteractions = true;
        
        // מפת יחסים עם מיניפולים אחרים
        private Dictionary<MinipollBrain, RelationshipData> _relationships = new Dictionary<MinipollBrain, RelationshipData>();
        
        // התייחסויות למערכות
        private MinipollEmotionalState _emotionalState;
        private MinipollLearningSystem _learningSystem;
        private MinipollBrain _brain;
        
        // אירועים
        public event System.Action<MinipollBrain, RelationshipType> OnRelationshipChanged;
        public event System.Action<MinipollBrain, string> OnExperienceShared;
        
        // פעולה אחרונה
        private float _lastSocialActionTime;
        private float _lastRelationshipUpdateTime;
        
        private void Awake()
        {
            _emotionalState = GetComponent<MinipollEmotionalState>();
            _learningSystem = GetComponent<MinipollLearningSystem>();
            _brain = GetComponent<MinipollBrain>();
        }
        
        private void Start()
        {
            _lastSocialActionTime = Time.time;
            _lastRelationshipUpdateTime = Time.time;
            
            // התחלת עדכון תקופתי של יחסים
            InvokeRepeating("UpdateRelationships", _socialUpdateInterval, _socialUpdateInterval);
        }
        
        // בדיקה אם יש מיניפולים אחרים בקרבת מקום
        public bool HasNearbyMinipolls()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _socialRadius, _minipollLayerMask);
            return colliders.Length > 1; // לפחות אחד (לא כולל את עצמנו)
        }
        
        // ניסיון אינטראקציה עם מיניפול קרוב
        public bool TryInteractWithNearbyMinipoll()
        {
            // מציאת מיניפולים קרובים
            Collider[] colliders = Physics.OverlapSphere(transform.position, _socialRadius, _minipollLayerMask);
            
            List<MinipollBrain> nearbyMinipolls = new List<MinipollBrain>();
            
            foreach (var collider in colliders)
            {
                MinipollBrain otherBrain = collider.GetComponent<MinipollBrain>();
                
                if (otherBrain != null && otherBrain != _brain)
                {
                    nearbyMinipolls.Add(otherBrain);
                }
            }
            
            // אם אין מיניפולים קרובים
            if (nearbyMinipolls.Count == 0)
            {
                return false;
            }
            
            // מיון לפי יחסים קיימים
            nearbyMinipolls = nearbyMinipolls
                .OrderByDescending(m => GetRelationshipScore(m))
                .ToList();
            
            // אינטראקציה עם המיניפול בעל היחסים הטובים ביותר
            MinipollBrain targetMinipoll = nearbyMinipolls[0];
            
            // קבלת היחסים הקיימים
            RelationshipData relationship = GetOrCreateRelationship(targetMinipoll);
            
            // החלטה אם לבצע אינטראקציה
            if (Random.value < _interactionProbability * relationship.GetOverallScore())
            {
                // ביצוע אינטראקציה
                PerformSocialInteraction(targetMinipoll, relationship);
                _lastSocialActionTime = Time.time;
                return true;
            }
            
            return false;
        }
        
        // ביצוע אינטראקציה חברתית
        private void PerformSocialInteraction(MinipollBrain otherMinipoll, RelationshipData relationship)
        {
            // סוג אינטראקציה
            SocialInteractionType interactionType = ChooseInteractionType(relationship);
            
            if (_logSocialInteractions)
            {
                Debug.Log($"Minipoll {gameObject.name} performs {interactionType} with {otherMinipoll.gameObject.name}");
            }
            
            // הפעלת האינטראקציה
            bool positiveOutcome = ExecuteInteraction(otherMinipoll, interactionType, relationship);
            
            // עדכון היחסים
            if (positiveOutcome)
            {
                // שיפור יחסים
                AdjustRelationship(otherMinipoll, 0.1f * _interactionImpactMultiplier, 0.05f * _interactionImpactMultiplier);
                
                // עידוד שיתוף חוויות
                if (Random.value < 0.3f)
                {
                    ShareRandomExperience(otherMinipoll, relationship);
                }
            }
            else
            {
                // פגיעה ביחסים
                AdjustRelationship(otherMinipoll, -0.05f * _interactionImpactMultiplier, -0.1f * _interactionImpactMultiplier);
            }
        }
        
        // בחירת סוג אינטראקציה חברתית
        private SocialInteractionType ChooseInteractionType(RelationshipData relationship)
        {
            // רשימת אפשרויות עם משקלים
            List<SocialInteractionOption> options = new List<SocialInteractionOption>();
            
            // פעולות ידידותיות
            if (relationship.friendship > 0.3f)
            {
                options.Add(new SocialInteractionOption { type = SocialInteractionType.Greeting, weight = 0.5f });
                options.Add(new SocialInteractionOption { type = SocialInteractionType.Play, weight = relationship.friendship });
                
                if (relationship.friendship > 0.6f)
                {
                    options.Add(new SocialInteractionOption { type = SocialInteractionType.ShareExperience, weight = relationship.trust });
                }
            }
            else
            {
                options.Add(new SocialInteractionOption { type = SocialInteractionType.Greeting, weight = 1.0f });
                
                if (relationship.trust < 0.2f)
                {
                    options.Add(new SocialInteractionOption { type = SocialInteractionType.Observe, weight = 0.8f });
                }
            }
            
            // מצב רגשי משפיע על בחירת האינטראקציה
            if (_emotionalState != null)
            {
                EmotionType dominantEmotion = _emotionalState.DominantEmotion;
                float intensity = _emotionalState.GetEmotionIntensity(dominantEmotion);
                
                switch (dominantEmotion)
                {
                    case EmotionType.Happy:
                        // להגביר משחק ופעולות חיוביות
                        options.Add(new SocialInteractionOption { type = SocialInteractionType.Play, weight = intensity * 1.5f });
                        break;
                    
                    case EmotionType.Sad:
                        // להגביר בקשת עזרה
                        options.Add(new SocialInteractionOption { type = SocialInteractionType.SeekComfort, weight = intensity });
                        break;
                    
                    case EmotionType.Scared:
                        // להגביר מרחק ותצפית
                        options.Add(new SocialInteractionOption { type = SocialInteractionType.Observe, weight = intensity });
                        break;
                }
            }
            
            // בחירת פעולה אקראית מתוך המשקלים
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
            
            // ברירת מחדל - ברכה פשוטה
            return SocialInteractionType.Greeting;
        }
        
        // ביצוע אינטראקציה
        private bool ExecuteInteraction(MinipollBrain otherMinipoll, SocialInteractionType interactionType, RelationshipData relationship)
        {
            // תשובה של המיניפול האחר - נקבעת לפי יחסים ומצב רגשי
            MinipollSocialRelations otherSocialSystem = otherMinipoll.GetComponent<MinipollSocialRelations>();
            bool otherRespondsPositively = CalculateResponseProbability(otherMinipoll, interactionType, relationship);
            
            // הפעלת אנימציה מתאימה
            PlayInteractionAnimation(interactionType);
            
            switch (interactionType)
            {
                case SocialInteractionType.Greeting:
                    // ברכה פשוטה - סיכויי הצלחה גבוהים
                    if (otherRespondsPositively)
                    {
                        // המיניפול השני משיב ברכה
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Greeting);
                        
                        // עדכון רגשי
                        _emotionalState?.ModifyEmotion(EmotionType.Happy, 0.05f);
                        return true;
                    }
                    else
                    {
                        // המיניפול השני מתעלם
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Ignore);
                        return false;
                    }
                
                case SocialInteractionType.Play:
                    // משחק - דורש יחסים טובים יותר
                    if (otherRespondsPositively)
                    {
                        // המיניפול השני משתתף במשחק
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Play);
                        
                        // עדכון רגשי עם השפעה חזקה יותר
                        _emotionalState?.ModifyEmotion(EmotionType.Happy, 0.15f);
                        _emotionalState?.ModifyEmotion(EmotionType.Tired, 0.05f);
                        return true;
                    }
                    else
                    {
                        // המיניפול השני מסרב לשחק
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Reject);
                        
                        // אכזבה קלה
                        _emotionalState?.ModifyEmotion(EmotionType.Sad, 0.05f);
                        return false;
                    }
                
                case SocialInteractionType.ShareExperience:
                    // שיתוף חוויה - דורש אמון
                    if (otherRespondsPositively)
                    {
                        // שיתוף חוויה הדדי
                        ShareRandomExperience(otherMinipoll, relationship);
                        
                        // המיניפול השני מקשיב ומשתף בחזרה
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.ShareExperience);
                        
                        // תחושת חיבור
                        _emotionalState?.ModifyEmotion(EmotionType.Happy, 0.1f);
                        return true;
                    }
                    else
                    {
                        // המיניפול השני לא מעוניין
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Ignore);
                        return false;
                    }
                
                case SocialInteractionType.SeekComfort:
                    // בקשת נחמה - תלוי ברמת חברות
                    if (otherRespondsPositively)
                    {
                        // המיניפול השני מספק נחמה
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Comfort);
                        
                        // הקלה רגשית
                        _emotionalState?.ModifyEmotion(EmotionType.Sad, -0.15f);
                        _emotionalState?.ModifyEmotion(EmotionType.Happy, 0.05f);
                        return true;
                    }
                    else
                    {
                        // המיניפול השני מתעלם מהמצוקה
                        otherSocialSystem?.PlayInteractionAnimation(SocialInteractionType.Ignore);
                        
                        // החמרה ברגש השלילי
                        _emotionalState?.ModifyEmotion(EmotionType.Sad, 0.05f);
                        return false;
                    }
                
                case SocialInteractionType.Observe:
                    // תצפית - לא דורשת תגובה
                    return true;
                
                default:
                    return false;
            }
        }
        
        // חישוב סיכויי תגובה חיובית מהמיניפול האחר
        private bool CalculateResponseProbability(MinipollBrain otherMinipoll, SocialInteractionType interactionType, RelationshipData relationship)
        {
            float baseChance = 0.5f; // סיכוי בסיסי של 50%
            
            // התאמה לפי סוג היחס
            switch (relationship.relationshipType)
            {
                case RelationshipType.Friend:
                case RelationshipType.BestFriend:
                    baseChance += 0.3f;
                    break;
                
                case RelationshipType.Acquaintance:
                    baseChance += 0.1f;
                    break;
                
                case RelationshipType.Stranger:
                    break;
                
                case RelationshipType.Afraid:
                case RelationshipType.Hostile:
                    baseChance -= 0.4f;
                    break;
            }
            
            // התאמה לפי סוג האינטראקציה
            switch (interactionType)
            {
                case SocialInteractionType.Greeting:
                    baseChance += 0.2f; // קל להשיב לברכה
                    break;
                
                case SocialInteractionType.Play:
                    baseChance += relationship.friendship * 0.2f - 0.1f; // דורש יחסי חברות
                    break;
                
                case SocialInteractionType.ShareExperience:
                    baseChance += relationship.trust * 0.3f - 0.1f; // דורש אמון
                    break;
                
                case SocialInteractionType.SeekComfort:
                    baseChance += relationship.friendship * 0.3f - 0.1f; // דורש יחסי חברות טובים
                    break;
            }
            
            // התאמה לפי מצב רגשי של המיניפול האחר
            MinipollEmotionalState otherEmotionalState = otherMinipoll.GetComponent<MinipollEmotionalState>();
            if (otherEmotionalState != null)
            {
                EmotionType dominantEmotion = otherEmotionalState.DominantEmotion;
                float intensity = otherEmotionalState.GetEmotionIntensity(dominantEmotion);
                
                switch (dominantEmotion)
                {
                    case EmotionType.Happy:
                        baseChance += intensity * 0.2f; // יותר סיכוי כששמח
                        break;
                    
                    case EmotionType.Sad:
                    case EmotionType.Tired:
                        baseChance -= intensity * 0.1f; // פחות סיכוי כשעצוב/עייף
                        break;
                    
                    case EmotionType.Scared:
                        baseChance -= intensity * 0.3f; // פחות סיכוי משמעותית כשמפחד
                        break;
                }
            }
            
            // הגבלת הטווח
            baseChance = Mathf.Clamp01(baseChance);
            
            return Random.value < baseChance;
        }
        
        // הפעלת אנימציית אינטראקציה
        private void PlayInteractionAnimation(SocialInteractionType interactionType)
        {
            MinipollVisualController visualController = GetComponent<MinipollVisualController>();
            
            if (visualController != null)
            {
                // הפעלת אנימציה מתאימה
                switch (interactionType)
                {
                    case SocialInteractionType.Greeting:
                        visualController.PlayAnimation("Greeting");
                        break;
                    
                    case SocialInteractionType.Play:
                        visualController.PlayAnimation("Play");
                        break;
                    
                    case SocialInteractionType.ShareExperience:
                        visualController.PlayAnimation("Talk");
                        break;
                    
                    case SocialInteractionType.SeekComfort:
                        visualController.PlayAnimation("Sad");
                        break;
                    
                    case SocialInteractionType.Comfort:
                        visualController.PlayAnimation("Comfort");
                        break;
                    
                    case SocialInteractionType.Observe:
                        visualController.PlayAnimation("Look");
                        break;
                    
                    case SocialInteractionType.Reject:
                        visualController.PlayAnimation("Reject");
                        break;
                    
                    case SocialInteractionType.Ignore:
                        visualController.PlayAnimation("Ignore");
                        break;
                }
            }
        }
        
        // שיתוף חוויה אקראית עם מיניפול אחר
        private void ShareRandomExperience(MinipollBrain otherMinipoll, RelationshipData relationship)
        {
            if (_learningSystem == null)
                return;
            
            // בחירת חוויה משמעותית לשיתוף
            string experienceID = _learningSystem.GetMostSignificantExperience();
            
            if (string.IsNullOrEmpty(experienceID))
                return;
            
            // שיתוף החוויה
            MinipollLearningSystem otherLearningSystem = otherMinipoll.GetComponent<MinipollLearningSystem>();
            if (otherLearningSystem != null)
            {
                // שיתוף החוויה עם התאמה לפי רמת האמון
                _learningSystem.ShareExperience(otherLearningSystem, experienceID, relationship.trust);
                
                // הוספה לחוויות משותפות
                if (!relationship.sharedExperiences.Contains(experienceID))
                {
                    relationship.sharedExperiences.Add(experienceID);
                    
                    // שיפור יחסים בגלל שיתוף חוויה חדשה
                    AdjustRelationship(otherMinipoll, _sharedExperienceBonus, _sharedExperienceBonus);
                }
                
                // רישום אירוע שיתוף
                if (_logSocialInteractions)
                {
                    Debug.Log($"Minipoll {gameObject.name} shared experience {experienceID} with {otherMinipoll.gameObject.name}");
                }
                
                // הפעלת אירוע
                OnExperienceShared?.Invoke(otherMinipoll, experienceID);
            }
        }
        
        // שינוי יחסים עם מיניפול אחר
        public void AdjustRelationship(MinipollBrain otherMinipoll, float trustChange, float friendshipChange)
        {
            RelationshipData relationship = GetOrCreateRelationship(otherMinipoll);
            RelationshipType oldType = relationship.relationshipType;
            
            // עדכון ערכי היחסים
            relationship.trust = Mathf.Clamp01(relationship.trust + trustChange);
            relationship.friendship = Mathf.Clamp01(relationship.friendship + friendshipChange);
            
            // עדכון סוג היחסים
            UpdateRelationshipType(relationship);
            
            // שמירת היחסים המעודכנים
            _relationships[otherMinipoll] = relationship;
            
            // הפעלת אירוע אם סוג היחסים השתנה
            if (oldType != relationship.relationshipType)
            {
                OnRelationshipChanged?.Invoke(otherMinipoll, relationship.relationshipType);
            }
        }
        
        // עדכון סוג היחסים
        private void UpdateRelationshipType(RelationshipData relationship)
        {
            float overallScore = relationship.GetOverallScore();
            
            if (relationship.friendship < 0.2f && relationship.trust < 0.2f)
            {
                relationship.relationshipType = RelationshipType.Stranger;
            }
            else if (relationship.friendship > 0.7f && relationship.trust > 0.7f)
            {
                relationship.relationshipType = RelationshipType.BestFriend;
            }
            else if (relationship.friendship > 0.4f)
            {
                relationship.relationshipType = RelationshipType.Friend;
            }
            else if (relationship.fear > 0.6f)
            {
                relationship.relationshipType = RelationshipType.Afraid;
            }
            else if (relationship.trust < 0.2f && relationship.friendship < 0.2f && relationship.fear < 0.2f)
            {
                relationship.relationshipType = RelationshipType.Hostile;
            }
            else
            {
                relationship.relationshipType = RelationshipType.Acquaintance;
            }
        }
        
        // קבלת או יצירת יחסים עם מיניפול
        private RelationshipData GetOrCreateRelationship(MinipollBrain otherMinipoll)
        {
            RelationshipData relationship;
            
            if (_relationships.TryGetValue(otherMinipoll, out relationship))
            {
                return relationship;
            }
            else
            {
                // יצירת יחסים חדשים
                relationship = new RelationshipData
                {
                    otherMinipoll = otherMinipoll,
                    trust = 0.2f,
                    friendship = 0.1f,
                    fear = 0.0f,
                    relationshipType = RelationshipType.Stranger,
                    lastInteractionTime = Time.time,
                    sharedExperiences = new List<string>()
                };
                
                _relationships.Add(otherMinipoll, relationship);
                
                // טריגר אירוע מפגש ראשון
                EventSystem.TriggerMinipollsMeeting(_brain, otherMinipoll);
                
                return relationship;
            }
        }
        
        // עדכון תקופתי של יחסים
        private void UpdateRelationships()
        {
            // ניקוי יחסים עם מיניפולים לא קיימים
            List<MinipollBrain> invalidMinipolls = new List<MinipollBrain>();
            
            foreach (var pair in _relationships)
            {
                if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy)
                {
                    invalidMinipolls.Add(pair.Key);
                }
            }
            
            foreach (var invalid in invalidMinipolls)
            {
                _relationships.Remove(invalid);
            }
            
            // Apply natural decay or improvement to relationships over time using _baseRelationshipChangeRate
            foreach (var pair in _relationships)
            {
                // Skip recently interacted relationships
                if (Time.time - pair.Value.lastInteractionTime < 60f) 
                    continue;
                
                // Apply slight decay to inactive relationships
                float trustChange = -_baseRelationshipChangeRate * Time.deltaTime;
                float friendshipChange = -_baseRelationshipChangeRate * 0.5f * Time.deltaTime;
                
                // Update the relationship
                AdjustRelationship(pair.Key, trustChange, friendshipChange);
            }
            
            // בדיקה אם יש יותר מדי יחסים ושמירה רק על המשמעותיים ביותר
            if (_relationships.Count > _maxRelationshipsCount)
            {
                var sortedRelationships = _relationships
                    .OrderByDescending(r => r.Value.GetOverallScore())
                    .Skip((int)_maxRelationshipsCount)
                    .Select(r => r.Key)
                    .ToList();
                
                foreach (var removeBrain in sortedRelationships)
                {
                    _relationships.Remove(removeBrain);
                }
            }
        }
        
        // קבלת רשימת מיניפולים חברים
        public List<MinipollBrain> GetFriends()
        {
            return _relationships
                .Where(r => r.Value.relationshipType == RelationshipType.Friend || 
                           r.Value.relationshipType == RelationshipType.BestFriend)
                .Select(r => r.Key)
                .ToList();
        }
        
        // קבלת רשימת מיניפולים מאיימים
        public List<MinipollBrain> GetThreats()
        {
            return _relationships
                .Where(r => r.Value.relationshipType == RelationshipType.Hostile || 
                          (r.Value.relationshipType == RelationshipType.Stranger && r.Value.fear > 0.5f))
                .Select(r => r.Key)
                .ToList();
        }
        
        // קבלת ציון יחסים כולל
        private float GetRelationshipScore(MinipollBrain other)
        {
            if (_relationships.TryGetValue(other, out RelationshipData relationship))
            {
                return relationship.GetOverallScore();
            }
            return 0f;
        }
    }
    
    // מחלקת עזר לאפשרויות אינטראקציה
    [System.Serializable]
    public class SocialInteractionOption
    {
        public SocialInteractionType type;
        public float weight;
    }
}