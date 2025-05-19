using UnityEngine;

namespace MinipollGame.Core
{
    // ממשק לאובייקטים שמיניפול יכול לקיים איתם אינטראקציה
    public interface IInteractable
    {
        string InteractionID { get; }
        Transform transform { get; }
        bool CanInteract(MinipollBrain minipoll);
        InteractionResult Interact(MinipollBrain minipoll);
    }

    // ממשק למאזינים לאירועי מיניפול
    public interface IMinipollListener
    {
        void OnEmotionChanged(EmotionType emotion, float intensity);
        void OnInteractionStarted(IInteractable target);
        void OnInteractionCompleted(IInteractable target, InteractionResult result);
        void OnLearningOccurred(string experienceID, bool wasPositive, float impact);
    }

    // תוצאה של אינטראקציה
    public struct InteractionResult
    {
        public bool Success;
        public float EmotionalImpact;
        public EmotionType PrimaryEmotion;
        public string ExperienceID;

        public static InteractionResult Positive(float impact, EmotionType emotion, string experienceID = "")
        {
            return new InteractionResult
            {
                Success = true,
                EmotionalImpact = Mathf.Abs(impact),
                PrimaryEmotion = emotion,
                ExperienceID = string.IsNullOrEmpty(experienceID) ? "generic_positive" : experienceID
            };
        }

        public static InteractionResult Negative(float impact, EmotionType emotion, string experienceID = "")
        {
            return new InteractionResult
            {
                Success = false,
                EmotionalImpact = Mathf.Abs(impact),
                PrimaryEmotion = emotion,
                ExperienceID = string.IsNullOrEmpty(experienceID) ? "generic_negative" : experienceID
            };
        }

        public static InteractionResult Neutral()
        {
            return new InteractionResult
            {
                Success = true,
                EmotionalImpact = 0,
                PrimaryEmotion = EmotionType.Neutral,
                ExperienceID = "neutral"
            };
        }
    }
}