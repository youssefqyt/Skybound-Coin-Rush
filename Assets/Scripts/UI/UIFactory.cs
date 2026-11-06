using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Skybound
{
    /// <summary>
    /// Runtime UI construction helpers. Everything is created in code so the project
    /// has no prefab or font dependency beyond the bundled TMP essentials.
    /// </summary>
    public static class UIFactory
    {
        private static TMP_FontAsset font;

        public static TMP_FontAsset Font
        {
            get
            {
                if (font == null)
                {
                    font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                    if (font == null && TMP_Settings.defaultFontAsset != null)
                        font = TMP_Settings.defaultFontAsset;
                }
                return font;
            }
        }

        public static Canvas CreateCanvas(Transform parent, string name = "Canvas", int order = 10)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<StandaloneInputModule>();
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name, float margin = 0f)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(margin, margin);
            rt.offsetMax = new Vector2(-margin, -margin);
            return rt;
        }

        public static Image Image(RectTransform rt, Sprite sprite, Color color, bool sliced = false,
            bool raycast = false)
        {
            // A GameObject can only host one Graphic. Reuse an existing Image
            // instead of throwing "Can't add 'Image' ... already added".
            Image image = rt.GetComponent<Image>();
            if (image == null) image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sliced)
            {
                image.type = UnityEngine.UI.Image.Type.Sliced;
                image.pixelsPerUnitMultiplier = 1f;
            }
            return image;
        }

        public static Image Panel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size,
            Color color, float margin = 0f)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            return Image(rt, Art.Panel, color, true);
        }

        public static Image FullPanel(Transform parent, string name, Color color, bool raycast = false)
        {
            RectTransform rt = Stretch(parent, name);
            return Image(rt, Art.White, color, false, raycast);
        }

        public static RectTransform Dim(Transform parent, float alpha = 0.78f)
        {
            RectTransform rt = Stretch(parent, "Dim");
            Image img = Image(rt, Art.White, new Color(0.02f, 0.04f, 0.09f, alpha), false, true);
            img.raycastTarget = true;
            return rt;
        }

        public static TextMeshProUGUI Text(Transform parent, string name, string value, float size, Color color,
            Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 box,
            TextAlignmentOptions align = TextAlignmentOptions.Center, bool bold = false)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, pivot, position, box);
            TextMeshProUGUI text = rt.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = Font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.richText = true;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;            if (bold) text.fontStyle = FontStyles.Bold;
            return text;
        }

        public static TextMeshProUGUI Label(Transform parent, string name, string value, float size, Color color,
            TextAlignmentOptions align, Vector2 anchor, Vector2 position, Vector2 box, bool bold = false)
        {
            Vector2 pivot = new Vector2(0f, 0.5f);
            if (align == TextAlignmentOptions.Center || align == TextAlignmentOptions.Top) pivot = new Vector2(0.5f, 0.5f);
            if (align == TextAlignmentOptions.Right || align == TextAlignmentOptions.TopRight) pivot = new Vector2(1f, 0.5f);
            return Text(parent, name, value, size, color, anchor, pivot, position, box, align, bold);
        }

        public static Image Icon(Transform parent, string iconName, Vector2 anchor, Vector2 position, float size,
            Color color)
        {
            RectTransform rt = Rect(parent, "Icon_" + iconName, anchor, anchor, new Vector2(0.5f, 0.5f), position,
                new Vector2(size, size));
            return Image(rt, Art.Icon(iconName), color);
        }

        public static Image Sprite(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pivot, Vector2 position,
            Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, "Sprite", anchor, anchor, pivot, position, size);
            return Image(rt, sprite, color);
        }

        public static Button Button(Transform parent, string name, string label, Color background,
            Color textColor, Vector2 anchor, Vector2 position, Vector2 size, UnityAction onClick,
            string iconName = null, float fontSize = 30f)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            Image bg = Image(rt, Art.Panel, background, true, true);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);

            if (!string.IsNullOrEmpty(iconName))
            {
                Vector2 iconPos = new Vector2(-size.x * 0.5f + 44f, 0f);
                Image icon = Icon(rt, iconName, new Vector2(0.5f, 0.5f), iconPos, 34f, textColor);
                Text(rt, "Label", label, fontSize, textColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(16f, 0f), new Vector2(size.x - 70f, size.y - 12f),
                    TextAlignmentOptions.Center, true);
            }
            else
            {
                Text(rt, "Label", label, fontSize, textColor, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(size.x - 24f, size.y - 12f), TextAlignmentOptions.Center, true);
            }
            return button;
        }

        public static Button IconButton(Transform parent, string name, string iconName, Color background,
            Color iconColor, Vector2 anchor, Vector2 position, Vector2 size, UnityAction onClick)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            Image bg = Image(rt, Art.Circle(128), background);
            bg.raycastTarget = true;
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(onClick);
            Icon(rt, iconName, new Vector2(0.5f, 0.5f), Vector2.zero, size.x * 0.46f, iconColor);
            return button;
        }

        public static Slider Slider(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size,
            float value, Color accent, UnityAction<float> onChanged)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            Image bg = Image(rt, Art.Pill, new Color(0.08f, 0.13f, 0.2f, 1f), true, true);

            RectTransform fillArea = Rect(rt, "FillArea", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-14f, -10f));
            Image fill = Image(fillArea, Art.Pill, accent, true);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;

            RectTransform handleArea = Rect(rt, "HandleArea", new Vector2(0f, 0f), new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-18f, 0f));
            RectTransform handle = Rect(handleArea, "Handle", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(28f, 28f));
            Image handleImage = Image(handle, Art.Circle(64), Color.white);

            Slider slider = rt.gameObject.AddComponent<Slider>();
            slider.fillRect = fillArea;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = Mathf.Clamp01(value);
            if (onChanged != null) slider.onValueChanged.AddListener(onChanged);
            return slider;
        }

        public static Toggle Toggle(Transform parent, string name, string label, Vector2 anchor, Vector2 position,
            Vector2 size, bool isOn, UnityAction<bool> onChanged)
        {
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            Image bg = Image(rt, Art.Pill, new Color(0.09f, 0.14f, 0.22f, 1f), true, true);

            RectTransform knobBox = Rect(rt, "Knob", new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(-34f, 0f), new Vector2(46f, 46f));
            Image knob = Image(knobBox, Art.Circle(64), isOn ? Palette.Success : Palette.Muted);

            TextMeshProUGUI state = Text(rt, "State", isOn ? "ON" : "OFF", 22f,
                isOn ? Palette.Success : Palette.Muted, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-76f, 0f), new Vector2(90f, 40f), TextAlignmentOptions.Right, true);
            Text(rt, "Label", label, 25f, Palette.Text, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(26f, 0f), new Vector2(size.x - 180f, 40f), TextAlignmentOptions.Left, true);

            Toggle toggle = rt.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = knob;
            toggle.isOn = isOn;
            toggle.onValueChanged.AddListener(value =>
            {
                knob.color = value ? Palette.Success : Palette.Muted;
                state.text = value ? "ON" : "OFF";
                state.color = value ? Palette.Success : Palette.Muted;
                if (onChanged != null) onChanged(value);
            });
            return toggle;
        }

        public class ProgressBar
        {
            public RectTransform Root;
            public Image Fill;
            public TextMeshProUGUI Label;
            public TextMeshProUGUI Title;

            public void Set(float value, string label = null)
            {
                if (Fill != null) Fill.fillAmount = Mathf.Clamp01(value);
                if (Label != null && label != null) Label.text = label;
            }
        }

        public static ProgressBar Bar(Transform parent, string name, string title, Vector2 anchor, Vector2 position,
            Vector2 size, Color fillColor, bool showLabel = false)
        {
            ProgressBar bar = new ProgressBar();
            RectTransform rt = Rect(parent, name, anchor, anchor, new Vector2(0.5f, 0.5f), position, size);
            bar.Root = rt;
            Image bg = Image(rt, Art.Pill, new Color(0.06f, 0.1f, 0.16f, 1f), true);
            bg.raycastTarget = false;
            RectTransform fillArea = Rect(rt, "FillArea", Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(-8f, -8f));
            Image fill = Image(fillArea, Art.Pill, fillColor, true);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.raycastTarget = false;
            bar.Fill = fill;

            if (!string.IsNullOrEmpty(title))
            {
                bar.Title = Text(rt, "Title", title, 18f, Palette.Muted, new Vector2(0f, 1f), new Vector2(0f, 0f),
                    new Vector2(2f, 10f), new Vector2(size.x - 10f, 26f), TextAlignmentOptions.Left, true);
            }
            if (showLabel)
            {
                bar.Label = Text(rt, "Value", "", 18f, Palette.Text, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                    new Vector2(-12f, 0f), new Vector2(size.x - 24f, size.y - 4f),
                    TextAlignmentOptions.Right, true);
            }
            return bar;
        }

        public static CanvasGroup Group(Transform parent, string name, out RectTransform rt)
        {
            rt = Stretch(parent, name);
            CanvasGroup group = rt.gameObject.AddComponent<CanvasGroup>();
            return group;
        }

        public static void Clear(Transform target)
        {
            for (int i = target.childCount - 1; i >= 0; i--)
                UnityEngine.Object.Destroy(target.GetChild(i).gameObject);
        }

        public static Color Lighten(Color c, float amount)
        {
            return Color.Lerp(c, Color.white, amount);
        }

        public static Color Darken(Color c, float amount)
        {
            return Color.Lerp(c, Color.black, amount);
        }
    }

    /// <summary>Fits a full-screen UI rect to the device safe area (notches, rounded corners).</summary>
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform rect;
        private Rect lastSafe;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != lastSafe || Screen.width != lastWidth || Screen.height != lastHeight)
                Apply();
        }

        private void Apply()
        {
            if (rect == null) return;
            Rect safe = Screen.safeArea;
            lastSafe = safe;
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            min.x /= Screen.width;
            min.y /= Screen.height;
            max.x /= Screen.width;
            max.y /= Screen.height;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
