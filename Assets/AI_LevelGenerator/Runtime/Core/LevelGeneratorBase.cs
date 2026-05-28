using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AILevelGenerator.Core
{
    /// <summary>
    /// Abstract base class for level generators providing common functionality.
    /// Uses Template Method pattern for extensibility.
    /// </summary>
    public abstract class LevelGeneratorBase : ILevelGenerator
    {
        #region Fields
        
        protected CancellationTokenSource _cancellationTokenSource;
        protected float _progress;
        protected bool _isGenerating;
        protected GenerationProfile _currentProfile;
        protected System.Random _random;
        
        #endregion
        
        #region Properties
        
        public float Progress => _progress;
        public bool IsGenerating => _isGenerating;
        
        /// <summary>
        /// The validator used for level validation.
        /// </summary>
        public ILevelValidator Validator { get; set; }
        
        /// <summary>
        /// The room template provider.
        /// </summary>
        public IRoomTemplateProvider TemplateProvider { get; set; }
        
        #endregion
        
        #region Events
        
        public event Action<float> OnProgressChanged;
        public event Action<LevelData> OnGenerationComplete;
        public event Action<string> OnGenerationFailed;
        
        #endregion
        
        #region Constructor
        
        protected LevelGeneratorBase()
        {
            DebugLogger.LogCore($"{GetType().Name} initialized");
        }
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Generates a level asynchronously using the specified profile.
        /// </summary>
        public async UniTask<LevelData> GenerateAsync(GenerationProfile profile)
        {
            if (_isGenerating)
            {
                DebugLogger.LogWarning("Generation already in progress. Cancelling previous...");
                CancelGeneration();
                await UniTask.Yield();
            }
            
            _currentProfile = profile ?? throw new ArgumentNullException(nameof(profile));
            _cancellationTokenSource = new CancellationTokenSource();
            _isGenerating = true;
            _progress = 0f;
            
            // Initialize random with seed
            var seed = profile.Seed >= 0 ? profile.Seed : Environment.TickCount;
            _random = new System.Random(seed);
            
            DebugLogger.LogGeneration($"Starting generation with seed: {seed}");
            
            try
            {
                using (DebugLogger.MeasurePerformance("Level Generation"))
                {
                    // Template Method Pattern - core generation steps
                    var level = await ExecuteGenerationPipeline(
                        profile, 
                        _cancellationTokenSource.Token
                    );
                    
                    level.Seed = seed;
                    
                    // Validate result
                    if (Validator != null)
                    {
                        var validationResult = Validator.Validate(level, profile);
                        if (!validationResult.IsValid)
                        {
                            var errors = string.Join(", ", validationResult.Errors);
                            throw new InvalidOperationException($"Level validation failed: {errors}");
                        }
                        DebugLogger.LogValidation($"Level validated with quality score: {validationResult.QualityScore:P0}");
                    }
                    
                    SetProgress(1f);
                    OnGenerationComplete?.Invoke(level);
                    
                    DebugLogger.LogGeneration($"Level generated successfully with {level.Rooms.Count} rooms");
                    return level;
                }
            }
            catch (OperationCanceledException)
            {
                DebugLogger.LogGeneration("Generation was cancelled");
                OnGenerationFailed?.Invoke("Generation cancelled by user");
                return null;
            }
            catch (Exception ex)
            {
                DebugLogger.LogException(ex, "GenerateAsync");
                OnGenerationFailed?.Invoke(ex.Message);
                throw;
            }
            finally
            {
                _isGenerating = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }
        
        /// <summary>
        /// Cancels the current generation in progress.
        /// </summary>
        public void CancelGeneration()
        {
            if (_isGenerating && _cancellationTokenSource != null)
            {
                DebugLogger.LogGeneration("Cancelling generation...");
                _cancellationTokenSource.Cancel();
            }
        }
        
        /// <summary>
        /// Validates a level using the configured validator.
        /// </summary>
        public bool ValidateLevel(LevelData level)
        {
            if (level == null) return false;
            
            if (Validator == null)
            {
                DebugLogger.LogWarning("No validator configured, using basic validation");
                return level.IsValid();
            }
            
            var result = Validator.Validate(level, _currentProfile ?? new GenerationProfile());
            return result.IsValid;
        }
        
        #endregion
        
        #region Protected Abstract Methods (Template Method Pattern)
        
        /// <summary>
        /// Executes the complete generation pipeline.
        /// Override in derived classes to customize generation flow.
        /// </summary>
        protected virtual async UniTask<LevelData> ExecuteGenerationPipeline(
            GenerationProfile profile, 
            CancellationToken token)
        {
            var level = new LevelData
            {
                Theme = profile.Theme,
                BaseDifficulty = profile.TargetDifficulty,
                Size = profile.LevelSize
            };
            
            // Step 1: Initialize layout structure
            SetProgress(0.1f);
            token.ThrowIfCancellationRequested();
            await InitializeLayoutAsync(level, profile, token);
            
            // Step 2: Generate room layout
            SetProgress(0.3f);
            token.ThrowIfCancellationRequested();
            await GenerateRoomLayoutAsync(level, profile, token);
            
            // Step 3: Create connections between rooms
            SetProgress(0.5f);
            token.ThrowIfCancellationRequested();
            await GenerateConnectionsAsync(level, profile, token);
            
            // Step 4: Place special rooms (entrance, exit, boss)
            SetProgress(0.7f);
            token.ThrowIfCancellationRequested();
            await PlaceSpecialRoomsAsync(level, profile, token);
            
            // Step 5: Finalize and polish
            SetProgress(0.9f);
            token.ThrowIfCancellationRequested();
            await FinalizeLayoutAsync(level, profile, token);
            
            return level;
        }
        
        /// <summary>
        /// Initializes the base layout structure.
        /// </summary>
        protected abstract UniTask InitializeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token);
        
        /// <summary>
        /// Generates the room layout using the specific algorithm.
        /// </summary>
        protected abstract UniTask GenerateRoomLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token);
        
        /// <summary>
        /// Creates connections (corridors, doors) between rooms.
        /// </summary>
        protected abstract UniTask GenerateConnectionsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token);
        
        /// <summary>
        /// Places special rooms like entrance, exit, and boss rooms.
        /// </summary>
        protected abstract UniTask PlaceSpecialRoomsAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token);
        
        /// <summary>
        /// Finalizes and polishes the generated layout.
        /// </summary>
        protected abstract UniTask FinalizeLayoutAsync(
            LevelData level, 
            GenerationProfile profile, 
            CancellationToken token);
        
        #endregion
        
        #region Protected Helper Methods
        
        /// <summary>
        /// Updates the generation progress and fires event.
        /// </summary>
        protected void SetProgress(float progress)
        {
            _progress = Mathf.Clamp01(progress);
            OnProgressChanged?.Invoke(_progress);
        }
        
        /// <summary>
        /// Gets a random integer in range [min, max).
        /// </summary>
        protected int RandomRange(int min, int max)
        {
            return _random.Next(min, max);
        }
        
        /// <summary>
        /// Gets a random float in range [0, 1).
        /// </summary>
        protected float RandomFloat()
        {
            return (float)_random.NextDouble();
        }
        
        /// <summary>
        /// Gets a random element from a list.
        /// </summary>
        protected T RandomElement<T>(IList<T> list)
        {
            if (list == null || list.Count == 0) return default;
            return list[_random.Next(list.Count)];
        }
        
        /// <summary>
        /// Shuffles a list in place using Fisher-Yates algorithm.
        /// </summary>
        protected void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
        
        /// <summary>
        /// Creates a weighted random selection.
        /// </summary>
        protected T WeightedRandom<T>(IList<T> items, IList<float> weights)
        {
            if (items.Count != weights.Count)
                throw new ArgumentException("Items and weights must have the same count");
            
            float totalWeight = 0f;
            foreach (var w in weights) totalWeight += w;
            
            float random = RandomFloat() * totalWeight;
            float current = 0f;
            
            for (int i = 0; i < items.Count; i++)
            {
                current += weights[i];
                if (random <= current)
                    return items[i];
            }
            
            return items[items.Count - 1];
        }
        
        #endregion
    }
}
