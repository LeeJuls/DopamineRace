using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    public enum ChartTextRole
    {
        AxisLabel,
        Legend,
        Tooltip,
        SeriesLabel,
    }

    internal static class ChartTextStyleApplier
    {
#if UNITY_EDITOR
        internal static bool EditorDebugLogs = true;

        private static HashSet<string> s_editorLogKeys;

        private static void EditorLogOnce(string key, string message)
        {
            if (!EditorDebugLogs) return;
            if (string.IsNullOrEmpty(key)) return;
            if (string.IsNullOrEmpty(message)) return;

            if (s_editorLogKeys == null) s_editorLogKeys = new HashSet<string>();
            if (!s_editorLogKeys.Add(key)) return;

            Debug.LogWarning(message);
        }

        private static string EditorCtorSummary(Type t)
        {
            if (t == null) return "<null>";
            try
            {
                var ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                if (ctors == null || ctors.Length == 0) return "<no ctors>";

                string s = string.Empty;
                for (int i = 0; i < ctors.Length; i++)
                {
                    var ps = ctors[i].GetParameters();
                    if (i > 0) s += "; ";
                    s += "(";
                    for (int j = 0; j < ps.Length; j++)
                    {
                        if (j > 0) s += ", ";
                        s += ps[j].ParameterType != null ? ps[j].ParameterType.FullName : "<null>";
                    }
                    s += ")";
                }

                return s;
            }
            catch
            {
                return "<exception>";
            }
        }
#endif

        private sealed class AppliedState
        {
            public int ThemeInstanceId;
            public int FontObjectInstanceId;
            public int Role;
            public float FontScale;
            public float BaseFontSize;
            public float AppliedFontSize;
            public float RoleFontSizeOverride;
        }

        private static bool s_styleFontDefinitionChecked;
        private static PropertyInfo s_unityFontDefinitionProp;
        private static ConstructorInfo s_styleFontDefinitionFromKeywordCtor;

        public static void ApplyLabel(Label label, object context, ChartTextRole role)
        {
            if (label == null) return;

            var theme = ResolveTheme(context);
            ApplyLabel(label, theme, role);
        }

        public static void ApplyLabel(Label label, ChartTheme theme, ChartTextRole role)
        {
            if (label == null) return;

            var state = label.userData as AppliedState;

            int themeId = theme != null ? theme.GetInstanceID() : 0;
            UnityEngine.Object fontObj = theme != null ? theme.primaryFont : null;
            int fontId = fontObj != null ? fontObj.GetInstanceID() : 0;

            float scale = theme != null ? theme.fontScale : 1f;
            float roleSizeOverride = ResolveRoleFontSizeOverride(theme, role);
            float appliedRoleSizeOverride = roleSizeOverride;
            if (roleSizeOverride >= 0f && HasInlineFontSize(label))
            {
                appliedRoleSizeOverride = -1f;
            }

            if (state != null
                && state.ThemeInstanceId == themeId
                && state.FontObjectInstanceId == fontId
                && state.Role == (int)role
                && Mathf.Approximately(state.FontScale, scale)
                && Mathf.Approximately(state.RoleFontSizeOverride, appliedRoleSizeOverride))
            {
                if (appliedRoleSizeOverride >= 0f)
                {
                    if (Mathf.Approximately(state.AppliedFontSize, appliedRoleSizeOverride)) return;
                }
                else
                {
                    if (scale == 1f || role == ChartTextRole.Tooltip)
                    {
                        return;
                    }

                    float current = GetCurrentFontSize(label);
                    if (current > 0f && Mathf.Approximately(current, state.AppliedFontSize)) return;
                }
            }

            if (state == null)
            {
                state = new AppliedState();
                label.userData = state;
            }

            state.ThemeInstanceId = themeId;
            state.FontObjectInstanceId = fontId;
            state.Role = (int)role;
            state.FontScale = scale;
            state.RoleFontSizeOverride = appliedRoleSizeOverride;

            ApplyFont(label, fontObj);

            if (appliedRoleSizeOverride >= 0f)
            {
                label.style.fontSize = appliedRoleSizeOverride;
                state.BaseFontSize = appliedRoleSizeOverride;
                state.AppliedFontSize = appliedRoleSizeOverride;
                return;
            }

            if (scale != 1f && role != ChartTextRole.Tooltip)
            {
                float current = GetCurrentFontSize(label);
                float baseSize;

                if (state.BaseFontSize > 0f && Mathf.Approximately(current, state.AppliedFontSize))
                {
                    baseSize = state.BaseFontSize;
                }
                else
                {
                    baseSize = current;
                }

                if (baseSize > 0f)
                {
                    float scaled = baseSize * scale;
                    label.style.fontSize = scaled;
                    state.BaseFontSize = baseSize;
                    state.AppliedFontSize = scaled;
                }
            }
            else
            {
                float current = GetCurrentFontSize(label);
                state.BaseFontSize = current;
                state.AppliedFontSize = current;
            }
        }

        private static bool HasInlineFontSize(Label label)
        {
            if (label == null) return false;

            try
            {
                return label.style.fontSize.keyword == StyleKeyword.Undefined;
            }
            catch
            {
                return false;
            }
        }

        private static ChartTheme ResolveTheme(object context)
        {
            if (context is ChartElement chart) return chart.GetEffectiveThemeInternal();

            if (context is VisualElement ve)
            {
                if (ve.userData is ChartElement chartFromUserData) return chartFromUserData.GetEffectiveThemeInternal();
            }

            return ChartThemeRegistry.GlobalTheme;
        }

        private static float ResolveRoleFontSizeOverride(ChartTheme theme, ChartTextRole role)
        {
            if (theme == null) return -1f;

            switch (role)
            {
                case ChartTextRole.AxisLabel:
                    return theme.axisFontSize;
                case ChartTextRole.Legend:
                    return theme.legendFontSize;
                case ChartTextRole.Tooltip:
                    return theme.tooltipFontSize;
                case ChartTextRole.SeriesLabel:
                    return theme.seriesLabelFontSize;
                default:
                    return -1f;
            }
        }

        private static void ApplyFont(Label label, UnityEngine.Object fontObj)
        {
            if (label == null) return;

            if (fontObj == null)
            {
                label.style.unityFont = null;
                TryClearUnityFontDefinition(label);
                return;
            }

            if (fontObj is Font font)
            {
                TryClearUnityFontDefinition(label);
                label.style.unityFont = font;
                return;
            }

            if (IsTextCoreFontAsset(fontObj))
            {
                if (TrySetUnityFontDefinition(label, fontObj))
                {
                    label.style.unityFont = null;
                }
                else
                {
                    label.style.unityFont = null;
                    TryClearUnityFontDefinition(label);

                    if (TryGetTextCoreSourceFont(fontObj, out var sourceFont) && sourceFont != null)
                    {
                        label.style.unityFont = sourceFont;
#if UNITY_EDITOR
                        EditorLogOnce($"fontasset_fallback_sourcefont:{fontObj.GetType().FullName}", $"[EasyChart] Fallback: using TextCore FontAsset.sourceFontFile for Label. label='{label.name}' sourceFont='{sourceFont.name}'");
#endif
                        return;
                    }
#if UNITY_EDITOR
                    EditorLogOnce($"fontasset_apply_fail:{fontObj.GetType().FullName}", $"[EasyChart] Failed to apply TextCore FontAsset to Label. label='{label.name}' fontObjType='{fontObj.GetType().FullName}'");
#endif
                }

                return;
            }

#if UNITY_EDITOR
            if (fontObj != null)
                EditorLogOnce($"fontobj_unsupported:{fontObj.GetType().FullName}", $"[EasyChart] Unsupported theme primaryFont type. label='{label.name}' fontObjType='{fontObj.GetType().FullName}'");
#endif
        }

        private static float GetCurrentFontSize(Label label)
        {
            if (label == null) return -1f;

            try
            {
                var fs = label.style.fontSize;
                if (fs.keyword == StyleKeyword.Undefined)
                {
                    return fs.value.value;
                }
            }
            catch
            {
            }

            return label.resolvedStyle.fontSize;
        }

        private static bool IsTextCoreFontAsset(UnityEngine.Object obj)
        {
            if (obj == null) return false;
            var t = obj.GetType();
            return t != null && t.Name == "FontAsset" && t.Namespace == "UnityEngine.TextCore.Text";
        }

        private static bool TryGetTextCoreSourceFont(UnityEngine.Object fontAsset, out Font sourceFont)
        {
            sourceFont = null;
            if (fontAsset == null) return false;

            var t = fontAsset.GetType();
            try
            {
                var p = t.GetProperty("sourceFontFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && typeof(Font).IsAssignableFrom(p.PropertyType))
                {
                    sourceFont = p.GetValue(fontAsset) as Font;
                    return sourceFont != null;
                }

                var f = t.GetField("sourceFontFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && typeof(Font).IsAssignableFrom(f.FieldType))
                {
                    sourceFont = f.GetValue(fontAsset) as Font;
                    return sourceFont != null;
                }

                var f2 = t.GetField("m_SourceFontFile", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f2 != null && typeof(Font).IsAssignableFrom(f2.FieldType))
                {
                    sourceFont = f2.GetValue(fontAsset) as Font;
                    return sourceFont != null;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void EnsureUnityFontDefinitionReflection(Label label)
        {
            if (s_styleFontDefinitionChecked) return;
            s_styleFontDefinitionChecked = true;

            if (label == null) return;

            s_unityFontDefinitionProp = typeof(IStyle).GetProperty("unityFontDefinition", BindingFlags.Instance | BindingFlags.Public);
            if (s_unityFontDefinitionProp == null)
            {
                var style = label.style;
                if (style != null)
                {
                    s_unityFontDefinitionProp = style.GetType().GetProperty("unityFontDefinition", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                }
            }

            s_styleFontDefinitionFromKeywordCtor = typeof(StyleFontDefinition).GetConstructor(new[] { typeof(StyleKeyword) });
        }

        private static bool TrySetUnityFontDefinition(Label label, object fontAsset)
        {
            if (label == null) return false;
            if (fontAsset == null) return false;

            EnsureUnityFontDefinitionReflection(label);
            if (s_unityFontDefinitionProp == null) return false;

            if (!TryCreateStyleFontDefinitionForFontAsset(fontAsset, out object styleValue)) return false;

            try
            {
                s_unityFontDefinitionProp.SetValue(label.style, styleValue);
                return true;
            }
            catch
            {
#if UNITY_EDITOR
                EditorLogOnce($"unityfontdef_set_fail:{fontAsset.GetType().FullName}", $"[EasyChart] Failed to set unityFontDefinition. styleProp='{s_unityFontDefinitionProp?.DeclaringType?.FullName}.{s_unityFontDefinitionProp?.Name}' fontAssetType='{fontAsset.GetType().FullName}'");
#endif
                return false;
            }
        }

        private static bool TryCreateStyleFontDefinitionForFontAsset(object fontAsset, out object styleValue)
        {
            styleValue = null;
            if (fontAsset == null) return false;

            var fontAssetType = fontAsset.GetType();
            var ctor = GetStyleFontDefinitionCtorForParamType(fontAssetType);
            if (ctor != null)
            {
                try
                {
                    styleValue = ctor.Invoke(new[] { fontAsset });
                    return styleValue != null;
                }
                catch
                {
                    return false;
                }
            }

            if (TryCreateFontDefinitionForFontAsset(fontAsset, out object fontDefinition, out Type fontDefinitionType))
            {
                var ctor2 = GetStyleFontDefinitionCtorForParamType(fontDefinitionType);
                if (ctor2 != null)
                {
                    try
                    {
                        styleValue = ctor2.Invoke(new[] { fontDefinition });
                        return styleValue != null;
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

#if UNITY_EDITOR
            var asm = typeof(StyleFontDefinition).Assembly;
            var fdType = asm.GetType("UnityEngine.UIElements.FontDefinition");
            EditorLogOnce(
                $"stylefontdef_create_fail:{fontAssetType.FullName}",
                $"[EasyChart] Cannot create StyleFontDefinition for FontAsset. stylePropFound={(s_unityFontDefinitionProp != null)} fontAssetType='{fontAssetType.FullName}' styleFontDefCtors={EditorCtorSummary(typeof(StyleFontDefinition))} fontDefType='{fdType?.FullName}' fontDefCtors={EditorCtorSummary(fdType)}");
#endif
            return false;
        }

        private static ConstructorInfo GetStyleFontDefinitionCtorForParamType(Type t)
        {
            if (t == null) return null;

            var ctors = typeof(StyleFontDefinition).GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < ctors.Length; i++)
            {
                var c = ctors[i];
                var ps = c.GetParameters();
                if (ps.Length != 1) continue;
                if (ps[0].ParameterType.IsAssignableFrom(t)) return c;
            }

            return null;
        }

        private static bool TryCreateFontDefinitionForFontAsset(object fontAsset, out object fontDefinition, out Type fontDefinitionType)
        {
            fontDefinition = null;
            fontDefinitionType = null;
            if (fontAsset == null) return false;

            var asm = typeof(StyleFontDefinition).Assembly;
            var fdType = asm.GetType("UnityEngine.UIElements.FontDefinition");
            if (fdType == null) return false;

            var assetType = fontAsset.GetType();
            try
            {
                var ctors = fdType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < ctors.Length; i++)
                {
                    var c = ctors[i];
                    var ps = c.GetParameters();
                    if (ps.Length != 1) continue;
                    if (!ps[0].ParameterType.IsAssignableFrom(assetType)) continue;

                    fontDefinition = c.Invoke(new[] { fontAsset });
                    fontDefinitionType = fdType;
                    return fontDefinition != null;
                }
            }
            catch
            {
            }

            object fd;
            try
            {
                fd = Activator.CreateInstance(fdType);
            }
            catch
            {
                return false;
            }

            try
            {
                var p = fdType.GetProperty("fontAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null && p.CanWrite && p.PropertyType.IsAssignableFrom(assetType))
                {
                    p.SetValue(fd, fontAsset);
                    fontDefinition = fd;
                    fontDefinitionType = fdType;
                    return true;
                }

                var f = fdType.GetField("m_FontAsset", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (f != null && f.FieldType.IsAssignableFrom(assetType))
                {
                    f.SetValue(fd, fontAsset);
                    fontDefinition = fd;
                    fontDefinitionType = fdType;
                    return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static void TryClearUnityFontDefinition(Label label)
        {
            if (label == null) return;

            EnsureUnityFontDefinitionReflection(label);
            if (s_unityFontDefinitionProp == null) return;
            if (s_styleFontDefinitionFromKeywordCtor == null) return;

            try
            {
                var nullValue = s_styleFontDefinitionFromKeywordCtor.Invoke(new object[] { StyleKeyword.Null });
                s_unityFontDefinitionProp.SetValue(label.style, nullValue);
            }
            catch
            {
            }
        }
    }
}
