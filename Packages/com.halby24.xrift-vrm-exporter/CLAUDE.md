# XRift VRM Exporter

NDMF対応のマルチプラットフォームエクスポーター

## アーキテクチャ (Multi-Platform)

### パッケージ構造
```
Editor/
├── Shared/              # 共有インフラ
│   ├── Core/           # 基底抽象クラス
│   └── Utils/          # 共通ユーティリティ
├── Platforms/          # プラットフォーム実装
│   ├── VRM/           # VRM 1.0 エクスポート
│   └── AvatarFormat/  # XRift Avatar Format
└── (旧Core/, Components/, UI/ は削除)
```

### プラットフォーム

#### VRM Platform (`com.halby24.xrift-vrm-exporter.vrm`)
- VRM 1.0形式でエクスポート
- PhysBone → SpringBone変換
- lilToon → MToon材質変換
- 拡張子: `.vrm`

#### Avatar Format Platform (`com.halby24.xrift-vrm-exporter.avatarformat`)
- XRift独自フォーマット（実装予定）
- 拡張子: `.xrift`
- 現在: スタブ実装

### 主要クラス

**Shared:**
- `XRiftBasePlatform : INDMFPlatformProvider` - プラットフォーム基底
- `XRiftBasePlugin<T> : Plugin<T>` - プラグイン基底
- `XRiftBaseExporter` - エクスポーター基底
- `XRiftBaseBuildUI : BuildUIElement` - BuildUI基底
- `XRiftBuildState` - ビルド状態管理

**VRM Platform:**
- `XRiftVrmPlatform : XRiftBasePlatform` - VRMプラットフォーム
- `XRiftVrmPlugin : Plugin<T>` - VRMプラグイン
- `XRiftVrmExporter` - VRMエクスポーター
- `XRiftVrmBuildUI : XRiftBaseBuildUI` - VRM BuildUI
- `XRiftVrmDescriptor` - VRM設定コンポーネント

**Avatar Format Platform:**
- `XRiftAvatarFormatPlatform : XRiftBasePlatform`
- `XRiftAvatarFormatPlugin : Plugin<T>`
- `XRiftAvatarFormatExporter`
- `XRiftAvatarFormatBuildUI : XRiftBaseBuildUI`
- `XRiftAvatarFormatDescriptor`

### 依存関係
- `nadena.dev.ndmf` >= 1.8.0
- `com.vrchat.avatars` (VRM Platform)
- `jp.lilxyzw.liltoon` (VRM Platform, オプション)

### ライセンス
MPL-2.0（ndmf-vrm-exporter由来のコードを含む）

### 参考実装
- ndmf-vrm-exporter: `Packages/com.github.hkrn.ndmf-vrm-exporter/Editor/`
  - NDMFVRMExporter.cs - 変換処理
  - Document.cs - glTF/VRM型定義
