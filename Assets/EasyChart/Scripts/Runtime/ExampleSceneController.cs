using System.Collections.Generic;
using UnityEngine;

namespace EasyChart
{
    /// <summary>
    /// Runtime controller for the example scene.
    /// Handles folder button clicks and shows/hides chart containers.
    /// Uses serialized lists to persist data across play mode.
    /// </summary>
    public class ExampleSceneController : MonoBehaviour
    {
        [System.Serializable]
        public class FolderEntry
        {
            public string folderName;
            public UnityEngine.UI.Button button;
            public GameObject container;
        }
        
        [SerializeField] private List<FolderEntry> _folderEntries = new List<FolderEntry>();
        private int _currentFolderIndex = 0;
        
        public void AddFolderEntry(string folderName, UnityEngine.UI.Button button, GameObject container)
        {
            _folderEntries.Add(new FolderEntry
            {
                folderName = folderName,
                button = button,
                container = container
            });
        }
        
        private void Start()
        {
            // Register button click events at runtime
            for (int i = 0; i < _folderEntries.Count; i++)
            {
                int index = i; // Capture for closure
                var entry = _folderEntries[i];
                if (entry.button != null)
                {
                    entry.button.onClick.AddListener(() => ShowFolder(index));
                }
            }
            
            // Show first folder by default
            if (_folderEntries.Count > 0)
            {
                ShowFolder(0);
            }
        }
        
        public void ShowFolder(int index)
        {
            if (index < 0 || index >= _folderEntries.Count) return;
            
            _currentFolderIndex = index;
            
            for (int i = 0; i < _folderEntries.Count; i++)
            {
                var entry = _folderEntries[i];
                bool isActive = (i == index);
                
                if (entry.container != null)
                {
                    entry.container.SetActive(isActive);
                }
                
                if (entry.button != null)
                {
                    var colors = entry.button.colors;
                    colors.normalColor = isActive 
                        ? new Color(0.4f, 0.6f, 0.8f, 1f) 
                        : new Color(0.3f, 0.3f, 0.3f, 1f);
                    entry.button.colors = colors;
                }
            }
        }
    }
}
