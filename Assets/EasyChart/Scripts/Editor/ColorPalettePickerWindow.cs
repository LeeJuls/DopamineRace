using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace EasyChart.Editor
{
    public class ColorPalettePickerWindow : EditorWindow
    {
        private List<SeriesColorPalette> _palettes;
        private Action<SeriesColorPalette> _onSelect;
        private Action _onManage;
        private Action _onResetDefaults;
        private Vector2 _scrollPos;
        
        private const float ColorSwatchSize = 16f;
        private const float ColorSpacing = 2f;
        private const float RowHeight = 28f;
        private const float Padding = 8f;

        public static void Show(Rect activatorRect, List<SeriesColorPalette> palettes, Action<SeriesColorPalette> onSelect, Action onManage, Action onResetDefaults)
        {
            var window = CreateInstance<ColorPalettePickerWindow>();
            window._palettes = palettes;
            window._onSelect = onSelect;
            window._onManage = onManage;
            window._onResetDefaults = onResetDefaults;
            
            // Calculate window size
            float width = 280f;
            float height = Mathf.Min(palettes.Count * RowHeight + 50f, 300f);
            
            window.ShowAsDropDown(activatorRect, new Vector2(width, height));
        }

        private void OnGUI()
        {
            if (_palettes == null) 
            {
                Close();
                return;
            }

            EditorGUILayout.BeginVertical();
            
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            
            foreach (var palette in _palettes)
            {
                if (DrawPaletteRow(palette))
                {
                    _onSelect?.Invoke(palette);
                    Close();
                    return;
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            EditorGUILayout.Space(4);
            
            // Separator
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            
            EditorGUILayout.Space(4);
            
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Manage Palettes...", GUILayout.Height(22)))
            {
                _onManage?.Invoke();
                Close();
            }

            if (GUILayout.Button("Reset Defaults", GUILayout.Height(22)))
            {
                if (EditorUtility.DisplayDialog(
                    "Reset to Defaults",
                    "This will replace all custom palettes with the default palettes.\n\nAny custom palettes you have created will be lost.\n\nAre you sure you want to continue?",
                    "Reset",
                    "Cancel"))
                {
                    _onResetDefaults?.Invoke();
                }
                Close();
            }

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(4);
            EditorGUILayout.EndVertical();
        }

        private bool DrawPaletteRow(SeriesColorPalette palette)
        {
            var rowRect = EditorGUILayout.GetControlRect(false, RowHeight);
            
            // Hover highlight
            bool isHover = rowRect.Contains(Event.current.mousePosition);
            if (isHover)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.3f, 0.5f, 0.8f, 0.3f));
            }
            
            // Name label
            var nameRect = new Rect(rowRect.x + Padding, rowRect.y + 4, 100, RowHeight - 8);
            EditorGUI.LabelField(nameRect, palette.name);
            
            // Color swatches
            float swatchX = rowRect.x + 110;
            float swatchY = rowRect.y + (RowHeight - ColorSwatchSize) / 2;
            
            int maxSwatches = Mathf.Min(palette.colorSets.Count, 8);
            for (int i = 0; i < maxSwatches; i++)
            {
                var colorSet = palette.colorSets[i];
                
                // Draw main color (use barColor as reference since line/point are white)
                var swatchRect = new Rect(swatchX, swatchY, ColorSwatchSize, ColorSwatchSize);
                EditorGUI.DrawRect(swatchRect, colorSet.barColor);
                
                // Border
                DrawRectBorder(swatchRect, new Color(0, 0, 0, 0.3f));
                
                swatchX += ColorSwatchSize + ColorSpacing;
            }
            
            // Click detection
            if (Event.current.type == EventType.MouseDown && rowRect.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
                return true;
            }
            
            return false;
        }

        private void DrawRectBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color); // Top
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color); // Bottom
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color); // Left
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color); // Right
        }

        private void OnLostFocus()
        {
            Close();
        }
    }
}
