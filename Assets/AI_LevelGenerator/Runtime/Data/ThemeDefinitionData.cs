using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace AILevelGenerator.Data
{
    using Core;
    
    /// <summary>
    /// Defines a visual/audio theme for generated levels.
    /// Contains colors, materials, and environment settings.
    /// </summary>
    [CreateAssetMenu(fileName = "ThemeDefinition", menuName = "AI Level Generator/Theme Definition")]
    public class ThemeDefinitionData : ScriptableObject
    {
        #region Basic Info
        
        #if ODIN_INSPECTOR
        [BoxGroup("Info")]
        #endif
        [SerializeField] private string _themeName = "New Theme";
        
        #if ODIN_INSPECTOR
        [BoxGroup("Info")]
        [TextArea(2, 4)]
        #endif
        [SerializeField] private string _description;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Info")]
        [EnumToggleButtons]
        #endif
        [SerializeField] private ThemeType _themeType = ThemeType.Custom;
        
        #if ODIN_INSPECTOR
        [BoxGroup("Info")]
        [PreviewField(50), HideLabel]
        #endif
        [SerializeField] private Sprite _icon;
        
        #endregion
        
        #region Colors
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        #endif
        [SerializeField] private Color _primaryColor = new Color(0.2f, 0.2f, 0.3f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        #endif
        [SerializeField] private Color _secondaryColor = new Color(0.3f, 0.3f, 0.4f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        #endif
        [SerializeField] private Color _accentColor = new Color(0.8f, 0.6f, 0.2f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        #endif
        [SerializeField] private Color _emissionColor = new Color(0.5f, 0.3f, 0.1f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Colors")]
        [ColorPalette("Level Colors")]
        #endif
        [SerializeField] private Gradient _colorGradient;
        
        #endregion
        
        #region Materials
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Materials")]
        [AssetsOnly]
        #endif
        [SerializeField] private Material _floorMaterial;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Materials")]
        [AssetsOnly]
        #endif
        [SerializeField] private Material _wallMaterial;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Materials")]
        [AssetsOnly]
        #endif
        [SerializeField] private Material _ceilingMaterial;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Materials")]
        [AssetsOnly]
        #endif
        [SerializeField] private Material _trimMaterial;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Materials")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<Material> _decorationMaterials = new List<Material>();
        
        #endregion
        
        #region Lighting
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        #endif
        [SerializeField] private Color _ambientColor = new Color(0.1f, 0.1f, 0.15f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        [Range(0f, 2f)]
        #endif
        [SerializeField] private float _ambientIntensity = 0.5f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        #endif
        [SerializeField] private Color _mainLightColor = new Color(1f, 0.9f, 0.7f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        [Range(0f, 3f)]
        #endif
        [SerializeField] private float _mainLightIntensity = 1f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        #endif
        [SerializeField] private Color _pointLightColor = new Color(1f, 0.7f, 0.4f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Lighting")]
        [Range(0f, 10f)]
        #endif
        [SerializeField] private float _pointLightRange = 5f;
        
        #endregion
        
        #region Fog
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Fog")]
        #endif
        [SerializeField] private bool _useFog = true;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Fog")]
        [ShowIf("_useFog")]
        #endif
        [SerializeField] private Color _fogColor = new Color(0.1f, 0.1f, 0.15f);
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Fog")]
        [ShowIf("_useFog")]
        [Range(0.001f, 0.1f)]
        #endif
        [SerializeField] private float _fogDensity = 0.02f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Fog")]
        [ShowIf("_useFog")]
        #endif
        [SerializeField] private float _fogStartDistance = 5f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Fog")]
        [ShowIf("_useFog")]
        #endif
        [SerializeField] private float _fogEndDistance = 50f;
        
        #endregion
        
        #region Audio
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Audio")]
        [AssetsOnly]
        #endif
        [SerializeField] private AudioClip _ambientLoop;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Audio")]
        [Range(0f, 1f)]
        #endif
        [SerializeField] private float _ambientVolume = 0.5f;
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Audio")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<AudioClip> _footstepSounds = new List<AudioClip>();
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Audio")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<AudioClip> _ambientSounds = new List<AudioClip>();
        
        #endregion
        
        #region Decorations
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Decorations")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<GameObject> _wallDecorations = new List<GameObject>();
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Decorations")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<GameObject> _floorDecorations = new List<GameObject>();
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Decorations")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<GameObject> _ceilingDecorations = new List<GameObject>();
        
        #if ODIN_INSPECTOR
        [FoldoutGroup("Decorations")]
        [ListDrawerSettings(ShowFoldout = true)]
        [AssetsOnly]
        #endif
        [SerializeField] private List<GameObject> _lightPrefabs = new List<GameObject>();
        
        #endregion
        
        #region Public Properties
        
        // Info
        public string ThemeName => _themeName;
        public string Description => _description;
        public ThemeType ThemeType => _themeType;
        public Sprite Icon => _icon;
        
        // Colors
        public Color PrimaryColor => _primaryColor;
        public Color SecondaryColor => _secondaryColor;
        public Color AccentColor => _accentColor;
        public Color EmissionColor => _emissionColor;
        public Gradient ColorGradient => _colorGradient;
        
        // Materials
        public Material FloorMaterial => _floorMaterial;
        public Material WallMaterial => _wallMaterial;
        public Material CeilingMaterial => _ceilingMaterial;
        public Material TrimMaterial => _trimMaterial;
        public IReadOnlyList<Material> DecorationMaterials => _decorationMaterials;
        
        // Lighting
        public Color AmbientColor => _ambientColor;
        public float AmbientIntensity => _ambientIntensity;
        public Color MainLightColor => _mainLightColor;
        public float MainLightIntensity => _mainLightIntensity;
        public Color PointLightColor => _pointLightColor;
        public float PointLightRange => _pointLightRange;
        
        // Fog
        public bool UseFog => _useFog;
        public Color FogColor => _fogColor;
        public float FogDensity => _fogDensity;
        public float FogStartDistance => _fogStartDistance;
        public float FogEndDistance => _fogEndDistance;
        
        // Audio
        public AudioClip AmbientLoop => _ambientLoop;
        public float AmbientVolume => _ambientVolume;
        public IReadOnlyList<AudioClip> FootstepSounds => _footstepSounds;
        public IReadOnlyList<AudioClip> AmbientSounds => _ambientSounds;
        
        // Decorations
        public IReadOnlyList<GameObject> WallDecorations => _wallDecorations;
        public IReadOnlyList<GameObject> FloorDecorations => _floorDecorations;
        public IReadOnlyList<GameObject> CeilingDecorations => _ceilingDecorations;
        public IReadOnlyList<GameObject> LightPrefabs => _lightPrefabs;
        
        #endregion
        
        #region Methods
        
        /// <summary>
        /// Applies this theme to the current scene's render settings.
        /// </summary>
        public void ApplyToScene()
        {
            RenderSettings.ambientLight = _ambientColor;
            RenderSettings.ambientIntensity = _ambientIntensity;
            
            if (_useFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogColor = _fogColor;
                RenderSettings.fogDensity = _fogDensity;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
            }
            else
            {
                RenderSettings.fog = false;
            }
            
            DebugLogger.LogCore($"Theme '{_themeName}' applied to scene");
        }
        
        /// <summary>
        /// Gets a random wall decoration prefab.
        /// </summary>
        public GameObject GetRandomWallDecoration()
        {
            if (_wallDecorations == null || _wallDecorations.Count == 0) return null;
            return _wallDecorations[UnityEngine.Random.Range(0, _wallDecorations.Count)];
        }
        
        /// <summary>
        /// Gets a random floor decoration prefab.
        /// </summary>
        public GameObject GetRandomFloorDecoration()
        {
            if (_floorDecorations == null || _floorDecorations.Count == 0) return null;
            return _floorDecorations[UnityEngine.Random.Range(0, _floorDecorations.Count)];
        }
        
        /// <summary>
        /// Gets a random light prefab.
        /// </summary>
        public GameObject GetRandomLightPrefab()
        {
            if (_lightPrefabs == null || _lightPrefabs.Count == 0) return null;
            return _lightPrefabs[UnityEngine.Random.Range(0, _lightPrefabs.Count)];
        }
        
        #if ODIN_INSPECTOR
        [Button("Preview in Scene"), FoldoutGroup("Info")]
        #endif
        private void PreviewInScene()
        {
            ApplyToScene();
        }
        
        #endregion
    }
}
