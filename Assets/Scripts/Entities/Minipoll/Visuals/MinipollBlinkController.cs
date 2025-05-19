using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Utils;

namespace MinipollGame
{
    // מערכת שליטה במצמוץ עיניים של המיניפול
    public class MinipollBlinkController : MonoBehaviour
    {
        [Header("Blink Settings")]
        [SerializeField] private float _averageBlinkInterval = 3f;
        [SerializeField] private float _blinkIntervalVariance = 0.5f;
        [SerializeField] private float _blinkDuration = 0.15f;
        [SerializeField] private float _currentBlinkFrequencyMultiplier = 1f;
        
        [Header("Eye References")]
        [SerializeField] private SpriteRenderer _leftEyeRenderer;
        [SerializeField] private SpriteRenderer _rightEyeRenderer;
        [SerializeField] private Sprite _eyeOpenSprite;
        [SerializeField] private Sprite _eyeHalfClosedSprite;
        [SerializeField] private Sprite _eyeClosedSprite;
        
        [Header("Alternative References")]
        [SerializeField] private bool _useAnimator = false;
        [SerializeField] private Animator _animator;
        [SerializeField] private string _blinkTriggerName = "Blink";
        
        [Header("Advanced")]
        [SerializeField] private bool _useAlphaBlink = true;
        [SerializeField] private bool _useSequencer = false;
        [SerializeField] private bool _useCoroutine = true;
        [SerializeField] private float _halfClosedBlinkPoint = 0.3f;
        
        // מצב פנימי
        private bool _isBlinking = false;
        private float _nextBlinkTime;
        private Coroutine _blinkCoroutine;
        private bool _blinkingEnabled = true;
        
        // אירועים
        public event System.Action OnBlinkStarted;
        public event System.Action OnBlinkEnded;
        
        // שכבת אלפא למצמוץ (אם משתמשים בשיטת אלפא)
        private SpriteRenderer _leftEyeAlphaOverlay;
        private SpriteRenderer _rightEyeAlphaOverlay;
        
        private void Awake()
        {
            // התאמת ברירות מחדל
            if (_animator == null && _useAnimator)
            {
                _animator = GetComponent<Animator>();
            }
            
            // יצירת אובייקטים למצמוץ אלפא אם צריך
            if (_useAlphaBlink && !_useAnimator && !_useSequencer)
            {
                SetupAlphaBlinkOverlays();
            }
        }
        
        private void Start()
        {
            ResetNextBlinkTime();
        }
        
        private void Update()
        {
            // רק אם מצמוץ מופעל ולא משתמשים בקורוטינה
            if (_blinkingEnabled && !_useCoroutine && !_isBlinking)
            {
                // בדיקה אם הגיע זמן המצמוץ הבא
                if (Time.time >= _nextBlinkTime)
                {
                    Blink();
                }
            }
        }
        
        // הגדרת פרמטרים של מצמוץ
        public void SetBlinkParameters(float averageInterval, float variance, float duration)
        {
            _averageBlinkInterval = averageInterval;
            _blinkIntervalVariance = variance;
            _blinkDuration = duration;
        }
        
        // קביעת מכפיל תדירות מצמוץ - שימושי למצבים רגשיים שונים
        public void SetBlinkFrequencyMultiplier(float multiplier)
        {
            _currentBlinkFrequencyMultiplier = Mathf.Clamp(multiplier, 0.2f, 3f);
            ResetNextBlinkTime(); // עדכון הזמן הבא
        }
        
        // התחלת מצמוץ
        public void StartBlinking()
        {
            _blinkingEnabled = true;
            
            if (_useCoroutine)
            {
                if (_blinkCoroutine != null)
                {
                    StopCoroutine(_blinkCoroutine);
                }
                
                _blinkCoroutine = StartCoroutine(BlinkRoutine());
            }
            
            ResetNextBlinkTime();
        }
        
        // עצירת מצמוץ
        public void StopBlinking()
        {
            _blinkingEnabled = false;
            
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;
            }
            
            // החזרת עיניים למצב פתוח
            SetEyesOpen();
        }
        
        // קורוטינה למצמוץ רציף
        private IEnumerator BlinkRoutine()
        {
            while (_blinkingEnabled)
            {
                // המתנה לזמן המצמוץ הבא
                float waitTime = _nextBlinkTime - Time.time;
                if (waitTime > 0)
                {
                    yield return new WaitForSeconds(waitTime);
                }
                
                if (_blinkingEnabled)
                {
                    // הפעלת מצמוץ
                    yield return StartCoroutine(PerformBlink());
                    
                    // חישוב זמן המצמוץ הבא
                    ResetNextBlinkTime();
                }
            }
        }
        
        // פעולת המצמוץ עצמה
        private IEnumerator PerformBlink()
        {
            _isBlinking = true;
            OnBlinkStarted?.Invoke();
            
            // טיפול שונה לפי אופן המצמוץ
            if (_useAnimator)
            {
                // שימוש באנימטור
                _animator.SetTrigger(_blinkTriggerName);
                
                // המתנה לסיום המצמוץ
                yield return new WaitForSeconds(_blinkDuration);
            }
            else if (_useSequencer)
            {
                // מימוש באמצעות מערכת סיקוונסר חיצונית יבוא כאן
                Debug.Log("Sequencer blink not implemented yet");
                yield return new WaitForSeconds(_blinkDuration);
            }
            else if (_useAlphaBlink)
            {
                // מצמוץ באמצעות שקיפות
                yield return PerformAlphaBlink();
            }
            else
            {
                // מצמוץ באמצעות החלפת ספרייטים
                yield return PerformSpriteBlink();
            }
            
            _isBlinking = false;
            OnBlinkEnded?.Invoke();
        }
        
        // מצמוץ באמצעות שקיפות
        private IEnumerator PerformAlphaBlink()
        {
            if (_leftEyeAlphaOverlay == null || _rightEyeAlphaOverlay == null)
            {
                yield break;
            }
            
            // מצמוץ הדרגתי - סגירה
            float startTime = Time.time;
            float halfDuration = _blinkDuration / 2f;
            
            // שלב סגירת עיניים
            while (Time.time < startTime + halfDuration)
            {
                float t = (Time.time - startTime) / halfDuration;
                float alphaValue = Mathf.Lerp(0, 1, t);
                
                // עדכון שקיפות
                Color leftColor = _leftEyeAlphaOverlay.color;
                Color rightColor = _rightEyeAlphaOverlay.color;
                
                leftColor.a = alphaValue;
                rightColor.a = alphaValue;
                
                _leftEyeAlphaOverlay.color = leftColor;
                _rightEyeAlphaOverlay.color = rightColor;
                
                // בדיקה אם הגענו לנקודת חצי-עיניים
                if (t >= _halfClosedBlinkPoint && _eyeHalfClosedSprite != null)
                {
                    _leftEyeRenderer.sprite = _eyeHalfClosedSprite;
                    _rightEyeRenderer.sprite = _eyeHalfClosedSprite;
                }
                
                yield return null;
            }
            
            // עיניים סגורות לחלוטין
            Color fullClosedLeft = _leftEyeAlphaOverlay.color;
            Color fullClosedRight = _rightEyeAlphaOverlay.color;
            
            fullClosedLeft.a = 1f;
            fullClosedRight.a = 1f;
            
            _leftEyeAlphaOverlay.color = fullClosedLeft;
            _rightEyeAlphaOverlay.color = fullClosedRight;
            
            if (_eyeClosedSprite != null)
            {
                _leftEyeRenderer.sprite = _eyeClosedSprite;
                _rightEyeRenderer.sprite = _eyeClosedSprite;
            }
            
            // שלב פתיחת עיניים
            startTime = Time.time;
            
            while (Time.time < startTime + halfDuration)
            {
                float t = (Time.time - startTime) / halfDuration;
                float alphaValue = Mathf.Lerp(1, 0, t);
                
                // עדכון שקיפות
                Color leftColor = _leftEyeAlphaOverlay.color;
                Color rightColor = _rightEyeAlphaOverlay.color;
                
                leftColor.a = alphaValue;
                rightColor.a = alphaValue;
                
                _leftEyeAlphaOverlay.color = leftColor;
                _rightEyeAlphaOverlay.color = rightColor;
                
                // בדיקה אם הגענו לנקודת חצי-עיניים
                if (t >= 1 - _halfClosedBlinkPoint && _eyeHalfClosedSprite != null)
                {
                    _leftEyeRenderer.sprite = _eyeHalfClosedSprite;
                    _rightEyeRenderer.sprite = _eyeHalfClosedSprite;
                }
                
                yield return null;
            }
            
            // עיניים פתוחות לחלוטין
            SetEyesOpen();
        }
        
        // מצמוץ באמצעות ספרייטים
        private IEnumerator PerformSpriteBlink()
        {
            if (_leftEyeRenderer == null || _rightEyeRenderer == null)
            {
                yield break;
            }
            
            // שמירת ספרייטים מקוריים
            Sprite originalLeftSprite = _leftEyeRenderer.sprite;
            Sprite originalRightSprite = _rightEyeRenderer.sprite;
            
            // חלוקת זמן המצמוץ לשלבים
            float totalTime = _blinkDuration;
            float halfClosedTime = totalTime * 0.25f;
            float closedTime = totalTime * 0.5f;
            
            // שלב 1: עיניים חצי סגורות
            if (_eyeHalfClosedSprite != null)
            {
                _leftEyeRenderer.sprite = _eyeHalfClosedSprite;
                _rightEyeRenderer.sprite = _eyeHalfClosedSprite;
                yield return new WaitForSeconds(halfClosedTime);
            }
            
            // שלב 2: עיניים סגורות
            if (_eyeClosedSprite != null)
            {
                _leftEyeRenderer.sprite = _eyeClosedSprite;
                _rightEyeRenderer.sprite = _eyeClosedSprite;
                yield return new WaitForSeconds(closedTime);
            }
            
            // שלב 3: עיניים חצי סגורות שוב
            if (_eyeHalfClosedSprite != null)
            {
                _leftEyeRenderer.sprite = _eyeHalfClosedSprite;
                _rightEyeRenderer.sprite = _eyeHalfClosedSprite;
                yield return new WaitForSeconds(halfClosedTime);
            }
            
            // שלב 4: חזרה למצב רגיל
            _leftEyeRenderer.sprite = originalLeftSprite;
            _rightEyeRenderer.sprite = originalRightSprite;
        }
        
        // ביצוע מצמוץ יחיד
        public void Blink()
        {
            if (!_isBlinking && _blinkingEnabled)
            {
                StartCoroutine(PerformBlink());
                ResetNextBlinkTime();
            }
        }
        
        // איפוס זמן המצמוץ הבא
        private void ResetNextBlinkTime()
        {
            // חישוב אקראי של הזמן הבא
            float intervalVariance = Random.Range(-_blinkIntervalVariance, _blinkIntervalVariance);
            float nextInterval = (_averageBlinkInterval + intervalVariance) / _currentBlinkFrequencyMultiplier;
            
            // הגבלת טווח
            nextInterval = Mathf.Max(0.5f, nextInterval);
            
            _nextBlinkTime = Time.time + nextInterval;
        }
        
        // הגדרת עיניים פתוחות לחלוטין
        private void SetEyesOpen()
        {
            if (_leftEyeRenderer != null && _rightEyeRenderer != null)
            {
                if (_eyeOpenSprite != null)
                {
                    _leftEyeRenderer.sprite = _eyeOpenSprite;
                    _rightEyeRenderer.sprite = _eyeOpenSprite;
                }
            }
            
            if (_useAlphaBlink && _leftEyeAlphaOverlay != null && _rightEyeAlphaOverlay != null)
            {
                Color leftColor = _leftEyeAlphaOverlay.color;
                Color rightColor = _rightEyeAlphaOverlay.color;
                
                leftColor.a = 0f;
                rightColor.a = 0f;
                
                _leftEyeAlphaOverlay.color = leftColor;
                _rightEyeAlphaOverlay.color = rightColor;
            }
        }
        
        // הגדרת שכבות אלפא למצמוץ
        private void SetupAlphaBlinkOverlays()
        {
            if (_leftEyeRenderer == null || _rightEyeRenderer == null)
            {
                Debug.LogError("Eye renderers not assigned for alpha blink!");
                return;
            }
            
            // יצירת אובייקט חדש לכל עין
            GameObject leftOverlay = new GameObject("LeftEyeBlinkOverlay");
            GameObject rightOverlay = new GameObject("RightEyeBlinkOverlay");
            
            // הגדרת היררכיה ומיקום
            leftOverlay.transform.SetParent(_leftEyeRenderer.transform);
            rightOverlay.transform.SetParent(_rightEyeRenderer.transform);
            
            leftOverlay.transform.localPosition = Vector3.zero;
            rightOverlay.transform.localPosition = Vector3.zero;
            
            // הוספת רנדררים
            _leftEyeAlphaOverlay = leftOverlay.AddComponent<SpriteRenderer>();
            _rightEyeAlphaOverlay = rightOverlay.AddComponent<SpriteRenderer>();
            
            // העתקת מאפיינים
            _leftEyeAlphaOverlay.sprite = _eyeClosedSprite;
            _rightEyeAlphaOverlay.sprite = _eyeClosedSprite;
            
            _leftEyeAlphaOverlay.sortingOrder = _leftEyeRenderer.sortingOrder + 1;
            _rightEyeAlphaOverlay.sortingOrder = _rightEyeRenderer.sortingOrder + 1;
            
            // הגדרת צבע שחור עם אלפא 0
            Color overlayColor = Color.black;
            overlayColor.a = 0f;
            
            _leftEyeAlphaOverlay.color = overlayColor;
            _rightEyeAlphaOverlay.color = overlayColor;
        }
        
        // הגדרת ספרייטים חדשים לעיניים
        public void SetEyeSprites(Sprite openSprite, Sprite halfClosedSprite, Sprite closedSprite)
        {
            _eyeOpenSprite = openSprite;
            _eyeHalfClosedSprite = halfClosedSprite;
            _eyeClosedSprite = closedSprite;
            
            // עדכון ספרייטים נוכחיים אם לא באמצע מצמוץ
            if (!_isBlinking && _leftEyeRenderer != null && _rightEyeRenderer != null)
            {
                _leftEyeRenderer.sprite = openSprite;
                _rightEyeRenderer.sprite = openSprite;
            }
            
            // עדכון ספרייטים של שכבות האלפא
            if (_useAlphaBlink && _leftEyeAlphaOverlay != null && _rightEyeAlphaOverlay != null)
            {
                _leftEyeAlphaOverlay.sprite = closedSprite;
                _rightEyeAlphaOverlay.sprite = closedSprite;
            }
        }
    }
}