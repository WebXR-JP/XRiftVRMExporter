# XRift VRM Exporter

Modular Avatar用VRM 1.0エクスポートシステム

## アーキテクチャ

### パッケージ構造
```
Editor/
├── Core/           # NDMFプラグイン・プラットフォーム
├── Converters/     # VRM変換処理
├── Components/     # 設定コンポーネント
├── UI/             # NDMF BuildUI + 独立ウィンドウ
├── VRM/            # glTF/VRM データ構造
└── Utils/          # ユーティリティ
```

### 主要クラス
- `XRiftVrmPlugin : Plugin<T>` - NDMFプラグイン登録
- `XRiftVrmPlatform : INDMFPlatformProvider` - プラットフォーム定義
- `XRiftVrmExporter` - VRMエクスポートコア

### 依存関係
- `nadena.dev.ndmf` >= 1.8.0
- `com.vrchat.avatars` (条件付き)
- `jp.lilxyzw.liltoon` (条件付き)

### 条件付きコンパイル
- `XRIFT_HAS_VRCHAT_SDK` - VRChat Avatar SDK
- `XRIFT_HAS_LILTOON` - lilToon シェーダー
- `XRIFT_HAS_NDMF_PLATFORM` - NDMF 1.8+ プラットフォーム機能
- `XRIFT_HAS_MODULAR_AVATAR` - Modular Avatar

### 参考実装
- ndmf-vrm-exporter: `Editor/NDMFVRMExporter.cs`
- modular-avatar.resonite: `Editor/ResoniteBuildPlugin.cs`
