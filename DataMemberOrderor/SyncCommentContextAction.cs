using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.CompleteStatement;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DataMemberOrderor
{
    [ContextAction(Name = "Use base type comment", Group = "C#", Description = "Use comment from base type.", Priority = -20)]
    public class SyncCommentContextAction : ContextActionBase
    {
        protected readonly ICSharpContextActionDataProvider Provider;

        private const string UsedKeyWord = "base type comment";
        
        private ITreeNode _currentNode;

        public SyncCommentContextAction(ICSharpContextActionDataProvider provider)
        {
            this.Provider = provider;
        }

        public override string Text
        {
            get
            {
                return string.Format("Use {0}", UsedKeyWord);
            }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            using (ReadLockCookie.Create())
            {
                this._currentNode = Provider.SelectedElement;

                return NodeIsMethodComment(this._currentNode) || NodeIsImplementingClass();
            }
        }

        private bool NodeIsImplementingClass()
        {
            return NodeIsClass() &&
                   ClassHasInheritance(GetParentClass(this._currentNode));
        }

        private bool NodeIsClass()
        {
            var classDeclaration = GetParentClass(this._currentNode);

            return null != classDeclaration;
        }

        private bool ClassHasInheritance(IClassDeclaration classDeclaration)
        {
            var superTypes = GetSupertypesRecursive(classDeclaration);

            return GetMethodInheritance(classDeclaration, superTypes).Any() ||
                   GetPropertyInheritance(classDeclaration, superTypes).Any();
        }

        private IEnumerable<TreeNodeInheritance> GetPropertyInheritance(IClassDeclaration classDeclaration, IEnumerable<IClassLikeDeclaration> superTypes)
        {
            var childProperties =
                classDeclaration.PropertyDeclarations.Where(IsPublicOrProtectedMethod).ToArray();

            if (!childProperties.Any()) return new TreeNodeInheritance[0];

            var baseProperties = superTypes.SelectMany(GetTypeProperties);

            return from baseProperty in baseProperties
                join childProperty in childProperties
                    on baseProperty.DeclaredElement.ToString() equals childProperty.DeclaredElement.ToString()
                select new TreeNodeInheritance()
                {
                    BaseTreeNode = baseProperty,
                    ChildTreeNode = childProperty
                };
        }
        private IEnumerable<TreeNodeInheritance> GetMethodInheritance(IClassDeclaration classDeclaration, IEnumerable<IClassLikeDeclaration> superTypes)
        {
            var publicMethods =
                classDeclaration.MethodDeclarations.Where(IsPublicOrProtectedMethod).ToArray();

            if (!publicMethods.Any()) return new TreeNodeInheritance[0];
            
            var baseMethods = superTypes.SelectMany(GetTypeMethods);

            return from baseMethod in baseMethods
                join localMethod in publicMethods
                    on baseMethod.DeclaredElement.ToString() equals localMethod.DeclaredElement.ToString()
                select new TreeNodeInheritance()
                {
                    BaseTreeNode = baseMethod,
                    ChildTreeNode = localMethod
                };
        }

        private bool IsPublicOrProtectedMethod(IPropertyDeclaration propertyDeclaration)
        {
            return propertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                   propertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD);
        }

        private bool IsPublicOrProtectedMethod(IMethodDeclaration methodDeclaration)
        {
            return methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                   methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD);
        }

        private IEnumerable<IClassLikeDeclaration> GetSupertypesRecursive(IClassLikeDeclaration classDeclaration)
        {
            var superTypes = new List<IClassLikeDeclaration>();

            var classLikeDeclarations = classDeclaration.SuperTypes.SelectMany(t => t.GetTypeElement().GetDeclarations().Select(d => d as IClassLikeDeclaration)).ToArray();
            superTypes.AddRange(classLikeDeclarations);

            if (classLikeDeclarations.Any())
            {
                var extraSuperTypes = superTypes.SelectMany(GetSupertypesRecursive).ToArray();
                superTypes.AddRange(extraSuperTypes);
            }

            return superTypes;
        }

        private IEnumerable<IPropertyDeclaration> GetTypeProperties(IClassLikeDeclaration superType)
        {
            if (superType is IInterfaceDeclaration)
                return this.GetProperties(superType as IInterfaceDeclaration);

            return this.GetProperties(superType as IClassDeclaration);
        }

        private IEnumerable<IMethodDeclaration> GetTypeMethods(IClassLikeDeclaration superType)
        {
            if (superType is IInterfaceDeclaration)
                return this.GetMethods(superType as IInterfaceDeclaration);

            return this.GetMethods(superType as IClassDeclaration);
        }

        private IClassDeclaration GetParentClass(ITreeNode treeNode)
        {
            return treeNode.Parent as IClassDeclaration;
        }

        private bool NodeIsMethodComment(ITreeNode currentNode)
        {
            var commentNode = currentNode as IDocCommentNode;
            if (null == commentNode) return false;

            return IsMethodDocComment(commentNode);
        }

        private static bool IsMethodDocComment(ITreeNode commentNode)
        {
            return commentNode.Parent is IDocCommentBlockNode && commentNode.Parent.Parent is IMethodDeclaration;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (null == this._currentNode)
            {
                return null;
            }

            if (NodeIsMethodComment(this._currentNode))
            {
                ITreeNode currentNode = this._currentNode.Parent as IDocCommentBlockNode;

                var methodDeclaration = currentNode.Parent as IMethodDeclaration;
                if (null == methodDeclaration)
                {
                    return null;
                }

                this.TryUseBaseTypeComments(methodDeclaration);
            }
            else
            {
                var classDeclaration = GetParentClass(this._currentNode);
                var superTypes = GetSupertypesRecursive(classDeclaration);

                var inheritances = GetMethodInheritance(classDeclaration, superTypes).Concat(GetPropertyInheritance(classDeclaration, superTypes));

                foreach (var methodPair in inheritances)
                {
                    ReplaceOrInsertComments(methodPair.BaseTreeNode, methodPair.ChildTreeNode);
                }
            }

            return null;
        }

        private void ReplaceOrInsertComments(ITreeNode baseMethod, ITreeNode childMethod)
        {
            var baseComments = GetMethodComments(baseMethod);

            if (null == baseComments) return;

            var childComments = GetMethodComments(childMethod);

            if (null == childComments)
            {
                InsertComments(childMethod, baseComments);
            }
            else
            {
                var replacement = baseComments.Copy();

                ModificationUtil.ReplaceChild(childComments, baseComments);
            }
        }

        private void InsertComments(ITreeNode method, IDocCommentBlockNode comments)
        {
            ModificationUtil.AddChildBefore(method.FirstChild, comments);
        }

        private IDocCommentBlockNode GetMethodComments(ITreeNode baseMethod)
        {
            return baseMethod.Children().SingleOrDefault(n => n is IDocCommentBlockNode) as IDocCommentBlockNode;
        }

        private void TryUseBaseTypeComments(IMethodDeclaration methodDeclaration)
        {
            var classDeclaration = methodDeclaration.GetContainingTypeDeclaration() as IClassDeclaration;

            if (null == classDeclaration)
            {
                return;
            }

            IEnumerable<IDeclaredType> superTypes = classDeclaration.SuperTypes;

            IEnumerable<IDeclaration> superTypeDeclarations =
                superTypes.SelectMany(s => s.GetTypeElement().GetDeclarations());

            var methodDeclarations = new List<IMethodDeclaration>();
            foreach (IDeclaration dec in superTypeDeclarations)
            {
                methodDeclarations.AddRange(this.GetMethods(dec as IInterfaceDeclaration));
                methodDeclarations.AddRange(this.GetMethods(dec as IClassDeclaration));
            }

            IMethodDeclaration matchingMehtod =
                methodDeclarations.FirstOrDefault(
                    m => MethodsAreEqual(m, methodDeclaration));

            if (null == matchingMehtod)
            {
                return;
            }

            ITreeNode originalNode = methodDeclaration.Children().FirstOrDefault(c => c is IDocCommentBlockNode);
            ITreeNode targetNode = matchingMehtod.Children().FirstOrDefault(c => c is IDocCommentBlockNode);

            if (null != originalNode && null != targetNode)
            {
                ModificationUtil.ReplaceChild(originalNode, targetNode);
            }
        }

        bool MethodsAreEqual(IMethodDeclaration declaration1, IMethodDeclaration declaration2)
        {
            return declaration1.DeclaredElement.ToString() == declaration2.DeclaredElement.ToString();
        }

        private IEnumerable<IPropertyDeclaration> GetProperties(IInterfaceDeclaration interfaceDeclaration)
        {
            if (null == interfaceDeclaration)
            {
                return new IPropertyDeclaration[0];
            }

            return interfaceDeclaration.PropertyDeclarations;
        }
        private IEnumerable<IMethodDeclaration> GetMethods(IInterfaceDeclaration interfaceDeclaration)
        {
            if (null == interfaceDeclaration)
            {
                return new IMethodDeclaration[0];
            }

            return interfaceDeclaration.MethodDeclarations;
        }

        private IEnumerable<IPropertyDeclaration> GetProperties(IClassDeclaration classDeclaration)
        {
            if (null == classDeclaration)
            {
                return new IPropertyDeclaration[0];
            }

            return classDeclaration.PropertyDeclarations.Where(IsVirtualOrAbstractMethod);
        }

        private IEnumerable<IMethodDeclaration> GetMethods(IClassDeclaration classDeclaration)
        {
            if (null == classDeclaration)
            {
                return new IMethodDeclaration[0];
            }

            return classDeclaration.MethodDeclarations.Where(IsVirtualOrAbstractMethod);
        }

        private bool IsVirtualOrAbstractMethod(IPropertyDeclaration prpertyDeclaration)
        {
            return prpertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.VIRTUAL_KEYWORD) ||
                   prpertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.ABSTRACT_KEYWORD);
        }
        private bool IsVirtualOrAbstractMethod(IMethodDeclaration methodDeclaration)
        {
            return methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.VIRTUAL_KEYWORD) ||
                   methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.ABSTRACT_KEYWORD);
        }
    }

    class TreeNodeInheritance
    {
        public ITreeNode BaseTreeNode { get; set; }
        public ITreeNode ChildTreeNode { get; set; }
    }
}