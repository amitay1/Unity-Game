using UnityEngine;
using System.Collections.Generic;
using MinipollGame.Core;

namespace MinipollGame.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewInteraction", menuName = "Minipoll/Interaction Data")]
    public class InteractionData : ScriptableObject
    {
        [Header("Basic Information")]
        public string interactionId;
        
        [TextArea(3, 5)]
        public string description;
        public Sprite icon;
        
        [Header("Interaction Settings")]
        public string displayName;
        [Range(0f, 1f)]
        public float baseSuccessChance = 0.8f;
        public bool requiresLearning = false;
        public bool isRepeatable = true;
        
        
        [System.Serializable]
        public class EmotionalImpact
        {
            public EmotionType emotion;
            [Range(-1f, 1f)]
            public float impactOnSuccess = 0.2f;
            [Range(-1f, 1f)]
            public float impactOnFailure = -0.2f;
        }
        
        public List<EmotionalImpact> emotionalImpacts = new List<EmotionalImpact>();
        
        [Header("Social Impact")]
        public float someField;
        [Range(-1f, 1f)]
        public float trustImpactOnSuccess = 0.1f;
        [Range(-1f, 1f)]
        public float trustImpactOnFailure = -0.05f;
        [Range(-1f, 1f)]
        public float friendshipImpactOnSuccess = 0.1f;
        [Range(-1f, 1f)]
        public float friendshipImpactOnFailure = -0.05f;
        
        [Header("Learning Impact")]
        [Range(0f, 1f)]
        public float learningValueOnSuccess = 0.2f;
        [Range(0f, 1f)]
        public float learningValueOnFailure = 0.1f;
        
        [Header("Animation Triggers")]
        public string successAnimationTrigger;
        public string failureAnimationTrigger;
        
        [Header("Particle Effects")]
        public GameObject successParticlesPrefab;
        public GameObject failureParticlesPrefab;
        
        [Header("Sound Effects")]
        public AudioClip successSound;
        public AudioClip failureSound;
        
        // מתודה המחשבת את תוצאת האינטראקציה
        public InteractionResult CalculateInteractionResult(MinipollBrain minipoll)
        {
            bool success = Random.value <= GetAdjustedSuccessChance(minipoll);
            
            if (success)
            {
                // מציאת הרגש עם ההשפעה החיובית הגדולה ביותר
                EmotionType primaryEmotion = EmotionType.Happy; // ברירת מחדל
                float highestImpact = 0;
                
                foreach (var impact in emotionalImpacts)
                {
                    if (impact.impactOnSuccess > highestImpact)
                    {
                        highestImpact = impact.impactOnSuccess;
                        primaryEmotion = impact.emotion;
                    }
                }
                
                return InteractionResult.Positive(
                    highestImpact > 0 ? highestImpact : 0.1f, 
                    primaryEmotion, 
                    interactionId
                );
            }
            else
            {
                // מציאת הרגש עם ההשפעה השלילית הגדולה ביותר
                EmotionType primaryEmotion = EmotionType.Sad; // ברירת מחדל
                float highestNegativeImpact = 0;
                
                foreach (var impact in emotionalImpacts)
                {
                    if (impact.impactOnFailure < 0 && Mathf.Abs(impact.impactOnFailure) > highestNegativeImpact)
                    {
                        highestNegativeImpact = Mathf.Abs(impact.impactOnFailure);
                        primaryEmotion = impact.emotion;
                    }
                }
                
                return InteractionResult.Negative(
                    highestNegativeImpact > 0 ? highestNegativeImpact : 0.1f, 
                    primaryEmotion, 
                    interactionId
                );
            }
        }
        
        // חישוב סיכויי הצלחה מותאמים לניסיון קודם
        private float GetAdjustedSuccessChance(MinipollBrain minipoll)
        {
            float adjustedChance = baseSuccessChance;
            
            // התאמה על פי ניסיון קודם
            if (minipoll.LearningSystem != null)
            {
                float experienceValue = minipoll.LearningSystem.EvaluateExperience(interactionId);
                
                // אם יש ניסיון חיובי, מגדיל את הסיכוי להצלחה
                if (experienceValue > 0)
                {
                    adjustedChance += experienceValue * 0.2f;
                }
                // אם יש ניסיון שלילי, מפחית במעט את הסיכוי להצלחה
                else if (experienceValue < 0)
                {
                    adjustedChance += experienceValue * 0.1f;
                }
            }
            
            // התאמה לפי מצב רגשי
            if (minipoll.EmotionalState != null)
            {
                EmotionType dominantEmotion = minipoll.EmotionalState.DominantEmotion;
                float intensity = minipoll.EmotionalState.GetEmotionIntensity(dominantEmotion);
                
                switch (dominantEmotion)
                {
                    case EmotionType.Happy:
                        adjustedChance += intensity * 0.1f;
                        break;
                    case EmotionType.Excited:
                        adjustedChance += intensity * 0.15f;
                        break;
                    case EmotionType.Sad:
                        adjustedChance -= intensity * 0.1f;
                        break;
                    case EmotionType.Scared:
                        adjustedChance -= intensity * 0.2f;
                        break;
                    case EmotionType.Tired:
                        adjustedChance -= intensity * 0.15f;
                        break;
                }
            }
            
            // וידוא שהתוצאה נמצאת בטווח תקין
            return Mathf.Clamp01(adjustedChance);
        }
    }
}