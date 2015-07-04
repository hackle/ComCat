using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Application;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DataMemberOrderor
{
    [ContextAction(Name = "Use base type comment", Group = "C#", Description = "Use comment from base type.", Priority = -20)]
    public class SyncCommentActionBase : ContextActionBase
    {
        protected readonly ICSharpContextActionDataProvider Provider;

        private const string UsedKeyWord = "base type comment";
        
        private IExpression _selectedExpression;

        private IDocCommentNode _oldCommentNode;

        public SyncCommentActionBase(ICSharpContextActionDataProvider provider)
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
                this._oldCommentNode = this.Provider.TokenBeforeCaret as IDocCommentNode;
                return null != this._oldCommentNode;
            }
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (null == this._oldCommentNode)
            {
                return null;
            }

            ITreeNode currentNode = this._oldCommentNode.Parent as IDocCommentBlockNode;

            var methodDeclaration = currentNode.Parent as IMethodDeclaration;
            if (null == methodDeclaration)
            {
                return null;
            }

            this.TryUseBaseTypeComments(methodDeclaration);

            return null;
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
                    m => m.DeclaredElement.ToString() == methodDeclaration.DeclaredElement.ToString());

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

        private IEnumerable<IMethodDeclaration> GetMethods(IInterfaceDeclaration interfaceDeclaration)
        {
            if (null == interfaceDeclaration)
            {
                return new IMethodDeclaration[0];
            }

            return interfaceDeclaration.MethodDeclarations;
        }

        private IEnumerable<IMethodDeclaration> GetMethods(IClassDeclaration classDeclaration)
        {
            if (null == classDeclaration)
            {
                return new IMethodDeclaration[0];
            }

            return classDeclaration.MethodDeclarations;
        }
    }
}