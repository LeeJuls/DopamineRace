using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using EasyChart.Layers;

namespace EasyChart
{
    internal sealed class ChartAxisGridController
    {
        public void RefreshAxisLayer(
            AxisLayer axisLayer,
            AxisId xAxisId,
            AxisId yAxisId,
            List<string> xCategoryLabels,
            List<string> yCategoryLabels,
            AxisConfig xAxis,
            AxisConfig yAxis,
            float xMin,
            float xMax,
            float yMin,
            float yMax)
        {
            if (axisLayer == null) return;

            axisLayer.XAxisId = xAxisId;
            axisLayer.YAxisId = yAxisId;

            axisLayer.XAxisConfig = xAxis;
            axisLayer.YAxisConfig = yAxis;

            axisLayer.XIsCategory = xAxis != null && xAxis.axisType == AxisType.Category;
            axisLayer.YIsCategory = yAxis != null && yAxis.axisType == AxisType.Category;

            if (axisLayer.XIsCategory)
            {
                axisLayer.XLabels = xCategoryLabels;
            }
            else
            {
                if (xAxis != null && xAxis.labels != null && xAxis.labels.Count > 0)
                {
                    axisLayer.XLabels = xAxis.labels;
                }
                else
                {
                    axisLayer.XLabels = null;
                }
                axisLayer.XSplitCount = xAxis != null ? Mathf.Max(1, xAxis.splitCount) : 5;
            }

            axisLayer.XMin = xMin;
            axisLayer.XMax = xMax;

            if (axisLayer.YIsCategory)
            {
                axisLayer.YLabels = yCategoryLabels;
            }
            else
            {
                if (yAxis != null && yAxis.labels != null && yAxis.labels.Count > 0)
                {
                    axisLayer.YLabels = yAxis.labels;
                }
                else
                {
                    axisLayer.YLabels = null;
                }
                axisLayer.YSplitCount = yAxis != null ? Mathf.Max(1, yAxis.splitCount) : 5;
            }

            axisLayer.YMin = yMin;
            axisLayer.YMax = yMax;
            axisLayer.RefreshLabels();
        }

        public void RefreshGridLayer(GridLayer gridLayer, AxisLayer axisLayer)
        {
            if (gridLayer == null) return;
            if (axisLayer == null) return;

            // Determine if windowing is active (labels > visible count)
            bool xWindowing = false;
            bool yWindowing = false;

            if (axisLayer.XIsCategory)
            {
                int count = (axisLayer.XLabels != null ? axisLayer.XLabels.Count : 0);
                if (count > 1)
                {
                    bool cellCenter = axisLayer.XAxisConfig != null
                        && axisLayer.XAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter;
                    gridLayer.VerticalGridCount = cellCenter ? count : (count - 1);

                    // Check if windowing is active (total labels > visible labels)
                    var xAxis = axisLayer.XAxisConfig;
                    int totalLabels = (xAxis != null && xAxis.labels != null) ? xAxis.labels.Count : 0;
                    if (totalLabels > count)
                    {
                        xWindowing = true;
                    }
                }
                else
                {
                    var xAxis = axisLayer.XAxisConfig;
                    int fallbackSplit = xAxis != null ? Mathf.Max(1, xAxis.splitCount) : 5;
                    gridLayer.VerticalGridCount = fallbackSplit;
                }
            }
            else
            {
                var xAxis = axisLayer.XAxisConfig;
                if (xAxis != null && xAxis.labels != null && xAxis.labels.Count > 0)
                {
                    int count = xAxis.labels.Count;
                    bool cellCenter = xAxis.labelPlacement == CategoryLabelPlacement.CellCenter;
                    gridLayer.VerticalGridCount = cellCenter ? Mathf.Max(1, count) : Mathf.Max(1, count - 1);
                }
                else
                {
                    gridLayer.VerticalGridCount = Mathf.Max(1, axisLayer.XSplitCount);
                }
            }

            if (axisLayer.YIsCategory)
            {
                int count = (axisLayer.YLabels != null ? axisLayer.YLabels.Count : 0);
                if (count > 1)
                {
                    bool cellCenter = axisLayer.YAxisConfig != null
                        && axisLayer.YAxisConfig.labelPlacement == CategoryLabelPlacement.CellCenter;
                    gridLayer.HorizontalGridCount = cellCenter ? count : (count - 1);

                    // Check if windowing is active (total labels > visible labels)
                    var yAxis = axisLayer.YAxisConfig;
                    if (yAxis != null && yAxis.labels != null && yAxis.labels.Count > count)
                    {
                        yWindowing = true;
                    }
                }
                else
                {
                    var yAxis = axisLayer.YAxisConfig;
                    int fallbackSplit = yAxis != null ? Mathf.Max(1, yAxis.splitCount) : 5;
                    gridLayer.HorizontalGridCount = fallbackSplit;
                }
            }
            else
            {
                var yAxis = axisLayer.YAxisConfig;
                if (yAxis != null && yAxis.labels != null && yAxis.labels.Count > 0)
                {
                    int count = yAxis.labels.Count;
                    bool cellCenter = yAxis.labelPlacement == CategoryLabelPlacement.CellCenter;
                    gridLayer.HorizontalGridCount = cellCenter ? Mathf.Max(1, count) : Mathf.Max(1, count - 1);
                }
                else
                {
                    gridLayer.HorizontalGridCount = Mathf.Max(1, axisLayer.YSplitCount);
                }
            }

            // Enable preload when windowing is active (for smoother scrolling)
            gridLayer.VerticalPreload = xWindowing;
            gridLayer.HorizontalPreload = yWindowing;

            gridLayer.Redraw();
        }
    }
}
