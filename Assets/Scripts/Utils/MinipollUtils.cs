using UnityEngine;
using MinipollGame.Core;
using System.Collections.Generic;
using System.Linq;

namespace MinipollGame.Utils
{
    public static class MinipollUtils
    {
        // מצמוץ עם אינטרפולציה טבעית - מפתח מצב עיניים (0-1) לערך אלפא
        public static float GetEyeBlinkAlpha(float blinkState)
        {
            // curve for natural blink animation (0 = open, 1 = closed)
            return Mathf.SmoothStep(0, 1, blinkState);
        }

        // חישוב מרחק תחת התחשבות בגבהים
        public static float GetPathfindingDistance(Vector3 start, Vector3 end)
        {
            float directDistance = Vector3.Distance(start, end);
            float heightDifference = Mathf.Abs(start.y - end.y);
            
            // If height difference is significant, add more weight to it
            if (heightDifference > 1.0f)
            {
                return directDistance + heightDifference * 0.5f;
            }
            
            return directDistance;
        }

        // קבלת רגש אקראי
        public static EmotionType GetRandomEmotion()
        {
            System.Array values = System.Enum.GetValues(typeof(EmotionType));
            return (EmotionType)values.GetValue(Random.Range(0, values.Length));
        }
        
        // קבלת עוצמת רגש אקראית
        public static float GetRandomEmotionIntensity()
        {
            // Usually emotions start mildly and grow stronger
            return Mathf.Pow(Random.value, 2f); // Bias towards lower values
        }
        
        // עיגול מספר לדיוק נתון
        public static float RoundToPrecision(float value, int decimals)
        {
            float multiplier = Mathf.Pow(10f, decimals);
            return Mathf.Round(value * multiplier) / multiplier;
        }
        
        // מיזוג בין שני רגשות לחישוב רגש משולב
        public static EmotionType BlendEmotions(EmotionType primary, EmotionType secondary, float primaryWeight = 0.7f)
        {
            // This is a simplified model of emotion blending that will depend on your game's emotional model
            // For now, we'll just return the primary emotion if weight is higher than threshold
            if (primaryWeight > 0.65f)
                return primary;
                
            // For certain combinations, return specific blended emotions
            if ((primary == EmotionType.Happy && secondary == EmotionType.Curious) ||
                (primary == EmotionType.Curious && secondary == EmotionType.Happy))
                return EmotionType.Excited;
                
            if ((primary == EmotionType.Sad && secondary == EmotionType.Tired) ||
                (primary == EmotionType.Tired && secondary == EmotionType.Sad))
                return EmotionType.Tired;
                
            // Default to secondary for low primary weight
            return primaryWeight < 0.35f ? secondary : primary;
        }
        
        // התאמת מהירות תנועה לפי מצב רגשי
        public static float AdjustSpeedByEmotion(float baseSpeed, EmotionType emotion, float intensity)
        {
            switch (emotion)
            {
                case EmotionType.Happy:
                    return baseSpeed * (1 + intensity * 0.3f);
                case EmotionType.Excited:
                    return baseSpeed * (1 + intensity * 0.5f);
                case EmotionType.Scared:
                    return baseSpeed * (1 + intensity * 0.7f); // Run faster when scared
                case EmotionType.Tired:
                    return baseSpeed * (1 - intensity * 0.6f);
                case EmotionType.Sad:
                    return baseSpeed * (1 - intensity * 0.4f);
                default:
                    return baseSpeed;
            }
        }
        
        // פונקציה למציאת נקודה אקראית בתוך אזור נתון
        public static Vector3 GetRandomPointInZone(Collider zone)
        {
            Bounds bounds = zone.bounds;
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(bounds.min.y, bounds.max.y),
                Random.Range(bounds.min.z, bounds.max.z)
            );
            
            // אם יש מערכת NavMesh, התאם את הנקודה אליה
            if (UnityEngine.AI.NavMesh.SamplePosition(randomPoint, out UnityEngine.AI.NavMeshHit hit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return randomPoint;
        }
    }
}