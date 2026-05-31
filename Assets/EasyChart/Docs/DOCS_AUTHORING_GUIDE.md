# LumaxForge 文档编写规范

本规范用于指导产品文档的编写，确保所有产品文档能够统一集成到 LumaxForge 官网。

## 目录结构

```
docs/
├── Manual/                         # 所有产品文档数据
│   └── {product-id}/               # 产品目录（如 easy-chart-lit）
│       ├── manual-config.json      # 必需：文档配置文件
│       ├── zh/                     # 中文章节目录
│       │   ├── 00_00-Index.md
│       │   ├── 00_01-QuickStart.md
│       │   └── ...
│       ├── en/                     # 英文章节目录（可选）
│       │   ├── 00_00-Index.md
│       │   └── ...
│       └── assets/                 # 文档资源文件（可选）
├── ManualWeb/                      # 页面渲染代码（无需修改）
└── DOCS_AUTHORING_GUIDE.md         # 本规范文件
```

**关键说明：**
- `Manual/` 目录存放所有产品的文档数据
- 每个产品一个子目录，目录名即为产品 ID（用于 URL）
- 章节内容使用 Markdown 文件，按语言分目录存放
- `ManualWeb/` 是渲染代码，添加新产品时无需修改

## 配置文件格式

### manual-config.json

```json
{
  "product": {
    "id": "your-product-id",
    "name": "产品显示名称",
    "version": "1.0.0",
    "description": "产品简短描述",
    "icon": "",
    "assetStoreUrl": "https://assetstore.unity.com/packages/..."
  },
  "meta": {
    "generatedAt": "2026-01-01 00:00:00",
    "defaultLang": "zh",
    "supportedLangs": ["zh", "en"]
  },
  "navigation": {
    "groups": [
      {
        "key": "getting_started",
        "title": {
          "zh": "快速开始",
          "en": "Getting Started"
        },
        "chapters": [
          "01_01-Introduction",
          "01_02-QuickStart"
        ]
      },
      {
        "key": "features",
        "title": {
          "zh": "功能详解",
          "en": "Features"
        },
        "chapters": [
          "02_01-Feature1",
          "02_02-Feature2"
        ]
      }
    ]
  }
}
```

### 字段说明

| 字段 | 必需 | 说明 |
|------|------|------|
| `product.id` | ✅ | 产品唯一标识，用于 URL 路径 |
| `product.name` | ✅ | 产品显示名称 |
| `product.version` | ❌ | 产品版本号 |
| `product.description` | ❌ | 产品简短描述 |
| `product.assetStoreUrl` | ❌ | Asset Store 链接 |
| `meta.generatedAt` | ❌ | 文档生成时间 |
| `meta.defaultLang` | ✅ | 默认语言 |
| `meta.supportedLangs` | ✅ | 支持的语言列表 |
| `navigation.groups` | ✅ | 导航分组配置 |

## 章节文件格式

每个章节是一个独立的 Markdown 文件，放在对应语言目录下：

```
docs/Manual/{product-id}/
├── zh/
│   ├── 00_00-Index.md
│   ├── 00_01-QuickStart.md
│   └── 01_01-EditorWorkflow.md
└── en/
    ├── 00_00-Index.md
    └── ...
```

### 章节文件示例

```markdown
# 快速开始

本章介绍如何快速上手使用产品。

## 安装

1. 打开 Unity Package Manager
2. 导入插件包

## 基本使用

...
```

**注意：** 文件的第一个 `# 标题` 会被自动提取为章节标题。

### 章节 ID 命名规范

章节 ID 采用 `{分组序号}_{章节序号}-{章节名称}` 格式：

- `00_00-Index` - 概览分组的索引页
- `01_01-Introduction` - 第一分组的第一章
- `02_03-AdvancedFeature` - 第二分组的第三章

**命名规则：**
- 分组序号：两位数字，从 00 开始
- 章节序号：两位数字，从 01 开始
- 章节名称：PascalCase，简洁明了

## Markdown 内容规范

### 标题层级

```markdown
# 章节标题（H1，每章只有一个）

## 主要小节（H2）

### 子小节（H3）
```

### 代码块

```markdown
​```csharp
// C# 代码示例
public class Example : MonoBehaviour
{
    void Start() { }
}
​```
```

### 列表

```markdown
- 无序列表项 1
- 无序列表项 2

1. 有序列表项 1
2. 有序列表项 2
```

### 链接

```markdown
[链接文字](./other-chapter.md)
[外部链接](https://example.com)
```

### 注意事项块

```markdown
> **注意**：这是一个重要提示。

> **警告**：这是一个警告信息。
```

## 多语言支持

1. 每种语言创建独立的目录：`zh/`、`en/`
2. 在 `manual-config.json` 中配置 `supportedLangs`
3. 导航标题使用多语言对象：`{ "zh": "中文标题", "en": "English Title" }`
4. 每个语言目录下的 Markdown 文件名必须一致

## 迁移到官网

完成文档编写后，将产品目录复制到 LumaxForge 项目的 `docs/Manual/` 下即可自动集成：

```bash
# 复制产品文档到官网
cp -r your-product/ LumaxForge/docs/Manual/your-product/
```

### 检查清单

- [ ] `manual-config.json` 配置正确
- [ ] 所有章节 ID 在导航 `chapters` 数组中都有引用
- [ ] 章节文件使用 Markdown 格式（.md 后缀）
- [ ] 多语言目录下的文件名一致
- [ ] 图片资源放在 `assets/` 目录

## AI 辅助编写

在其他项目中使用 AI 编写文档时，可以提供以下提示：

```
请按照 LumaxForge 文档规范编写产品文档：

1. 创建目录结构：{product-id}/manual-config.json, {product-id}/zh/*.md
2. 编写 manual-config.json 配置文件（产品信息 + 导航分组）
3. 每个章节一个 Markdown 文件，文件名格式：{分组序号}_{章节序号}-{章节名称}.md
4. 文件第一行必须是 # 标题（会被提取为章节标题）
5. 导航分组标题支持多语言：{ "zh": "中文", "en": "English" }

参考格式见 LumaxForge/docs/DOCS_AUTHORING_GUIDE.md
```

## 示例

完整示例参见：`docs/Manual/easy-chart-lit/`（Unity Easy Chart 文档）
