using System;
using System.Collections.Generic;
using UnityEngine;

namespace MinipollGame.Core
{
    // מערכת אירועים מרכזית
    public class EventSystem : MonoBehaviour
    {
        // אירועים ברמת העולם
        public static event Action<Vector3> OnInterestingLocationDiscovered;
        public static event Action<MinipollBrain, MinipollBrain> OnMinipollsFirstMeet;
        public static event Action<string, Vector3> OnWorldEventTriggered;
        public static event Action<TimeOfDay> OnTimeOfDayChanged;

        // אירועים ברמת היצור (מיניפול)
        public static event Action<MinipollBrain, EmotionType, float> OnMinipollEmotionChanged;
        public static event Action<MinipollBrain, IInteractable, InteractionResult> OnMinipollInteraction;
        public static event Action<MinipollBrain, MinipollLifeState> OnMinipollStateChanged;
        public static event Action<MinipollBrain, MinipollMovementState> OnMinipollMovementChanged;

        // מתודות להפעלת אירועים
        public static void TriggerInterestingLocation(Vector3 location)
        {
            OnInterestingLocationDiscovered?.Invoke(location);
        }

        public static void TriggerMinipollsMeeting(MinipollBrain minipoll1, MinipollBrain minipoll2)
        {
            OnMinipollsFirstMeet?.Invoke(minipoll1, minipoll2);
        }

        public static void TriggerWorldEvent(string eventId, Vector3 location)
        {
            OnWorldEventTriggered?.Invoke(eventId, location);
        }

        public static void TriggerTimeOfDayChange(TimeOfDay timeOfDay)
        {
            OnTimeOfDayChanged?.Invoke(timeOfDay);
        }

        public static void TriggerEmotionChange(MinipollBrain minipoll, EmotionType emotion, float intensity)
        {
            OnMinipollEmotionChanged?.Invoke(minipoll, emotion, intensity);
        }

        public static void TriggerInteraction(MinipollBrain minipoll, IInteractable target, InteractionResult result)
        {
            OnMinipollInteraction?.Invoke(minipoll, target, result);
        }

        public static void TriggerStateChange(MinipollBrain minipoll, MinipollLifeState state)
        {
            OnMinipollStateChanged?.Invoke(minipoll, state);
        }

        public static void TriggerMovementChange(MinipollBrain minipoll, MinipollMovementState movement)
        {
            OnMinipollMovementChanged?.Invoke(minipoll, movement);
        }
    }
}