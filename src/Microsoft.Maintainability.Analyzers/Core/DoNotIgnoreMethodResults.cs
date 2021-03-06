// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Semantics;

namespace Microsoft.Maintainability.Analyzers
{
    /// <summary>
    /// CA1806: Do not ignore method results
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public sealed class DoNotIgnoreMethodResultsAnalyzer : DiagnosticAnalyzer
    {
        internal const string RuleId = "CA1806";

        private static readonly ImmutableHashSet<string> s_stringMethodNames = ImmutableHashSet.CreateRange(
            new[] {
                "ToUpper",
                "ToLower",
                "Trim",
                "TrimEnd",
                "TrimStart",
                "ToUpperInvariant",
                "ToLowerInvariant",
                "Clone",
                "Format",
                "Concat",
                "Copy",
                "Insert",
                "Join",
                "Normalize",
                "Remove",
                "Replace",
                "Split",
                "PadLeft",
                "PadRight",
                "Substring",
            });

        private static readonly LocalizableString s_localizableTitle = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsTitle), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));

        private static readonly LocalizableString s_localizableMessageObjectCreation = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsMessageObjectCreation), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageStringCreation = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsMessageStringCreation), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageHResultOrErrorCode = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsMessageHResultOrErrorCode), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));
        private static readonly LocalizableString s_localizableMessageTryParse = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsMessageTryParse), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));
        private static readonly LocalizableString s_localizableDescription = new LocalizableResourceString(nameof(MicrosoftMaintainabilityAnalyzersResources.DoNotIgnoreMethodResultsDescription), MicrosoftMaintainabilityAnalyzersResources.ResourceManager, typeof(MicrosoftMaintainabilityAnalyzersResources));

        internal static DiagnosticDescriptor ObjectCreationRule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessageObjectCreation,
                                                                             DiagnosticCategory.Performance,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "https://msdn.microsoft.com/en-us/library/ms182273.aspx",
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        internal static DiagnosticDescriptor StringCreationRule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessageStringCreation,
                                                                             DiagnosticCategory.Performance,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "https://msdn.microsoft.com/en-us/library/ms182273.aspx",
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        internal static DiagnosticDescriptor HResultOrErrorCodeRule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessageHResultOrErrorCode,
                                                                             DiagnosticCategory.Performance,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "https://msdn.microsoft.com/en-us/library/ms182273.aspx",
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        internal static DiagnosticDescriptor TryParseRule = new DiagnosticDescriptor(RuleId,
                                                                             s_localizableTitle,
                                                                             s_localizableMessageTryParse,
                                                                             DiagnosticCategory.Performance,
                                                                             DiagnosticSeverity.Warning,
                                                                             isEnabledByDefault: true,
                                                                             description: s_localizableDescription,
                                                                             helpLinkUri: "https://msdn.microsoft.com/en-us/library/ms182273.aspx",
                                                                             customTags: WellKnownDiagnosticTags.Telemetry);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ObjectCreationRule, StringCreationRule, HResultOrErrorCodeRule, TryParseRule);

        public override void Initialize(AnalysisContext analysisContext)
        {
            analysisContext.EnableConcurrentExecution();
            analysisContext.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);

            analysisContext.RegisterOperationBlockStartAction(osContext =>
            {
                var method = osContext.OwningSymbol as IMethodSymbol;
                if (method == null)
                {
                    return;
                }

                osContext.RegisterOperationAction(opContext =>
                {
                    IOperation expression = ((IExpressionStatement)opContext.Operation).Expression;
                    DiagnosticDescriptor rule = null;
                    string targetMethodName = null;
                    switch (expression.Kind)
                    {
                        case OperationKind.ObjectCreationExpression:
                            IMethodSymbol ctor = ((IObjectCreationExpression)expression).Constructor;
                            if (ctor != null)
                            {
                                rule = ObjectCreationRule;
                                targetMethodName = ctor.ContainingType.Name;
                            }
                            break;

                        case OperationKind.InvocationExpression:
                            IInvocationExpression invocationExpression = ((IInvocationExpression)expression);
                            IMethodSymbol targetMethod = invocationExpression.TargetMethod;
                            if (targetMethod == null)
                            {
                                break;
                            }

                            if (IsStringCreatingMethod(targetMethod))
                            {
                                rule = StringCreationRule;
                            }
                            else if (IsTryParseMethod(targetMethod))
                            {
                                rule = TryParseRule;
                            }
                            else if (IsHResultOrErrorCodeReturningMethod(targetMethod))
                            {
                                rule = HResultOrErrorCodeRule;
                            }

                            targetMethodName = targetMethod.Name;
                            break;
                    }

                    if (rule != null)
                    {
                        Diagnostic diagnostic = Diagnostic.Create(rule, expression.Syntax.GetLocation(), method.Name, targetMethodName);
                        opContext.ReportDiagnostic(diagnostic);
                    }
                }, OperationKind.ExpressionStatement);
            });
        }

        private static bool IsStringCreatingMethod(IMethodSymbol method)
        {
            return method.ContainingType.SpecialType == SpecialType.System_String &&
                s_stringMethodNames.Contains(method.Name);
        }

        private static bool IsTryParseMethod(IMethodSymbol method)
        {
            return method.Name.StartsWith("TryParse", StringComparison.OrdinalIgnoreCase) &&
                method.ReturnType.SpecialType == SpecialType.System_Boolean &&
                method.Parameters.Length >= 2 &&
                method.Parameters[1].RefKind != RefKind.None;
        }

        private static bool IsHResultOrErrorCodeReturningMethod(IMethodSymbol method)
        {
            // Tune this method to match the FxCop behavior once https://github.com/dotnet/roslyn/issues/7282 is addressed.
            return method.GetDllImportData() != null &&
                (method.ReturnType.SpecialType == SpecialType.System_Int32 ||
                method.ReturnType.SpecialType == SpecialType.System_UInt32);
        }
    }
}