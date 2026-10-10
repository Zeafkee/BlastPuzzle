using BlastPuzzle.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace BlastPuzzle.EditorTools
{
    internal static class Ui
    {
        public static TMP_FontAsset Font;
        public static Material OutlineMaterial;

        public static readonly Color Ink = new Color32(74, 56, 140, 255);
        public static readonly Color Cream = new Color32(255, 248, 232, 255);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        public static RectTransform Stretch(this RectTransform rect, float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
            return rect;
        }

        public static RectTransform Place(this RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform PlaceRow(this RectTransform rect, float anchorY, float y, float height, float margin)
        {
            rect.anchorMin = new Vector2(0f, anchorY);
            rect.anchorMax = new Vector2(1f, anchorY);
            rect.pivot = new Vector2(0.5f, anchorY);
            rect.anchoredPosition = new Vector2(0f, y);
            rect.sizeDelta = new Vector2(-margin * 2f, height);
            return rect;
        }

        public static Image Image(string name, Transform parent, Sprite sprite, Color color, bool raycast = false)
        {
            RectTransform rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            if (sprite != null && sprite.border.sqrMagnitude > 0f)
                image.type = UnityEngine.UI.Image.Type.Sliced;
            else
                image.preserveAspect = sprite != null;
            return image;
        }

        public static TMP_Text Text(string name, Transform parent, string text, float size, Color color,
            bool outlined = true, TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            RectTransform rect = Rect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font;
            if (outlined && OutlineMaterial != null) label.fontSharedMaterial = OutlineMaterial;
            label.text = text;
            label.fontSize = size;
            label.fontStyle = FontStyles.Bold;
            label.color = color;
            label.alignment = align;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.richText = false;
            return label;
        }

        public static Button Button(string name, Transform parent, Sprite sprite)
        {
            Image image = Image(name, parent, sprite, Color.white, raycast: true);
            image.pixelsPerUnitMultiplier = 1f;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            ColorBlock colors = button.colors;
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f);
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
            var navigation = button.navigation;
            navigation.mode = Navigation.Mode.None;
            button.navigation = navigation;
            image.gameObject.AddComponent<PressScale>();
            return button;
        }

        public static Button TextButton(string name, Transform parent, Sprite sprite, string text, float fontSize, out TMP_Text label)
        {
            Button button = Button(name, parent, sprite);
            label = Text("Label", button.transform, text, fontSize, Color.white);
            ((RectTransform)label.transform).Stretch(16, 22, 16, 8);
            return button;
        }

        public static Button IconButton(string name, Transform parent, Sprite sprite, Sprite icon, float iconSize, out Image iconImage)
        {
            Button button = Button(name, parent, sprite);
            iconImage = Image("Icon", button.transform, icon, Color.white);
            iconImage.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(iconSize, iconSize));
            return button;
        }

        public static CanvasGroup Group(GameObject go)
        {
            var group = go.GetComponent<CanvasGroup>();
            return group != null ? group : go.AddComponent<CanvasGroup>();
        }

        public static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"{target.GetType().Name} has no serialized field '{field}'.", target);
                return;
            }

            switch (value)
            {
                case null: prop.objectReferenceValue = null; break;
                case Object obj: prop.objectReferenceValue = obj; break;
                case Object[] array:
                    prop.arraySize = array.Length;
                    for (int i = 0; i < array.Length; i++)
                        prop.GetArrayElementAtIndex(i).objectReferenceValue = array[i];
                    break;
                case float f: prop.floatValue = f; break;
                case int i: prop.intValue = i; break;
                case bool b: prop.boolValue = b; break;
                case Color c: prop.colorValue = c; break;
                case string s: prop.stringValue = s; break;
                default: Debug.LogError($"Unsupported value type {value.GetType()} for '{field}'."); break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
