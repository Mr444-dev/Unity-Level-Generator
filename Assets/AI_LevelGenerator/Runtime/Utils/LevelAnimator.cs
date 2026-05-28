using System.Collections.Generic;
using UnityEngine;
#if DOTWEEN
using DG.Tweening;
#endif

namespace AILevelGenerator.Utils
{
    using Core;
    
    /// <summary>
    /// Provides animated level instantiation and room reveal effects using DoTween.
    /// Falls back to instant placement if DoTween is not installed.
    /// </summary>
    public static class LevelAnimator
    {
        #region Configuration
        
        /// <summary>
        /// Duration of room reveal animation in seconds.
        /// </summary>
        public static float RevealDuration { get; set; } = 0.5f;
        
        /// <summary>
        /// Delay between each room reveal.
        /// </summary>
        public static float RevealDelay { get; set; } = 0.05f;
        
        /// <summary>
        /// Scale multiplier during reveal animation (starts small, scales up).
        /// </summary>
        public static float StartScale { get; set; } = 0f;
        
        /// <summary>
        /// Vertical offset during reveal animation (drops from above).
        /// </summary>
        public static float DropHeight { get; set; } = 3f;
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Animates level reveal from entrance to exit.
        /// </summary>
        public static void AnimateReveal(GameObject levelRoot, LevelData levelData)
        {
            if (levelRoot == null || levelData == null) return;
            
#if DOTWEEN
            AnimateRevealDoTween(levelRoot, levelData);
#else
            // Instant reveal fallback
            levelRoot.SetActive(true);
#endif
        }
        
        /// <summary>
        /// Animates a single room appearing.
        /// </summary>
        public static void AnimateRoom(GameObject roomObject, float delay = 0f)
        {
            if (roomObject == null) return;
            
#if DOTWEEN
            AnimateRoomDoTween(roomObject, delay);
#else
            roomObject.SetActive(true);
#endif
        }
        
        /// <summary>
        /// Animates room destruction/collapse.
        /// </summary>
        public static void AnimateDestroy(GameObject roomObject, float duration = 0.5f)
        {
            if (roomObject == null) return;
            
#if DOTWEEN
            AnimateDestroyDoTween(roomObject, duration);
#else
            Object.Destroy(roomObject);
#endif
        }
        
        /// <summary>
        /// Pulses a room to highlight it.
        /// </summary>
        public static void PulseRoom(GameObject roomObject, int pulseCount = 3)
        {
            if (roomObject == null) return;
            
#if DOTWEEN
            PulseRoomDoTween(roomObject, pulseCount);
#endif
        }
        
        /// <summary>
        /// Shows connection between two rooms with animated line.
        /// </summary>
        public static void AnimateConnection(Vector3 from, Vector3 to, LineRenderer lineRenderer, float duration = 0.5f)
        {
            if (lineRenderer == null) return;
            
#if DOTWEEN
            AnimateConnectionDoTween(from, to, lineRenderer, duration);
#else
            lineRenderer.SetPositions(new[] { from, to });
#endif
        }
        
        #endregion
        
#if DOTWEEN
        #region DoTween Implementations
        
        private static void AnimateRevealDoTween(GameObject levelRoot, LevelData levelData)
        {
            // Get all room transforms
            var rooms = new List<Transform>();
            foreach (Transform child in levelRoot.transform)
            {
                rooms.Add(child);
                child.gameObject.SetActive(false);
            }
            
            // Sort by distance from entrance
            if (levelData.EntranceRoom != null)
            {
                var entrancePos = levelData.EntranceRoom.WorldPosition;
                rooms.Sort((a, b) => 
                    Vector3.Distance(a.position, entrancePos)
                        .CompareTo(Vector3.Distance(b.position, entrancePos)));
            }
            
            // Create animation sequence
            var sequence = DOTween.Sequence();
            
            for (int i = 0; i < rooms.Count; i++)
            {
                var room = rooms[i];
                var targetPos = room.position;
                var targetScale = room.localScale;
                
                // Set initial state
                room.position = targetPos + Vector3.up * DropHeight;
                room.localScale = targetScale * StartScale;
                room.gameObject.SetActive(true);
                
                // Animate
                float delay = i * RevealDelay;
                sequence.Insert(delay, room.DOMove(targetPos, RevealDuration).SetEase(Ease.OutBounce));
                sequence.Insert(delay, room.DOScale(targetScale, RevealDuration).SetEase(Ease.OutBack));
            }
            
            sequence.Play();
        }
        
        private static void AnimateRoomDoTween(GameObject roomObject, float delay)
        {
            var transform = roomObject.transform;
            var targetPos = transform.position;
            var targetScale = transform.localScale;
            
            transform.position = targetPos + Vector3.up * DropHeight;
            transform.localScale = targetScale * StartScale;
            roomObject.SetActive(true);
            
            var sequence = DOTween.Sequence();
            sequence.SetDelay(delay);
            sequence.Append(transform.DOMove(targetPos, RevealDuration).SetEase(Ease.OutBounce));
            sequence.Join(transform.DOScale(targetScale, RevealDuration).SetEase(Ease.OutBack));
            sequence.Play();
        }
        
        private static void AnimateDestroyDoTween(GameObject roomObject, float duration)
        {
            var transform = roomObject.transform;
            
            var sequence = DOTween.Sequence();
            sequence.Append(transform.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            sequence.Join(transform.DOMove(transform.position + Vector3.down * 2f, duration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => Object.Destroy(roomObject));
            sequence.Play();
        }
        
        private static void PulseRoomDoTween(GameObject roomObject, int pulseCount)
        {
            var transform = roomObject.transform;
            var originalScale = transform.localScale;
            
            transform.DOScale(originalScale * 1.2f, 0.2f)
                .SetLoops(pulseCount * 2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        
        private static void AnimateConnectionDoTween(Vector3 from, Vector3 to, LineRenderer lineRenderer, float duration)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, from);
            lineRenderer.SetPosition(1, from);
            
            DOTween.To(
                () => lineRenderer.GetPosition(1),
                x => lineRenderer.SetPosition(1, x),
                to,
                duration
            ).SetEase(Ease.OutQuad);
        }
        
        #endregion
#endif
        
        #region Shader-Based Effects
        
        /// <summary>
        /// Gets material property block for room based on type.
        /// </summary>
        public static void ApplyRoomTypeVisuals(GameObject roomObject, RoomType type)
        {
            var renderers = roomObject.GetComponentsInChildren<Renderer>();
            var mpb = new MaterialPropertyBlock();
            
            Color emissionColor = type switch
            {
                RoomType.Entrance => Color.green * 0.5f,
                RoomType.Exit => Color.blue * 0.5f,
                RoomType.Boss => Color.red * 0.5f,
                RoomType.Treasure => Color.yellow * 0.5f,
                RoomType.Secret => Color.magenta * 0.5f,
                _ => Color.black
            };
            
            foreach (var renderer in renderers)
            {
                renderer.GetPropertyBlock(mpb);
                mpb.SetColor("_EmissionColor", emissionColor);
                renderer.SetPropertyBlock(mpb);
            }
        }
        
        #endregion
    }
}
