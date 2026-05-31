using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart
{
    /// <summary>
    /// Runtime controller for UIToolkit example scenes.
    /// Handles sidebar button clicks to switch between chart containers.
    /// Attach this to the same GameObject as UIDocument.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UIToolkitExampleController : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private List<Button> _sidebarButtons = new List<Button>();
        private List<VisualElement> _chartContainers = new List<VisualElement>();
        private int _currentIndex = 0;
        
        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                return;
            }
            
            // Wait for the visual tree to be ready
            _uiDocument.rootVisualElement.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }
        
        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Unregister to only run once
            _uiDocument.rootVisualElement.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            
            Initialize();
        }
        
        private void Initialize()
        {
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;
            
            _sidebarButtons.Clear();
            _chartContainers.Clear();
            
            // Find all sidebar buttons (buttons with name starting with "btn-")
            var allButtons = root.Query<Button>().ToList();
            foreach (var button in allButtons)
            {
                if (button.name != null && button.name.StartsWith("btn-"))
                {
                    _sidebarButtons.Add(button);
                    
                    // Extract folder name from button name
                    string folderName = button.name.Substring(4); // Remove "btn-" prefix
                    
                    // Find corresponding chart container
                    var container = root.Q<VisualElement>($"charts-{folderName}");
                    _chartContainers.Add(container);
                    
                    // Register click handler
                    int index = _sidebarButtons.Count - 1;
                    button.clicked += () => ShowFolder(index);
                }
            }
            
            // Show first folder by default
            if (_sidebarButtons.Count > 0)
            {
                ShowFolder(0);
            }
        }
        
        private void ShowFolder(int index)
        {
            if (index < 0 || index >= _chartContainers.Count) return;
            
            _currentIndex = index;
            
            for (int i = 0; i < _chartContainers.Count; i++)
            {
                var container = _chartContainers[i];
                if (container != null)
                {
                    container.style.display = (i == index) ? DisplayStyle.Flex : DisplayStyle.None;
                }
                
                var button = _sidebarButtons[i];
                if (button != null)
                {
                    // Update button style to show active state
                    if (i == index)
                    {
                        button.style.backgroundColor = new Color(0.4f, 0.6f, 0.8f, 1f);
                    }
                    else
                    {
                        button.style.backgroundColor = new Color(0.235f, 0.235f, 0.235f, 1f); // rgb(60, 60, 60)
                    }
                }
            }
        }
    }
}
