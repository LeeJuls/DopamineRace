# 快速上手：2 分钟做出第一张图

本章目标：按 EasyChart 推荐的最快路径跑通“**编辑 → 导出 → 在 UI 中使用**”的闭环。

---

## 打开编辑器窗口

在 Unity 菜单栏选择：

- `EasyChart/Library Editor`

你会看到一个类似“资源库/配置面板/预览区”的窗口（后续章节会解释每个区域）。

---

## 克隆一个 Library（推荐）

如果你想快速开始并保持风格一致，建议：

- 先在窗口顶部工具栏选择一个已有 Library（例如内置 Demo 库）
- 点击工具栏的 **Clone**，克隆出你的个人库（例如 `MyLibrary`）

这样你后续所有修改都发生在自己的库里，避免污染原始示例。

---

## 克隆一个 ChartProfile（推荐）

在资源树里找到一个接近你目标效果的图表（`ChartProfile`），右键：

- `Clone`

克隆后，你会得到一个新的 Profile（用于做“同款变体”）。选中它，右侧 Inspector 会显示你可以直接修改的所有配置。

---

## 修改配置并保存

最少改动建议：

- `coordinateSystem`：确保与你要的 Series 匹配（例如 Line/Bar/Scatter 用 `Cartesian2D`）
- `series`：确认 `type` 正确，并填充 `seriesData`
- `axes`：最少保证 X/Y 轴类型与数据含义匹配

完成修改后，点击窗口顶部工具栏的保存按钮（如果你的版本有），或等待 Unity 自动保存资产。

---

## 导出 UXML（用于 UI Builder 复用）

推荐做法是把 Profile 导出为可复用的 `.uxml`：

- 在资源树里右键你的 Profile
- 选择 `Export to UXML`

导出的 UXML 会进入：

- `Assets/EasyChart/LibraryUxml/`（Mirror/Backup 相关操作也会在这个根目录下管理导出物）

---

## 在 QuickStart 场景里用 UIDocument + UI Builder 使用

打开示例场景：

- `Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity`

然后在 Project 中找到：

- `Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`

双击它打开（或在 UI Builder 中打开）。接下来：

- 从 Project 里把你刚导出的图表 `.uxml` 拖入到 `NewUXMLTemplate.uxml` 的层级中
- 保存 UXML
- 确认场景里的 `UIDocument` 引用了你编辑后的 `NewUXMLTemplate.uxml`

运行场景，你会看到图表渲染在 UI Toolkit 页面中。

---

## 备选：导出为 UGUI 预制体并使用

如果你希望用 UGUI（Canvas/RectTransform）工作流，也可以在 Library Editor 中把选中的 Profile 导出为 UGUI 预制体并直接放进场景 UI（具体入口与细节取决于你当前版本提供的菜单项）。

---

## 下一步你应该看什么

- 你要系统理解 UI Toolkit 推荐工作流：`00-WorkflowAndLibrary.md`
- 你要用 UGUI（Canvas/RectTransform）把图表用起来：`33-UGUIWorkflow.md`

