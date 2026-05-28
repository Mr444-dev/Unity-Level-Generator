using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace AILevelGenerator.AI
{
    using Core;
    
    /// <summary>
    /// Pattern-based natural language command parser.
    /// Extracts generation parameters from user input.
    /// Supports English and Polish commands.
    /// </summary>
    public class CommandParser : ICommandParser
    {
        #region Pattern Definitions
        
        // Size patterns
        private static readonly Dictionary<string, float> SizeModifiers = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            // English
            { "tiny", 0.3f }, { "very small", 0.4f }, { "small", 0.5f },
            { "medium", 1.0f }, { "normal", 1.0f }, { "average", 1.0f },
            { "large", 1.5f }, { "big", 1.5f },
            { "very large", 1.8f }, { "huge", 2.0f }, { "massive", 2.5f }, { "enormous", 3.0f },
            // Polish
            { "mały", 0.5f }, { "mala", 0.5f }, { "male", 0.5f },
            { "średni", 1.0f }, { "sredni", 1.0f }, { "normalny", 1.0f },
            { "duży", 1.5f }, { "duzy", 1.5f }, { "duża", 1.5f }, { "duza", 1.5f },
            { "ogromny", 2.0f }, { "wielki", 2.0f }
        };
        
        // Difficulty patterns
        private static readonly Dictionary<string, Difficulty> DifficultyPatterns = new Dictionary<string, Difficulty>(StringComparer.OrdinalIgnoreCase)
        {
            // English
            { "very easy", Difficulty.VeryEasy }, { "trivial", Difficulty.VeryEasy },
            { "easy", Difficulty.Easy }, { "simple", Difficulty.Easy }, { "beginner", Difficulty.Easy },
            { "medium", Difficulty.Medium }, { "normal", Difficulty.Medium }, { "average", Difficulty.Medium },
            { "hard", Difficulty.Hard }, { "difficult", Difficulty.Hard }, { "challenging", Difficulty.Hard },
            { "very hard", Difficulty.VeryHard }, { "extreme", Difficulty.VeryHard },
            { "nightmare", Difficulty.Nightmare }, { "impossible", Difficulty.Nightmare },
            // Polish
            { "bardzo łatwy", Difficulty.VeryEasy }, { "bardzo latwy", Difficulty.VeryEasy },
            { "łatwy", Difficulty.Easy }, { "latwy", Difficulty.Easy }, { "prosty", Difficulty.Easy },
            { "średni", Difficulty.Medium }, { "sredni", Difficulty.Medium }, { "normalny", Difficulty.Medium },
            { "trudny", Difficulty.Hard }, { "ciężki", Difficulty.Hard }, { "ciezki", Difficulty.Hard },
            { "bardzo trudny", Difficulty.VeryHard }, { "ekstremalny", Difficulty.VeryHard },
            { "koszmar", Difficulty.Nightmare }, { "niemożliwy", Difficulty.Nightmare }
        };
        
        // Theme patterns
        private static readonly Dictionary<string, ThemeType> ThemePatterns = new Dictionary<string, ThemeType>(StringComparer.OrdinalIgnoreCase)
        {
            // English - Dungeon
            { "dungeon", ThemeType.DarkDungeon }, { "dark dungeon", ThemeType.DarkDungeon },
            { "dark", ThemeType.DarkDungeon }, { "underground", ThemeType.DarkDungeon },
            // English - Cave
            { "cave", ThemeType.Cave }, { "cavern", ThemeType.Cave }, { "grotto", ThemeType.Cave },
            // English - Temple
            { "temple", ThemeType.BrightTemple }, { "bright temple", ThemeType.BrightTemple },
            { "shrine", ThemeType.BrightTemple }, { "holy", ThemeType.BrightTemple },
            // English - Castle
            { "castle", ThemeType.Castle }, { "fortress", ThemeType.Castle },
            { "stronghold", ThemeType.Castle }, { "keep", ThemeType.Castle },
            // English - Crypt
            { "crypt", ThemeType.Crypt }, { "tomb", ThemeType.Crypt },
            { "mausoleum", ThemeType.Crypt }, { "necropolis", ThemeType.Crypt },
            // English - Forest
            { "forest", ThemeType.Forest }, { "woods", ThemeType.Forest },
            { "jungle", ThemeType.Forest }, { "grove", ThemeType.Forest },
            // English - Industrial
            { "industrial", ThemeType.Industrial }, { "factory", ThemeType.Industrial },
            { "machine", ThemeType.Industrial }, { "tech", ThemeType.Industrial },
            // Polish - Dungeon
            { "loch", ThemeType.DarkDungeon }, { "lochy", ThemeType.DarkDungeon },
            { "ciemny", ThemeType.DarkDungeon }, { "podziemia", ThemeType.DarkDungeon },
            // Polish - Cave
            { "jaskinia", ThemeType.Cave }, { "grota", ThemeType.Cave },
            // Polish - Temple
            { "świątynia", ThemeType.BrightTemple }, { "swiatynia", ThemeType.BrightTemple },
            { "jasna", ThemeType.BrightTemple },
            // Polish - Castle
            { "zamek", ThemeType.Castle }, { "twierdza", ThemeType.Castle },
            { "forteca", ThemeType.Castle },
            // Polish - Crypt
            { "krypta", ThemeType.Crypt }, { "grobowiec", ThemeType.Crypt },
            // Polish - Forest
            { "las", ThemeType.Forest }, { "puszcza", ThemeType.Forest },
            // Polish - Industrial
            { "fabryka", ThemeType.Industrial }, { "przemysłowy", ThemeType.Industrial }
        };
        
        // Room count extraction patterns
        private static readonly Regex RoomCountRegex = new Regex(
            @"(\d+)\s*(?:room|rooms|pokoi|pokój|pokoj|pomieszczeń|pomieszczen|sal|sali)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled
        );
        
        private static readonly Regex NumberOnlyRegex = new Regex(
            @"\b(\d+)\b",
            RegexOptions.Compiled
        );
        
        // Action patterns
        private static readonly HashSet<string> CreateKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "create", "generate", "make", "build", "design", "spawn", "craft",
            "stwórz", "stworz", "generuj", "zrób", "zrob", "utwórz", "utworz", "buduj"
        };
        
        #endregion
        
        #region Fields
        
        private readonly float _confidenceThreshold = 0.3f;
        
        #endregion
        
        #region Constructor
        
        public CommandParser()
        {
            DebugLogger.LogAI("CommandParser initialized with multilingual support");
        }
        
        #endregion
        
        #region ICommandParser Implementation
        
        /// <summary>
        /// Parses natural language input into a structured ParsedIntent.
        /// </summary>
        public ParsedIntent Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ParsedIntent
                {
                    RawInput = input ?? "",
                    IsValid = false,
                    Confidence = 0f,
                    Warnings = new List<string> { "Empty input provided" }
                };
            }
            
            var intent = new ParsedIntent
            {
                RawInput = input,
                IsValid = true,
                Confidence = 0f
            };
            
            var normalizedInput = NormalizeInput(input);
            float totalConfidence = 0f;
            int matchCount = 0;
            
            DebugLogger.LogAI($"Parsing: \"{input}\"");
            
            // Check for action keyword (create, generate, etc.)
            bool hasActionKeyword = HasActionKeyword(normalizedInput);
            if (hasActionKeyword)
            {
                totalConfidence += 0.2f;
                matchCount++;
            }
            
            // Extract room count
            var roomCount = ExtractRoomCount(normalizedInput);
            if (roomCount.HasValue)
            {
                intent.RoomCount = roomCount.Value;
                totalConfidence += 0.3f;
                matchCount++;
                DebugLogger.LogAI($"  Room count: {roomCount.Value}");
            }
            
            // Extract difficulty
            var difficulty = ExtractDifficulty(normalizedInput);
            if (difficulty.HasValue)
            {
                intent.Difficulty = difficulty.Value;
                totalConfidence += 0.2f;
                matchCount++;
                DebugLogger.LogAI($"  Difficulty: {difficulty.Value}");
            }
            
            // Extract theme
            var theme = ExtractTheme(normalizedInput);
            if (theme.HasValue)
            {
                intent.Theme = theme.Value;
                totalConfidence += 0.2f;
                matchCount++;
                DebugLogger.LogAI($"  Theme: {theme.Value}");
            }
            
            // Extract size modifier
            var sizeModifier = ExtractSizeModifier(normalizedInput);
            if (sizeModifier.HasValue)
            {
                intent.SizeModifier = sizeModifier.Value;
                totalConfidence += 0.1f;
                matchCount++;
                DebugLogger.LogAI($"  Size modifier: {sizeModifier.Value:F2}x");
            }
            
            // Calculate final confidence
            intent.Confidence = matchCount > 0 ? totalConfidence / matchCount : 0f;
            intent.IsValid = intent.Confidence >= _confidenceThreshold || 
                            intent.RoomCount.HasValue || 
                            intent.Theme.HasValue;
            
            // Add warnings for missing parameters
            if (!intent.RoomCount.HasValue)
            {
                intent.Warnings.Add("Room count not specified, using default");
            }
            if (!intent.Theme.HasValue)
            {
                intent.Warnings.Add("Theme not specified, using default dungeon");
            }
            if (!intent.Difficulty.HasValue)
            {
                intent.Warnings.Add("Difficulty not specified, using medium");
            }
            
            DebugLogger.LogAI($"Parse result: {intent}");
            return intent;
        }
        
        /// <summary>
        /// Gets auto-complete suggestions based on partial input.
        /// </summary>
        public IReadOnlyList<string> GetSuggestions(string partialInput)
        {
            var suggestions = new List<string>();
            var normalized = NormalizeInput(partialInput ?? "");
            
            // If input is empty or just starting
            if (string.IsNullOrWhiteSpace(normalized))
            {
                suggestions.Add("Create a dark dungeon with 10 rooms");
                suggestions.Add("Generate a large cave, hard difficulty");
                suggestions.Add("Create a small dungeon with 5 rooms");
                return suggestions;
            }
            
            // Suggest themes if no theme detected
            if (!ExtractTheme(normalized).HasValue)
            {
                suggestions.Add($"{partialInput} dungeon");
                suggestions.Add($"{partialInput} cave");
                suggestions.Add($"{partialInput} temple");
            }
            
            // Suggest room count if not present
            if (!ExtractRoomCount(normalized).HasValue)
            {
                suggestions.Add($"{partialInput} with 10 rooms");
                suggestions.Add($"{partialInput} with 20 rooms");
            }
            
            // Suggest difficulty if not present
            if (!ExtractDifficulty(normalized).HasValue)
            {
                suggestions.Add($"{partialInput}, easy");
                suggestions.Add($"{partialInput}, hard");
            }
            
            return suggestions.Count > 5 ? suggestions.GetRange(0, 5) : suggestions;
        }
        
        #endregion
        
        #region Private Methods
        
        private string NormalizeInput(string input)
        {
            // Convert to lowercase and remove extra whitespace
            return Regex.Replace(input.ToLowerInvariant().Trim(), @"\s+", " ");
        }
        
        private bool HasActionKeyword(string input)
        {
            foreach (var keyword in CreateKeywords)
            {
                if (input.Contains(keyword.ToLowerInvariant()))
                    return true;
            }
            return false;
        }
        
        private int? ExtractRoomCount(string input)
        {
            // Try explicit room count pattern first
            var match = RoomCountRegex.Match(input);
            if (match.Success && int.TryParse(match.Groups[1].Value, out int count))
            {
                return Mathf.Clamp(count, 1, 200);
            }
            
            // Try to find standalone numbers (less confident)
            var numberMatch = NumberOnlyRegex.Match(input);
            if (numberMatch.Success && int.TryParse(numberMatch.Groups[1].Value, out int num))
            {
                // Only accept reasonable room counts
                if (num >= 3 && num <= 100)
                    return num;
            }
            
            return null;
        }
        
        private Difficulty? ExtractDifficulty(string input)
        {
            // Check longer patterns first to avoid partial matches
            var sortedPatterns = new List<KeyValuePair<string, Difficulty>>(DifficultyPatterns);
            sortedPatterns.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            
            foreach (var kvp in sortedPatterns)
            {
                if (input.Contains(kvp.Key.ToLowerInvariant()))
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        private ThemeType? ExtractTheme(string input)
        {
            // Check longer patterns first
            var sortedPatterns = new List<KeyValuePair<string, ThemeType>>(ThemePatterns);
            sortedPatterns.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            
            foreach (var kvp in sortedPatterns)
            {
                if (input.Contains(kvp.Key.ToLowerInvariant()))
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        private float? ExtractSizeModifier(string input)
        {
            // Check longer patterns first
            var sortedPatterns = new List<KeyValuePair<string, float>>(SizeModifiers);
            sortedPatterns.Sort((a, b) => b.Key.Length.CompareTo(a.Key.Length));
            
            foreach (var kvp in sortedPatterns)
            {
                if (input.Contains(kvp.Key.ToLowerInvariant()))
                {
                    return kvp.Value;
                }
            }
            
            return null;
        }
        
        #endregion
    }
}
