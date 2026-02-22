# Library 面板（资源树）

本章说明 `Unity Easy Chart/Library Editor` 窗口左侧的 **Library** 面板：用于管理你的图表资产（`ChartProfile`）与文件夹结构，并决定右侧各面板正在编辑的是哪个 Profile。

---

## 功能概览

- **展示结构**：以树状结构展示“图表库根目录”下的文件夹与 `ChartProfile`（`.asset`）。
- **选择驱动编辑**：点击某个 `ChartProfile` 后，会驱动：
  - Inspector 面板绑定到该 Profile
  - Series 面板绑定到该 Profile 的 `series`
  - Preview 预览刷新
  - JSON Injection 生成示例 JSON（并可 Apply 回写到该 Profile）
- **管理资产**：提供创建、重命名、删除、拖拽移动、展开/收起等操作。

---

## 界面结构说明

Library 面板的顶部标题栏从左到右通常包含：

- **Library**：标题。
- **(当前库名称)**：括号内显示当前选中的库/根目录名称（用于区分你可能配置的多个库）。
- **Menu（菜单按钮）**：常用操作入口（和右键菜单类似，但更集中）。
- **Help（帮助按钮）**：打开本章节。

窗口顶部全局工具栏（Window Toolbar）中，Library 下拉框右侧包含：

- **+**：新增一个 Library。
- **-**：删除当前 Library。
- **Clone（克隆当前 Library）**：把当前 Library 复制为一个新 Library（详见下文）。

标题栏下面是：

- **资源树（TreeView）**：
  - 文件夹
  - `ChartProfile` 资产（图表配置文件）

---

## 选择逻辑（非常重要）

- **选中的是文件夹**：
  - 右侧 Inspector/Series 会清空（因为没有具体 Profile 可以编辑）。
  - JSON Injection 会切换为“无选中 Profile”的示例或保持当前示例（具体以实现为准）。
- **选中的是 ChartProfile**：
  - Inspector/Series 立即绑定到该 Profile 的序列化数据。
  - 任何字段变化会触发 Preview 延迟刷新（`delayCall`）。

建议：如果你发现右侧面板没有内容，先确认左侧是否选中了一个 `ChartProfile`。

---

## 常用操作（标题栏 Menu）

点击标题栏右侧 **Menu**（小菜单图标）会弹出操作菜单，常见项包括：

- **New Chart**：在“目标文件夹”下创建新的 `ChartProfile`。
- **New Folder**：在“目标文件夹”下创建新文件夹。
- **Refresh**：重新扫描并重建树（当你在 Project 视图中手动移动/复制文件后很有用）。
- **Expand All**：展开所有文件夹。
- **Collapse All**：收起所有文件夹。
- **Rename / Delete**：对“当前选中项”执行重命名/删除。
  - 如果当前选中的是库根目录，通常会被禁用。
- **Export UGUI Prefab**（当选中的是 Profile 时可用）：把选中 Profile 导出为 UGUI Prefab（用于运行时展示）。

### 目标文件夹是如何决定的

Menu 中的 **New Chart / New Folder** 会把资源创建在“目标文件夹”下：

- 如果你当前选中的是 **文件夹**：目标就是该文件夹。
- 如果你当前选中的是 **ChartProfile**：目标通常是该 Profile 所在的文件夹。
- 如果没有选中或不明确：目标通常回退到库根目录。

---

## Clone Library（克隆当前 Library）

当你需要把一整套图表库（包含 Profile 与 UXML）复制成一个新的库（用于分支/版本/主题变体等）时，可以使用窗口顶部工具栏里的 **Clone**。

### 入口与操作

- 点击 Library 下拉框右侧的 **Clone** 图标。
- 输入新库名称并确认。

### 克隆内容

- `Assets/EasyChart/Library/<当前库>` 会被复制到 `Assets/EasyChart/Library/<新库>`。
- `Assets/EasyChart/LibraryUxml/<当前库>` 会被复制到 `Assets/EasyChart/LibraryUxml/<新库>`（如果源库存在对应 UXML 目录）。

### 限制与命名规则

- `<Root>` 库不允许克隆。
- 新名称会做基础清理（移除非法文件名字符），空白名称会被忽略。
- 如果目标库已存在（同名文件夹已存在），会提示并取消。

### 克隆后的行为

- 会自动切换当前选中的 Library 为新库。
- 会刷新 Library 下拉列表与左侧资源树，并触发右侧面板/预览的刷新。

---

## 常用操作（右键菜单）

你也可以在树上的条目上 **右键**：

### 右键文件夹

- **New Folder...**：在该文件夹下创建子文件夹。
- **New Chart...**：在该文件夹下创建新的 `ChartProfile`。
- **Export Folder to UXML (Mirror/Backup)**：导出该文件夹下的内容到 UXML（用于备份/分发/版本化）。
- **Rename...**：重命名文件夹。
- **Delete**：删除文件夹（请谨慎，属于破坏性操作）。

### 右键 ChartProfile

- **Export to UXML**：导出当前 Profile 的 UXML。
- **Clone**：克隆一个新的 Profile（用于快速派生相似图表）。
- **Rename...**：重命名资产（同时会尝试同步更新 `profile.name` / `profile.chartName`）。
- **Ping**：在 Project 视图中定位该资产。
- **Delete**：删除资产。

---

## 拖拽移动与排序

Library 树支持拖拽移动文件夹或 `ChartProfile`：

- **拖拽 ChartProfile 到文件夹**：会触发 `AssetDatabase.MoveAsset`，把 `.asset` 移动到目标文件夹。
- **拖拽文件夹到文件夹**：会把整个文件夹移动到目标文件夹下。

注意：

- 如果目标无效（例如拖到自身/子目录），会拒绝（鼠标提示为 Rejected）。
- 移动后会自动刷新树。

---

## 重命名（双击与内联编辑）

在树上 **双击** 条目会进入内联重命名流程（等价于执行 Rename）。

实现上会对名字做基础清理（移除非法文件名字符）。如果你输入空白或与原名相同，会取消重命名。

---

## 常见问题与排错

- **右侧面板为空**：
  - 先确认左侧是否选中了 `ChartProfile`（而不是文件夹）。
- **改了名字但 chartName 没更新**：
  - ChartProfile 可能有额外同步逻辑；建议在 Inspector 里确认 `Chart Name` 字段是否一致。
- **拖拽失败**：
  - 常见原因：拖到了自身、拖到子目录、或目标路径已存在同名资源。

---

## Help

- 点击标题栏最右侧 **Help** 图标可回到本章节。
