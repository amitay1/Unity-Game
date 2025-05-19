using UnityEngine;
using System.Collections;
using MinipollGame.Core;
using MinipollGame.ScriptableObjects;

namespace MinipollGame
{
    // בסיס לאובייקטים שמיניפולים יכולים לתקשר איתם
    [RequireComponent(typeof(Collider))]
   public class InteractableObject : MonoBehaviour, IInteractable
    {
        [Header("Interaction Settings")]
        [SerializeField] private InteractionData _interactionData;
        [SerializeField] private string _customInteractionID;
        [SerializeField] private float _interactionCooldown = 3f;
        [SerializeField] private int _maxInteractions = -1; // -1 = ללא הגבלה
        [SerializeField] private bool _requireSpecificEmotion = false;
        [SerializeField] private EmotionType _requiredEmotion = EmotionType.Neutral;
        
        [Header("Visual Feedback")]
        [SerializeField] private GameObject _interactionEffectPrefab;
        [SerializeField] private float _effectDuration = 1.5f;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _successAnimTrigger = "Success";
        [SerializeField] private string _failAnimTrigger = "Fail";
        
        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _successSound;
        [SerializeField] private AudioClip _failSound;
        
        [Header("Advanced")]
        [SerializeField] private bool _useCustomInteractionLogic = false;
        [SerializeField] private bool _useCustomAvailabilityLogic = false;
        [SerializeField] private bool _useDayCycle = false;
        [SerializeField] private TimeOfDay.DayPhase[] _availablePhases;
        
        // מצב פנימי
        private int _interactionCount = 0;
        private float _lastInteractionTime = -1000f;
        private bool _isBusy = false;
        private GameObject _currentEffect;
        
        // ממשק IInteractable
        public string InteractionID => string.IsNullOrEmpty(_customInteractionID) ? 
            (_interactionData != null ? _interactionData.interactionId : gameObject.name) : _customInteractionID;
        
        private void Start()
        {
            // מציאת רכיבים אם לא הוגדרו
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
            
            // הגדרת קולידר כטריגר
            Collider objectCollider = GetComponent<Collider>();
            if (objectCollider != null)
            {
                objectCollider.isTrigger = true;
            }
        }
        
        // בדיקה אם אפשר לתקשר עם האובייקט
        public bool CanInteract(MinipollBrain minipoll)
        {
            // אם האובייקט עסוק, אי אפשר לתקשר איתו
            if (_isBusy)
            {
                return false;
            }
            
            // בדיקת קולדאון
            if (Time.time - _lastInteractionTime < _interactionCooldown)
            {
                return false;
            }
            
            // בדיקת מספר אינטראקציות מקסימלי
            if (_maxInteractions > 0 && _interactionCount >= _maxInteractions)
            {
                return false;
            }
            
            // בדיקת רגש נדרש
            if (_requireSpecificEmotion && minipoll.EmotionalState != null)
            {
                if (minipoll.EmotionalState.DominantEmotion != _requiredEmotion)
                {
                    return false;
                }
            }
            
            // בדיקת מחזור יום/לילה
            if (_useDayCycle)
            {
                GameManager gameManager = GameManager.Instance;
                
                if (gameManager != null)
                {
                    TimeOfDay.DayPhase currentPhase = gameManager.CurrentTimeOfDay.CurrentPhase;
                    bool isPhaseAvailable = false;
                    
                    for (int i = 0; i < _availablePhases.Length; i++)
                    {
                        if (_availablePhases[i] == currentPhase)
                        {
                            isPhaseAvailable = true;
                            break;
                        }
                    }
                    
                    if (!isPhaseAvailable)
                    {
                        return false;
                    }
                }
            }
            
            // אם יש לוגיקת זמינות מותאמת אישית
            if (_useCustomAvailabilityLogic)
            {
                return CustomAvailabilityCheck(minipoll);
            }
            
            return true;
        }
        
        // ביצוע אינטראקציה
        public InteractionResult Interact(MinipollBrain minipoll)
        {
            // אם האובייקט לא זמין, מחזיר תוצאה שלילית
            if (!CanInteract(minipoll))
            {
                return InteractionResult.Negative(0.1f, EmotionType.Sad, InteractionID + "_unavailable");
            }
            
            // עדכון מצב
            _lastInteractionTime = Time.time;
            _interactionCount++;
            _isBusy = true;
            
            // השמעת סאונד התחלה אם יש
            
            // אינטראקציה מותאמת אישית או סטנדרטית
            InteractionResult result;
            
            if (_useCustomInteractionLogic)
            {
                result = CustomInteractionLogic(minipoll);
            }
            else if (_interactionData != null)
            {
                // שימוש בנתוני אינטראקציה מהסקריפטאבל אובייקט
                result = _interactionData.CalculateInteractionResult(minipoll);
            }
            else
            {
                // אינטראקציה בסיסית
                bool randomSuccess = Random.value < 0.7f;
                
                if (randomSuccess)
                {
                    result = InteractionResult.Positive(0.2f, EmotionType.Happy, InteractionID);
                }
                else
                {
                    result = InteractionResult.Negative(0.1f, EmotionType.Sad, InteractionID);
                }
            }
            
            // הפעלת ויזואלים בהתאם לתוצאה
            StartCoroutine(PlayInteractionEffects(result, minipoll));
            
            // החזרת תוצאה
            return result;
        }
        
        // הפעלת אפקטים ויזואליים
        private IEnumerator PlayInteractionEffects(InteractionResult result, MinipollBrain minipoll)
        {
            // הפעלת אנימציה
            if (_animator != null)
            {
                if (result.Success && !string.IsNullOrEmpty(_successAnimTrigger))
                {
                    _animator.SetTrigger(_successAnimTrigger);
                }
                else if (!result.Success && !string.IsNullOrEmpty(_failAnimTrigger))
                {
                    _animator.SetTrigger(_failAnimTrigger);
                }
            }
            
            // הפעלת אפקט חלקיקים
            if (_interactionEffectPrefab != null)
            {
                // הסרת אפקט קודם אם יש
                if (_currentEffect != null)
                {
                    Destroy(_currentEffect);
                }
                
                // יצירת אפקט חדש
                _currentEffect = Instantiate(_interactionEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
                
                // התאמת צבע לפי הצלחה/כישלון
                ParticleSystem particleSystem = _currentEffect.GetComponent<ParticleSystem>();
                
                if (particleSystem != null)
                {
                    var mainModule = particleSystem.main;
                    
                    if (result.Success)
                    {
                        mainModule.startColor = new Color(0.3f, 1f, 0.3f);
                    }
                    else
                    {
                        mainModule.startColor = new Color(1f, 0.3f, 0.3f);
                    }
                }
            }
            
            // השמעת סאונד
            if (_audioSource != null)
            {
                if (result.Success && _successSound != null)
                {
                    _audioSource.PlayOneShot(_successSound);
                }
                else if (!result.Success && _failSound != null)
                {
                    _audioSource.PlayOneShot(_failSound);
                }
            }
            
            // המתנה
            yield return new WaitForSeconds(_effectDuration);
            
            // ניקוי
            if (_currentEffect != null)
            {
                Destroy(_currentEffect);
                _currentEffect = null;
            }
            
            _isBusy = false;
        }
        
        // אפשר לחברות בת לממש לוגיקה מותאמת אישית
        protected virtual InteractionResult CustomInteractionLogic(MinipollBrain minipoll)
        {
            // מחלקת בסיס - תמיד מחזירה תוצאה חיובית
            return InteractionResult.Positive(0.3f, EmotionType.Happy, InteractionID);
        }
        
        // אפשר לחברות בת לממש לוגיקת זמינות מותאמת אישית
        protected virtual bool CustomAvailabilityCheck(MinipollBrain minipoll)
        {
            return true;
        }
        
        // איפוס האובייקט
        public void ResetObject()
        {
            _interactionCount = 0;
            _lastInteractionTime = -1000f;
            _isBusy = false;
            
            if (_currentEffect != null)
            {
                Destroy(_currentEffect);
                _currentEffect = null;
            }
        }
        
        // Gizmos לתצוגה בעורך
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}