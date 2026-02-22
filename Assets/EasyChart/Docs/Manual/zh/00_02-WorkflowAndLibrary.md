# UIToolKit工作流（推荐）

本章目标：先把 EasyChart 推荐的整体实现思路讲清楚（以 UI Toolkit 为主）：

1. 在编辑器中用 **`EasyChartLibraryWindow`** 编辑 `ChartProfile`
2. 将 `ChartProfile` **导出为 `.uxml`**，作为你的“图表库”资源
3. 在 UI Toolkit 中用 **UI Builder** / UXML 组合页面，实现快速搭建 UI

本章专注于 **UI Toolkit（推荐）**。如果你需要 UGUI（Canvas/RectTransform）工作流，请看：

- `33-UGUIWorkflow.md`


## 0. 为什么推荐 UI Toolkit 工作流

核心原因：把“图表配置源（ChartProfile）”和“UI 落地物（UXML）”分层管理。

- `ChartProfile`：负责图表“长什么样/用什么轴/有哪些 Series/数据如何解释”，适合在编辑器中集中维护。
- 导出的 `.uxml`：负责把图表以 UI Toolkit 组件形式落地到页面中，适合复用/组合/版本控制。

你最终得到的是：

- 配置可复用（多个页面引用同一套图表风格）
- 页面可组装（UI Builder 拖拽组合，不需要每次从零搭 UI）
- 团队协作更清晰（Profile 作为“源”，UXML 作为“产物/组件库”）

---

## 1. 为什么要导出 UXML

在项目里，`ChartProfile` 负责描述“图表长什么样、用什么轴、有什么 Series、数据点怎么解释”。

当你导出 `.uxml` 后，你得到的是一个可在 UI Toolkit 中复用的 UI 资源：

- 可以被 UI Builder 直接拖拽使用
- 可以被多个页面复用（同一套图表样式）
- 可以被版本控制与资产管理（你的“图表库”）

---

## 2. 推荐工作流（从配置到页面）

### Step 1：克隆出你的工作库与图表（推荐）

- 在 Unity 菜单栏打开：`EasyChart/Library Editor`

推荐流程：

- **Clone Library**：先克隆一个你自己的 Library（避免直接改内置示例库）
- **Clone ChartProfile**：在你的库里右键某个接近目标效果的 Profile，选择 `Clone` 生成变体
- 在右侧 Inspector 修改：
  - `coordinateSystem`
  - `series`
  - `axes`

> 建议：把你自己的 Profile 统一放在 `Assets/EasyChart/Library/Custom/`（或团队约定目录）。

### Step 2：导出 UXML（生成库资源）

你可以在 Library Editor 里执行导出：

- 对某个 Profile：`Export to UXML`
- 对某个文件夹：
  - `Export Folder to UXML (Mirror)`
  - `Export Folder to UXML (Backup)`
- 全量：
  - `Export All UXML (Mirror)`
  - `Export All UXML (Backup)`

导出根目录：

- `Assets/EasyChart/LibraryUxml/`

多 Library 时通常结构为：

- `Assets/EasyChart/LibraryUxml/<LibraryName>/...`

其中 `_Backups` 子目录用于存放备份导出（以及一些导出过程附带的 JSON 备份文件）：

- `Assets/EasyChart/LibraryUxml/<LibraryName>/_Backups/...`

导出的 UXML 核心结构类似：

- 一个 `<ec:ChartElement profile-name="..." />`
- `profile-name` 对应某个 ChartProfile 的 key（通常是资产文件名）
- 同时会写入图表的 width/height 样式

> 重点：你应该把导出的 `.uxml` 当作“可复用图表组件”，而不是每次手写 UI。

### Mirror vs Backup（你应该怎么选）

- **Mirror**：
  - 用于“把 Profile 当前状态镜像到 UXML”
  - 通常会覆盖同名导出物，并可能清理不再存在的旧文件（保持镜像一致）
- **Backup**：
  - 用于“按时间/标签做一次备份导出”
  - 不建议作为页面直接引用的主路径（更适合作为历史快照/回滚）

### Step 3：在 UI Builder 里组装页面

在 UI Builder 中：

- 打开你的页面 UXML
- 从 Project 里拖入导出的图表 `.uxml`
- 将它与其他 UI（Label、Button、ListView 等）组合成完整页面

如果你要最快验证导出链路，可以直接使用示例场景与模板：

- 场景：`Assets/EasyChart/Demo/Scenes/EasyChart_QuickStart.unity`
- 模板：`Assets/EasyChart/Demo/UIToolKit/NewUXMLTemplate.uxml`

把导出的图表 `.uxml` 拖入 `NewUXMLTemplate.uxml`，然后确认场景中的 `UIDocument` 引用了该模板。

#### 在 UI Builder 中的具体操作（建议按这个顺序）

1. 打开 UI Builder（或直接双击你的页面 `.uxml`）。
2. 在 Project 中找到你导出的图表 `.uxml`（通常位于 `Assets/EasyChart/LibraryUxml/<LibraryName>/...`）。
3. 将该 `.uxml` 拖入页面的 Hierarchy（建议放进一个容器 `VisualElement` 内）。
4. 保存页面 UXML。
5. 回到场景，确认 `UIDocument` 引用的是你刚保存的页面 `.uxml`。

#### 导出的图表 UXML 内部是什么

导出的 `.uxml` 通常包含一个 `EasyChart.ChartElement`，并带有属性：

- `profile-name`：通常对应 `ChartProfile` 的资产文件名（key）
- `profile-guid`：更稳定的资产定位方式

因此：

- 如果你只是修改了 Profile，页面不会自动变化：需要重新导出（Mirror）让 UXML 更新。
- 如果你改了 Profile 的资产文件名，导出的 `profile-name` 也会随之变化（建议保持命名稳定）。

### Step 4：运行时加载/替换数据（按你的业务决定）

`ChartProfile`/UXML 负责“样式与结构”，而数据来源通常来自你的业务逻辑。

- 静态展示：直接在 Profile 的 `seriesData` 中填写
- 动态展示：运行时写入/替换 `seriesData`（并保持 `SeriesData.id` 稳定性）

---

## 3. 图表库的目录建议

建议把“配置源”和“导出物”分开：

- `Assets/EasyChart/Library/Custom/`：你维护的 `ChartProfile`
- `Assets/EasyChart/LibraryUxml/`：导出的 UXML（镜像/备份都会落在这个根目录下）

当你使用多 Library 时，导出物通常会按库名分子目录：

- `Assets/EasyChart/LibraryUxml/<LibraryName>/...`

建议约定：

- **Profile 只在 `Assets/EasyChart/Library/...` 下维护**（作为配置源）
- **页面只引用 Mirror 的导出物**（作为组件库）
- Backup 永远只当“历史快照”

> 这样做的好处是：
> - 配置源可读、可编辑
> - 导出物可复用、可组合、可直接用于 UI Builder

---

## 4. 常见问题与排错

- **UI Builder 里找不到导出的图表 UXML**
  - 优先检查 `Assets/EasyChart/LibraryUxml/` 下是否已生成
  - 如果你使用了多 Library，检查是否在 `Assets/EasyChart/LibraryUxml/<LibraryName>/` 下
  - Mirror/Backup 的导出物可能位于 `_Backups`，不建议页面引用 `_Backups` 里的文件

- **页面里引用了 UXML 但运行时不显示**
  - 确认场景里的 `UIDocument` 引用了你编辑的页面 `.uxml`
  - 回到 Library Editor 的 Preview 看该 Profile 是否能正常显示（先排除 Profile 配置问题）

- **修改了 Profile 但页面没变化**
  - Profile 是“源”，页面引用的是导出的 UXML
  - 修改 Profile 后请重新执行导出（Mirror），再回到页面刷新/保存

- **UI Builder 里看得到组件，但运行时仍然不显示**
  - 优先确认：场景里的 `UIDocument` 是否真的引用了该页面（而不是另一个旧页面）
  - 再确认：Profile 在 Library Editor 的 Preview 是否能正常显示（先排除配置问题）

---

## 5. 下一步你应该看什么

- 你想快速跑通一张图：`01-QuickStart.md`
- 你要用 UGUI（Canvas/RectTransform）把图表用起来：`33-UGUIWorkflow.md`

---

## 6. 编辑器工作流与面板速查（Library Editor）

本节把原先分散在多个章节中的“编辑器工作流与面板说明”合并到一起，作为你在 `EasyChart/Library Editor` 中操作时的速查。

### 6.1 你在编辑的是什么？（ChartProfile）

在 Library Editor 里被选中的图表，本质上是一个 `ChartProfile` 资产。

- 它是可复用配置：同一个 Profile 可以被多个场景/Prefab 引用。
- 它是可预览配置：在编辑器里修改后可以立即看到预览变化。

### 6.2 Library Editor 的核心区域

你可以把窗口理解为四块：

- 左侧：Library（资源树）
- 中部：Preview（预览区）
- 右侧：Inspector（配置）
- 右侧：Series（系列与数据）

此外左侧通常还有 JSON Injection（JSON 注入面板）。

### 6.3 Library 面板（资源树）

功能概览：

- 以树状结构展示图表库目录下的文件夹与 `ChartProfile`（`.asset`）。
- 选中某个 `ChartProfile` 后，会驱动 Inspector/Series/Preview 的绑定与刷新。
- 支持创建/重命名/删除/拖拽移动与排序。

选择逻辑：

- 选中的是文件夹：右侧 Inspector/Series 清空（无 Profile 可编辑）。
- 选中的是 ChartProfile：右侧面板绑定到该 Profile。

常用操作（标题栏与右键菜单，具体以版本为准）：

- Folder：New Folder / New Chart / Export Folder to UXML（Mirror/Backup）/ Rename / Delete
- ChartProfile：Export to UXML / Clone / Rename / Delete

### 6.4 Preview 面板（预览区）

Preview 的作用是把当前选中的 `ChartProfile` 直接渲染出来，方便你在编辑配置时即时验证效果。

常见问题：

- 预览为空：确认是否至少 1 条 `Serie`，且该 serie 的 `seriesData` 不为空。
- 数据有但显示怪：确认坐标系与 SeriesType 匹配，轴范围是否把数据排除在外。

### 6.5 Inspector 面板（配置面板）

Inspector 的定位是以“配置视角”编辑 Profile 的序列化字段（坐标系、轴、网格、交互、图例等），并驱动 Preview 更新。

提示：

- 如果你发现某些字段修改后没效果，先确认页面引用的是你导出的 UXML，而不是直接引用 Profile。

### 6.6 Series 面板（系列与数据）

Series 面板以“图表结构”的方式编辑 `ChartProfile.series`：

- 添加/删除/排序系列
- 为每条 serie 选择 `type` 并编辑 `settings`
- 编辑 `seriesData`（数据点）

### 6.7 JSON Injection 面板（JSON 注入）

定位：把当前 Profile 的信息表达为可复制的 JSON，并支持解析 JSON 回写到当前 Profile。

推荐工作流：

1. 从当前 Profile 生成示例 JSON
2. Copy 到外部编辑器做批量修改
3. 粘贴回来并 ApplyToChart

---

## 7. 轴与范围（Axis & Range）

### 7.1 AxisType：Category vs Value

- Category（类目轴）：用 `labels` 定义离散类目（A/B/C 或 周一/周二/周三）。
- Value（数值轴）：连续数值范围（0~100，-3~3，0~1e6）。

#### 7.1.1 什么时候用 Category

- X 轴是“文本标签序列”
- 你希望数据点落在 `labels[i]` 上
- 典型：柱状图（每类一组柱）、折线图（按类目对齐）

Category 的关键点：

- `labels[0]` 对应类目索引 `0`
- `labels[1]` 对应类目索引 `1`

#### 7.1.2 什么时候用 Value

- X 或 Y 轴是连续数值（例如时间戳、金额、温度）
- 你希望轴可以按数值缩放/平移

Value 的关键点：

- 轴范围通常由自动范围计算得到（如果开启 auto range）
- 你可以只锁定一端（例如固定最小值为 0，最大值自动）

### 7.2 Category 轴：labels 与 LabelPlacement

`labels` 决定类目个数与标签文本。

`LabelPlacement` 影响对齐方式：

- `Tick`：标签对齐刻度点，更适合 Line/Scatter。
- `CellCenter`：标签对齐格子中心，更适合 Bar/Heatmap。

常见现象：

- 柱子落在两个标签之间：优先把 `LabelPlacement` 调成 `CellCenter`。

### 7.3 Value 轴：autoRangeMin / autoRangeMax

如果你看到范围“锁死”导致数据不显示，先把范围回退到全自动：

- 打开 `autoRangeMin/autoRangeMax`

确认可见后，再逐步加入业务约束（例如柱状图纵轴从 0 起）。

#### 7.3.1 常见模板：Y 轴从 0 开始

- `axisType = Value`
- 固定 `minValue = 0`
- `autoRangeMax = true`

#### 7.3.2 常见模板：只锁定 Max（例如百分比）

- 固定 `maxValue = 100`
- `autoRangeMin = true`

### 7.4 rounding / unit / labelFormat

- rounding：让范围吸附到更“整”的单位。
- unit：显示单位压缩（个/万/百万）。
- labelFormat：控制数字格式（N0/N2/F1/百分比等）。

#### 7.4.1 单位显示（showUnit / unitText）

当数值很大（例如 10,000 以上）时，常见做法是让轴末端显示单位（如“万”“k”“M”）。

#### 7.4.2 快速排错

- 标签对不齐 / 柱子夹在标签中间：优先检查 Category 轴的 `LabelPlacement`
- 轴范围很怪（特别大/特别小）：检查是否锁死 min/max；检查 rounding/unit
- 刻度小数太多：优先设置 `labelFormat`

---

## 8. Series 与数据（Serie / SeriesData）

### 8.1 Serie（一条序列）

在 `ChartProfile.series` 中每个元素是一个 `Serie`：

- `name`
- `type`
- `visible`
- `settings`
- `labelSettings`
- `seriesData`

补充：`settings` 通常是多态对象（`SerializeReference`）。切换 `type` 时，会尝试保留每种类型上一次的 settings（编辑体验更好）。

### 8.2 SeriesData（一个数据点）

`SeriesData` 常见字段：

- `id`：稳定标识（tooltip/hover/隐藏状态）。
- `x`：X 坐标或 Category 索引。
- `value`：主要数值。
- `y`：第二维坐标（散点/热力图等）。
- `z`：第三维（sizeMapping 等）。
- `name`：点名称（Radar/Pie/Ring 等可能用到）。
- `useColor` + `color`：点级颜色覆盖。

如果启用了交互，建议保证 `SeriesData.id` 稳定，避免每次刷新数据都生成一套新的 id。

### 8.3 SerieType 与坐标系的匹配

- Cartesian2D：Line/Bar/Scatter/Heatmap
- Polar2D：Radar

不建议在同一个 ChartProfile（非 Pie）里混用 Polar 与 Cartesian 系列；如果你真的混用，要特别小心 axes/grid 语义是否一致。

### 8.4 常用数据写法（按类型）

#### 8.4.1 Line

- 常见：Category X + Value Y
  - 数据点：`x=类目索引`，`value=数值`
- 连续：Value X + Value Y
  - 数据点：`x=横轴数值`，`value=纵轴数值`

#### 8.4.2 Bar

- Category X + Value Y
  - 每个柱子一个点：`x=类目索引`，`value=柱高`
- 并列：多条 Bar serie 共享同一套 Category X
- 堆叠：`stacked=true` 且 `stackGroup` 相同的系列会堆叠

#### 8.4.3 Scatter

- 常用：X=Value，Y=Value
- 数据点推荐显式写 `x/y`

#### 8.4.4 Heatmap

- 三元组：`x=列索引`，`y=行索引`，`value=强度`

#### 8.4.5 Radar

- 常见理解：`x=维度索引`，`value=该维度数值`，`name=维度名称`

### 8.5 数据常见坑（按现象排查）

- Category 图表 X 轴是 Category，但数据点 x 不是 0/1/2...
  - 现象：点/柱子不在标签上
  - 处理：确保 `x=类目索引`，或者把 X 改成 Value

- 出现 NaN/Infinity
  - 现象：整张图不渲染、范围爆炸
  - 处理：在数据源侧过滤异常值

- 看不到图（但 seriesData 不为空）
  - 检查：坐标系是否匹配（Cartesian vs Polar）
  - 检查：AxisType 是否匹配数据含义

- 交互/tooltip 指向错乱
  - 检查：`SeriesData.id` 是否稳定（不要每次刷新都随机生成一套新的点）
