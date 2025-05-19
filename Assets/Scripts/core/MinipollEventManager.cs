using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinipollGame.Core
{
    // מנהל אירועים שניתן להצמיד לאובייקט במשחק
    public class MinipollEventManager : MonoBehaviour
    {
        // יצירת מופע סינגלטוני
        public static MinipollEventManager Instance { get; private set; }

        // אירועים ברמת העולם
        public event Action<Vector3> OnInterestingLocationDiscovered;
        public event Action<MinipollBrain, MinipollBrain> OnMinipollsFirstMeet;
        public event Action<string, Vector3> OnWorldEventTriggered;
        public event Action<TimeOfDay> OnTimeOfDayChanged;

        // אירועים ברמת היצור (מיניפול)
        public event Action<MinipollBrain, EmotionType, float> OnMinipollEmotionChanged;
        public event Action<MinipollBrain, IInteractable, InteractionResult> OnMinipollInteraction;
        public event Action<MinipollBrain, MinipollLifeState> OnMinipollStateChanged;
        public event Action<MinipollBrain, MinipollMovementState> OnMinipollMovementChanged;

        private void Awake()
        {
            // וידוא שיש רק מופע אחד של מנהל האירועים
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // מתודות להפעלת אירועים
        public void TriggerInterestingLocation(Vector3 location)
        {
            OnInterestingLocationDiscovered?.Invoke(location);
        }

        public void TriggerMinipollsMeeting(MinipollBrain minipoll1, MinipollBrain minipoll2)
        {
            OnMinipollsFirstMeet?.Invoke(minipoll1, minipoll2);
        }

        public void TriggerWorldEvent(string eventId, Vector3 location)
        {
            OnWorldEventTriggered?.Invoke(eventId, location);
        }

        public void TriggerTimeOfDayChange(TimeOfDay timeOfDay)
        {
            OnTimeOfDayChanged?.Invoke(timeOfDay);
        }

        public void TriggerEmotionChange(MinipollBrain minipoll, EmotionType emotion, float intensity)
        {
            OnMinipollEmotionChanged?.Invoke(minipoll, emotion, intensity);
        }

        public void TriggerInteraction(MinipollBrain minipoll, IInteractable target, InteractionResult result)
        {
            OnMinipollInteraction?.Invoke(minipoll, target, result);
        }

        public void TriggerStateChange(MinipollBrain minipoll, MinipollLifeState state)
        {
            OnMinipollStateChanged?.Invoke(minipoll, state);
        }

        public void TriggerMovementChange(MinipollBrain minipoll, MinipollMovementState movement)
        {
            OnMinipollMovementChanged?.Invoke(minipoll, movement);
        }
    }

    // מבנה לתיאור זמן היום - העברתי אותו לקובץ נפרד לשמירה על סדר
}