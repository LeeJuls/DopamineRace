using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyChart.Demo
{
    public class ExamplesChildSwitcher : MonoBehaviour
    {
        [SerializeField] private Transform _examples;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _lastButton;

        private readonly List<GameObject> _children = new List<GameObject>();
        private int _index;

        private void OnEnable()
        {
            RefreshChildren();

            if (_nextButton != null) _nextButton.onClick.AddListener(Next);
            if (_lastButton != null) _lastButton.onClick.AddListener(Last);

            ApplyActive();
        }

        private void OnDisable()
        {
            if (_nextButton != null) _nextButton.onClick.RemoveListener(Next);
            if (_lastButton != null) _lastButton.onClick.RemoveListener(Last);
        }

        public void RefreshChildren()
        {
            _children.Clear();

            if (_examples == null) return;

            for (int i = 0; i < _examples.childCount; i++)
            {
                var child = _examples.GetChild(i);
                if (child == null) continue;
                _children.Add(child.gameObject);
            }

            if (_children.Count == 0)
            {
                _index = 0;
                return;
            }

            int activeIndex = -1;
            for (int i = 0; i < _children.Count; i++)
            {
                if (_children[i] != null && _children[i].activeSelf)
                {
                    activeIndex = i;
                    break;
                }
            }

            _index = activeIndex >= 0 ? activeIndex : 0;
        }

        public void Show(int index)
        {
            if (_children.Count == 0) return;

            _index = ((index % _children.Count) + _children.Count) % _children.Count;
            ApplyActive();
        }

        private void Next()
        {
            if (_children.Count == 0) return;

            _index = (_index + 1) % _children.Count;
            ApplyActive();
        }

        private void Last()
        {
            if (_children.Count == 0) return;

            _index = (_index - 1 + _children.Count) % _children.Count;
            ApplyActive();
        }

        private void ApplyActive()
        {
            if (_children.Count == 0) return;

            for (int i = 0; i < _children.Count; i++)
            {
                var go = _children[i];
                if (go == null) continue;
                go.SetActive(i == _index);
            }
        }
    }
}
