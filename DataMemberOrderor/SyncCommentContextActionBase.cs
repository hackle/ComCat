using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
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
    public class SyncCommentContextActionBase : ContextActionBase
    {
        private readonly string _contextMenuKeyWord;
        public SyncDirection Direction = SyncDirection.FromBaseTypes;
        protected readonly ICSharpContextActionDataProvider Provider;

        private ITreeNode _currentNode;

        public SyncCommentContextActionBase(ICSharpContextActionDataProvider provider, string contextMenuKeyWord)
        {
            Provider = provider;
            _contextMenuKeyWord = contextMenuKeyWord;
        }

        public override string Text
        {
            get { return _contextMenuKeyWord; }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            using (ReadLockCookie.Create())
            {
                _currentNode = Provider.SelectedElement;

                bool isOnMethodOrProperty = NodeIsDocComment(_currentNode) || NodeIsMethod(_currentNode) ||
                                            NodeIsProperty(_currentNode);
                bool methodOrPropertyIsInheriting = NodeIsImplementingMethod(_currentNode) ||
                                                    NodeIsImplementingProperty(_currentNode);

                return (isOnMethodOrProperty && methodOrPropertyIsInheriting) || NodeIsImplementingClass(_currentNode);
            }
        }

        private bool NodeIsImplementingProperty(ITreeNode currentNode)
        {
            var ancestor = currentNode.GetAncestor<IPropertyDeclaration>();
            if (null == ancestor) return false;

            return PropertyHasInheritance(ancestor);
        }

        private bool NodeIsImplementingMethod(ITreeNode currentNode)
        {
            var methodDeclaration = currentNode.GetAncestor<IMethodDeclaration>();
            if (null == methodDeclaration) return false;

            return MethodHasInheritance(methodDeclaration);
        }

        private bool PropertyHasInheritance(IPropertyDeclaration propertyDeclaration)
        {
            var classDeclaration = propertyDeclaration.GetAncestor<IClassDeclaration>();
            if (null == classDeclaration) return false;

            IEnumerable<IClassLikeDeclaration> superTypes = GetSupertypesRecursive(classDeclaration);

            IEnumerable<TreeNodeInheritance> inheritances = GetPropertyInheritance(classDeclaration, superTypes);

            return inheritances.Any(i => PropertiesAreEqual(i.BaseTreeNode as IPropertyDeclaration, propertyDeclaration));
        }

        private bool MethodHasInheritance(IMethodDeclaration methodDeclaration)
        {
            var classDeclaration = methodDeclaration.GetAncestor<IClassDeclaration>();
            if (null == classDeclaration) return false;

            IEnumerable<IClassLikeDeclaration> superTypes = GetSupertypesRecursive(classDeclaration);

            IEnumerable<TreeNodeInheritance> inheritances = GetMethodInheritance(classDeclaration, superTypes);

            return inheritances.Any(i => MethodsAreEqual(i.BaseTreeNode as IMethodDeclaration, methodDeclaration));
        }

        private bool NodeIsProperty(ITreeNode currentNode)
        {
            return currentNode is IIdentifier && currentNode.Parent is IPropertyDeclaration;
        }

        private bool NodeIsMethod(ITreeNode currentNode)
        {
            return currentNode is IIdentifier && currentNode.Parent is IMethodDeclaration;
        }

        private bool NodeIsImplementingClass(ITreeNode currentNode)
        {
            return NodeIsClass(currentNode) &&
                   ClassHasInheritance(GetParentClass(currentNode));
        }

        private bool NodeIsClass(ITreeNode currentNode)
        {
            return currentNode is IIdentifier && currentNode.Parent is IClassDeclaration;
        }

        private bool ClassHasInheritance(IClassDeclaration classDeclaration)
        {
            IEnumerable<IClassLikeDeclaration> superTypes = GetSupertypesRecursive(classDeclaration);

            return GetMethodInheritance(classDeclaration, superTypes).Any() ||
                   GetPropertyInheritance(classDeclaration, superTypes).Any();
        }

        private IEnumerable<TreeNodeInheritance> GetPropertyInheritance(IClassDeclaration classDeclaration,
            IEnumerable<IClassLikeDeclaration> superTypes)
        {
            IPropertyDeclaration[] childProperties =
                classDeclaration.PropertyDeclarations.Where(IsPublicOrProtectedMethod).ToArray();

            if (!childProperties.Any()) return new TreeNodeInheritance[0];

            IEnumerable<IPropertyDeclaration> baseProperties = superTypes.SelectMany(GetTypeProperties);

            return from baseProperty in baseProperties
                join childProperty in childProperties
                    on baseProperty.DeclaredName equals childProperty.DeclaredName
                select new TreeNodeInheritance
                {
                    BaseTreeNode = baseProperty,
                    ChildTreeNode = childProperty
                };
        }

        private IEnumerable<TreeNodeInheritance> GetMethodInheritance(IClassDeclaration classDeclaration,
            IEnumerable<IClassLikeDeclaration> superTypes)
        {
            IMethodDeclaration[] publicMethods =
                classDeclaration.MethodDeclarations.Where(IsPublicOrProtectedMethod).ToArray();

            if (!publicMethods.Any()) return new TreeNodeInheritance[0];

            IEnumerable<IMethodDeclaration> baseMethods = superTypes.SelectMany(GetTypeMethods);

            return from baseMethod in baseMethods
                join localMethod in publicMethods
                    on baseMethod.DeclaredElement.ToString() equals localMethod.DeclaredElement.ToString()
                select new TreeNodeInheritance
                {
                    BaseTreeNode = baseMethod,
                    ChildTreeNode = localMethod
                };
        }

        private bool IsPublicOrProtectedMethod(IPropertyDeclaration propertyDeclaration)
        {
            return null != propertyDeclaration.ModifiersList && (
                propertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                   propertyDeclaration.ModifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD));
        }

        private bool IsPublicOrProtectedMethod(IMethodDeclaration methodDeclaration)
        {
            return null != methodDeclaration.ModifiersList && (
                methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.PUBLIC_KEYWORD) ||
                methodDeclaration.ModifiersList.HasModifier(CSharpTokenType.PROTECTED_KEYWORD));
        }

        private IEnumerable<IClassLikeDeclaration> GetSupertypesRecursive(IClassLikeDeclaration classDeclaration)
        {
            var superTypes = new List<IClassLikeDeclaration>();

            IClassLikeDeclaration[] classLikeDeclarations =
                classDeclaration.SuperTypes.SelectMany(
                    t => t.GetTypeElement().GetDeclarations().Select(d => d as IClassLikeDeclaration)).ToArray();
            superTypes.AddRange(classLikeDeclarations);

            if (classLikeDeclarations.Any())
            {
                IClassLikeDeclaration[] extraSuperTypes = superTypes.SelectMany(GetSupertypesRecursive).ToArray();
                superTypes.AddRange(extraSuperTypes);
            }

            return superTypes;
        }

        private IEnumerable<IPropertyDeclaration> GetTypeProperties(IClassLikeDeclaration superType)
        {
            if (superType is IInterfaceDeclaration)
                return GetProperties(superType as IInterfaceDeclaration);

            return GetProperties(superType as IClassDeclaration);
        }

        private IEnumerable<IMethodDeclaration> GetTypeMethods(IClassLikeDeclaration superType)
        {
            if (superType is IInterfaceDeclaration)
                return GetMethods(superType as IInterfaceDeclaration);

            return GetMethods(superType as IClassDeclaration);
        }

        private IClassDeclaration GetParentClass(ITreeNode treeNode)
        {
            return treeNode.GetAncestor<IClassDeclaration>();
        }

        private bool NodeIsDocComment(ITreeNode currentNode)
        {
            var commentNode = currentNode as IDocCommentNode;
            if (null == commentNode) return false;

            return IsMethodOrPropertyDocComment(commentNode);
        }

        private static bool IsMethodOrPropertyDocComment(ITreeNode commentNode)
        {
            return commentNode.Parent is IDocCommentBlockNode &&
                   (commentNode.Parent.Parent is IMethodDeclaration || commentNode.Parent.Parent is IPropertyDeclaration);
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (null == _currentNode)
            {
                return null;
            }

            ITreeNode targetNode = null;
            if (NodeIsDocComment(_currentNode) && ParentIsPropertyOrMethod(_currentNode))
            {
                targetNode = _currentNode.Parent;
            }

            SyncComments(targetNode, Direction);

            return null;
        }

        private bool ParentIsPropertyOrMethod(ITreeNode node)
        {
            return node.Parent is IMethodDeclaration ||
                   node.Parent is IPropertyDeclaration;
        }

        private void SyncComments(ITreeNode targetNode, SyncDirection direction)
        {
            IClassDeclaration classDeclaration = GetParentClass(_currentNode);
            IEnumerable<IClassLikeDeclaration> superTypes = GetSupertypesRecursive(classDeclaration);

            IEnumerable<TreeNodeInheritance> inheritances =
                GetMethodInheritance(classDeclaration, superTypes)
                    .Concat(GetPropertyInheritance(classDeclaration, superTypes));

            if (null != targetNode)
            {
                inheritances = inheritances.Where(i => NodesAreEqual(i.ChildTreeNode, targetNode));
            }

            foreach (TreeNodeInheritance methodPair in inheritances)
            {
                if (SyncDirection.FromBaseTypes == direction)
                {
                    ReplaceOrInsertComments(methodPair.BaseTreeNode, methodPair.ChildTreeNode);
                }
                else
                {
                    ReplaceOrInsertComments(methodPair.ChildTreeNode, methodPair.BaseTreeNode);
                }
            }
        }

        private bool NodesAreEqual(ITreeNode childTreeNode, ITreeNode targetNode)
        {
            if (targetNode is IPropertyDeclaration && childTreeNode is IPropertyDeclaration)
                return PropertiesAreEqual(targetNode as IPropertyDeclaration, childTreeNode as IPropertyDeclaration);

            if (targetNode is IMethodDeclaration && childTreeNode is IMethodDeclaration)
                return MethodsAreEqual(targetNode as IMethodDeclaration, childTreeNode as IMethodDeclaration);

            return false;
        }

        private void ReplaceOrInsertComments(ITreeNode newNode, ITreeNode oldNode)
        {
            IDocCommentBlockNode newComments = GetNodeComments(newNode);

            if (null == newComments) return;

            IDocCommentBlockNode replacement = newComments.Copy();

            IDocCommentBlockNode oldComments = GetNodeComments(oldNode);

            if (null == oldComments)
            {
                InsertComments(oldNode, replacement);
            }
            else
            {
                ModificationUtil.ReplaceChild(oldComments, replacement);
            }
        }

        private void InsertComments(ITreeNode method, IDocCommentBlockNode comments)
        {
            ModificationUtil.AddChildBefore(method.FirstChild, comments);
        }

        private IDocCommentBlockNode GetNodeComments(ITreeNode baseMethod)
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
                methodDeclarations.AddRange(GetMethods(dec as IInterfaceDeclaration));
                methodDeclarations.AddRange(GetMethods(dec as IClassDeclaration));
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

        private bool MethodsAreEqual(IMethodDeclaration declaration1, IMethodDeclaration declaration2)
        {
            return declaration1.DeclaredElement.ToString() == declaration2.DeclaredElement.ToString();
        }

        private bool PropertiesAreEqual(IPropertyDeclaration declaration1, IPropertyDeclaration declaration2)
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

        public enum SyncDirection { FromBaseTypes, ToBaseTypes }
    }
}