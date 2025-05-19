using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Core;
using MinipollGame.Utils;

namespace MinipollGame
{
    // מבקר תנועה - אחראי על כל תנועות המיניפול
    [RequireComponent(typeof(NavMeshAgent))]
    public class MinipollMovementController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _baseSpeed = 2f;
        [SerializeField] private float _runMultiplier = 1.8f;
        [SerializeField] private float _randomWanderRadius = 5f;
        [SerializeField] private float _exploreRadius = 10f;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private float _rotationSpeed = 10f;
        
        [Header("Wander Settings")]
        [SerializeField] private float _minWanderDelay = 2f;
        [SerializeField] private float _maxWanderDelay = 8f;
        [SerializeField] private int _maxPathfindingAttempts = 5;
        
        [Header("Reference")]
        [SerializeField] private Transform _bodyTransform;
        
        // התייחסויות למערכות
        private NavMeshAgent _navAgent;
        private MinipollEmotionalState _emotionalState;
        private MinipollLifeState _lifeState;
        private MinipollMovementState _movementState = MinipollMovementState.Idle;
        
        // מצב תנועה
        private bool _isMoving = false;
        private Vector3 _currentDestination;
        private float _lastWanderTime;
        private Coroutine _wanderCoroutine;
        
        // אירועים
        public event System.Action<MinipollMovementState> OnMovementStateChanged;
        public event System.Action OnDestinationReached;
        public event System.Action<Vector3> OnNewDestinationSet;
        
        // Properties פומביות
        public bool IsMoving => _isMoving;
        public Vector3 CurrentDestination => _currentDestination;
        public bool HasDestination => _isMoving;
        public MinipollMovementState MovementState => _movementState;
        
        public float BaseSpeed
        {
            get => _baseSpeed;
            set
            {
                _baseSpeed = value;
                UpdateMovementSpeed();
            }
        }
        
        private void Awake()
        {
            _navAgent = GetComponent<NavMeshAgent>();
            _emotionalState = GetComponent<MinipollEmotionalState>();
            
            // אם לא נקבע Transform גוף, משתמשים בזה של האובייקט
            if (_bodyTransform == null)
            {
                _bodyTransform = transform;
            }
        }
        
        private void Start()
        {
            // הגדרות התחלתיות
            _navAgent.stoppingDistance = _stoppingDistance;
            _navAgent.speed = _baseSpeed;
            _lastWanderTime = Time.time;
            
            if (_emotionalState != null)
            {
                _emotionalState.OnEmotionChanged += UpdateMovementByEmotion;
            }
        }
        
        private void OnDestroy()
        {
            if (_emotionalState != null)
            {
                _emotionalState.OnEmotionChanged -= UpdateMovementByEmotion;
            }
            
            if (_wanderCoroutine != null)
            {
                StopCoroutine(_wanderCoroutine);
            }
        }
        
        private void Update()
        {
            // עדכון תנועה בהתאם למצב
            UpdateMovement();
        }
        
        // עדכון מהירות תנועה
        private void UpdateMovementSpeed()
        {
            float adjustedSpeed = _baseSpeed;
            
            // התאמה לפי הרגש
            if (_emotionalState != null)
            {
                EmotionType dominantEmotion = _emotionalState.DominantEmotion;
                float intensity = _emotionalState.GetEmotionIntensity(dominantEmotion);
                
                adjustedSpeed = MinipollUtils.AdjustSpeedByEmotion(_baseSpeed, dominantEmotion, intensity);
            }
            
            // התאמה למצב תנועה
            if (_movementState == MinipollMovementState.Running)
            {
                adjustedSpeed *= _runMultiplier;
            }
            
            // עדכון מהירות ב-NavMeshAgent
            _navAgent.speed = adjustedSpeed;
        }
        
        // עדכון תנועה בהתאם לרגש
        private void UpdateMovementByEmotion(EmotionType emotion, float intensity)
        {
            // עדכון מהירות
            UpdateMovementSpeed();
            
            // במצבי קיצון של פחד, מיניפול יכול לברוח או להתחבא
            if (emotion == EmotionType.Scared && intensity > 0.7f)
            {
                // בריחה אקראית
                if (Random.value < 0.5f)
                {
                    SetMovementState(MinipollMovementState.Running);
                    RunToSafeArea();
                }
                else
                {
                    Hide();
                }
            }
            // אם שמח מאוד, יכול לרקוד/לקפוץ
            else if (emotion == EmotionType.Happy && intensity > 0.8f)
            {
                if (Random.value < 0.3f)
                {
                    Dance();
                }
            }
        }
        
        // התחלת תנועה ליעד
        public bool SetDestination(Vector3 destination)
        {
            if (_lifeState == MinipollLifeState.Asleep || _lifeState == MinipollLifeState.Hibernating)
            {
                return false;
            }
            
            // בדיקה אם היעד נגיש
            NavMeshPath path = new NavMeshPath();
            bool validPath = NavMesh.CalculatePath(transform.position, destination, NavMesh.AllAreas, path);
            
            if (validPath && path.status == NavMeshPathStatus.PathComplete)
            {
                _navAgent.SetDestination(destination);
                _currentDestination = destination;
                _isMoving = true;
                
                // קביעת מצב תנועה מתאים
                if (_movementState != MinipollMovementState.Running)
                {
                    SetMovementState(MinipollMovementState.Walking);
                }
                
                OnNewDestinationSet?.Invoke(destination);
                return true;
            }
            
            return false;
        }
        
        // עצירת תנועה
        public void StopMoving()
        {
            if (_isMoving)
            {
                _navAgent.ResetPath();
                _isMoving = false;
                SetMovementState(MinipollMovementState.Idle);
            }
            
            if (_wanderCoroutine != null)
            {
                StopCoroutine(_wanderCoroutine);
                _wanderCoroutine = null;
            }
        }
        
        // תנועה אקראית
        public void WanderRandomly()
        {
            if (_wanderCoroutine != null)
            {
                StopCoroutine(_wanderCoroutine);
            }
            
            _wanderCoroutine = StartCoroutine(WanderCoroutine());
        }
        
        // חקירת אזור חדש
        public void ExploreNewArea()
        {
            // ניסיון למצוא מקום רחוק יותר
            for (int i = 0; i < _maxPathfindingAttempts; i++)
            {
                Vector3 randomDirection = Random.insideUnitSphere * _exploreRadius;
                randomDirection += transform.position;
                NavMeshHit hit;
                
                if (NavMesh.SamplePosition(randomDirection, out hit, _exploreRadius, NavMesh.AllAreas))
                {
                    SetDestination(hit.position);
                    return;
                }
            }
            
            // אם לא הצליח למצוא יעד, משתמש בתנועה אקראית רגילה
            WanderRandomly();
        }
        
        // בריחה לאזור בטוח
        private void RunToSafeArea()
        {
            // מציאת כיוון הפוך למקור הפחד - לצורך פשטות נשתמש בכיוון אקראי
            Vector3 randomDirection = Random.insideUnitSphere.normalized * _exploreRadius;
            NavMeshHit hit;
            
            if (NavMesh.SamplePosition(transform.position + randomDirection, out hit, _exploreRadius, NavMesh.AllAreas))
            {
                _movementState = MinipollMovementState.Running;
                UpdateMovementSpeed();
                SetDestination(hit.position);
            }
        }
        
        // התחבאות
        private void Hide()
        {
            SetMovementState(MinipollMovementState.Hiding);
            StopMoving();
        }
        
        // ריקוד
        private void Dance()
        {
            SetMovementState(MinipollMovementState.Dancing);
            StopMoving();
        }
        
        // קביעת מצב תנועה
        private void SetMovementState(MinipollMovementState newState)
        {
            if (_movementState != newState)
            {
                MinipollMovementState oldState = _movementState;
                _movementState = newState;
                
                // הפעלת אירוע
                OnMovementStateChanged?.Invoke(newState);
                
                // הפעלת אירוע גלובלי
                EventSystem.TriggerMovementChange(GetComponent<MinipollBrain>(), newState);
                
                // עדכון מהירות
                UpdateMovementSpeed();
            }
        }
        
        // עדכון תנועה ובדיקה אם הגיע ליעד
        private void UpdateMovement()
        {
            if (_isMoving)
            {
                if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
                {
                    _isMoving = false;
                    SetMovementState(MinipollMovementState.Idle);
                    OnDestinationReached?.Invoke();
                }
            }
        }
        
        // סיבוב גוף מיניפול לכיוון התנועה
        private void LateUpdate()
        {
            if (_isMoving && _navAgent.velocity.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_navAgent.velocity.normalized);
                _bodyTransform.rotation = Quaternion.Slerp(_bodyTransform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }
        }
        
        // קורוטינה לתנועה אקראית
        private IEnumerator WanderCoroutine()
        {
            while (true)
            {
                // רק אם במצב ער
                if (_lifeState != MinipollLifeState.Asleep && _lifeState != MinipollLifeState.Hibernating)
                {
                    for (int i = 0; i < _maxPathfindingAttempts; i++)
                    {
                        Vector3 randomDirection = Random.insideUnitSphere * _randomWanderRadius;
                        randomDirection += transform.position;
                        NavMeshHit hit;
                        
                        if (NavMesh.SamplePosition(randomDirection, out hit, _randomWanderRadius, NavMesh.AllAreas))
                        {
                            SetDestination(hit.position);
                            _lastWanderTime = Time.time;
                            break;
                        }
                    }
                }
                
                // המתנה בין תנועות
                float delay = Random.Range(_minWanderDelay, _maxWanderDelay);
                
                // התאמת ההמתנה למצב רגשי
                if (_emotionalState != null)
                {
                    if (_emotionalState.DominantEmotion == EmotionType.Curious)
                    {
                        delay *= 0.7f; // פחות המתנה עבור מיניפול סקרן
                    }
                    else if (_emotionalState.DominantEmotion == EmotionType.Tired)
                    {
                        delay *= 1.5f; // יותר המתנה עבור מיניפול עייף
                    }
                }
                
                yield return new WaitForSeconds(delay);
            }
        }
        
        // עדכון מצב החיים
        public void SetLifeState(MinipollLifeState state)
        {
            _lifeState = state;
            
            if (state == MinipollLifeState.Asleep || state == MinipollLifeState.Hibernating)
            {
                StopMoving();
            }
        }
    }
}