using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

namespace AILevelGenerator.Editor
{
    using AILevelGenerator.Core;
    using AILevelGenerator.Data;
    using AILevelGenerator.AI;
    using AILevelGenerator;
    
    /// <summary>
    /// Main editor window for AI Level Generator.
    /// Provides natural language input and real-time preview.
    /// </summary>
    public class LevelGeneratorWindow : EditorWindow
    {
        #region Window Setup
        
        [MenuItem("Window/AI Level Generator %#g")]
        public static void ShowWindow()
        {
            var window = GetWindow<LevelGeneratorWindow>();
            window.titleContent = new GUIContent("AI Level Generator", EditorGUIUtility.IconContent("d_SceneViewFx").image);
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        #endregion
        
        #region Fields
        
        // Input
        private string _commandInput = "";
        private bool _inputFocused;
        private List<string> _suggestions = new List<string>();
        private int _selectedSuggestion = -1;
        
        // Generation
        private GenerationProfile _currentProfile;
        private LevelData _previewLevel;
        private float _generationProgress;
        private bool _isGenerating;
        private string _lastError;
        
        // Preview
        private Vector2 _previewScrollPos;
        private float _previewZoom = 1f;
        private Vector2 _previewOffset;
        private bool _isDragging;
        private Vector2 _lastMousePos;
        
        // History
        private List<GenerationHistoryEntry> _history = new List<GenerationHistoryEntry>();
        private Vector2 _historyScrollPos;
        
        // Settings
        private bool _autoPreview = true;
        private bool _showGrid = true;
        private bool _showConnections = true;
        private bool _showRoomTypes = true;
        
        // Facade
        private AILevelGeneratorFacade _facade;
        private CommandParser _parser;
        
        // Styles
        private GUIStyle _commandInputStyle;
        private GUIStyle _suggestionStyle;
        private GUIStyle _headerStyle;
        private bool _stylesInitialized;
        
        // Tabs
        private enum Tab { Generate, Templates, Settings, History }
        private Tab _currentTab = Tab.Generate;
        
        #endregion
        
        #region Unity Callbacks
        
        private void OnEnable()
        {
            _parser = new CommandParser();
            InitializeFacade();
        }
        
        private void OnDisable()
        {
            if (_facade != null)
            {
                _facade.OnProgressChanged -= OnProgressChanged;
                _facade.OnGenerationComplete -= OnGenerationComplete;
                _facade.OnGenerationFailed -= OnGenerationFailed;
            }
        }
        
        private void OnGUI()
        {
            InitializeStyles();
            
            // Main container with padding
            EditorGUILayout.BeginVertical();
            
            // Header with gradient background
            DrawModernHeader();
            
            GUILayout.Space(8);
            
            // Tab bar with better styling
            DrawModernTabs();
            
            GUILayout.Space(12);
            
            // Content area with scroll
            _previewScrollPos = EditorGUILayout.BeginScrollView(_previewScrollPos);
            
            // Add padding around content
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            EditorGUILayout.BeginVertical();
            
            switch (_currentTab)
            {
                case Tab.Generate:
                    DrawGenerateTab();
                    break;
                case Tab.Templates:
                    DrawTemplatesTab();
                    break;
                case Tab.Settings:
                    DrawSettingsTab();
                    break;
                case Tab.History:
                    DrawHistoryTab();
                    break;
            }
            
            EditorGUILayout.EndVertical();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            HandleEvents();
        }
        
        private void Update()
        {
            if (_isGenerating)
            {
                Repaint();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeFacade()
        {
            _facade = FindObjectOfType<AILevelGeneratorFacade>();
            
            if (_facade == null)
            {
                // In editor without play mode, we need to create a temporary instance
                var go = new GameObject("[AI Level Generator - Editor]");
                go.hideFlags = HideFlags.HideAndDontSave;
                _facade = go.AddComponent<AILevelGeneratorFacade>();
                
                // Load templates from Resources
                var templates = Resources.LoadAll<RoomTemplateData>("ExampleTemplates");
                if (templates.Length > 0)
                {
                    foreach (var t in templates)
                    {
                        _facade.AddTemplate(t);
                    }
                    DebugLogger.LogCore($"Loaded {templates.Length} templates from Resources");
                }
                else
                {
                    // Try finding in project
                    var guids = AssetDatabase.FindAssets("t:RoomTemplateData");
                    foreach (var guid in guids)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        var template = AssetDatabase.LoadAssetAtPath<RoomTemplateData>(path);
                        if (template != null)
                        {
                            _facade.AddTemplate(template);
                        }
                    }
                    DebugLogger.LogCore($"Loaded {guids.Length} templates from project");
                }
            }
            
            // Ensure initialization
            if (_facade != null)
            {
                // Force initialization
                _facade.Initialize();
                
                _facade.OnProgressChanged += OnProgressChanged;
                _facade.OnGenerationComplete += OnGenerationComplete;
                _facade.OnGenerationFailed += OnGenerationFailed;
            }
        }
        
        private void InitializeStyles()
        {
            if (_stylesInitialized) return;
            
            _commandInputStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 14,
                padding = new RectOffset(10, 10, 8, 8),
                fixedHeight = 36
            };
            
            _suggestionStyle = new GUIStyle(EditorStyles.label)
            {
                padding = new RectOffset(10, 10, 4, 4),
                hover = { textColor = Color.white }
            };
            
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            
            _stylesInitialized = true;
        }
        
        #endregion
        
        #region Drawing
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            GUILayout.Label("🧠 AI Level Generator", _headerStyle, GUILayout.Height(24));
            
            GUILayout.FlexibleSpace();
            
            // Quick actions
            if (GUILayout.Button("📖 Docs", EditorStyles.toolbarButton))
            {
                Application.OpenURL("https://github.com/your-repo/ai-level-generator");
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawModernHeader()
        {
            // Dark gradient header
            var headerRect = GUILayoutUtility.GetRect(position.width, 50);
            
            // Draw gradient background
            var darkColor = new Color(0.12f, 0.12f, 0.14f);
            var lightColor = new Color(0.18f, 0.18f, 0.22f);
            DrawGradientRect(headerRect, darkColor, lightColor, true);
            
            // Draw accent line
            var accentRect = new Rect(headerRect.x, headerRect.yMax - 2, headerRect.width, 2);
            EditorGUI.DrawRect(accentRect, new Color(0.4f, 0.6f, 1f, 0.8f));
            
            // Icon and title
            var iconRect = new Rect(headerRect.x + 15, headerRect.y + 10, 30, 30);
            GUI.Label(iconRect, "🧠", new GUIStyle { fontSize = 24 });
            
            var titleRect = new Rect(headerRect.x + 50, headerRect.y + 8, 250, 20);
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) 
            { 
                fontSize = 16, 
                normal = { textColor = new Color(0.9f, 0.9f, 0.95f) }
            };
            GUI.Label(titleRect, "AI Level Generator", titleStyle);
            
            var subtitleRect = new Rect(headerRect.x + 50, headerRect.y + 28, 250, 16);
            var subtitleStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = new Color(0.6f, 0.6f, 0.7f) }
            };
            GUI.Label(subtitleRect, "Natural Language Level Design", subtitleStyle);
            
            // Docs button
            var docsRect = new Rect(headerRect.xMax - 80, headerRect.y + 12, 70, 26);
            if (GUI.Button(docsRect, "📖 Docs", CreateButtonStyle(new Color(0.3f, 0.3f, 0.35f))))
            {
                Application.OpenURL("https://github.com/your-repo/ai-level-generator");
            }
        }
        
        private void DrawTabs()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Toggle(_currentTab == Tab.Generate, "Generate", EditorStyles.toolbarButton))
                _currentTab = Tab.Generate;
            if (GUILayout.Toggle(_currentTab == Tab.Templates, "Templates", EditorStyles.toolbarButton))
                _currentTab = Tab.Templates;
            if (GUILayout.Toggle(_currentTab == Tab.Settings, "Settings", EditorStyles.toolbarButton))
                _currentTab = Tab.Settings;
            if (GUILayout.Toggle(_currentTab == Tab.History, "History", EditorStyles.toolbarButton))
                _currentTab = Tab.History;
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawModernTabs()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(12);
            
            var tabs = new[] { ("🎲", "Generate"), ("📦", "Templates"), ("⚙️", "Settings"), ("📜", "History") };
            var tabEnums = new[] { Tab.Generate, Tab.Templates, Tab.Settings, Tab.History };
            
            for (int i = 0; i < tabs.Length; i++)
            {
                bool isSelected = _currentTab == tabEnums[i];
                var style = CreateTabStyle(isSelected);
                
                if (GUILayout.Button($"{tabs[i].Item1} {tabs[i].Item2}", style, GUILayout.Height(32), GUILayout.MinWidth(90)))
                {
                    _currentTab = tabEnums[i];
                }
                
                GUILayout.Space(4);
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.Space(12);
            EditorGUILayout.EndHorizontal();
        }
        
        private GUIStyle CreateTabStyle(bool selected)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(12, 12, 6, 6),
                margin = new RectOffset(0, 0, 0, 0)
            };
            
            if (selected)
            {
                style.normal.textColor = Color.white;
                style.normal.background = MakeTexture(1, 1, new Color(0.25f, 0.45f, 0.8f));
            }
            else
            {
                style.normal.textColor = new Color(0.7f, 0.7f, 0.75f);
                style.normal.background = MakeTexture(1, 1, new Color(0.22f, 0.22f, 0.25f));
                style.hover.textColor = Color.white;
                style.hover.background = MakeTexture(1, 1, new Color(0.28f, 0.28f, 0.32f));
            }
            
            return style;
        }
        
        private GUIStyle CreateButtonStyle(Color bgColor)
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(8, 8, 4, 4)
            };
            style.normal.background = MakeTexture(1, 1, bgColor);
            style.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            style.hover.background = MakeTexture(1, 1, bgColor * 1.2f);
            return style;
        }
        
        private void DrawGradientRect(Rect rect, Color top, Color bottom, bool vertical)
        {
            int steps = 20;
            float stepHeight = rect.height / steps;
            
            for (int i = 0; i < steps; i++)
            {
                float t = (float)i / steps;
                Color color = Color.Lerp(top, bottom, t);
                var stepRect = new Rect(rect.x, rect.y + i * stepHeight, rect.width, stepHeight + 1);
                EditorGUI.DrawRect(stepRect, color);
            }
        }
        
        private Texture2D MakeTexture(int width, int height, Color col)
        {
            var pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            var result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
        
        private void DrawGenerateTab()
        {
            // Section: Command Input
            DrawSectionHeader("💬 Command Input");
            GUILayout.Space(8);
            
            DrawModernCommandInput();
            
            GUILayout.Space(8);
            
            // Suggestions
            if (_suggestions.Count > 0 && _inputFocused)
            {
                DrawModernSuggestions();
            }
            
            GUILayout.Space(12);
            
            // Action buttons with modern style
            EditorGUILayout.BeginHorizontal();
            
            // Generate button (primary)
            GUI.enabled = !_isGenerating && !string.IsNullOrEmpty(_commandInput);
            var generateStyle = CreatePrimaryButtonStyle();
            if (GUILayout.Button("🎲 Generate Level", generateStyle, GUILayout.Height(40)))
            {
                GenerateLevel();
            }
            
            GUILayout.Space(8);
            
            // Random button (secondary)
            GUI.enabled = !_isGenerating;
            var secondaryStyle = CreateSecondaryButtonStyle();
            if (GUILayout.Button("🎯 Random", secondaryStyle, GUILayout.Height(40), GUILayout.Width(100)))
            {
                GenerateRandom();
            }
            
            // Cancel button (danger)
            if (_isGenerating)
            {
                GUILayout.Space(8);
                var dangerStyle = CreateDangerButtonStyle();
                if (GUILayout.Button("❌ Cancel", dangerStyle, GUILayout.Height(40), GUILayout.Width(90)))
                {
                    _facade?.CancelGeneration();
                }
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            
            // Progress bar with custom style
            if (_isGenerating)
            {
                GUILayout.Space(12);
                DrawModernProgressBar(_generationProgress, "Generating level...");
            }
            
            // Error display
            if (!string.IsNullOrEmpty(_lastError))
            {
                GUILayout.Space(8);
                DrawErrorBox(_lastError);
            }
            
            GUILayout.Space(16);
            
            // Section: Preview
            DrawSectionHeader("🗺️ Level Preview");
            GUILayout.Space(8);
            
            DrawModernPreview();
            
            // Actions for generated level
            if (_previewLevel != null)
            {
                GUILayout.Space(12);
                DrawLevelActions();
            }
        }
        
        private void DrawSectionHeader(string title)
        {
            var rect = GUILayoutUtility.GetRect(position.width - 40, 24);
            
            // Background
            var bgRect = new Rect(rect.x - 12, rect.y, position.width - 10, rect.height);
            EditorGUI.DrawRect(bgRect, new Color(0.18f, 0.18f, 0.2f));
            
            // Left accent
            EditorGUI.DrawRect(new Rect(bgRect.x, bgRect.y, 3, bgRect.height), new Color(0.4f, 0.6f, 1f));
            
            // Title
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                padding = new RectOffset(14, 0, 2, 2)
            };
            style.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            GUI.Label(rect, title, style);
        }
        
        private void DrawModernCommandInput()
        {
            // Input container with border
            var containerRect = GUILayoutUtility.GetRect(position.width - 40, 52);
            
            // Draw border
            EditorGUI.DrawRect(containerRect, new Color(0.22f, 0.22f, 0.25f));
            var innerRect = new Rect(containerRect.x + 1, containerRect.y + 1, containerRect.width - 2, containerRect.height - 2);
            EditorGUI.DrawRect(innerRect, new Color(0.16f, 0.16f, 0.18f));
            
            // Input field
            var inputRect = new Rect(innerRect.x + 10, innerRect.y + 10, innerRect.width - 20, 32);
            
            var inputStyle = new GUIStyle(EditorStyles.textField)
            {
                fontSize = 14,
                padding = new RectOffset(12, 12, 8, 8),
                alignment = TextAnchor.MiddleLeft
            };
            inputStyle.normal.background = MakeTexture(1, 1, new Color(0.12f, 0.12f, 0.14f));
            inputStyle.normal.textColor = new Color(0.9f, 0.9f, 0.95f);
            inputStyle.focused.background = MakeTexture(1, 1, new Color(0.14f, 0.14f, 0.16f));
            inputStyle.focused.textColor = Color.white;
            
            GUI.SetNextControlName("CommandInput");
            EditorGUI.BeginChangeCheck();
            _commandInput = EditorGUI.TextField(inputRect, _commandInput, inputStyle);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSuggestions();
            }
            
            _inputFocused = GUI.GetNameOfFocusedControl() == "CommandInput";
            
            // Placeholder
            if (string.IsNullOrEmpty(_commandInput) && !_inputFocused)
            {
                var placeholderStyle = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Italic,
                    padding = new RectOffset(14, 12, 8, 8)
                };
                placeholderStyle.normal.textColor = new Color(0.45f, 0.45f, 0.5f);
                EditorGUI.LabelField(inputRect, "e.g., \"Create a dark dungeon with 10 rooms, medium difficulty\"", placeholderStyle);
            }
        }
        
        private void DrawModernSuggestions()
        {
            var bgRect = GUILayoutUtility.GetRect(position.width - 40, _suggestions.Count * 28 + 8);
            EditorGUI.DrawRect(bgRect, new Color(0.2f, 0.2f, 0.22f));
            
            float y = bgRect.y + 4;
            for (int i = 0; i < _suggestions.Count; i++)
            {
                var itemRect = new Rect(bgRect.x + 4, y, bgRect.width - 8, 24);
                
                bool isHover = itemRect.Contains(Event.current.mousePosition);
                bool isSelected = i == _selectedSuggestion;
                
                if (isHover || isSelected)
                {
                    EditorGUI.DrawRect(itemRect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
                }
                
                var style = new GUIStyle(EditorStyles.label)
                {
                    padding = new RectOffset(10, 10, 4, 4)
                };
                style.normal.textColor = isHover ? Color.white : new Color(0.8f, 0.8f, 0.85f);
                
                if (GUI.Button(itemRect, _suggestions[i], style))
                {
                    _commandInput = _suggestions[i];
                    _suggestions.Clear();
                    GUI.FocusControl(null);
                }
                
                y += 28;
            }
        }
        
        private void DrawModernProgressBar(float progress, string text)
        {
            var rect = GUILayoutUtility.GetRect(position.width - 40, 28);
            
            // Background
            EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.17f));
            
            // Progress fill with gradient
            var fillRect = new Rect(rect.x, rect.y, rect.width * progress, rect.height);
            var fillColor = Color.Lerp(new Color(0.3f, 0.5f, 0.9f), new Color(0.4f, 0.8f, 0.4f), progress);
            EditorGUI.DrawRect(fillRect, fillColor);
            
            // Text
            var textStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold
            };
            textStyle.normal.textColor = Color.white;
            GUI.Label(rect, $"{text} {progress:P0}", textStyle);
        }
        
        private void DrawErrorBox(string error)
        {
            var rect = GUILayoutUtility.GetRect(position.width - 40, 40);
            
            // Red background
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.15f, 0.15f));
            
            // Left accent
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 4, rect.height), new Color(0.9f, 0.2f, 0.2f));
            
            // Icon and text
            var iconRect = new Rect(rect.x + 12, rect.y + 10, 20, 20);
            GUI.Label(iconRect, "⚠️", new GUIStyle { fontSize = 16 });
            
            var textRect = new Rect(rect.x + 38, rect.y + 4, rect.width - 50, rect.height - 8);
            var style = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 11
            };
            style.normal.textColor = new Color(1f, 0.8f, 0.8f);
            GUI.Label(textRect, error, style);
        }
        
        private void DrawModernPreview()
        {
            var previewRect = GUILayoutUtility.GetRect(position.width - 40, 280);
            
            // Dark background with border
            EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.14f));
            var borderRect = new Rect(previewRect.x - 1, previewRect.y - 1, previewRect.width + 2, previewRect.height + 2);
            
            // Draw subtle grid pattern
            if (_showGrid)
            {
                DrawGridPattern(previewRect);
            }
            
            if (_previewLevel != null && _previewLevel.Rooms.Count > 0)
            {
                DrawLevelPreview(previewRect);
            }
            else
            {
                // Empty state
                var centerStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize = 14
                };
                centerStyle.normal.textColor = new Color(0.4f, 0.4f, 0.45f);
                GUI.Label(previewRect, "🗺️ Generate a level to see preview", centerStyle);
            }
            
            GUILayout.Space(8);
            
            // Preview controls
            EditorGUILayout.BeginHorizontal();
            
            _showGrid = GUILayout.Toggle(_showGrid, "📐 Grid", CreateToggleStyle(_showGrid), GUILayout.Width(70));
            GUILayout.Space(4);
            _showConnections = GUILayout.Toggle(_showConnections, "🔗 Links", CreateToggleStyle(_showConnections), GUILayout.Width(70));
            GUILayout.Space(4);
            _showRoomTypes = GUILayout.Toggle(_showRoomTypes, "🏷️ Types", CreateToggleStyle(_showRoomTypes), GUILayout.Width(75));
            
            GUILayout.FlexibleSpace();
            
            // Zoom controls
            if (GUILayout.Button("➖", CreateSmallButtonStyle(), GUILayout.Width(28), GUILayout.Height(24)))
                _previewZoom = Mathf.Max(0.25f, _previewZoom - 0.25f);
            
            GUILayout.Label($"{_previewZoom:0.0}x", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter }, GUILayout.Width(35));
            
            if (GUILayout.Button("➕", CreateSmallButtonStyle(), GUILayout.Width(28), GUILayout.Height(24)))
                _previewZoom = Mathf.Min(3f, _previewZoom + 0.25f);
            
            GUILayout.Space(8);
            if (GUILayout.Button("🔄 Reset", CreateSmallButtonStyle(), GUILayout.Height(24)))
            {
                _previewZoom = 1f;
                _previewOffset = Vector2.zero;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawGridPattern(Rect rect)
        {
            float gridSize = 20 * _previewZoom;
            var gridColor = new Color(0.2f, 0.2f, 0.22f, 0.5f);
            
            for (float x = rect.x; x < rect.xMax; x += gridSize)
            {
                EditorGUI.DrawRect(new Rect(x, rect.y, 1, rect.height), gridColor);
            }
            for (float y = rect.y; y < rect.yMax; y += gridSize)
            {
                EditorGUI.DrawRect(new Rect(rect.x, y, rect.width, 1), gridColor);
            }
        }
        
        private GUIStyle CreatePrimaryButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.background = MakeTexture(1, 1, new Color(0.25f, 0.5f, 0.9f));
            style.normal.textColor = Color.white;
            style.hover.background = MakeTexture(1, 1, new Color(0.3f, 0.55f, 0.95f));
            style.active.background = MakeTexture(1, 1, new Color(0.2f, 0.45f, 0.85f));
            return style;
        }
        
        private GUIStyle CreateSecondaryButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.background = MakeTexture(1, 1, new Color(0.28f, 0.28f, 0.32f));
            style.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
            style.hover.background = MakeTexture(1, 1, new Color(0.35f, 0.35f, 0.4f));
            return style;
        }
        
        private GUIStyle CreateDangerButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
            style.normal.background = MakeTexture(1, 1, new Color(0.7f, 0.2f, 0.2f));
            style.normal.textColor = Color.white;
            style.hover.background = MakeTexture(1, 1, new Color(0.8f, 0.25f, 0.25f));
            return style;
        }
        
        private GUIStyle CreateToggleStyle(bool active)
        {
            var style = new GUIStyle(EditorStyles.miniButton)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            if (active)
            {
                style.normal.background = MakeTexture(1, 1, new Color(0.3f, 0.5f, 0.8f, 0.6f));
                style.normal.textColor = Color.white;
            }
            return style;
        }
        
        private GUIStyle CreateSmallButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(4, 4, 2, 2)
            };
            style.normal.background = MakeTexture(1, 1, new Color(0.25f, 0.25f, 0.28f));
            style.normal.textColor = new Color(0.8f, 0.8f, 0.85f);
            style.hover.background = MakeTexture(1, 1, new Color(0.32f, 0.32f, 0.36f));
            return style;
        }
        
        private void DrawCommandInput()
        {
            EditorGUILayout.LabelField("Enter command (Supports English and Polish):", EditorStyles.miniLabel);
            
            EditorGUI.BeginChangeCheck();
            GUI.SetNextControlName("CommandInput");
            _commandInput = EditorGUILayout.TextField(_commandInput, _commandInputStyle);
            
            if (EditorGUI.EndChangeCheck())
            {
                UpdateSuggestions();
            }
            
            _inputFocused = GUI.GetNameOfFocusedControl() == "CommandInput";
            
            // Placeholder text
            if (string.IsNullOrEmpty(_commandInput) && !_inputFocused)
            {
                var rect = GUILayoutUtility.GetLastRect();
                var style = new GUIStyle(EditorStyles.label) { fontStyle = FontStyle.Italic };
                style.normal.textColor = Color.gray;
                EditorGUI.LabelField(new Rect(rect.x + 10, rect.y, rect.width - 20, rect.height),
                    "e.g., \"Create a dark dungeon with 10 rooms, medium difficulty\"", style);
            }
        }
        
        private void DrawSuggestions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            for (int i = 0; i < _suggestions.Count; i++)
            {
                var style = new GUIStyle(_suggestionStyle);
                if (i == _selectedSuggestion)
                {
                    style.normal.background = EditorGUIUtility.whiteTexture;
                    style.normal.textColor = Color.white;
                }
                
                if (GUILayout.Button(_suggestions[i], style))
                {
                    _commandInput = _suggestions[i];
                    _suggestions.Clear();
                    GUI.FocusControl(null);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Level Preview", EditorStyles.boldLabel);
            
            var previewRect = GUILayoutUtility.GetRect(position.width - 20, 250);
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f));
            
            if (_previewLevel != null && _previewLevel.Rooms.Count > 0)
            {
                DrawLevelPreview(previewRect);
            }
            else
            {
                var center = previewRect.center;
                var style = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 12 };
                GUI.Label(new Rect(center.x - 100, center.y - 10, 200, 20), 
                    "Generate a level to see preview", style);
            }
            
            // Preview controls
            EditorGUILayout.BeginHorizontal();
            
            _showGrid = GUILayout.Toggle(_showGrid, "Grid", EditorStyles.miniButton, GUILayout.Width(50));
            _showConnections = GUILayout.Toggle(_showConnections, "Connections", EditorStyles.miniButton, GUILayout.Width(80));
            _showRoomTypes = GUILayout.Toggle(_showRoomTypes, "Types", EditorStyles.miniButton, GUILayout.Width(50));
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("🔍-", EditorStyles.miniButton, GUILayout.Width(30)))
                _previewZoom = Mathf.Max(0.25f, _previewZoom - 0.25f);
            GUILayout.Label($"{_previewZoom:0.0}x", GUILayout.Width(30));
            if (GUILayout.Button("🔍+", EditorStyles.miniButton, GUILayout.Width(30)))
                _previewZoom = Mathf.Min(4f, _previewZoom + 0.25f);
            if (GUILayout.Button("⊙", EditorStyles.miniButton, GUILayout.Width(25)))
            {
                _previewZoom = 1f;
                _previewOffset = Vector2.zero;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawLevelPreview(Rect previewRect)
        {
            GUI.BeginGroup(previewRect);
            
            var center = new Vector2(previewRect.width / 2, previewRect.height / 2);
            var cellSize = 30f * _previewZoom;
            
            // Find bounds
            Vector3Int min = Vector3Int.one * int.MaxValue;
            Vector3Int max = Vector3Int.one * int.MinValue;
            foreach (var room in _previewLevel.Rooms)
            {
                min = Vector3Int.Min(min, room.GridPosition);
                max = Vector3Int.Max(max, room.GridPosition);
            }
            
            // Draw grid
            if (_showGrid)
            {
                Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                for (int x = min.x - 1; x <= max.x + 1; x++)
                {
                    for (int z = min.z - 1; z <= max.z + 1; z++)
                    {
                        var pos = center + new Vector2(
                            (x - (min.x + max.x) / 2f) * cellSize,
                            (z - (min.z + max.z) / 2f) * cellSize
                        ) + _previewOffset;
                        
                        var rect = new Rect(pos.x - cellSize / 2 + 1, pos.y - cellSize / 2 + 1, cellSize - 2, cellSize - 2);
                        if (rect.Overlaps(new Rect(0, 0, previewRect.width, previewRect.height)))
                        {
                            EditorGUI.DrawRect(rect, new Color(0.2f, 0.2f, 0.2f, 0.3f));
                        }
                    }
                }
            }
            
            // Draw connections
            if (_showConnections)
            {
                Handles.color = new Color(0.5f, 0.5f, 0.5f);
                foreach (var room in _previewLevel.Rooms)
                {
                    var roomPos = center + new Vector2(
                        (room.GridPosition.x - (min.x + max.x) / 2f) * cellSize,
                        (room.GridPosition.z - (min.z + max.z) / 2f) * cellSize
                    ) + _previewOffset;
                    
                    foreach (var connectedId in room.ConnectedRoomIds)
                    {
                        var connected = _previewLevel.FindRoom(connectedId);
                        if (connected != null)
                        {
                            var connectedPos = center + new Vector2(
                                (connected.GridPosition.x - (min.x + max.x) / 2f) * cellSize,
                                (connected.GridPosition.z - (min.z + max.z) / 2f) * cellSize
                            ) + _previewOffset;
                            
                            Handles.DrawLine(
                                new Vector3(roomPos.x, roomPos.y, 0),
                                new Vector3(connectedPos.x, connectedPos.y, 0)
                            );
                        }
                    }
                }
            }
            
            // Draw rooms
            foreach (var room in _previewLevel.Rooms)
            {
                var roomPos = center + new Vector2(
                    (room.GridPosition.x - (min.x + max.x) / 2f) * cellSize,
                    (room.GridPosition.z - (min.z + max.z) / 2f) * cellSize
                ) + _previewOffset;
                
                var rect = new Rect(roomPos.x - cellSize / 2 + 2, roomPos.y - cellSize / 2 + 2, cellSize - 4, cellSize - 4);
                
                if (!rect.Overlaps(new Rect(0, 0, previewRect.width, previewRect.height)))
                    continue;
                
                var color = GetRoomColor(room.Type);
                EditorGUI.DrawRect(rect, color);
                
                // Draw room type indicator
                if (_showRoomTypes)
                {
                    var label = GetRoomLabel(room.Type);
                    if (!string.IsNullOrEmpty(label))
                    {
                        var style = new GUIStyle(EditorStyles.miniLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = Mathf.RoundToInt(10 * _previewZoom)
                        };
                        style.normal.textColor = Color.white;
                        GUI.Label(rect, label, style);
                    }
                }
            }
            
            GUI.EndGroup();
        }
        
        private void DrawLevelActions()
        {
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("🏗️ Instantiate in Scene", GUILayout.Height(25)))
            {
                InstantiateLevel();
            }
            
            if (GUILayout.Button("💾 Save Data", GUILayout.Height(25)))
            {
                SaveLevelData();
            }
            
            if (GUILayout.Button("♻️ Regenerate", GUILayout.Height(25)))
            {
                GenerateLevel();
            }
            
            EditorGUILayout.EndHorizontal();
            
            // Level info
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            GUILayout.Label($"Rooms: {_previewLevel.Rooms.Count}", EditorStyles.miniLabel);
            GUILayout.Label($"Theme: {_previewLevel.Theme}", EditorStyles.miniLabel);
            GUILayout.Label($"Difficulty: {_previewLevel.BaseDifficulty}", EditorStyles.miniLabel);
            GUILayout.Label($"Seed: {_previewLevel.Seed}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
            
            // Feedback
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Rate this level:", EditorStyles.miniLabel, GUILayout.Width(80));
            
            for (int i = 1; i <= 5; i++)
            {
                if (GUILayout.Button(i <= 3 ? "⭐" : "★", GUILayout.Width(25)))
                {
                    _facade?.RecordFeedback(_previewLevel, i / 5f);
                    Debug.Log($"Recorded feedback: {i}/5");
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawTemplatesTab()
        {
            EditorGUILayout.HelpBox(
                "Drag and drop RoomTemplateData assets here or create new ones via the menu:\n" +
                "Assets > Create > AI Level Generator > Room Template",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            if (GUILayout.Button("Create New Room Template"))
            {
                var template = ScriptableObject.CreateInstance<RoomTemplateData>();
                var path = EditorUtility.SaveFilePanelInProject(
                    "Save Room Template",
                    "NewRoomTemplate",
                    "asset",
                    "Save room template"
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(template, path);
                    AssetDatabase.SaveAssets();
                    Selection.activeObject = template;
                }
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Find All Templates in Project"))
            {
                var guids = AssetDatabase.FindAssets("t:RoomTemplateData");
                Debug.Log($"Found {guids.Length} room templates in project");
            }
        }
        
        private void DrawSettingsTab()
        {
            EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
            
            _autoPreview = EditorGUILayout.Toggle("Auto Preview", _autoPreview);
            
            EditorGUILayout.Space(10);
            
            var settings = GenerationSettingsData.Instance;
            if (settings != null)
            {
                EditorGUILayout.LabelField("Default Room Count", settings.DefaultRoomCount.ToString());
                EditorGUILayout.LabelField("Default Theme", settings.DefaultTheme.ToString());
                EditorGUILayout.LabelField("Default Difficulty", settings.DefaultDifficulty.ToString());
                
                EditorGUILayout.Space(5);
                
                if (GUILayout.Button("Open Settings Asset"))
                {
                    Selection.activeObject = settings;
                }
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            
            DebugLogger.IsEnabled = EditorGUILayout.Toggle("Enable Logging", DebugLogger.IsEnabled);
            DebugLogger.ShowCategories = EditorGUILayout.Toggle("Show Categories", DebugLogger.ShowCategories);
        }
        
        private void DrawHistoryTab()
        {
            EditorGUILayout.LabelField($"Generation History ({_history.Count})", EditorStyles.boldLabel);
            
            _historyScrollPos = EditorGUILayout.BeginScrollView(_historyScrollPos);
            
            for (int i = _history.Count - 1; i >= 0; i--)
            {
                var entry = _history[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                EditorGUILayout.BeginVertical();
                EditorGUILayout.LabelField(entry.Command, EditorStyles.wordWrappedLabel);
                EditorGUILayout.LabelField(
                    $"{entry.RoomCount} rooms • {entry.Theme} • {entry.Timestamp:HH:mm:ss}",
                    EditorStyles.miniLabel
                );
                EditorGUILayout.EndVertical();
                
                if (GUILayout.Button("Load", GUILayout.Width(50)))
                {
                    _commandInput = entry.Command;
                    _previewLevel = entry.Level;
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndScrollView();
            
            if (_history.Count > 0 && GUILayout.Button("Clear History"))
            {
                _history.Clear();
            }
        }
        
        #endregion
        
        #region Generation
        
        private async void GenerateLevel()
        {
            if (_isGenerating || _facade == null) return;
            
            _isGenerating = true;
            _lastError = null;
            
            try
            {
                var level = await _facade.GenerateAsync(_commandInput);
                
                if (level != null)
                {
                    _history.Add(new GenerationHistoryEntry
                    {
                        Command = _commandInput,
                        Level = level,
                        RoomCount = level.Rooms.Count,
                        Theme = level.Theme.ToString(),
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
            }
            
            _isGenerating = false;
            Repaint();
        }
        
        private async void GenerateRandom()
        {
            if (_isGenerating || _facade == null) return;
            
            var themes = Enum.GetValues(typeof(ThemeType));
            var randomTheme = (ThemeType)themes.GetValue(UnityEngine.Random.Range(0, themes.Length - 1));
            var randomRooms = UnityEngine.Random.Range(5, 20);
            var difficulties = Enum.GetValues(typeof(Difficulty));
            var randomDifficulty = (Difficulty)difficulties.GetValue(UnityEngine.Random.Range(0, difficulties.Length));
            
            _commandInput = $"Create a {randomDifficulty.ToString().ToLower()} {randomTheme.ToString().ToLower()} with {randomRooms} rooms";
            await System.Threading.Tasks.Task.Yield(); // Yield to update UI
            GenerateLevel();
        }
        
        private void InstantiateLevel()
        {
            if (_previewLevel == null || _facade == null) return;
            
            var levelObject = _facade.InstantiateLevel(_previewLevel);
            if (levelObject != null)
            {
                Selection.activeGameObject = levelObject;
                SceneView.FrameLastActiveSceneView();
            }
        }
        
        private void SaveLevelData()
        {
            if (_previewLevel == null) return;
            
            var path = EditorUtility.SaveFilePanelInProject(
                "Save Level Data",
                $"Level_{_previewLevel.LevelId}",
                "json",
                "Save level data"
            );
            
            if (!string.IsNullOrEmpty(path))
            {
                var json = JsonUtility.ToJson(_previewLevel, true);
                System.IO.File.WriteAllText(path, json);
                AssetDatabase.Refresh();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnProgressChanged(float progress)
        {
            _generationProgress = progress;
        }
        
        private void OnGenerationComplete(LevelData level)
        {
            _previewLevel = level;
            _isGenerating = false;
            Repaint();
        }
        
        private void OnGenerationFailed(string error)
        {
            _lastError = error;
            _isGenerating = false;
            Repaint();
        }
        
        private void UpdateSuggestions()
        {
            if (string.IsNullOrEmpty(_commandInput))
            {
                _suggestions.Clear();
                return;
            }
            
            _suggestions = new List<string>(_parser?.GetSuggestions(_commandInput) ?? new List<string>());
        }
        
        private void HandleEvents()
        {
            var e = Event.current;
            
            // Handle keyboard navigation in suggestions
            if (_suggestions.Count > 0 && _inputFocused)
            {
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.DownArrow)
                    {
                        _selectedSuggestion = Mathf.Min(_selectedSuggestion + 1, _suggestions.Count - 1);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.UpArrow)
                    {
                        _selectedSuggestion = Mathf.Max(_selectedSuggestion - 1, 0);
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Return && _selectedSuggestion >= 0)
                    {
                        _commandInput = _suggestions[_selectedSuggestion];
                        _suggestions.Clear();
                        _selectedSuggestion = -1;
                        e.Use();
                    }
                    else if (e.keyCode == KeyCode.Escape)
                    {
                        _suggestions.Clear();
                        e.Use();
                    }
                }
            }
            
            // Handle Enter to generate
            if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return && _inputFocused && !_isGenerating)
            {
                if (_suggestions.Count == 0 && !string.IsNullOrEmpty(_commandInput))
                {
                    GenerateLevel();
                    e.Use();
                }
            }
        }
        
        #endregion
        
        #region Helpers
        
        private Color GetRoomColor(RoomType type)
        {
            return type switch
            {
                RoomType.Entrance => new Color(0.2f, 0.8f, 0.2f),
                RoomType.Exit => new Color(0.2f, 0.5f, 1f),
                RoomType.Boss => new Color(0.9f, 0.2f, 0.2f),
                RoomType.Treasure => new Color(1f, 0.8f, 0.2f),
                RoomType.Secret => new Color(0.8f, 0.2f, 0.8f),
                RoomType.Shop => new Color(0.2f, 0.8f, 0.8f),
                RoomType.Hub => new Color(0.6f, 0.6f, 0.8f),
                RoomType.Corridor => new Color(0.4f, 0.4f, 0.4f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }
        
        private string GetRoomLabel(RoomType type)
        {
            return type switch
            {
                RoomType.Entrance => "E",
                RoomType.Exit => "X",
                RoomType.Boss => "B",
                RoomType.Treasure => "T",
                RoomType.Secret => "?",
                RoomType.Shop => "$",
                RoomType.Hub => "H",
                _ => ""
            };
        }
        
        #endregion
        
        #region Data Structures
        
        private class GenerationHistoryEntry
        {
            public string Command;
            public LevelData Level;
            public int RoomCount;
            public string Theme;
            public DateTime Timestamp;
        }
        
        #endregion
    }
}
