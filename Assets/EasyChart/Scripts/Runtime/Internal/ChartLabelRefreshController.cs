using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartLabelRefreshController
    {
        private bool _scheduled;

        public void Request(
            VisualElement owner,
            AxisLayer axisLayer,
            List<BaseSeriesRenderer> renderers,
            bool isEditorCoordSwitchInProgress,
            bool editorDiagnosticsEnabled
        )
        {
            if (_scheduled) return;
            _scheduled = true;
            if (!Application.isPlaying && isEditorCoordSwitchInProgress)
            {
                _scheduled = false;
                if (editorDiagnosticsEnabled) Debug.Log("[ChartElement] ScheduleLayoutRefresh deferred (coord switch)");
                return;
            }

            if (owner == null)
            {
                _scheduled = false;
                return;
            }

            owner.schedule.Execute(() =>
            {
                _scheduled = false;

                if (axisLayer != null) axisLayer.RefreshLabels();
                if (renderers == null) return;

                for (int i = 0; i < renderers.Count; i++)
                {
                    var r = renderers[i];
                    if (r == null) continue;
                    if (!r.visible) continue;
                    if (r.parent == null) continue;
                    if (r.panel == null) continue;
                    r.UpdateLabels();
                }
            }).ExecuteLater(0);
        }
    }
}
