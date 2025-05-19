using UnityEngine;
using System.Collections.Generic;
using MinipollGame.Core;

namespace MinipollGame.ScriptableObjects
{
    [CreateAssetMenu(fileName = "NewMinipollType", menuName = "Minipoll/Minipoll Type")]
    public class MinipollData : ScriptableObject
    {
        [Header("Basic Information")]
        public string minipollName = "Standard Minipoll";
        public string description = "A basic Minipoll creature.";
        public Sprite icon;
        
        [Header("Visual Settings")]
        public Color bodyColor = Color.white;
        public Color eyeColor = new Color(0.2f, 0.2f, 0.8f);
        [Range(0.5f, 2f)]
        public float sizeMultiplier = 1f;
        
        [Header("Behavior Parameters")]
        [Range(0.1f, 5f)]
        public float baseMovementSpeed = 2f;
        [Range(0.1f, 10f)]
        public float curiosityFactor = 1f;
        [Range(0.1f, 10f)]
        public float socialFactor = 1f;
        [Range(0.1f, 10f)]
        public float fearFactor = 1f;
        
        [Header("Blinking Settings")]
        [Range(0.5f, 10f)]
        public float averageBlinkInterval = 3f;
        [Range(0f, 2f)]
        public float blinkIntervalVariance = 0.5f;
        [Range(0.1f, 2f)]
        public float blinkDuration = 0.15f;
        
        [Header("Learning Parameters")]
        [Range(0.01f, 1f)]
        public float learningRate = 0.1f;
        [Range(0.001f, 0.1f)]
        public float forgettingRate = 0.01f;
        public int maxMemories = 50;
        
        [Header("Emotional Settings")]
        public float emotionalResponseFactor; // Renamed from someField to give it a meaningful name

        [System.Serializable]
        public class EmotionalTendency
        {
            public EmotionType emotion;
            [Range(0f, 2f)]
            public float baseValue;
            [Range(0f, 2f)]
            public float responseMultiplier;
            [Range(0.001f, 0.2f)]
            public float decayRate;
        }
        
        public List<EmotionalTendency> emotionalTendencies = new List<EmotionalTendency>
        {
            new EmotionalTendency { emotion = EmotionType.Happy, baseValue = 0.3f, responseMultiplier = 1f, decayRate = 0.02f },
            new EmotionalTendency { emotion = EmotionType.Sad, baseValue = 0.1f, responseMultiplier = 1f, decayRate = 0.01f },
            new EmotionalTendency { emotion = EmotionType.Curious, baseValue = 0.4f, responseMultiplier = 1.2f, decayRate = 0.03f },
            new EmotionalTendency { emotion = EmotionType.Tired, baseValue = 0.1f, responseMultiplier = 0.8f, decayRate = 0.005f },
            new EmotionalTendency { emotion = EmotionType.Scared, baseValue = 0.1f, responseMultiplier = 1.5f, decayRate = 0.04f }
        };
        
        [System.Serializable]
        public class NeedSettings
        {
            public string needName;
            [Range(0.001f, 0.1f)]
            public float decayRate;
            public float satisfactionThreshold = 0.8f;
            public float criticalThreshold = 0.2f;
            public EmotionType linkedEmotion;
        }
        
        [Header("Need Settings")]
        public List<NeedSettings> needs = new List<NeedSettings>
        {
            new NeedSettings { needName = "Energy", decayRate = 0.005f, linkedEmotion = EmotionType.Tired },
            new NeedSettings { needName = "Happiness", decayRate = 0.003f, linkedEmotion = EmotionType.Sad }
        };
        
        [Header("Behavioral Tendencies")]
        [Range(0f, 1f)]
        public float explorationTendency = 0.5f;
        [Range(0f, 1f)]
        public float socializingTendency = 0.5f;
        [Range(0f, 1f)]
        public float restingTendency = 0.3f;
        
        // מתודה להתאמת מיניפול חדש
        public void ApplyToMinipoll(GameObject minipollObject)
        {
            var brain = minipollObject.GetComponent<MinipollBrain>();
            if (brain != null)
            {
                // Apply visual settings
                var visualController = minipollObject.GetComponent<MinipollVisualController>();
                if (visualController != null)
                {
                    visualController.SetBodyColor(bodyColor);
                    visualController.SetEyeColor(eyeColor);
                    visualController.SetScale(sizeMultiplier);
                }
                
                // Apply movement settings
                var movementController = minipollObject.GetComponent<MinipollMovementController>();
                if (movementController != null)
                {
                    movementController.BaseSpeed = baseMovementSpeed;
                }
                
                // Apply blink settings
                var blinkController = minipollObject.GetComponent<MinipollBlinkController>();
                if (blinkController != null)
                {
                    blinkController.SetBlinkParameters(averageBlinkInterval, blinkIntervalVariance, blinkDuration);
                }
                
                // Apply emotional settings
                var emotionalState = minipollObject.GetComponent<MinipollEmotionalState>();
                if (emotionalState != null)
                {
                    foreach (var tendency in emotionalTendencies)
                    {
                        emotionalState.SetEmotionalTendency(tendency.emotion, tendency.baseValue, tendency.responseMultiplier, tendency.decayRate);
                    }
                }
                
                // Apply learning settings
                var learningSystem = minipollObject.GetComponent<MinipollLearningSystem>();
                if (learningSystem != null)
                {
                    // Use reflection to set private fields
                    System.Type type = learningSystem.GetType();
                    
                    // Set maximumExperienceValue to learningRate
                    var maximumExperienceValueField = type.GetField("maximumExperienceValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (maximumExperienceValueField != null)
                    {
                        maximumExperienceValueField.SetValue(learningSystem, learningRate);
                    }
                    
                    // Set experienceDecayRate to forgettingRate
                    var experienceDecayRateField = type.GetField("experienceDecayRate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (experienceDecayRateField != null)
                    {
                        experienceDecayRateField.SetValue(learningSystem, forgettingRate);
                    }
                    
                    // Set maxExperienceEntries to maxMemories
                    var maxExperienceEntriesField = type.GetField("maxExperienceEntries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (maxExperienceEntriesField != null)
                    {
                        maxExperienceEntriesField.SetValue(learningSystem, maxMemories);
                    }
                }
            }
        }
    }
}