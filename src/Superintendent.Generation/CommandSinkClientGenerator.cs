using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Superintendent.Generation
{
    public sealed class GenerateClientAttribute : Attribute { }

    [Generator]
    public class CommandSinkClientGenerator : IIncrementalGenerator
    {
        public static DiagnosticDescriptor PartialTypeDecl = new DiagnosticDescriptor("SI-1000", "ClientOffset type must be partial for generation", "ClientOffset type must be partial for generation", "Usage", DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor ManagedArgOrRet = new DiagnosticDescriptor("SI-1001", "Argument and return types must be unmanaged types", "Argument and return types must be unmanaged types", "Usage", DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor ParamNamesLength = new DiagnosticDescriptor("SI-1002", "ParamNames must be an equal amount of string literals as parameter type arguments", "ParamNames must be an equal amount of string literals as parameter type arguments", "Usage", DiagnosticSeverity.Error, true);
        public static DiagnosticDescriptor TooManyPointerTypeArgs = new DiagnosticDescriptor("SI-1003", "Pointer references should only have a single type argument", "Pointer references should only have a single type argument", "Usage", DiagnosticSeverity.Error, true);

        public class GenInfo : IEquatable<GenInfo>
        {
            public GenInfo(TypeDeclarationSyntax typeDecl)
            {
                this.TypeDecl = typeDecl;
            }

            public TypeDeclarationSyntax TypeDecl { get; }

            public bool Equals(GenInfo other)
            {
                return this.TypeDecl.Identifier.Equals(other.TypeDecl.Identifier);
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
                typeof(GenerateClientAttribute).FullName,
                IsOffsetType,
                (g, c) => (TypeDeclarationSyntax)g.TargetNode)
                .Combine(context.CompilationProvider);

            context.RegisterSourceOutput(provider, static (c, input) => GenerateOffsetClient(c, input.Right, input.Left));
        }

        private static void GenerateOffsetClient(SourceProductionContext spc, Compilation comp, TypeDeclarationSyntax decl)
        {
            if (!decl.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
            {
                var diag = Diagnostic.Create(PartialTypeDecl, decl.GetLocation());
                spc.ReportDiagnostic(diag);
                return;
            }
            var clientName = "Client";

            var model = comp.GetSemanticModel(decl.SyntaxTree);

            var props = decl.Members.OfType<PropertyDeclarationSyntax>();

            var clientClassMembers = new List<MemberDeclarationSyntax>
            {
                GenerateCtor(decl.Identifier.ToString(), clientName)
            };

            foreach (var prop in props)
            {
                if (model.GetSymbolInfo(prop.Type).Symbol is INamedTypeSymbol namedType)
                {
                    clientClassMembers.AddRange(namedType.Name switch
                    {
                        "Fun" => GenerateFunction(spc, comp, model, prop),
                        "FunVoid" => GenerateFunction(spc, comp, model, prop, typeListContainsReturn: false),
                        "Ptr" => GenerateDataReadWrite(spc, namedType, prop),
                        _ => Array.Empty<MemberDeclarationSyntax>()
                    });
                }
            }

            var clientClass = ClassDeclaration(clientName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(
                    GenericName(Identifier("CommandSinkClient"))
                        .WithTypeArgumentList(TypeArgumentList(SingletonSeparatedList<TypeSyntax>(IdentifierName(decl.Identifier))))
                    ))))
                .WithMembers(List(clientClassMembers));

            var newRoot = CopyNamespaceWithUsingsFor(decl, new[]
                {
                    "Superintendent.Core.CommandSink",
                    "System.Runtime.CompilerServices"
                })
                .WithMembers(SingletonList<MemberDeclarationSyntax>(decl
                    .WithMembers(List(new[] { clientClass, GenerateCreateClientMethod(clientName) }))
                    .WithAttributeLists(List<AttributeListSyntax>())));

            var source = newRoot
                .NormalizeWhitespace()
                .ToString();

            spc.AddSource($"{decl.Identifier}.{clientName}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

        private static MemberDeclarationSyntax GenerateCtor(string offsetType, string typeName)
        {
            return ConstructorDeclaration(
                        Identifier(typeName))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        ParameterList(
                            SeparatedList<ParameterSyntax>(
                                new SyntaxNodeOrToken[]{
                                    Parameter(
                                        Identifier("sink"))
                                    .WithType(
                                        IdentifierName("ICommandSink")),
                                    Token(SyntaxKind.CommaToken),
                                    Parameter(
                                        Identifier("offsets"))
                                    .WithType(
                                        IdentifierName(offsetType))})))
                    .WithInitializer(
                        ConstructorInitializer(
                            SyntaxKind.BaseConstructorInitializer,
                            ArgumentList(
                                SeparatedList<ArgumentSyntax>(
                                    new SyntaxNodeOrToken[]{
                                        Argument(
                                            IdentifierName("sink")),
                                        Token(SyntaxKind.CommaToken),
                                        Argument(
                                            IdentifierName("offsets"))}))))
                    .WithBody(
                        Block());
        }

        private static MemberDeclarationSyntax[] GenerateDataReadWrite(SourceProductionContext spc, INamedTypeSymbol namedType, PropertyDeclarationSyntax prop)
        {
            if (prop.Type is not GenericNameSyntax propType)
            {
                return Array.Empty<MemberDeclarationSyntax>();
            }

            if (propType.TypeArgumentList.Arguments.Count > 1)
            {
                spc.ReportDiagnostic(Diagnostic.Create(TooManyPointerTypeArgs, prop.GetLocation()));
            }

            var dataType = propType.TypeArgumentList.Arguments.First();

            var offsetAccess = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("Offsets"),
                IdentifierName(prop.Identifier.ToString()));

            var sinkRead = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("CommandSink"),
                GenericName("Read")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(dataType))));

            var readMethod = MethodDeclaration(dataType, "Read" + prop.Identifier.ToString())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithBody(Block(
                    ExpressionStatement(InvocationExpression(sinkRead, ArgumentList(SeparatedList(new[]{ 
                        Argument(offsetAccess), 
                        Argument(DeclarationExpression(IdentifierName(Identifier(TriviaList(), SyntaxKind.VarKeyword, "var", "var", TriviaList())),
                            SingleVariableDesignation(Identifier("value")))).WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword))})))),
                    ReturnStatement(IdentifierName("value"))
                ));

            var sinkWrite = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName("CommandSink"),
                GenericName("Write")
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SingletonSeparatedList<TypeSyntax>(dataType))));

            var writeMethod = MethodDeclaration(IdentifierName("void"), "Write" + prop.Identifier.ToString())
                .WithModifiers(TokenList(Token(SyntaxKind.PublicKeyword)))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier("value")).WithType(dataType))))
                .WithBody(Block(
                    ExpressionStatement(InvocationExpression(sinkWrite, ArgumentList(SeparatedList(new[] { Argument(offsetAccess), Argument(IdentifierName("value")) }))))
                ));

            return new[] { readMethod, writeMethod };
        }

        private static MemberDeclarationSyntax[] GenerateFunction(SourceProductionContext spc, Compilation compilation, SemanticModel model, PropertyDeclarationSyntax prop, bool typeListContainsReturn = true)
        {
            var body = new List<StatementSyntax>();

            string[] paramNames = Array.Empty<string>();
            IdentifierNameSyntax[] paramTypes = Array.Empty<IdentifierNameSyntax>();

            TypeSyntax? returnType = null;
            var returnFloat = false;

            var args = new List<ArgumentSyntax>();

            args.Add(Argument(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Offsets"),
                    IdentifierName(prop.Identifier.ToString()))));

            // generate marshalling and storage of args for each generic param
            if (prop.Type is GenericNameSyntax genType)
            {
                var typeArgs = genType.TypeArgumentList.Arguments.ToArray();

                var parameterTypeCount = typeArgs.Length;

                if (typeListContainsReturn)
                    parameterTypeCount--;

                paramNames = Enumerable.Range(0, parameterTypeCount).Select(i => "arg" + i).ToArray();
                paramTypes = new IdentifierNameSyntax[parameterTypeCount];

                var paramNameAttrs = prop.AttributeLists.Select(a => a.Attributes.FirstOrDefault(at => at.Name.ToString() is "ParamNames" or "ParamNamesAttribute")).Where(a => a.ArgumentList?.Arguments != null);
                if (paramNameAttrs.Any())
                {
                    var desiredType = compilation.GetTypeByMetadataName("Superintendent.Core.ParamNamesAttribute");

                    foreach (var attr in paramNameAttrs)
                    {
                        var argValues = attr.ArgumentList!.Arguments.Select(a => model.GetConstantValue(a.Expression));

                        if (argValues.Any(v => !v.HasValue || v.Value is not string))
                            continue;

                        if (!SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(attr).Type, desiredType))
                            continue;

                        paramNames = argValues.Select(v => (string)v.Value!).ToArray();

                        if (paramNames.Length != parameterTypeCount)
                        {
                            spc.ReportDiagnostic(Diagnostic.Create(ParamNamesLength, attr.GetLocation()));
                            throw new Exception("Inconsistent param names length");
                        }
                    }
                }

                for (var i = 0; i < parameterTypeCount; i++)
                {
                    var targ = typeArgs[i];
                    paramTypes[i] = IdentifierName(targ.ToString());

                    var typeSym = model.GetTypeInfo(targ).Type;
                    if (!typeSym.IsUnmanagedType)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(ManagedArgOrRet, targ.GetLocation()));
                    }

                    args.Add(Argument(UnsafeAs(paramTypes[i], IdentifierName("nint"), IdentifierName(paramNames[i]))));
                }

                if (typeListContainsReturn)
                {
                    returnType = typeArgs[typeArgs.Length - 1];
                    var typeSym = model.GetTypeInfo(returnType).Type;
                    returnFloat = typeSym.SpecialType == SpecialType.System_Single;
                }
            }

            body.AddRange(GenerateCall(ArgumentList(SeparatedList(args)), returnFloat));

            if (returnType != null)
            {
                var callFuncReturn = returnFloat ? "float" : "nint";
                body.Add(ReturnStatement(UnsafeAs(IdentifierName(callFuncReturn), returnType, IdentifierName("result"))));
            }

            var methodType = returnType != null ? returnType : IdentifierName("void");

            var method = MethodDeclaration(methodType, prop.Identifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword));

            if (paramNames.Length > 0)
            {
                var parms = paramNames.Zip(paramTypes, (s, t) => (s, t)).Select(p => Parameter(Identifier(p.s)).WithType(p.t));
                method = method.WithParameterList(ParameterList(SeparatedList(parms)));
            }

            method = method.WithBody(Block(List(body)));

            return new[] { method };
        }

        private static IEnumerable<StatementSyntax> GenerateCall(ArgumentListSyntax offsetAndArgs, bool isFloatReturn = false)
        {
            var callFuncReturn = isFloatReturn ? "float" : "nint";

            yield return ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                DeclarationExpression(
                                    IdentifierName(
                                        Identifier(
                                            TriviaList(),
                                            SyntaxKind.VarKeyword,
                                            "var",
                                            "var",
                                            TriviaList())),
                                    ParenthesizedVariableDesignation(
                                        SeparatedList<VariableDesignationSyntax>(
                                            new SyntaxNodeOrToken[]{
                                                SingleVariableDesignation(
                                                    Identifier("success")),
                                                Token(SyntaxKind.CommaToken),
                                                SingleVariableDesignation(
                                                    Identifier("result"))}))),
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            ThisExpression(),
                                            IdentifierName("CommandSink")),
                                        GenericName(
                                            Identifier("CallFunction"))
                                        .WithTypeArgumentList(
                                            TypeArgumentList(
                                                SingletonSeparatedList<TypeSyntax>(
                                                    IdentifierName(callFuncReturn))))))
                                .WithArgumentList(offsetAndArgs)));

            yield return IfStatement(
                            PrefixUnaryExpression(
                                SyntaxKind.LogicalNotExpression,
                                IdentifierName("success")),
                            Block(
                                SingletonList<StatementSyntax>(
                                    ThrowStatement(
                                        ObjectCreationExpression(
                                            IdentifierName("InvocationException"))
                                        .WithArgumentList(
                                            ArgumentList())))));
        }

        private static bool IsOffsetType(SyntaxNode node, CancellationToken cancellationToken)
        {
            if (node is not TypeDeclarationSyntax decl)
            {
                return false;
            }

            return true;
        }

        private static InvocationExpressionSyntax UnsafeAs(TypeSyntax from, TypeSyntax to, IdentifierNameSyntax variable)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName("Unsafe"),
                    GenericName(
                        Identifier("As"))
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SeparatedList<TypeSyntax>(
                                new SyntaxNodeOrToken[]{
                                    from,
                                    Token(SyntaxKind.CommaToken),
                                    to})))))
            .WithArgumentList(
                ArgumentList(
                    SingletonSeparatedList<ArgumentSyntax>(
                        Argument(variable)
                        .WithRefOrOutKeyword(
                            Token(SyntaxKind.RefKeyword)))));
        }

        private static MemberDeclarationSyntax GenerateCreateClientMethod(string clientTypeName)
        {
            return MethodDeclaration(
                        IdentifierName(clientTypeName),
                        Identifier("CreateClient"))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword)))
                    .WithParameterList(
                        ParameterList(
                            SingletonSeparatedList<ParameterSyntax>(
                                Parameter(
                                    Identifier("sink"))
                                .WithType(
                                    IdentifierName("ICommandSink")))))
                    .WithExpressionBody(
                        ArrowExpressionClause(
                            ObjectCreationExpression(
                                IdentifierName(clientTypeName))
                            .WithArgumentList(
                                ArgumentList(
                                    SeparatedList<ArgumentSyntax>(
                                        new SyntaxNodeOrToken[]{
                                            Argument(
                                                IdentifierName("sink")),
                                            Token(SyntaxKind.CommaToken),
                                            Argument(
                                                ThisExpression())})))))
                    .WithSemicolonToken(
                        Token(SyntaxKind.SemicolonToken));
        }

        private static List<UsingDirectiveSyntax> CollectUsings(SyntaxNode node)
        {
            var col = new UsingCollector();
            col.Visit(node.SyntaxTree.GetRoot());
            return col.Usings;
        }

        private static NamespaceDeclarationSyntax CopyNamespaceWithUsingsFor(SyntaxNode node, IEnumerable<string> additionalUsings)
        {
            var usings = CollectUsings(node);
            var ns = node.FirstAncestorOrSelf<NamespaceDeclarationSyntax>();

            usings.AddRange(additionalUsings.Select(u => UsingDirective(DottedName(u))));

            return NamespaceDeclaration(ns.Name).WithUsings(List(usings));
        }

        private class UsingCollector : CSharpSyntaxWalker
        {
            public List<UsingDirectiveSyntax> Usings { get; private set; } = new();

            public override void VisitNamespaceDeclaration(NamespaceDeclarationSyntax node)
            {
                Usings.AddRange(node.Usings);
                base.VisitNamespaceDeclaration(node);
            }


            public override void VisitCompilationUnit(CompilationUnitSyntax node)
            {
                Usings.AddRange(node.Usings);
                base.VisitCompilationUnit(node);
            }
        }

        public static NameSyntax DottedName(string dottedName)
        {
            if (dottedName == null)
            {
                throw new ArgumentNullException(nameof(dottedName));
            }

            var parts = dottedName.Split('.');
            Debug.Assert(parts.Length > 0);

            NameSyntax? name = null;
            foreach (var part in parts)
            {
                if (name == null)
                {
                    name = IdentifierName(part);
                }
                else
                {
                    name = QualifiedName(name, IdentifierName(part));
                }
            }

            return name;
        }

        private static LocalDeclarationStatementSyntax LocalVar(string name, ExpressionSyntax initializer)
        {
            return LocalDeclarationStatement(
                VariableDeclaration(
                    IdentifierName(
                        Identifier(
                            TriviaList(),
                            SyntaxKind.VarKeyword,
                            "var",
                            "var",
                            TriviaList())))
                .WithVariables(
                    SingletonSeparatedList<VariableDeclaratorSyntax>(
                        VariableDeclarator(
                            Identifier(name))
                        .WithInitializer(
                            EqualsValueClause(initializer)))));
        }
    }
}
