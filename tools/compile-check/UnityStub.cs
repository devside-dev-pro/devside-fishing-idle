// Stub UnityEngine — OUTIL DE VÉRIFICATION UNIQUEMENT, jamais commité dans le jeu.
// But : compiler Assets/Scripts/Game hors de Unity pour attraper les fautes de frappe,
// les membres inexistants et les erreurs de type AVANT de pousser. Les signatures
// reproduisent l'API réelle d'Unity ; toute divergence ici fabriquerait de fausses
// erreurs (ou en masquerait de vraies).
using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector2 normalized => this;
        public void Normalize() { }
        public static Vector2 zero => default;
        public static Vector2 one => default;
        public static Vector2 right => default;
        public static Vector2 up => default;
        public static Vector2 operator +(Vector2 a, Vector2 b) => a;
        public static Vector2 operator -(Vector2 a, Vector2 b) => a;
        public static Vector2 operator *(Vector2 a, float b) => a;
        public static Vector2 operator *(float a, Vector2 b) => b;
        public static Vector2 operator /(Vector2 a, float b) => a;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a;
        public static Vector2 ClampMagnitude(Vector2 v, float maxLength) => v;
        public static Vector2 down => default(Vector2);
        public static Vector2 left => default(Vector2);
        public static float Distance(Vector2 a, Vector2 b) => 0f;
        public static implicit operator Vector2(Vector3 v) => default;
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public float magnitude => 0f;
        public float sqrMagnitude => 0f;
        public Vector3 normalized => this;
        public void Normalize() { }
        public static Vector3 zero => default;
        public static Vector3 one => default;
        public static Vector3 up => default;
        public static Vector3 right => default;
        public static Vector3 forward => default(Vector3);
        public static Vector3 down => default(Vector3);
        public static Vector3 back => default(Vector3);
        public static Vector3 left => default(Vector3);
        public static Vector3 operator +(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a, Vector3 b) => a;
        public static Vector3 operator -(Vector3 a) => a;
        public static Vector3 operator *(Vector3 a, float b) => a;
        public static Vector3 operator *(float a, Vector3 b) => b;
        public static Vector3 operator /(Vector3 a, float b) => a;
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t) => a;
        public static Vector3 LerpUnclamped(Vector3 a, Vector3 b, float t) => a;
        public static float Distance(Vector3 a, Vector3 b) => 0f;
        public static Vector3 Cross(Vector3 a, Vector3 b) => a;
        public static float Dot(Vector3 a, Vector3 b) => 0f;
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float d) => a;
        public static implicit operator Vector3(Vector2 v) => default;
    }

    public struct Vector4
    {
        public float x, y, z, w;
        public Vector4(float x, float y, float z, float w) { this.x = x; this.y = y; this.z = z; this.w = w; }
    }

    public struct Quaternion
    {
        public static Quaternion identity => default;
        public static Quaternion Euler(float x, float y, float z) => default;
        public static Quaternion LookRotation(Vector3 forward) => default;
        public static Quaternion RotateTowards(Quaternion a, Quaternion b, float maxDegrees) => default(Quaternion);
        public static Quaternion Slerp(Quaternion a, Quaternion b, float t) => a;
        public static Quaternion AngleAxis(float angle, Vector3 axis) => default(Quaternion);
        public static Quaternion operator *(Quaternion a, Quaternion b) => a;
        public static Vector3 operator *(Quaternion a, Vector3 b) => b;
        public Vector3 eulerAngles { get; set; }
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b) { this.r = r; this.g = g; this.b = b; a = 1f; }
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => default;
        public static Color black => default;
        public static Color clear => default;
        public static Color Lerp(Color a, Color b, float t) => a;
        public static Color operator *(Color a, float b) => a;
    }

    public struct Rect
    {
        public Rect(float x, float y, float width, float height) { }
        public float width => 0f;
        public float height => 0f;
    }

    public struct Bounds
    {
        public Bounds(Vector3 center, Vector3 size) { }
        public Vector3 center => default;
        public Vector3 size => default;
        public Vector3 min => default;
        public Vector3 max => default;
        public void Encapsulate(Bounds b) { }
    }

    public struct Plane
    {
        public Plane(Vector3 normal, Vector3 point) { }
        public Plane(Vector3 normal, float d) { }
        public bool Raycast(Ray ray, out float distance) { distance = 0f; return true; }
    }

    public struct Ray
    {
        public Vector3 origin => default;
        public Vector3 direction => default;
        public Vector3 GetPoint(float distance) => default;
    }

    public struct RaycastHit
    {
        public Vector3 point => default(Vector3);
        public Vector3 normal => default(Vector3);
        public float distance => 0f;
        public Transform transform => null;
        public Collider collider => null;
    }

    public static class Mathf
    {
        public const float PI = 3.14159265f;
        public const float Rad2Deg = 57.29578f;
        public const float Deg2Rad = 0.0174533f;
        public const float Infinity = float.PositiveInfinity;
        public static float Sin(float f) => 0f;
        public static float Cos(float f) => 0f;
        public static float Atan2(float y, float x) => 0f;
        public static float Sqrt(float f) => 0f;
        public static float Abs(float f) => 0f;
        public static float Min(float a, float b) => a;
        public static float Max(float a, float b) => a;
        public static int Max(int a, int b) => a;
        public static int Min(int a, int b) => a;
        public static float Clamp(float v, float min, float max) => v;
        public static float Clamp01(float v) => v;
        public static float Lerp(float a, float b, float t) => a;
        public static float MoveTowards(float a, float b, float d) => a;
        public static float SmoothStep(float a, float b, float t) => a;
        public static int CeilToInt(float f) => 0;
        public static int FloorToInt(float f) => 0;
        public static int RoundToInt(float f) => 0;
        public static float Pow(float f, float p) => 0f;
        public static float Repeat(float t, float length) => 0f;
    }

    public static class Random
    {
        public static float value => 0f;
        public static float Range(float min, float max) => min;
        public static int Range(int min, int max) => min;
        public static Vector2 insideUnitCircle => default;
    }

    public static class Time
    {
        public static float time => 0f;
        public static float deltaTime => 0f;
        public static float unscaledTime => 0f;
    }

    public static class Screen
    {
        public static int width => 1080;
        public static int height => 1920;
    }

    public static class Debug
    {
        public static void Log(object message) { }
        public static void LogWarning(object message) { }
        public static void LogError(object message) { }
    }

    public class Object
    {
        public string name { get; set; }
        public string tag { get; set; }
        public HideFlags hideFlags { get; set; }
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object => new T[0];
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sortMode) where T : Object => new T[0];
        public static void Destroy(Object o) { }
        public static void Destroy(Object o, float t) { }
        public static void DestroyImmediate(Object o) { }
        public static T Instantiate<T>(T original) where T : Object => original;
        public static T Instantiate<T>(T original, Transform parent) where T : Object => original;
        public static implicit operator bool(Object o) => true;
    }

    public class Component : Object
    {
        public Transform transform => null;
        public GameObject gameObject => null;
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default;
        public T GetComponentInChildren<T>(bool includeInactive) => default;
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) => new T[0];
        public T AddComponent<T>() where T : Component => default(T);
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour
    {
        public Coroutine StartCoroutine(IEnumerator routine) => null;
        public void StopCoroutine(Coroutine routine) { }
        public void StopAllCoroutines() { }
        public static T FindAnyObjectByType<T>() where T : Object => default;
        public static T FindObjectOfType<T>() where T : Object => default;
    }

    public class Coroutine : Object { }
    public class WaitForSeconds : IEnumerator
    {
        public WaitForSeconds(float seconds) { }
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }
    public class WaitForEndOfFrame : IEnumerator
    {
        public object Current => null;
        public bool MoveNext() => false;
        public void Reset() { }
    }

    public class Transform : Component, IEnumerable
    {
        public Vector3 position { get; set; }
        public Vector3 localPosition { get; set; }
        public Vector3 localScale { get; set; }
        public Vector3 lossyScale => default;
        public Quaternion rotation { get; set; }
        public Quaternion localRotation { get; set; }
        public Vector3 eulerAngles { get; set; }
        public Vector3 localEulerAngles { get; set; }
        public Vector3 forward => default;
        public Vector3 right => default;
        public Vector3 up => default;
        public Transform parent { get; set; }
        public int childCount => 0;
        public Transform GetChild(int index) => null;
        public void SetParent(Transform parent) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public void SetAsFirstSibling() { }
        public void SetAsLastSibling() { }
        public void LookAt(Vector3 target) { }
        public bool IsChildOf(Transform parent) => false;
        public Transform Find(string name) => null;
        public Transform root => null;
        public void Translate(Vector3 t) { }
        public void Rotate(Vector3 r) { }
        public IEnumerator GetEnumerator() => null;
    }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name) { }
        public GameObject(string name, params Type[] components) { }
        public Transform transform => null;
        public bool activeSelf => true;
        public bool activeInHierarchy => true;
        public int layer { get; set; }
        public void SetActive(bool value) { }
        public T GetComponent<T>() => default;
        public T GetComponentInChildren<T>() => default(T);
        public T GetComponentInChildren<T>(bool includeInactive) => default(T);
        public T[] GetComponentsInChildren<T>() => new T[0];
        public T[] GetComponentsInChildren<T>(bool includeInactive) => new T[0];
        public T AddComponent<T>() where T : Component => default(T);
        public Component AddComponent(Type type) => null;
        public static GameObject CreatePrimitive(PrimitiveType type) => null;
        public static GameObject Find(string name) => null;
    }

    public enum PrimitiveType { Sphere, Capsule, Cylinder, Cube, Plane, Quad }
    public enum FindObjectsSortMode { None, InstanceID }
    public enum FindObjectsInactive { Exclude, Include }
    public enum HideFlags { None, HideInHierarchy, DontSave }

    public class RequireComponent : Attribute
    {
        public RequireComponent(Type type) { }
        public RequireComponent(Type a, Type b) { }
    }

    public class TextMesh : Component
    {
        public string text { get; set; }
        public int fontSize { get; set; }
        public float characterSize { get; set; }
        public float lineSpacing { get; set; }
        public TextAnchor anchor { get; set; }
        public TextAlignment alignment { get; set; }
        public Color color { get; set; }
        public Font font { get; set; }
        public FontStyle fontStyle { get; set; }
    }

    public enum TextAlignment { Left, Center, Right }

    public static class RenderSettings
    {
        public static Rendering.AmbientMode ambientMode { get; set; }
        public static Color ambientLight { get; set; }
        public static Color ambientSkyColor { get; set; }
        public static Color ambientEquatorColor { get; set; }
        public static Color ambientGroundColor { get; set; }
        public static float ambientIntensity { get; set; }
        public static bool fog { get; set; }
        public static Color fogColor { get; set; }
        public static float fogDensity { get; set; }
        public static Material skybox { get; set; }
    }

    public static class GUILayout
    {
        public static bool Button(string text, params GUILayoutOption[] options) => false;
        public static void Label(string text, params GUILayoutOption[] options) { }
        public static void Space(float pixels) { }
        public static void BeginVertical(params GUILayoutOption[] options) { }
        public static void EndVertical() { }
        public static void BeginHorizontal(params GUILayoutOption[] options) { }
        public static void EndHorizontal() { }
        public static void BeginArea(Rect screenRect) { }
        public static void BeginArea(Rect screenRect, GUIStyle style) { }
        public static Vector2 BeginScrollView(Vector2 scrollPosition, params GUILayoutOption[] options) => scrollPosition;
        public static void EndScrollView() { }
        public static void Box(string text, params GUILayoutOption[] options) { }
        public static bool Toggle(bool value, string text, params GUILayoutOption[] options) => value;
        public static void EndArea() { }
        public static GUILayoutOption Height(float value) => null;
        public static GUILayoutOption Width(float value) => null;
        public static GUILayoutOption ExpandWidth(bool expand) => null;
    }

    public class GUILayoutOption { }

    public static class GUI
    {
        public static bool enabled { get; set; }
        public static int depth { get; set; }
        public static Color color { get; set; }
        public static Color backgroundColor { get; set; }
        public static GUISkin skin { get; set; }
    }

    public class GUISkin : Object
    {
        public GUIStyle label { get; set; }
        public GUIStyle button { get; set; }
        public GUIStyle box { get; set; }
        public GUIStyle window { get; set; }
        public GUIStyle textField { get; set; }
    }

    public class GUIStyle
    {
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
    }

    public class Renderer : Component
    {
        public Material material { get; set; }
        public Material[] materials { get; set; }
        public Material sharedMaterial { get; set; }
        public Material[] sharedMaterials { get; set; }
        public Bounds bounds => default;
        public bool enabled { get; set; }
        public ShadowCastingModeHolder shadowCastingMode { get; set; }
        public bool receiveShadows { get; set; }
        public class ShadowCastingModeHolder { }
    }

    public class MeshRenderer : Renderer { }
    public class LineRenderer : Renderer
    {
        public int positionCount { get; set; }
        public float widthMultiplier { get; set; }
        public float startWidth { get; set; }
        public float endWidth { get; set; }
        public Color startColor { get; set; }
        public Color endColor { get; set; }
        public bool useWorldSpace { get; set; }
        public void SetPosition(int index, Vector3 position) { }
    }

    public class Mesh : Object
    {
        public Vector3[] vertices { get; set; }
        public int[] triangles { get; set; }
        public Vector2[] uv { get; set; }
        public void RecalculateNormals() { }
    }

    public class MeshFilter : Component
    {
        public Mesh mesh { get; set; }
        public Mesh sharedMesh { get; set; }
    }

    public class Collider : Component { }
    public class MeshCollider : Collider { public Mesh sharedMesh { get; set; } }
    public class BoxCollider : Collider { }

    public static class Physics
    {
        public static bool Raycast(Ray ray, out RaycastHit hit, float maxDistance) { hit = default(RaycastHit); return false; }
        public static bool Raycast(Vector3 origin, Vector3 direction, out RaycastHit hit, float maxDistance) { hit = default(RaycastHit); return false; }
        public static RaycastHit[] RaycastAll(Ray ray, float maxDistance) => new RaycastHit[0];
        public static RaycastHit[] RaycastAll(Vector3 origin, Vector3 direction, float maxDistance) => new RaycastHit[0];
        public static void SyncTransforms() { }
    }

    public class Material : Object
    {
        public Material(Shader shader) { }
        public Color color { get; set; }
        public Texture mainTexture { get; set; }
        public Shader shader { get; set; }
        public bool HasProperty(string name) => false;
        public Color GetColor(string name) => default;
        public void SetColor(string name, Color value) { }
        public void SetFloat(string name, float value) { }
        public void SetVector(string name, Vector4 value) { }
        public void SetTexture(string name, Texture value) { }
        public void EnableKeyword(string keyword) { }
    }

    public class Shader : Object
    {
        public static Shader Find(string name) => null;
    }

    public class Texture : Object { }
    public class Texture2D : Texture
    {
        public Texture2D(int width, int height) { }
        public Texture2D(int width, int height, TextureFormat format, bool mipChain) { }
        public int width => 0;
        public int height => 0;
        public FilterMode filterMode { get; set; }
        public TextureWrapMode wrapMode { get; set; }
        public void SetPixel(int x, int y, Color color) { }
        public void SetPixels(Color[] colors) { }
        public void Apply() { }
    }

    public enum TextureFormat { RGBA32, ARGB32, RGB24 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Repeat, Clamp }

    public class Sprite : Object
    {
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot) => null;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => null;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit, uint extrude, SpriteMeshType meshType, Vector4 border) => null;
    }

    public enum SpriteMeshType { FullRect, Tight }

    public class Camera : Behaviour
    {
        public static Camera main => null;
        public bool orthographic { get; set; }
        public float orthographicSize { get; set; }
        public float fieldOfView { get; set; }
        public float nearClipPlane { get; set; }
        public float farClipPlane { get; set; }
        public Color backgroundColor { get; set; }
        public CameraClearFlags clearFlags { get; set; }
        public Ray ScreenPointToRay(Vector3 position) => default;
        public Ray ScreenPointToRay(Vector2 position) => default;
        public Vector3 WorldToScreenPoint(Vector3 position) => default;
    }

    public enum CameraClearFlags { Skybox, SolidColor, Depth, Nothing }

    public class Light : Behaviour
    {
        public LightType type { get; set; }
        public Color color { get; set; }
        public float intensity { get; set; }
        public LightShadows shadows { get; set; }
    }

    public enum LightType { Spot, Directional, Point, Area }
    public enum LightShadows { None, Hard, Soft }

    public static class Resources
    {
        public static T GetBuiltinResource<T>(string path) where T : Object => default(T);
        public static T Load<T>(string path) where T : Object => default(T);
        public static Object Load(string path) => null;
        public static T[] LoadAll<T>(string path) where T : Object => new T[0];
    }

    public static class Input
    {
        public static Vector3 mousePosition => default;
        public static bool GetMouseButton(int button) => false;
        public static bool GetMouseButtonDown(int button) => false;
        public static bool GetMouseButtonUp(int button) => false;
        public static int touchCount => 0;
        public static Touch GetTouch(int index) => default;
    }

    public struct Touch
    {
        public Vector2 position => default;
        public TouchPhase phase => default;
        public int fingerId => 0;
    }

    public enum TouchPhase { Began, Moved, Stationary, Ended, Canceled }

    public static class Application
    {
        public static bool isPlaying => true;
        public static string persistentDataPath => "";
        public static bool isEditor => false;
    }

    public class Font : Object
    {
        public Material material { get; set; }
        public static Font CreateDynamicFontFromOSFont(string fontname, int size) => null;
        public static string[] GetOSInstalledFontNames() => new string[0];
    }

    public enum TextAnchor
    {
        UpperLeft, UpperCenter, UpperRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        LowerLeft, LowerCenter, LowerRight,
    }

    public enum FontStyle { Normal, Bold, Italic, BoldAndItalic }
    public enum HorizontalWrapMode { Wrap, Overflow }
    public enum VerticalWrapMode { Truncate, Overflow }

    public class Animation : Behaviour { public void Play() { } }
    public class Animator : Behaviour { public void Play(string name) { } public void SetTrigger(string name) { } }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Rect rect => default;
    }

    public class Canvas : Behaviour
    {
        public RenderMode renderMode { get; set; }
        public Camera worldCamera { get; set; }
        public int sortingOrder { get; set; }
    }

    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }

    public class Space { }

    public static class JsonUtility
    {
        public static string ToJson(object obj) => "";
        public static string ToJson(object obj, bool prettyPrint) => "";
        public static T FromJson<T>(string json) => default(T);
        public static void FromJsonOverwrite(string json, object objectToOverwrite) { }
    }
}

namespace UnityEngine.Rendering
{
    public enum ShadowCastingMode { Off, On, TwoSided, ShadowsOnly }
    public enum AmbientMode { Skybox, Trilight, Flat, Custom }
}

namespace UnityEngine.UI
{
    using UnityEngine;

    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public RectTransform rectTransform => null;
        public Material material { get; set; }
    }

    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
        public Image.Type type { get; set; }
        public bool preserveAspect { get; set; }
        public float fillAmount { get; set; }
        public bool fillCenter { get; set; }
        public enum Type { Simple, Sliced, Tiled, Filled }
    }

    public class Text : Graphic
    {
        public string text { get; set; }
        public Font font { get; set; }
        public int fontSize { get; set; }
        public FontStyle fontStyle { get; set; }
        public TextAnchor alignment { get; set; }
        public bool resizeTextForBestFit { get; set; }
        public HorizontalWrapMode horizontalOverflow { get; set; }
        public VerticalWrapMode verticalOverflow { get; set; }
        public float lineSpacing { get; set; }
        public bool supportRichText { get; set; }
    }

    public class Shadow : Behaviour
    {
        public Color effectColor { get; set; }
        public Vector2 effectDistance { get; set; }
    }

    public class Outline : Shadow { }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public ColorBlock colors { get; set; }
        public Graphic targetGraphic { get; set; }
        public Transition transition { get; set; }
        public enum Transition { None, ColorTint, SpriteSwap, Animation }
    }

    public class ButtonClickedEvent
    {
        public void AddListener(UnityEngine.Events.UnityAction call) { }
        public void RemoveAllListeners() { }
    }

    public struct ColorBlock
    {
        public Color normalColor { get; set; }
        public Color highlightedColor { get; set; }
        public Color pressedColor { get; set; }
        public Color selectedColor { get; set; }
        public Color disabledColor { get; set; }
        public float colorMultiplier { get; set; }
        public float fadeDuration { get; set; }
    }

    public class Button : Selectable
    {
        public ButtonClickedEvent onClick => null;
    }

    public class LayoutElement : Behaviour
    {
        public float preferredHeight { get; set; }
        public float preferredWidth { get; set; }
        public float minHeight { get; set; }
        public float minWidth { get; set; }
        public float flexibleHeight { get; set; }
        public float flexibleWidth { get; set; }
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding { get; set; }
        public TextAnchor childAlignment { get; set; }
    }

    public class HorizontalOrVerticalLayoutGroup : LayoutGroup
    {
        public float spacing { get; set; }
        public bool childForceExpandWidth { get; set; }
        public bool childForceExpandHeight { get; set; }
        public bool childControlWidth { get; set; }
        public bool childControlHeight { get; set; }
    }

    public class VerticalLayoutGroup : HorizontalOrVerticalLayoutGroup { }
    public class HorizontalLayoutGroup : HorizontalOrVerticalLayoutGroup { }

    public class ContentSizeFitter : Behaviour
    {
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
        public enum FitMode { Unconstrained, MinSize, PreferredSize }
    }

    public class ScrollRect : Behaviour
    {
        public RectTransform content { get; set; }
        public RectTransform viewport { get; set; }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public float scrollSensitivity { get; set; }
        public MovementType movementType { get; set; }
        public enum MovementType { Unrestricted, Elastic, Clamped }
    }

    public class Mask : Behaviour { public bool showMaskGraphic { get; set; } }
    public class RectMask2D : Behaviour { }
    public class CanvasScaler : Behaviour
    {
        public ScaleMode uiScaleMode { get; set; }
        public Vector2 referenceResolution { get; set; }
        public float matchWidthOrHeight { get; set; }
        public enum ScaleMode { ConstantPixelSize, ScaleWithScreenSize, ConstantPhysicalSize }
    }
    public class GraphicRaycaster : Behaviour { }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction();
}

namespace UnityEngine.EventSystems
{
    using UnityEngine;

    public class EventSystem : Behaviour
    {
        public static EventSystem current => null;
        public bool IsPointerOverGameObject() => false;
        public bool IsPointerOverGameObject(int pointerId) => false;
    }

    public class StandaloneInputModule : Behaviour { }
    public class PointerEventData { public PointerEventData(EventSystem eventSystem) { } }
}

namespace UnityEngine
{
    public class RectOffset
    {
        public RectOffset() { }
        public RectOffset(int left, int right, int top, int bottom) { }
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }
}
