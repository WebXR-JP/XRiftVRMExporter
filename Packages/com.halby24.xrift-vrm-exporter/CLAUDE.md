# XRift VRM Exporter

Modular Avatar用VRM 1.0エクスポートシステム

## アーキテクチャ

### パッケージ構造
```
Editor/
├── Core/           # NDMFプラグイン・プラットフォーム
│   ├── XRiftVrmPlugin.cs       # NDMFプラグイン登録
│   ├── XRiftVrmPlatform.cs     # INDMFPlatformProvider実装
│   ├── XRiftVrmExporter.cs     # メインエクスポーター
│   └── XRiftBuildState.cs      # ビルド状態管理
├── Converters/     # VRM変換処理
│   └── BoneResolver.cs         # ボーンウェイト解決
├── Components/     # 設定コンポーネント（未実装）
├── UI/             # NDMF BuildUI
│   └── XRiftBuildUI.cs         # ビルドUIコントロール
├── VRM/            # glTF/VRM データ構造
│   └── Document.cs             # glTF/VRM型定義
└── Utils/          # ユーティリティ
    ├── UnityExtensions.cs      # Unity型拡張メソッド
    ├── MaterialVariant.cs      # MaterialVariant型
    └── AssetPathUtils.cs       # パスユーティリティ
```

### 主要クラス
- `XRiftVrmPlugin : Plugin<T>` - NDMFプラグイン登録
- `XRiftVrmPlatform : INDMFPlatformProvider` - プラットフォーム定義
- `XRiftVrmExporter` - VRMエクスポートコア
- `XRiftBuildState` - ビルドプロセス状態管理
- `BoneResolver` - SkinnedMeshRendererボーンウェイト解決

### 依存関係
- `nadena.dev.ndmf` >= 1.8.0
- `com.vrchat.avatars` (条件付き)
- `jp.lilxyzw.liltoon` (条件付き)

### 条件付きコンパイル
- `XRIFT_HAS_VRCHAT_SDK` - VRChat Avatar SDK
- `XRIFT_HAS_LILTOON` - lilToon シェーダー
- `XRIFT_HAS_NDMF_PLATFORM` - NDMF 1.8+ プラットフォーム機能
- `XRIFT_HAS_MODULAR_AVATAR` - Modular Avatar

### ライセンス
MPL-2.0（ndmf-vrm-exporter由来のコードを含む）

### 参考実装
- ndmf-vrm-exporter: `Packages/com.github.hkrn.ndmf-vrm-exporter/Editor/`
  - NDMFVRMExporter.cs - 変換処理
  - Document.cs - glTF/VRM型定義
