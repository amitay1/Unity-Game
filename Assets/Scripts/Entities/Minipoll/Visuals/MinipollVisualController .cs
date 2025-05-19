using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Core;

namespace MinipollGame
{
    // מערכת שליטה בויזואלים של המיניפול
    public class MinipollVisualController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _bodyRenderer;
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private ParticleSystem _emotionParticles;
        [SerializeField] private AudioSource _audioSource;
        
        [Header("Body Settings")]
        [SerializeField] private Color _defaultBodyColor = Color.white;
        [SerializeField] private Vector3 _defaultScale = Vector3.one;
        [SerializeField] private float _pulseSpeed = 1f;
        [SerializeField] private float _pulseAmount = 0.05f;
        
        [Header("Eyes")]
        [SerializeField] private Transform _leftEyeTransform;
        [SerializeField] private Transform _rightEyeTransform;
        [SerializeField] private SpriteRenderer _leftEyeRenderer;
        [SerializeField] private SpriteRenderer _rightEyeRenderer;
        [SerializeField] private Color _defaultEyeColor = new Color(0.2f, 0.2f, 0.8f);
        
        [Header("Emotional Visuals")]
        [SerializeField] private EmotionVisualSettings[] _emotionVisuals;
        
        [Header("Animations")]
        [SerializeField] private float _bounceHeight = 0.2f;
        [SerializeField] private float _bounceSpeed = 2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip[] _happySounds;
        [SerializeField] private AudioClip[] _sadSounds;
        [SerializeField] private AudioClip[] _scarySounds;
        [SerializeField] private AudioClip[] _neutralSounds;
        
        // מצב נוכחי
        private EmotionType _currentVisualEmotion = EmotionType.Neutral;
        private float _emotionIntensity = 0f;
        private bool _isPulsing = false;
        private bool _isBouncing = false;
        private Coroutine _currentEmotionCoroutine;
        private Coroutine _currentAnimationCoroutine;
        
        // מצב עיניים
        private Vector3 _defaultLeftEyePosition;
        private Vector3 _defaultRightEyePosition;
        private Vector3 _targetLeftEyePosition;
        private Vector3 _targetRightEyePosition;
        private float _eyeMovementSpeed = 3f;
        private float _eyeMovementTimer = 0f;
        
        private void Awake()
        {
            // איתחול רפרנסים
            if (_animator == null)
            {
                _animator = GetComponent<Animator>();
            }
            
            if (_bodyRenderer == null)
            {
                _bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            
            if (_bodyTransform == null)
            {
                _bodyTransform = transform;
            }
            
            if (_audioSource == null)
            {
                _audioSource = GetComponent<AudioSource>();
            }
            
            // איתחול מצב עיניים
            if (_leftEyeTransform != null && _rightEyeTransform != null)
            {
                _defaultLeftEyePosition = _leftEyeTransform.localPosition;
                _defaultRightEyePosition = _rightEyeTransform.localPosition;
                _targetLeftEyePosition = _defaultLeftEyePosition;
                _targetRightEyePosition = _defaultRightEyePosition;
            }
        }
        
        private void Start()
        {
            // הגדרת צבעים
            SetBodyColor(_defaultBodyColor);
            SetEyeColor(_defaultEyeColor);
            
            // התחלת התנהגויות אנימציה בסיסיות
            StartCoroutine(IdleAnimationCoroutine());
            StartCoroutine(RandomEyeMovementCoroutine());
        }
        
        private void Update()
        {
            // עדכון תנועת עיניים
            UpdateEyeMovement();
        }
        
        // עדכון ויזואלים בהתאם לרגש
        public void UpdateEmotionVisuals(EmotionType emotion, float intensity)
        {
            // אם הרגש שונה או שהעוצמה השתנתה משמעותית
            if (emotion != _currentVisualEmotion || Mathf.Abs(intensity - _emotionIntensity) > 0.2f)
            {
                // שמירת המצב החדש
                _currentVisualEmotion = emotion;
                _emotionIntensity = intensity;
                
                // עצירת קורוטינות קודמות
                if (_currentEmotionCoroutine != null)
                {
                    StopCoroutine(_currentEmotionCoroutine);
                }
                
                // מציאת הגדרות ויזואליות מתאימות
                EmotionVisualSettings visualSettings = GetEmotionVisualSettings(emotion);
                
                if (visualSettings != null)
                {
                    // הפעלת אפקטים ויזואליים
                    _currentEmotionCoroutine = StartCoroutine(ApplyEmotionVisuals(visualSettings, intensity));
                    
                    // הפעלת אנימציה מתאימה
                    if (_animator != null && !string.IsNullOrEmpty(visualSettings.animationTrigger))
                    {
                        _animator.SetTrigger(visualSettings.animationTrigger);
                    }
                    
                    // השמעת סאונד מתאים
                    PlayEmotionSound(emotion);
                }
            }
        }
        
        // קבלת הגדרות ויזואליות לרגש
        private EmotionVisualSettings GetEmotionVisualSettings(EmotionType emotion)
        {
            if (_emotionVisuals != null)
            {
                foreach (var settings in _emotionVisuals)
                {
                    if (settings.emotion == emotion)
                    {
                        return settings;
                    }
                }
            }
            
            // ברירת מחדל - ניטרלי
            return new EmotionVisualSettings
            {
                emotion = EmotionType.Neutral,
                colorTint = Color.white,
                pulseEffect = false,
                particleColor = Color.white,
                eyeScale = Vector3.one
            };
        }
        
        // קורוטינה להחלת אפקטים ויזואליים של רגש
        private IEnumerator ApplyEmotionVisuals(EmotionVisualSettings settings, float intensity)
        {
            // שינוי צבעים
            Color targetBodyColor = Color.Lerp(_defaultBodyColor, settings.colorTint, intensity * 0.6f);
            
            // שינוי תזמון מצמוץ - נעשה על ידי רכיב ה-BlinkController
            MinipollBlinkController blinkController = GetComponent<MinipollBlinkController>();
            if (blinkController != null)
            {
                if (settings.emotion == EmotionType.Surprised || settings.emotion == EmotionType.Scared)
                {
                    // פחות מצמוץ במצבי הפתעה/פחד
                    blinkController.SetBlinkFrequencyMultiplier(0.3f);
                }
                else if (settings.emotion == EmotionType.Tired)
                {
                    // יותר מצמוץ במצב עייפות
                    blinkController.SetBlinkFrequencyMultiplier(2.0f);
                }
                else
                {
                    // חזרה לנורמלי
                    blinkController.SetBlinkFrequencyMultiplier(1.0f);
                }
            }
            
            // החלת צבע בהדרגה
            float transitionTime = 0.5f;
            float startTime = Time.time;
            Color startColor = _bodyRenderer.color;
            
            while (Time.time < startTime + transitionTime)
            {
                float t = (Time.time - startTime) / transitionTime;
                _bodyRenderer.color = Color.Lerp(startColor, targetBodyColor, t);
                yield return null;
            }
            
            _bodyRenderer.color = targetBodyColor;
            
            // שינוי גודל עיניים
            if (_leftEyeTransform != null && _rightEyeTransform != null)
            {
                Vector3 targetEyeScale = Vector3.Lerp(Vector3.one, settings.eyeScale, intensity);
                
                startTime = Time.time;
                Vector3 leftStartScale = _leftEyeTransform.localScale;
                Vector3 rightStartScale = _rightEyeTransform.localScale;
                
                while (Time.time < startTime + transitionTime)
                {
                    float t = (Time.time - startTime) / transitionTime;
                    _leftEyeTransform.localScale = Vector3.Lerp(leftStartScale, targetEyeScale, t);
                    _rightEyeTransform.localScale = Vector3.Lerp(rightStartScale, targetEyeScale, t);
                    yield return null;
                }
                
                _leftEyeTransform.localScale = targetEyeScale;
                _rightEyeTransform.localScale = targetEyeScale;
            }
            
            // הפעלת מערכת חלקיקים
            if (_emotionParticles != null && settings.enableParticles)
            {
                var main = _emotionParticles.main;
                main.startColor = settings.particleColor;
                
                _emotionParticles.Emit((int)(10 * intensity));
            }
            
            // הפעלת אפקט פעימה
            _isPulsing = settings.pulseEffect;
            if (_isPulsing)
            {
                StartCoroutine(PulseEffect(_pulseSpeed, _pulseAmount * intensity));
            }
        }
        
        // אפקט פעימה לגוף המיניפול
        private IEnumerator PulseEffect(float speed, float amount)
        {
            Vector3 originalScale = _bodyTransform.localScale;
            
            while (_isPulsing)
            {
                float pulse = 1 + Mathf.Sin(Time.time * speed) * amount;
                _bodyTransform.localScale = originalScale * pulse;
                yield return null;
            }
            
            // החזרה לגודל המקורי
            _bodyTransform.localScale = originalScale;
        }
        
        // הפעלת אנימציה ספציפית
        public void PlayAnimation(string animationName)
        {
            if (_animator != null && !string.IsNullOrEmpty(animationName))
            {
                _animator.SetTrigger(animationName);
            }
            
            // הפעלת אנימציות מיוחדות
            switch (animationName)
            {
                case "Jump":
                case "Happy":
                    if (_currentAnimationCoroutine != null)
                    {
                        StopCoroutine(_currentAnimationCoroutine);
                    }
                    _currentAnimationCoroutine = StartCoroutine(BounceAnimation());
                    break;
                
                case "Rest":
                case "Sleep":
                    // עצירת פעימה ואפקטים
                    _isPulsing = false;
                    _isBouncing = false;
                    break;
            }
        }
        
        // אנימציית קפיצה
        private IEnumerator BounceAnimation()
        {
            _isBouncing = true;
            Vector3 startPos = _bodyTransform.localPosition;
            float jumpTime = 0.5f;
            
            for (int i = 0; i < 2; i++) // שתי קפיצות
            {
                float startTime = Time.time;
                
                while (Time.time < startTime + jumpTime && _isBouncing)
                {
                    float progress = (Time.time - startTime) / jumpTime;
                    float height = Mathf.Sin(progress * Mathf.PI) * _bounceHeight;
                    
                    _bodyTransform.localPosition = startPos + new Vector3(0f, height, 0f);
                    yield return null;
                }
            }
            
            _bodyTransform.localPosition = startPos;
            _isBouncing = false;
        }
        
        // אנימציית אידל בסיסית
        private IEnumerator IdleAnimationCoroutine()
        {
            Vector3 originalPosition = _bodyTransform.localPosition;
            float sinTime = 0f;
            
            while (true)
            {
                // רק אם אין אנימציות אחרות פעילות
                if (!_isBouncing)
                {
                    sinTime += Time.deltaTime * _bounceSpeed * 0.3f;
                    float offset = Mathf.Sin(sinTime) * _bounceHeight * 0.2f;
                    
                    _bodyTransform.localPosition = originalPosition + new Vector3(0f, offset, 0f);
                }
                
                yield return null;
            }
        }
        
        // תנועת עיניים אקראית
        private IEnumerator RandomEyeMovementCoroutine()
        {
            while (true)
            {
                // המתנה אקראית
                yield return new WaitForSeconds(Random.Range(2f, 5f));
                
                // מיקום חדש לעיניים
                float offsetX = Random.Range(-0.15f, 0.15f);
                float offsetY = Random.Range(-0.1f, 0.1f);
                
                // מיקום אקראי
                _targetLeftEyePosition = _defaultLeftEyePosition + new Vector3(offsetX, offsetY, 0);
                _targetRightEyePosition = _defaultRightEyePosition + new Vector3(offsetX, offsetY, 0);
                
                _eyeMovementTimer = 0f;
                
                // הזזת העיניים בהדרגה
                float moveTime = 0.5f;
                yield return new WaitForSeconds(moveTime);
                
                // במקרים אקראיים, מתמקדים במבט קדימה
                if (Random.value < 0.4f)
                {
                    _targetLeftEyePosition = _defaultLeftEyePosition;
                    _targetRightEyePosition = _defaultRightEyePosition;
                    
                    _eyeMovementTimer = 0f;
                    yield return new WaitForSeconds(moveTime);
                }
            }
        }
        
        // עדכון תנועת עיניים בכל פריים
        private void UpdateEyeMovement()
        {
            if (_leftEyeTransform != null && _rightEyeTransform != null)
            {
                _eyeMovementTimer += Time.deltaTime * _eyeMovementSpeed;
                float t = Mathf.Min(1, _eyeMovementTimer);
                
                _leftEyeTransform.localPosition = Vector3.Lerp(_leftEyeTransform.localPosition, _targetLeftEyePosition, t);
                _rightEyeTransform.localPosition = Vector3.Lerp(_rightEyeTransform.localPosition, _targetRightEyePosition, t);
            }
        }
        
        // השמעת סאונד מתאים לרגש
        private void PlayEmotionSound(EmotionType emotion)
        {
            if (_audioSource == null)
                return;
            
            AudioClip[] clips = _neutralSounds;
            
            switch (emotion)
            {
                case EmotionType.Happy:
                case EmotionType.Excited:
                    clips = _happySounds;
                    break;
                
                case EmotionType.Sad:
                case EmotionType.Tired:
                    clips = _sadSounds;
                    break;
                
                case EmotionType.Scared:
                case EmotionType.Surprised:
                    clips = _scarySounds;
                    break;
            }
            
            if (clips != null && clips.Length > 0 && Random.value < 0.7f)
            {
                AudioClip clip = clips[Random.Range(0, clips.Length)];
                _audioSource.PlayOneShot(clip, _emotionIntensity);
            }
        }
        
        #region Public API
        
        // הגדרת צבע גוף
        public void SetBodyColor(Color color)
        {
            _defaultBodyColor = color;
            if (_bodyRenderer != null)
            {
                _bodyRenderer.color = color;
            }
        }
        
        // הגדרת צבע עיניים
        public void SetEyeColor(Color color)
        {
            _defaultEyeColor = color;
            if (_leftEyeRenderer != null)
            {
                _leftEyeRenderer.color = color;
            }
            if (_rightEyeRenderer != null)
            {
                _rightEyeRenderer.color = color;
            }
        }
        
        // הגדרת גודל
        public void SetScale(float scale)
        {
            _defaultScale = new Vector3(scale, scale, scale);
            transform.localScale = _defaultScale;
        }
        
        // כיוון עיניים לכיוון מסוים
        public void LookAt(Vector3 worldPosition)
        {
            if (_leftEyeTransform == null || _rightEyeTransform == null)
                return;
            
            // חישוב כיוון במרחב
            Vector3 direction = (worldPosition - transform.position).normalized;
            
            // המרה לקואורדינטות מקומיות
            Vector3 localDirection = transform.InverseTransformDirection(direction);
            
            // חישוב הזזה של העיניים (מוגבל לגבולות מסוימים)
            float eyeOffsetX = Mathf.Clamp(localDirection.x * 0.3f, -0.15f, 0.15f);
            float eyeOffsetY = Mathf.Clamp(localDirection.y * 0.2f, -0.1f, 0.1f);
            
            // קביעת מיקום יעד
            _targetLeftEyePosition = _defaultLeftEyePosition + new Vector3(eyeOffsetX, eyeOffsetY, 0);
            _targetRightEyePosition = _defaultRightEyePosition + new Vector3(eyeOffsetX, eyeOffsetY, 0);
            
            // איפוס טיימר לתנועה חלקה
            _eyeMovementTimer = 0f;
        }
        
        // החזרת המבט למצב רגיל
        public void ResetLook()
        {
            _targetLeftEyePosition = _defaultLeftEyePosition;
            _targetRightEyePosition = _defaultRightEyePosition;
            _eyeMovementTimer = 0f;
        }
        
        // הפסקת כל האפקטים
        public void StopAllEffects()
        {
            _isPulsing = false;
            _isBouncing = false;
            
            if (_currentEmotionCoroutine != null)
            {
                StopCoroutine(_currentEmotionCoroutine);
            }
            
            if (_currentAnimationCoroutine != null)
            {
                StopCoroutine(_currentAnimationCoroutine);
            }
            
            // איפוס מיקום ומידות
            _bodyTransform.localPosition = Vector3.zero;
            _bodyTransform.localScale = _defaultScale;
            
            if (_leftEyeTransform != null && _rightEyeTransform != null)
            {
                _leftEyeTransform.localPosition = _defaultLeftEyePosition;
                _rightEyeTransform.localPosition = _defaultRightEyePosition;
                _leftEyeTransform.localScale = Vector3.one;
                _rightEyeTransform.localScale = Vector3.one;
            }
            
            // איפוס צבעים
            SetBodyColor(_defaultBodyColor);
            SetEyeColor(_defaultEyeColor);
        }
        
        #endregion
    }
    
    // מחלקה להגדרות חזותיות לכל רגש
    [System.Serializable]
    public class EmotionVisualSettings
    {
        public EmotionType emotion;
        public Color colorTint = Color.white;
        public bool pulseEffect = false;
        public bool enableParticles = false;
        public Color particleColor = Color.white;
        public Vector3 eyeScale = Vector3.one;
        public string animationTrigger;
    }
}