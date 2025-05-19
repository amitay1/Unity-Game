using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MinipollGame.Core;
using MinipollGame.Utils; // Added this namespace to access MinipollUtils

namespace MinipollGame
{
    // מנהל עולם - אחראי על יצירת וניהול האובייקטים בעולם המשחק
    public class WorldManager : MonoBehaviour
    {
        [Header("World Objects")]
        [SerializeField] private Transform _worldContainer;
        [SerializeField] private GameObject[] _interactablePrefabs;
        [SerializeField] private GameObject[] _sceneryPrefabs;
        [SerializeField] private GameObject[] _weatherEffectPrefabs;

        [Header("Spawn Settings")]
        [SerializeField] private int _initialInteractableCount = 10;
        [SerializeField] private int _maxInteractables = 30;
        [SerializeField] private float _spawnRadius = 20f;
        [SerializeField] private float _respawnInterval = 60f;
        [SerializeField] private Transform[] _interactableZones;
        [SerializeField] private Transform[] _minipollSpawnPoints;

        [Header("Environment Settings")]
        [SerializeField] private Light _directionalLight; // for sun/moon
        [SerializeField] private Gradient _dayNightColorGradient;
        [SerializeField] private float _maxLightIntensity = 1f;
        [SerializeField] private float _minLightIntensity = 0.1f;
        [SerializeField] private Material _skyboxMaterial;

        [Header("Weather")]
        [SerializeField] private bool _enableWeather = true;
        [SerializeField] private float _weatherChangeChance = 0.3f;
        [SerializeField] private float _minWeatherDuration = 60f;
        [SerializeField] private float _maxWeatherDuration = 300f;

        // אובייקטים פעילים
        private List<GameObject> _activeInteractables = new List<GameObject>();
        private List<GameObject> _activeScenery = new List<GameObject>();
        private GameObject _activeWeatherEffect;

        // מצב עולם
        private WeatherType _currentWeather = WeatherType.Clear;
        private float _weatherEndTime;
        private float _nextRespawnTime;

        // התייחסויות
        private GameManager _gameManager;

        private void Awake()
        {
            // מציאת מנהל המשחק
            
            // if (_gameManager == null)
            // {
            //     Debug.LogError("GameManager not found!");
            // }
        }
        public Transform GetRandomMinipollSpawnPoint()
        {
            if (_minipollSpawnPoints == null || _minipollSpawnPoints.Length == 0)
                return null;

            return _minipollSpawnPoints[Random.Range(0, _minipollSpawnPoints.Length)];
        }
        // איתחול העולם
        public void Initialize()
        {
            // יצירת אובייקטים התחלתיים
            _gameManager = GameManager.Instance;
            SpawnInitialObjects();

            // עדכון זמן הרספון הבא
            _nextRespawnTime = Time.time + _respawnInterval;

            // מציאת מרכיבים אם לא הוגדרו
            if (_directionalLight == null)
            {
                _directionalLight = FindFirstObjectByType<Light>();
            }

            // רישום לאירועי זמן
            if (_gameManager != null)
            {
                _gameManager.OnTimeOfDayChanged += UpdateEnvironmentByTimeOfDay;
            }

            // איתחול שמיים
            if (_skyboxMaterial != null)
            {
                RenderSettings.skybox = _skyboxMaterial;
            }

            // קביעת מזג אוויר התחלתי
            if (_enableWeather)
            {
                SetRandomWeather();
            }
        }

        private void OnDestroy()
        {
            // ביטול הרשמה לאירועים
            if (_gameManager != null)
            {
                _gameManager.OnTimeOfDayChanged -= UpdateEnvironmentByTimeOfDay;
            }
        }

        private void Update()
        {
            // בדיקה אם צריך להחליף מזג אוויר
            if (_enableWeather && Time.time > _weatherEndTime)
            {
                SetRandomWeather();
            }

            // בדיקה אם צריך לרענן אובייקטים
            if (Time.time > _nextRespawnTime)
            {
                RespawnObjects();
                _nextRespawnTime = Time.time + _respawnInterval;
            }
        }

        #region Object Management

        // יצירת אובייקטים התחלתיים
        private void SpawnInitialObjects()
        {
            // יצירת מיכל אם לא קיים
            if (_worldContainer == null)
            {
                GameObject container = new GameObject("World_Objects");
                _worldContainer = container.transform;
            }

            // הסרת כל האובייקטים קיימים
            ClearAllWorldObjects();

            // יצירת אובייקטי אינטראקציה
            for (int i = 0; i < _initialInteractableCount; i++)
            {
                SpawnRandomInteractable();
            }

            // יצירת אובייקטי נוף
            SpawnSceneryObjects();
        }

        // ניקוי כל אובייקטי העולם
        private void ClearAllWorldObjects()
        {
            // ניקוי אובייקטי אינטראקציה
            foreach (var obj in _activeInteractables)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _activeInteractables.Clear();

            // ניקוי אובייקטי נוף
            foreach (var obj in _activeScenery)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            _activeScenery.Clear();

            // ניקוי אפקט מזג אוויר
            if (_activeWeatherEffect != null)
            {
                Destroy(_activeWeatherEffect);
                _activeWeatherEffect = null;
            }
        }

        // יצירת אובייקט אינטראקציה אקראי
        private GameObject SpawnRandomInteractable()
        {
            if (_interactablePrefabs == null || _interactablePrefabs.Length == 0)
            {
                Debug.LogWarning("No interactable prefabs defined!");
                return null;
            }

            // בחירת פריפאב אקראי
            GameObject prefab = _interactablePrefabs[Random.Range(0, _interactablePrefabs.Length)];

            // מציאת מיקום
            Vector3 spawnPosition = GetRandomSpawnPosition();

            // יצירת האובייקט
            GameObject newObject = Instantiate(prefab, spawnPosition, Quaternion.Euler(0, Random.Range(0, 360), 0), _worldContainer);

            // הוספה לרשימה
            _activeInteractables.Add(newObject);

            return newObject;
        }

        // יצירת אובייקטי נוף
        private void SpawnSceneryObjects()
        {
            if (_sceneryPrefabs == null || _sceneryPrefabs.Length == 0)
            {
                return;
            }

            // מספר אובייקטי נוף אקראי
            int sceneryCount = Random.Range(10, 30);

            for (int i = 0; i < sceneryCount; i++)
            {
                // בחירת פריפאב אקראי
                GameObject prefab = _sceneryPrefabs[Random.Range(0, _sceneryPrefabs.Length)];

                // רדיוס גדול יותר מאובייקטי אינטראקציה
                Vector3 position = GetRandomSpawnPosition(1.5f * _spawnRadius);

                // יצירת האובייקט
                GameObject sceneryObject = Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), _worldContainer);

                // הוספה לרשימה
                _activeScenery.Add(sceneryObject);

                // שינוי קנה מידה אקראי
                float scale = Random.Range(0.8f, 1.2f);
                sceneryObject.transform.localScale = new Vector3(scale, scale, scale);
            }
        }

        // מציאת מיקום הולדה אקראי
        private Vector3 GetRandomSpawnPosition(float radius = 0)
        {
            if (radius == 0)
            {
                radius = _spawnRadius;
            }

            Vector3 position;

            // שימוש באזורי הולדה אם יש
            if (_interactableZones != null && _interactableZones.Length > 0)
            {
                Transform zone = _interactableZones[Random.Range(0, _interactableZones.Length)];

                // אם זה קולידר, משתמשים בו למציאת נקודה
                Collider zoneCollider = zone.GetComponent<Collider>();

                if (zoneCollider != null)
                {
                    return MinipollUtils.GetRandomPointInZone(zoneCollider);
                }
                else
                {
                    // אחרת, מעגל מסביב לנקודה
                    Vector2 randomCircle = Random.insideUnitCircle * radius;
                    position = zone.position + new Vector3(randomCircle.x, 0, randomCircle.y);
                }
            }
            else
            {
                // נקודה אקראית במעגל סביב מרכז העולם
                Vector2 randomCircle = Random.insideUnitCircle * radius;
                position = new Vector3(randomCircle.x, 0, randomCircle.y);
            }

            // התאמה לגובה קרקע אם יש
            if (Physics.Raycast(position + Vector3.up * 10, Vector3.down, out RaycastHit hit, 20f, LayerMask.GetMask("Ground")))
            {
                position.y = hit.point.y;
            }
            else
            {
                position.y = 0;
            }

            return position;
        }

        // חידוש אובייקטים
        private void RespawnObjects()
        {
            // הסרת אובייקטים לא פעילים מהרשימה
            _activeInteractables.RemoveAll(obj => obj == null);

            // בדיקה אם יש מקום לאובייקטים נוספים
            if (_activeInteractables.Count < _maxInteractables)
            {
                // הוספת מספר אקראי של אובייקטים חדשים
                int newObjectCount = Random.Range(1, 4);

                for (int i = 0; i < newObjectCount; i++)
                {
                    if (_activeInteractables.Count < _maxInteractables)
                    {
                        SpawnRandomInteractable();
                    }
                }
            }
        }

        #endregion

        #region Environment Control

        // עדכון סביבה לפי זמן יום
        private void UpdateEnvironmentByTimeOfDay(TimeOfDay timeOfDay)
        {
            // הגדרת צבע ועוצמת אור בהתאם לזמן
            if (_directionalLight != null)
            {
                float normalizedTime = timeOfDay.NormalizedTime;

                // שינוי כיוון לאור לפי זמן היום
                float sunAngle = normalizedTime * 360f;
                _directionalLight.transform.rotation = Quaternion.Euler(sunAngle, -30f, 0f);

                // התאמת עוצמה וצבע
                _directionalLight.intensity = CalculateLightIntensity(normalizedTime);

                if (_dayNightColorGradient != null)
                {
                    _directionalLight.color = _dayNightColorGradient.Evaluate(normalizedTime);
                }
            }

            // התאמות סקייבוקס
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetFloat("_Exposure", CalculateSkyboxExposure(timeOfDay.NormalizedTime));

                // עדכון פרמטרים נוספים אם יש
                if (_skyboxMaterial.HasProperty("_SunSize"))
                {
                    float sunSize = (timeOfDay.CurrentPhase == TimeOfDay.DayPhase.Noon) ? 0.04f : 0.03f;
                    _skyboxMaterial.SetFloat("_SunSize", sunSize);
                }
            }

            // התאמת ערפל בהתאם לזמן
            float fogDensity = 0.01f;

            switch (timeOfDay.CurrentPhase)
            {
                case TimeOfDay.DayPhase.Dawn:
                    fogDensity = 0.02f;
                    break;
                case TimeOfDay.DayPhase.Night:
                case TimeOfDay.DayPhase.Midnight:
                    fogDensity = 0.03f;
                    break;
            }

            // התאמת ערפל בהתאם למזג אוויר
            if (_currentWeather == WeatherType.Foggy)
            {
                fogDensity *= 3f;
            }
            else if (_currentWeather == WeatherType.Rainy)
            {
                fogDensity *= 1.5f;
            }

            RenderSettings.fogDensity = fogDensity;
        }

        // חישוב עוצמת אור בהתאם לזמן
        private float CalculateLightIntensity(float normalizedTime)
        {
            // עקומה סינוסית לאור
            if (normalizedTime < 0.25f) // שחר עד צהריים
            {
                return Mathf.Lerp(_minLightIntensity, _maxLightIntensity, normalizedTime * 4f);
            }
            else if (normalizedTime < 0.75f) // צהריים עד ערב
            {
                float t = (normalizedTime - 0.25f) * 2f;
                return Mathf.Lerp(_maxLightIntensity, _minLightIntensity, t);
            }
            else // לילה
            {
                return _minLightIntensity;
            }
        }

        // חישוב חשיפת סקייבוקס
        private float CalculateSkyboxExposure(float normalizedTime)
        {
            if (normalizedTime < 0.25f) // שחר
            {
                return Mathf.Lerp(0.5f, 1.2f, normalizedTime * 4f);
            }
            else if (normalizedTime < 0.5f) // יום
            {
                return 1.2f;
            }
            else if (normalizedTime < 0.75f) // שקיעה
            {
                float t = (normalizedTime - 0.5f) * 4f;
                return Mathf.Lerp(1.2f, 0.5f, t);
            }
            else // לילה
            {
                return 0.5f;
            }
        }

        // קביעת מזג אוויר אקראי
        private void SetRandomWeather()
        {
            // קביעת משך מזג האוויר
            float duration = Random.Range(_minWeatherDuration, _maxWeatherDuration);
            _weatherEndTime = Time.time + duration;

            // בחירת מזג אוויר חדש
            WeatherType newWeather;

            // הסתברות גבוהה יותר למזג אוויר נקי
            if (Random.value < 0.6f)
            {
                newWeather = WeatherType.Clear;
            }
            else
            {
                // בחירה אקראית מבין שאר האפשרויות
                newWeather = (WeatherType)Random.Range(1, System.Enum.GetValues(typeof(WeatherType)).Length);
            }

            // הפעלת מזג האוויר החדש
            SetWeather(newWeather);
        }

        // הפעלת מזג אוויר ספציפי
        public void SetWeather(WeatherType weatherType)
        {
            _currentWeather = weatherType;

            // הסרת אפקט מזג אוויר קודם
            if (_activeWeatherEffect != null)
            {
                Destroy(_activeWeatherEffect);
                _activeWeatherEffect = null;
            }

            // בדיקה אם יש פריפאבים
            if (_weatherEffectPrefabs == null || _weatherEffectPrefabs.Length == 0)
            {
                return;
            }

            // התאמה בהתאם לסוג מזג האוויר
            GameObject prefab = null;

            switch (weatherType)
            {
                case WeatherType.Rainy:
                    prefab = FindWeatherPrefab("Rain");
                    break;
                case WeatherType.Snowy:
                    prefab = FindWeatherPrefab("Snow");
                    break;
                case WeatherType.Foggy:
                    prefab = FindWeatherPrefab("Fog");
                    break;
                case WeatherType.Windy:
                    prefab = FindWeatherPrefab("Wind");
                    break;
            }

            // יצירת אפקט מזג אוויר
            if (prefab != null)
            {
                _activeWeatherEffect = Instantiate(prefab, Vector3.zero, Quaternion.identity, _worldContainer);
            }

            // התאמת הגדרות נוספות
            switch (weatherType)
            {
                case WeatherType.Foggy:
                    RenderSettings.fogDensity = 0.05f;
                    break;
                case WeatherType.Clear:
                    RenderSettings.fogDensity = 0.01f;
                    break;
            }

            // הפעלת אירוע עולמי
            string weatherEventId = "weather_" + weatherType.ToString().ToLower();
            EventSystem.TriggerWorldEvent(weatherEventId, Vector3.zero);

            Debug.Log($"Weather changed to {weatherType}. Duration: {(_weatherEndTime - Time.time):F0} seconds");
        }

        // מציאת פריפאב מזג אוויר לפי שם
        private GameObject FindWeatherPrefab(string nameContains)
        {
            foreach (var prefab in _weatherEffectPrefabs)
            {
                if (prefab != null && prefab.name.Contains(nameContains))
                {
                    return prefab;
                }
            }

            // אם לא נמצא התאמה ספציפית, מחזיר את הראשון
            return _weatherEffectPrefabs.Length > 0 ? _weatherEffectPrefabs[0] : null;
        }

        #endregion

        #region Public Methods

        // עדכון עולם ליום חדש
        public void UpdateWorldForNewDay(int dayNumber)
        {
            // רענון חלק מהאובייקטים באופן אקראי
            int objectsToRefresh = Random.Range(1, 5);

            for (int i = 0; i < objectsToRefresh; i++)
            {
                if (_activeInteractables.Count > 0)
                {
                    int indexToRemove = Random.Range(0, _activeInteractables.Count);

                    if (_activeInteractables[indexToRemove] != null)
                    {
                        Destroy(_activeInteractables[indexToRemove]);
                    }

                    _activeInteractables.RemoveAt(indexToRemove);

                    // יצירת אובייקט חדש
                    SpawnRandomInteractable();
                }
            }

            // שינוי מזג אוויר בהסתברות נתונה
            if (Random.value < _weatherChangeChance)
            {
                SetRandomWeather();
            }

            Debug.Log($"World updated for day {dayNumber}");
        }

        // מחזיר מזג אוויר נוכחי
        public WeatherType GetCurrentWeather()
        {
            return _currentWeather;
        }

        // מחזיר את מספר האובייקטים הפעילים
        public int GetActiveInteractablesCount()
        {
            // ניקוי רשימה מאובייקטים שנהרסו
            _activeInteractables.RemoveAll(item => item == null);
            return _activeInteractables.Count;
        }

        // מציאת אובייקט להתעסקות קרוב למיקום
        public GameObject FindNearestInteractable(Vector3 position, float maxDistance = 10f)
        {
            GameObject nearest = null;
            float nearestDistance = maxDistance;

            foreach (var obj in _activeInteractables)
            {
                if (obj != null)
                {
                    float distance = Vector3.Distance(obj.transform.position, position);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearest = obj;
                    }
                }
            }

            return nearest;
        }

        #endregion
    }

    // סוגי מזג אוויר
    public enum WeatherType
    {
        Clear,
        Rainy,
        Snowy,
        Foggy,
        Windy
    }
}
