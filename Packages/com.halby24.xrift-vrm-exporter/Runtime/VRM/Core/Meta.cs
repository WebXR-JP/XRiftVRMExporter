// SPDX-FileCopyrightText: 2024-present halby24
// SPDX-License-Identifier: MPL-2.0

#nullable enable

using UnityEngine;

namespace XRift.VrmExporter.VRM.Core
{
    /// <summary>
    /// Avatar使用許可
    /// </summary>
    public enum AvatarPermission
    {
        OnlyAuthor,
        ExplicitlyLicensedPerson,
        Everyone,
    }

    /// <summary>
    /// 商用利用
    /// </summary>
    public enum CommercialUsage
    {
        PersonalNonProfit,
        PersonalProfit,
        Corporation,
    }

    /// <summary>
    /// クレジット表記
    /// </summary>
    public enum CreditNotation
    {
        Required,
        Unnecessary,
    }

    /// <summary>
    /// 改変
    /// </summary>
    public enum Modification
    {
        Prohibited,
        AllowModification,
        AllowModificationRedistribution,
    }

    /// <summary>
    /// Expression Override Type (VRM 1.0)
    /// </summary>
    public enum ExpressionOverrideType
    {
        None,
        Block,
        Blend,
    }

    /// <summary>
    /// Expression Settings Interface
    /// </summary>
    public interface IExpressionSettings
    {
        // Note: Using object? instead of VrmExpressionProperty? to avoid circular dependency
        object? HappyExpression { get; }
        object? AngryExpression { get; }
        object? SadExpression { get; }
        object? RelaxedExpression { get; }
        object? SurprisedExpression { get; }
        System.Collections.Generic.IEnumerable<object> CustomExpressions { get; }
    }

    /// <summary>
    /// MToon変換設定
    /// </summary>
    public class MToonConvertSettings
    {
        public bool EnableRimLight { get; set; }
        public bool EnableMatCap { get; set; }
        public bool EnableOutline { get; set; }
    }

    /// <summary>
    /// メタデータ
    /// </summary>
    public static class Meta
    {
        public const string DefaultLicenseUrl = "https://vrm.dev/licenses/1.0/";
    }
}
