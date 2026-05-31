using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Editor
{
    /// <summary>
    /// Centralized editor style helper for consistent UI styling across EasyChart editor windows.
    /// Modify colors and styles here to apply changes globally.
    /// </summary>
    public static class EditorStyleHelper
    {
        // ========== Foldout Highlight Colors ==========
        
        /// <summary>
        /// Background color when foldout is expanded.
        /// </summary>
        public static readonly Color ExpandedBackgroundColor = new Color(1f, 1f, 1f, 0.05f);
        
        /// <summary>
        /// Left border color when foldout is expanded (default/unfocused).
        /// </summary>
        public static readonly Color ExpandedBorderColor = new Color(0.4f, 0.6f, 0.9f, 0.8f);
        
        /// <summary>
        /// Left border color when foldout is expanded AND focused (brighter cyan).
        /// </summary>
        public static readonly Color FocusedBorderColor = new Color(0.3f, 0.9f, 1f, 1f);
        
        /// <summary>
        /// Left border width when foldout is expanded.
        /// </summary>
        public const float ExpandedBorderWidth = 3f;
        
        /// <summary>
        /// Border radius for expanded foldout.
        /// </summary>
        public const float ExpandedBorderRadius = 3f;
        
        /// <summary>
        /// Padding left when foldout is expanded.
        /// </summary>
        public const float ExpandedPaddingLeft = 2f;
        
        /// <summary>
        /// Margin top/bottom when foldout is expanded.
        /// </summary>
        public const float ExpandedMargin = 2f;

        // ========== Methods ==========

        /// <summary>
        /// Creates a styled foldout container with expand/collapse highlight effect.
        /// </summary>
        /// <param name="foldout">The foldout element to wrap</param>
        /// <param name="isExpanded">Initial expanded state</param>
        /// <returns>Container element with the foldout inside</returns>
        public static VisualElement CreateStyledFoldoutContainer(Foldout foldout, bool isExpanded)
        {
            var container = new VisualElement();
            container.Add(foldout);
            
            ApplyExpandedStyle(container, isExpanded);
            
            foldout.RegisterValueChangedCallback(evt =>
            {
                ApplyExpandedStyle(container, evt.newValue);
            });
            
            return container;
        }

        /// <summary>
        /// Applies expanded or collapsed style to a container element.
        /// </summary>
        /// <param name="container">The container element</param>
        /// <param name="isExpanded">Whether the foldout is expanded</param>
        /// <param name="isFocused">Whether the foldout is focused (default false for initialization)</param>
        public static void ApplyExpandedStyle(VisualElement container, bool isExpanded, bool isFocused = false)
        {
            if (container == null) return;
            
            if (isExpanded)
            {
                container.style.borderLeftWidth = ExpandedBorderWidth;
                container.style.borderLeftColor = isFocused ? FocusedBorderColor : ExpandedBorderColor;
                container.style.paddingLeft = ExpandedPaddingLeft;
                container.style.marginTop = ExpandedMargin;
                container.style.marginBottom = ExpandedMargin;
                container.style.borderTopLeftRadius = ExpandedBorderRadius;
                container.style.borderBottomLeftRadius = ExpandedBorderRadius;
            }
            else
            {
                container.style.borderLeftWidth = 0;
                container.style.paddingLeft = 0;
                container.style.marginTop = 0;
                container.style.marginBottom = 0;
            }
        }
        
        /// <summary>
        /// Applies focus style to a container element (brighter border color).
        /// </summary>
        public static void ApplyFocusStyle(VisualElement container, bool isFocused)
        {
            if (container == null) return;
            
            // Only change border color if the container has a border (is expanded)
            if (container.style.borderLeftWidth.value > 0)
            {
                container.style.borderLeftColor = isFocused ? FocusedBorderColor : ExpandedBorderColor;
            }
        }
        
        /// <summary>
        /// Registers focus/blur callbacks on a foldout to update its container's border color.
        /// Uses a flag to prevent initial focus events from triggering the highlight.
        /// </summary>
        public static void RegisterFocusCallbacks(VisualElement container, Foldout foldout)
        {
            if (container == null || foldout == null) return;
            
            // Flag to track if user has interacted with this foldout
            bool userHasInteracted = false;
            
            // Track focus state - only apply if user has interacted and foldout is expanded
            foldout.RegisterCallback<FocusInEvent>(evt =>
            {
                if (userHasInteracted && foldout.value)
                {
                    ApplyFocusStyle(container, true);
                }
            });
            
            foldout.RegisterCallback<FocusOutEvent>(evt =>
            {
                if (userHasInteracted && foldout.value)
                {
                    ApplyFocusStyle(container, false);
                }
            });
            
            // Track clicks on the foldout toggle - this marks user interaction
            // Use schedule to apply style after the value has changed
            foldout.RegisterCallback<ClickEvent>(evt =>
            {
                userHasInteracted = true;
                // Schedule to run after the value change has been processed
                foldout.schedule.Execute(() =>
                {
                    if (foldout.value)
                    {
                        ApplyFocusStyle(container, true);
                    }
                });
            });
        }

        /// <summary>
        /// Removes Unity's default selection highlight from a list item.
        /// Call this on the root element returned by PropertyDrawer.CreatePropertyGUI().
        /// </summary>
        /// <param name="element">The element to remove selection highlight from</param>
        public static void RemoveSelectionHighlight(VisualElement element)
        {
            if (element == null) return;
            
            // Override Unity's default selection background
            element.RegisterCallback<AttachToPanelEvent>(evt =>
            {
                // Find and style the parent list item element
                var parent = element.parent;
                while (parent != null)
                {
                    // Unity uses these class names for list items
                    if (parent.ClassListContains("unity-list-view__item") ||
                        parent.ClassListContains("unity-collection-view__item"))
                    {
                        // Override selection style
                        parent.RegisterCallback<PointerDownEvent>(e =>
                        {
                            parent.schedule.Execute(() =>
                            {
                                // Reset background after Unity applies selection
                                if (parent.ClassListContains("unity-collection-view__item--selected"))
                                {
                                    parent.style.backgroundColor = StyleKeyword.Null;
                                }
                            });
                        });
                        break;
                    }
                    parent = parent.parent;
                }
            });
        }

        /// <summary>
        /// Creates a warning box with consistent styling.
        /// </summary>
        /// <param name="message">Warning message text</param>
        /// <returns>Styled warning box element</returns>
        public static VisualElement CreateWarningBox(string message)
        {
            var warnBox = new VisualElement();
            warnBox.style.borderTopWidth = 1;
            warnBox.style.borderBottomWidth = 1;
            warnBox.style.borderLeftWidth = 1;
            warnBox.style.borderRightWidth = 1;

            var borderColor = new Color(0.1f, 0.1f, 0.1f);
            warnBox.style.borderTopColor = borderColor;
            warnBox.style.borderBottomColor = borderColor;
            warnBox.style.borderLeftColor = borderColor;
            warnBox.style.borderRightColor = borderColor;

            warnBox.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);
            warnBox.style.marginTop = 4;
            warnBox.style.marginBottom = 4;
            warnBox.style.paddingLeft = 6;
            warnBox.style.paddingRight = 6;
            warnBox.style.paddingTop = 4;
            warnBox.style.paddingBottom = 4;
            warnBox.style.borderTopLeftRadius = 3;
            warnBox.style.borderTopRightRadius = 3;
            warnBox.style.borderBottomLeftRadius = 3;
            warnBox.style.borderBottomRightRadius = 3;

            var warn = new Label(message);
            warn.style.opacity = 0.9f;
            warn.style.whiteSpace = WhiteSpace.Normal;
            warnBox.Add(warn);

            return warnBox;
        }
    }
}
