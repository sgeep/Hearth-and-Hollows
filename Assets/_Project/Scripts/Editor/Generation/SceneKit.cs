using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

namespace Hearthdelve.Editor
{
    /// <summary>Scene-building pieces shared by the Phase 1 and Phase 2 generators.</summary>
    public static class SceneKit
    {
        public const int ReferenceWidth = 640;
        public const int ReferenceHeight = 360;
        public static float OrthoSize => ReferenceHeight / 2f / PixelArtImportPostprocessor.PixelsPerUnit;

        /// <summary>Main camera with the URP Pixel Perfect Camera at 640×360 / 32 PPU.</summary>
        public static Camera CreatePixelCamera(Vector3 position, Color background, bool withCinemachineBrain)
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = position;
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = OrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            camGo.AddComponent<UniversalAdditionalCameraData>();
            var ppc = camGo.AddComponent<PixelPerfectCamera>();
            ppc.assetsPPU = PixelArtImportPostprocessor.PixelsPerUnit;
            ppc.refResolutionX = ReferenceWidth;
            ppc.refResolutionY = ReferenceHeight;
            ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;
            ppc.cropFrame = PixelPerfectCamera.CropFrame.None;
            if (withCinemachineBrain) camGo.AddComponent<CinemachineBrain>();
            return cam;
        }

        public static void CreateGlobalLight()
        {
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        public static PanelSettings PanelSettings()
        {
            var panel = ContentGenerator.LoadOrCreate<PanelSettings>($"{EditorPaths.UI}/HearthdelvePanelSettings.asset", p =>
            {
                p.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                p.referenceResolution = new Vector2Int(1280, 720);
                p.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                p.match = 0.5f;
            });
            panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>($"{EditorPaths.UI}/HearthdelveTheme.tss");
            EditorUtility.SetDirty(panel);
            return panel;
        }

        public static T AddDocument<T>(Transform parent, string name, PanelSettings panel, string uxml, float sortingOrder) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var doc = go.AddComponent<UIDocument>();
            doc.panelSettings = panel;
            doc.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{EditorPaths.UI}/{uxml}");
            doc.sortingOrder = sortingOrder;
            return go.AddComponent<T>();
        }

        /// <summary>EventSystem driving UI Toolkit from the UI action map of the project-wide actions.</summary>
        public static void CreateEventSystem(InputActionAsset actions)
        {
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem));
            var module = eventSystem.AddComponent<InputSystemUIInputModule>();
            module.actionsAsset = actions;
            var refs = AssetDatabase.LoadAllAssetsAtPath(EditorPaths.InputActions).OfType<InputActionReference>().ToArray();
            InputActionReference Ref(string action) => refs.FirstOrDefault(r => r.action != null && r.action.actionMap.name == "UI" && r.action.name == action);
            module.move = Ref("Navigate");
            module.submit = Ref("Submit");
            module.cancel = Ref("Cancel");
            module.point = Ref("Point");
            module.leftClick = Ref("Click");
            module.rightClick = Ref("RightClick");
            module.middleClick = Ref("MiddleClick");
            module.scrollWheel = Ref("ScrollWheel");
        }

        /// <summary>Adds a scene to the build list, keeping the scenes already there.</summary>
        public static void AddToBuild(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static SpriteRenderer AddSprite(Transform parent, string name, Sprite sprite, string sortingLayer, int order, Color color, Vector3 localPosition)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = sortingLayer;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }
    }
}
