using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AILevelGenerator.Core
{
    /// <summary>
    /// Centralized debug logging system with category filtering and editor toggle.
    /// Provides formatted, color-coded console output for development.
    /// </summary>
    public static class DebugLogger
    {
        #region Configuration
        
        /// <summary>
        /// Master toggle for all debug logging.
        /// </summary>
        public static bool IsEnabled { get; set; } = true;
        
        /// <summary>
        /// Show timestamps in log messages.
        /// </summary>
        public static bool ShowTimestamps { get; set; } = false;
        
        /// <summary>
        /// Show category prefixes in log messages.
        /// </summary>
        public static bool ShowCategories { get; set; } = true;
        
        /// <summary>
        /// Active log categories filter.
        /// </summary>
        public static LogCategory ActiveCategories { get; set; } = LogCategory.All;
        
        #endregion
        
        #region Log Categories
        
        [Flags]
        public enum LogCategory
        {
            None = 0,
            Core = 1 << 0,
            Generation = 1 << 1,
            AI = 1 << 2,
            Validation = 1 << 3,
            Editor = 1 << 4,
            Performance = 1 << 5,
            Learning = 1 << 6,
            All = ~0
        }
        
        #endregion
        
        #region Color Definitions
        
        private static readonly string COLOR_CORE = "#4FC3F7";       // Light Blue
        private static readonly string COLOR_GENERATION = "#81C784"; // Green
        private static readonly string COLOR_AI = "#BA68C8";         // Purple
        private static readonly string COLOR_VALIDATION = "#FFD54F"; // Amber
        private static readonly string COLOR_EDITOR = "#64B5F6";     // Blue
        private static readonly string COLOR_PERFORMANCE = "#FF8A65";// Deep Orange
        private static readonly string COLOR_LEARNING = "#4DB6AC";   // Teal
        private static readonly string COLOR_WARNING = "#FFA726";    // Orange
        private static readonly string COLOR_ERROR = "#EF5350";      // Red
        
        #endregion
        
        #region Public Methods
        
        /// <summary>
        /// Logs a message under the Core category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogCore(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Core, message, COLOR_CORE, context);
        }
        
        /// <summary>
        /// Logs a message under the Generation category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogGeneration(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Generation, message, COLOR_GENERATION, context);
        }
        
        /// <summary>
        /// Logs a message under the AI category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogAI(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.AI, message, COLOR_AI, context);
        }
        
        /// <summary>
        /// Logs a message under the Validation category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogValidation(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Validation, message, COLOR_VALIDATION, context);
        }
        
        /// <summary>
        /// Logs a message under the Editor category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogEditor(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Editor, message, COLOR_EDITOR, context);
        }
        
        /// <summary>
        /// Logs a message under the Performance category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogPerformance(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Performance, message, COLOR_PERFORMANCE, context);
        }
        
        /// <summary>
        /// Logs a message under the Learning category.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogLearning(string message, UnityEngine.Object context = null)
        {
            Log(LogCategory.Learning, message, COLOR_LEARNING, context);
        }
        
        /// <summary>
        /// Logs a warning message (always visible if enabled).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogWarning(string message, UnityEngine.Object context = null)
        {
            if (!IsEnabled) return;
            
            var formattedMessage = FormatMessage("⚠️ WARNING", message, COLOR_WARNING);
            Debug.LogWarning(formattedMessage, context);
        }
        
        /// <summary>
        /// Logs an error message (always visible if enabled).
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("ENABLE_LOGS")]
        public static void LogError(string message, UnityEngine.Object context = null)
        {
            if (!IsEnabled) return;
            
            var formattedMessage = FormatMessage("❌ ERROR", message, COLOR_ERROR);
            Debug.LogError(formattedMessage, context);
        }
        
        /// <summary>
        /// Logs an exception with context.
        /// </summary>
        public static void LogException(Exception exception, string context, UnityEngine.Object unityContext = null)
        {
            if (!IsEnabled) return;
            
            var message = $"[{context}] {exception.Message}\n{exception.StackTrace}";
            var formattedMessage = FormatMessage("💥 EXCEPTION", message, COLOR_ERROR);
            Debug.LogError(formattedMessage, unityContext);
        }
        
        /// <summary>
        /// Starts a performance measurement scope.
        /// </summary>
        public static PerformanceScope MeasurePerformance(string operationName)
        {
            return new PerformanceScope(operationName);
        }
        
        #endregion
        
        #region Private Methods
        
        private static void Log(LogCategory category, string message, string color, UnityEngine.Object context)
        {
            if (!IsEnabled) return;
            if ((ActiveCategories & category) == 0) return;
            
            var categoryName = GetCategoryEmoji(category) + " " + category.ToString().ToUpper();
            var formattedMessage = FormatMessage(categoryName, message, color);
            Debug.Log(formattedMessage, context);
        }
        
        private static string FormatMessage(string prefix, string message, string color)
        {
            var timestamp = ShowTimestamps ? $"[{DateTime.Now:HH:mm:ss.fff}] " : "";
            var categoryPrefix = ShowCategories ? $"<color={color}><b>[{prefix}]</b></color> " : "";
            return $"{timestamp}{categoryPrefix}{message}";
        }
        
        private static string GetCategoryEmoji(LogCategory category)
        {
            return category switch
            {
                LogCategory.Core => "🎯",
                LogCategory.Generation => "⚙️",
                LogCategory.AI => "🧠",
                LogCategory.Validation => "✅",
                LogCategory.Editor => "🎨",
                LogCategory.Performance => "⏱️",
                LogCategory.Learning => "📚",
                _ => "📝"
            };
        }
        
        #endregion
        
        #region Performance Scope
        
        /// <summary>
        /// Disposable scope for measuring operation performance.
        /// </summary>
        public readonly struct PerformanceScope : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;
            
            public PerformanceScope(string operationName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }
            
            public void Dispose()
            {
                _stopwatch.Stop();
                LogPerformance($"{_operationName} completed in {_stopwatch.ElapsedMilliseconds}ms");
            }
        }
        
        #endregion
    }
}
