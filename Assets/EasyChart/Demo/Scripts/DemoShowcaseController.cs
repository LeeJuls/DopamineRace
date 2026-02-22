using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace EasyChart.Demo
{
    /// <summary>
    /// Controller for the EasyChart Demo Showcase.
    /// Manages multiple groups of 16 demos each (4x4 grid).
    /// </summary>
    public class DemoShowcaseController : MonoBehaviour
    {
        public const int DemosPerGroup = 16;

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Demo Profiles")]
        [Tooltip("Drag ChartProfile assets here in the order you want them displayed")]
        [SerializeField] private List<ChartProfile> _demoProfiles = new List<ChartProfile>();

        [Header("Group Control")]
        [Tooltip("Current active group index (0-based)")]
        [SerializeField] private int _activeGroup = 0;

        [Header("Navigation")]
        [SerializeField] private KeyCode _nextGroupKey = KeyCode.RightArrow;
        [SerializeField] private KeyCode _prevGroupKey = KeyCode.LeftArrow;

        [Header("UGUI Navigation Buttons")]
        [SerializeField] private UnityEngine.UI.Button _nextbutton;
        [SerializeField] private UnityEngine.UI.Button _lastbutton;

        [Header("Appearance")]
        [SerializeField] private Color _slotBackgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.9f);
        [SerializeField] private Color _slotBorderColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        private VisualElement _root;
        private VisualElement _groupContainer;
        private List<VisualElement> _slots = new List<VisualElement>();
        private List<ChartElement> _chartElements = new List<ChartElement>();

        /// <summary>
        /// Total number of groups based on demo count.
        /// </summary>
        public int GroupCount => Mathf.CeilToInt(_demoProfiles.Count / (float)DemosPerGroup);

        /// <summary>
        /// Current active group index.
        /// </summary>
        public int ActiveGroup => _activeGroup;

        /// <summary>
        /// Total demo count.
        /// </summary>
        public int DemoCount => _demoProfiles.Count;

        private void OnEnable()
        {
            if (_uiDocument == null)
            {
                _uiDocument = GetComponent<UIDocument>();
            }

            if (_uiDocument != null)
            {
                _root = _uiDocument.rootVisualElement;
                BuildUI();
                ShowGroup(_activeGroup);
            }

            if (_nextbutton != null) _nextbutton.onClick.AddListener(OnNextButtonClicked);
            if (_lastbutton != null) _lastbutton.onClick.AddListener(OnLastButtonClicked);
        }

        private void OnDisable()
        {
            if (_nextbutton != null) _nextbutton.onClick.RemoveListener(OnNextButtonClicked);
            if (_lastbutton != null) _lastbutton.onClick.RemoveListener(OnLastButtonClicked);
        }

        private void OnNextButtonClicked()
        {
            int groupCount = GroupCount;
            if (groupCount <= 1) return;

            int next = (_activeGroup + 1) % groupCount;
            ShowGroup(next);
        }

        private void OnLastButtonClicked()
        {
            int groupCount = GroupCount;
            if (groupCount <= 1) return;

            int prev = (_activeGroup - 1 + groupCount) % groupCount;
            ShowGroup(prev);
        }

        private void Update()
        {
            int groupCount = GroupCount;
            if (groupCount <= 1) return;

            if (Input.GetKeyDown(_nextGroupKey))
            {
                int next = (_activeGroup + 1) % groupCount;
                ShowGroup(next);
            }
            else if (Input.GetKeyDown(_prevGroupKey))
            {
                int prev = (_activeGroup - 1 + groupCount) % groupCount;
                ShowGroup(prev);
            }

            // Number keys 1-9 for quick group access
            for (int i = 0; i < Mathf.Min(9, groupCount); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ShowGroup(i);
                    break;
                }
            }
        }

        private void BuildUI()
        {
            if (_root == null) return;

            _root.Clear();
            _chartElements.Clear();
            _slots.Clear();

            // Create main container
            _groupContainer = new VisualElement();
            _groupContainer.name = "GroupContainer";
            _groupContainer.style.width = Length.Percent(100);
            _groupContainer.style.height = Length.Percent(100);
            _groupContainer.style.flexDirection = FlexDirection.Column;
            _groupContainer.style.justifyContent = Justify.SpaceAround;
            _groupContainer.style.alignItems = Align.Center;
            _groupContainer.style.paddingLeft = 25;
            _groupContainer.style.paddingRight = 25;
            _groupContainer.style.paddingTop = 25;
            _groupContainer.style.paddingBottom = 25;
            _root.Add(_groupContainer);

            // Build 4x4 grid (16 slots)
            for (int row = 0; row < 4; row++)
            {
                var rowElement = new VisualElement();
                rowElement.name = $"Row{row}";
                rowElement.style.flexDirection = FlexDirection.Row;
                rowElement.style.justifyContent = Justify.SpaceAround;
                rowElement.style.alignItems = Align.Center;
                rowElement.style.width = Length.Percent(100);
                rowElement.style.flexGrow = 1;
                _groupContainer.Add(rowElement);

                for (int col = 0; col < 4; col++)
                {
                    var slot = new VisualElement();
                    slot.name = $"Slot{row * 4 + col}";
                    slot.style.width = 450;
                    slot.style.height = 300;
                    slot.style.backgroundColor = _slotBackgroundColor;
                    slot.style.borderTopWidth = 1;
                    slot.style.borderBottomWidth = 1;
                    slot.style.borderLeftWidth = 1;
                    slot.style.borderRightWidth = 1;
                    slot.style.borderTopColor = _slotBorderColor;
                    slot.style.borderBottomColor = _slotBorderColor;
                    slot.style.borderLeftColor = _slotBorderColor;
                    slot.style.borderRightColor = _slotBorderColor;
                    slot.style.borderTopLeftRadius = 4;
                    slot.style.borderTopRightRadius = 4;
                    slot.style.borderBottomLeftRadius = 4;
                    slot.style.borderBottomRightRadius = 4;
                    rowElement.Add(slot);

                    _slots.Add(slot);

                    var chartElement = new ChartElement();
                    chartElement.style.width = Length.Percent(100);
                    chartElement.style.height = Length.Percent(100);
                    slot.Add(chartElement);
                    _chartElements.Add(chartElement);
                }
            }

            // Add group indicator label
            var groupLabel = new Label();
            groupLabel.name = "GroupLabel";
            groupLabel.style.position = Position.Absolute;
            groupLabel.style.bottom = 10;
            groupLabel.style.right = 20;
            groupLabel.style.fontSize = 14;
            groupLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
            _root.Add(groupLabel);
        }

        /// <summary>
        /// Show the specified group.
        /// </summary>
        public void ShowGroup(int groupIndex)
        {
            int groupCount = GroupCount;
            if (groupCount == 0)
            {
                _activeGroup = 0;
                ClearAllSlots();
                UpdateGroupLabel();
                return;
            }

            int clampedGroup = Mathf.Clamp(groupIndex, 0, groupCount - 1);
            _activeGroup = clampedGroup;

            int startIndex = _activeGroup * DemosPerGroup;

            for (int i = 0; i < DemosPerGroup; i++)
            {
                var slot = i < _slots.Count ? _slots[i] : null;
                if (slot == null) continue;

                int profileIndex = startIndex + i;
                ChartProfile profile = profileIndex < _demoProfiles.Count ? _demoProfiles[profileIndex] : null;

                var chart = new ChartElement();
                chart.style.width = Length.Percent(100);
                chart.style.height = Length.Percent(100);

                chart.Profile = profile;

                slot.Clear();
                slot.Add(chart);

                if (i < _chartElements.Count) _chartElements[i] = chart;
            }

            UpdateGroupLabel();
        }

        private void ClearAllSlots()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot == null) continue;

                var chart = new ChartElement();
                chart.style.width = Length.Percent(100);
                chart.style.height = Length.Percent(100);

                slot.Clear();
                slot.Add(chart);

                if (i < _chartElements.Count) _chartElements[i] = chart;
            }
        }

        private void UpdateGroupLabel()
        {
            var label = _root?.Q<Label>("GroupLabel");
            if (label != null)
            {
                int groupCount = GroupCount;
                if (groupCount > 1)
                {
                    label.text = $"Group {_activeGroup + 1} / {groupCount}  (← → to navigate)";
                    label.style.display = DisplayStyle.Flex;
                }
                else
                {
                    label.style.display = DisplayStyle.None;
                }
            }
        }

        /// <summary>
        /// Swap two demo positions.
        /// </summary>
        public void SwapDemos(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= _demoProfiles.Count) return;
            if (indexB < 0 || indexB >= _demoProfiles.Count) return;

            var temp = _demoProfiles[indexA];
            _demoProfiles[indexA] = _demoProfiles[indexB];
            _demoProfiles[indexB] = temp;

            ShowGroup(_activeGroup);
        }

        /// <summary>
        /// Set a specific demo at a position.
        /// </summary>
        public void SetDemo(int index, ChartProfile profile)
        {
            if (index < 0) return;

            while (_demoProfiles.Count <= index)
            {
                _demoProfiles.Add(null);
            }

            _demoProfiles[index] = profile;
            ShowGroup(_activeGroup);
        }

        /// <summary>
        /// Add profiles to the list.
        /// </summary>
        public void AddProfiles(IEnumerable<ChartProfile> profiles)
        {
            foreach (var p in profiles)
            {
                if (p != null && !_demoProfiles.Contains(p))
                {
                    _demoProfiles.Add(p);
                }
            }
            ShowGroup(_activeGroup);
        }

        /// <summary>
        /// Clear all profiles.
        /// </summary>
        public void ClearProfiles()
        {
            _demoProfiles.Clear();
            _activeGroup = 0;
            ShowGroup(0);
        }

        /// <summary>
        /// Refresh current group display.
        /// </summary>
        public void RefreshCurrentGroup()
        {
            ShowGroup(_activeGroup);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor access to demo profiles list.
        /// </summary>
        public List<ChartProfile> DemoProfiles => _demoProfiles;
#endif
    }
}
