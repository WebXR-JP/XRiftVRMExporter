# UniVRM統合によるコードベース削減計画

**作成日:** 2026-01-02
**最終更新:** 2026-01-02
**対象:** XRift VRM Exporter (com.halby24.xrift-vrm-exporter)
**ステータス:** ✅ **統合完了**

---

## エグゼクティブサマリー

VRM Consortiumが提供する公式VRM 1.0パッケージ（`com.vrmc.vrm`）に含まれる`Vrm10Exporter`を活用し、XRift VRM Exporterのコードベースを**大幅削減**しました。

すべての独自実装を公式実装で置き換え完了：
- ✅ メッシュ変換 → `ModelExporter`
- ✅ Humanoidボーンマッピング → `Vrm10Exporter`
- ✅ Expression（表情） → `Vrm10Exporter`
- ✅ LookAt（視線制御） → `Vrm10Exporter`
- ✅ SpringBone（揺れもの） → `Vrm10Exporter`
- ✅ Constraint（コンストレイント） → `Vrm10Exporter`
- ✅ MToon 1.0マテリアル → `Vrm10MaterialExporterUtility`
- ✅ glTF構造全般 → UniGLTF標準

---

## 統合結果

### 現在のXRift VRM Exporter構成（統合後）

**総ファイル数:** 11個のC#ファイル

**主要コンポーネント:**
- `XRiftVrmExporter.cs` - メインエクスポーター (240行、UniVRM統合版)
- NDMFプラグイン統合
  - `XRiftVrmPlugin.cs` - NDMFプラグイン登録
  - `XRiftBuildState.cs` - ビルド状態管理
  - `XRiftVrmPlatform.cs` - プラットフォーム定義
- XRift固有機能
  - `XRiftVrmDescriptor.cs` - VRMメタ情報管理
  - `VrmExpressionProperty.cs` - Expression設定
  - `MaterialVariant.cs` - マテリアルバリアント
  - `MaterialBaker.cs` - マテリアルベイク
- ユーティリティ
  - `AssetPathUtils.cs` - パス処理
  - `UnityExtensions.cs` - Unity拡張
- UI
  - `XRiftBuildUI.cs` - ビルドUI

**削除されたConverter群（すべてUniVRMに統合）:**
- ❌ `MeshConverter.cs` - メッシュ変換
- ❌ `HumanoidConverter.cs` - Humanoid変換
- ❌ `ExpressionConverter.cs` - Expression変換
- ❌ `LookAtConverter.cs` - LookAt変換
- ❌ `SpringBoneConverter.cs` - SpringBone変換
- ❌ `ConstraintConverter.cs` - Constraint変換
- ❌ `MToonConverter.cs` - MToonマテリアル変換
- ❌ `BoneResolver.cs` - ボーン解決
- ❌ `VRM.Gltf.*` - 独自glTF構造

### 発見: com.vrmc.vrm (VRM 1.0公式パッケージ)

**バージョン:** v0.131.0
**既にプロジェクトに導入済み** (`Packages/manifest.json` L42)

**提供される機能:**

#### 1. Vrm10Exporter.cs (1034行)
VRM 1.0フォーマット完全対応のエクスポーター

**対応拡張:**
- `VRMC_vrm` - VRM 1.0コア
- `VRMC_springBone` - SpringBone 1.0
- `VRMC_springBone_extended_collider` - 拡張コライダー
- `VRMC_node_constraint` - ノードコンストレイント
- `VRMC_materials_mtoon` - MToon 1.0

**エクスポート機能:**
- Humanoid (必須・オプションボーン完全対応)
- Meta情報
- Expression（プリセット・カスタム）
- LookAt（Bone/BlendShape両対応）
- FirstPerson（メッシュアノテーション）
- SpringBone（Sphere/Capsule/Plane）
- Constraint（Aim/Roll/Rotation）

#### 2. ModelExporter.cs
UnityヒエラルキーをVrmLib.Modelに変換

**機能:**
- GameObject階層の走査
- メッシュ・マテリアル収集
- Skinning情報の処理
- Humanoidボーンマッピング

#### 3. 便利なstatic関数

```csharp
public static byte[] Export(
    GltfExportSettings settings,
    GameObject go,
    IMaterialExporter materialExporter = null,
    ITextureSerializer textureSerializer = null,
    VRM10ObjectMeta vrmMeta = null)
```

**使用例（サンプルより）:**
```csharp
using (var arrayManager = new NativeArrayManager())
{
    var converter = new UniVRM10.ModelExporter();
    var model = converter.Export(settings, arrayManager, root);

    // 右手系に変換
    model.ConvertCoordinate(VrmLib.Coordinates.Vrm1, ignoreVrm: false);

    // export vrm-1.0
    var exporter = new Vrm10Exporter(settings);
    exporter.Export(root, model, converter, new VrmLib.ExportArgs(), meta);

    return exporter.Storage.ToGlbBytes();
}
```

---

## 実装完了アーキテクチャ

### XRiftVrmExporter.cs（統合完了版）

```csharp
// XRiftVrmExporter.cs (UniVRM統合版)
internal sealed class XRiftVrmExporter : IDisposable
{
    private readonly GameObject _gameObject;
    private readonly IAssetSaver _assetSaver;
    private readonly IReadOnlyList<MaterialVariant> _materialVariants;

    public string Export(Stream stream)
    {
        // エクスポート設定を作成
        var settings = CreateExportSettings();

        // VRMメタ情報を取得
        var meta = GetOrCreateVrmMeta();

        byte[] bytes;
        using (var arrayManager = new NativeArrayManager())
        {
            // UnityヒエラルキーをVrmLib.Modelに変換
            var converter = new ModelExporter();
            var model = converter.Export(settings, arrayManager, _gameObject);

            // 座標系変換（Unity左手系 → VRM右手系）
            model.ConvertCoordinate(Coordinates.Vrm1, ignoreVrm: false);

            // Material Exporterを作成
            var materialExporter = CreateMaterialExporter();
            var textureSerializer = new RuntimeTextureSerializer();

            // VRM 1.0エクスポート
            using (var exporter = new Vrm10Exporter(settings, materialExporter, textureSerializer))
            {
                exporter.Export(_gameObject, model, converter, new ExportArgs(), meta);
                bytes = exporter.Storage.ToGlbBytes();
            }
        }

        // ストリームに書き込み
        stream.Write(bytes, 0, bytes.Length);
        return "{}";
    }
}
```

### NDMF統合（XRiftVrmPlugin.cs）

```csharp
[assembly: ExportsPlugin(typeof(XRiftVrmPlugin))]

internal class XRiftVrmPlugin : Plugin<XRiftVrmPlugin>
{
    protected override void Configure()
    {
        InPhase(BuildPhase.Optimizing)
            .Run(XRiftVrmExportPass.Instance);
    }
}

internal class XRiftVrmExportPass : Pass<XRiftVrmExportPass>
{
    protected override void Execute(BuildContext context)
    {
        var state = context.GetState<XRiftBuildState>();
        if (!state.ExportEnabled) return;

        var gameObject = context.AvatarRootObject;
        using var exporter = new XRiftVrmExporter(gameObject, assetSaver, state.MaterialVariants);
        using var memoryStream = new MemoryStream();

        exporter.Export(memoryStream);
        state.ExportedVrmData = memoryStream.ToArray();
    }
}
```

---

## 今後の拡張予定

### Material Variant対応強化

Material Variantが必要な場合はカスタムMaterialExporterを実装予定:

```csharp
public class XRiftMaterialExporter : IMaterialExporter
{
    private readonly IReadOnlyList<MaterialVariant> _variants;
    private readonly IMaterialExporter _baseExporter;

    public glTFMaterial ExportMaterial(
        Material m,
        ITextureExporter textureExporter,
        GltfExportSettings settings)
    {
        // Variant適用
        var targetMaterial = ApplyVariant(m);
        return _baseExporter.ExportMaterial(targetMaterial, textureExporter, settings);
    }
}
```

### テスト強化項目

- [ ] Humanoidボーンマッピング自動テスト
- [ ] Expression動作確認テスト
- [ ] LookAt動作確認テスト
- [ ] SpringBone動作確認テスト
- [ ] Constraint動作確認テスト
- [ ] MToonマテリアル確認テスト
- [ ] VRMバリデーター自動チェック

---

## 統合によるメリット

### ✅ コードベース大幅削減達成
- **約50%のファイル削減** (23ファイル → 11ファイル)
- Converter群をすべて削除
- メンテナンス負担の大幅軽減

### ✅ 信頼性向上
- VRM Consortium公式実装を使用
- 広範囲でのテスト済みコードベース
- コミュニティの検証済み

### ✅ 仕様追従が容易
- VRM 1.0仕様変更に自動追従
- UniVRMのアップデートで最新機能が利用可能
- バグ修正も公式で提供

### ✅ 開発効率向上
- 新機能追加が容易
- UniVRMドキュメント参照可能
- VRM仕様実装の負担削減

---

## 注意点・制約

### 外部依存
- `com.vrmc.vrm` (v0.131.0以上) への依存
- UniVRMのバージョンアップ対応が必要
- **対策:** バージョンピンニング、定期的な動作確認

### カスタマイズ制約
- UniVRMの実装に依存
- 独自拡張が必要な場合はIMaterialExporter等のインターフェースを活用
- Material Variant等のXRift固有機能は独自実装維持

---

## 参考資料

### UniVRM公式
- リポジトリ: https://github.com/vrm-c/UniVRM
- ドキュメント: https://vrm.dev/
- VRM 1.0仕様: https://github.com/vrm-c/vrm-specification/tree/master/specification/VRMC_vrm-1.0

### サンプルコード
- `Library\PackageCache\com.vrmc.vrm@3b99078d26\Samples~\VRM10RuntimeExporterSample\VRM10RuntimeExporter.cs`
- `Library\PackageCache\com.vrmc.vrm@3b99078d26\Runtime\IO\Vrm10Exporter.cs`
- `Library\PackageCache\com.vrmc.vrm@3b99078d26\Runtime\IO\Model\ModelExporter.cs`

### 関連ファイル
- `Packages/manifest.json` - パッケージ依存関係
- `Packages/com.halby24.xrift-vrm-exporter/package.json` - XRiftパッケージ定義

---

## 変更履歴

| 日付 | バージョン | 変更内容 |
|------|-----------|---------|
| 2026-01-02 | 1.0 | 初版作成 |
| 2026-01-02 | 2.0 | UniVRM統合完了を反映、不要なセクション削除 |

---

## 参考：実装完了済みアーキテクチャ詳細

### UniVRM主要API活用状況

#### Vrm10Exporter
XRiftVrmExporter.csで使用中:
```csharp
using (var exporter = new Vrm10Exporter(settings, materialExporter, textureSerializer))
{
    exporter.Export(_gameObject, model, converter, new ExportArgs(), meta);
    bytes = exporter.Storage.ToGlbBytes();
}
```

#### ModelExporter
UnityヒエラルキーからVrmLib.Modelへの変換:
```csharp
var converter = new ModelExporter();
var model = converter.Export(settings, arrayManager, _gameObject);
```

#### 座標系変換
Unity左手系からVRM右手系への変換:
```csharp
model.ConvertCoordinate(Coordinates.Vrm1, ignoreVrm: false);
```

#### VRM10ObjectMeta
XRiftVrmDescriptor経由でメタ情報を管理:
```csharp
var descriptor = _gameObject.GetComponent<XRiftVrmDescriptor>();
var meta = descriptor?.ToVrm10Meta() ?? CreateDefaultMeta();
```

---

**End of Document**
