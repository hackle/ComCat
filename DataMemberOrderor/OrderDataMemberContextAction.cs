using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using JetBrains.Application.Progress;
using JetBrains.DataFlow;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Intentions.VB.ContextActions.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Caches2;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DataMemberOrderor
{
    using System.Windows.Forms;

    [ContextAction(Name = "OrderDataMember", Group = "C#", Description = "Order DataMember Elements")]
    public class OrderDataMemberContextAction : ContextActionBase
    {
        private ICSharpContextActionDataProvider _actionDataProvider;
        private IClassDeclaration _class;

        public OrderDataMemberContextAction(ICSharpContextActionDataProvider actionDataProvider)
        {
            _actionDataProvider = actionDataProvider;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (null == _class) return null;

            var properties = _class.PropertyDeclarations.Where(HasDataMember).ToArray();

            if (!properties.Any()) return null;

            var reordered = ReorderNodes(properties);

            var anchor = properties[0];

            for (var i = properties.Length - 1; i >= 0; i--)
            {
                SetOrder(reordered[i], i);
                ModificationUtil.AddChildAfter(anchor, reordered[i]);
            }

            //remove old nodes
            foreach (var node in properties)
            {
                ModificationUtil.DeleteChild(node);
            }

            return null;
        }

        private static IPropertyDeclaration[] ReorderNodes(IPropertyDeclaration[] properties)
        {
            var orderer = new DialogReorder(properties);
            orderer.ShowDialog();

            return orderer.PropertiesInOrder;
        }

        private void SetOrder(IPropertyDeclaration propertyDeclaration, int i)
        {
            var factory = CSharpElementFactory.GetInstance(this._actionDataProvider.PsiModule);

            var attribute = propertyDeclaration.Attributes.SingleOrDefault(IsDataMemberAttribute);

            if (null == attribute) return;

            var anchor = attribute.Children().FirstOrDefault(n => n.NodeType == CSharpTokenType.LPARENTH);
            
            if (null != anchor)
            {
                var orderPara = attribute.PropertyAssignments.SingleOrDefault(a => a.Reference.GetName() == "Order");
            
                if (null != orderPara)
                {
                    ModificationUtil.DeleteChild(orderPara);
                }

                var orderNew = factory.CreateObjectCreationExpressionMemberInitializer("Order", factory.CreateExpression("0"));

                ModificationUtil.AddChildAfter(anchor, orderNew);
            }
        }

        public override string Text
        {
            get { return "Reorder DataMember in this class"; }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            //reset
            _class = null;

            var element = _actionDataProvider.SelectedElement;

            if (null == element) return false;

            _class = element.Parent as IClassDeclaration;

            if (null == _class) return false;

            return HasDataMembers(_class);
        }

        private bool HasDataMembers(IClassDeclaration classDeclaration)
        {
            return classDeclaration.PropertyDeclarations.Any(HasDataMember);
        }

        private bool HasDataMember(IPropertyDeclaration propertyDeclaration)
        {
            return propertyDeclaration.Attributes.Any(IsDataMemberAttribute);
        }

        private static bool IsDataMemberAttribute(IAttribute a)
        {
            return null != a.Reference && a.Name.QualifiedName == "DataMember";
        }
    }
}
