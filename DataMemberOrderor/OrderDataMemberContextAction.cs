using System;
using System.Linq;
using System.Windows.Forms;

using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
using JetBrains.ReSharper.Intentions.Extensibility;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DataMemberOrderor
{
    [ContextAction(Name = "OrderDataMember", Group = "C#", Description = "Order DataMember Elements")]
    public class OrderDataMemberContextAction : ContextActionBase
    {
        private readonly ICSharpContextActionDataProvider _actionDataProvider;

        private IClassDeclaration _class;

        public OrderDataMemberContextAction(ICSharpContextActionDataProvider actionDataProvider)
        {
            this._actionDataProvider = actionDataProvider;
        }

        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            if (null == this._class)
            {
                return null;
            }

            IPropertyDeclaration[] properties = this._class.PropertyDeclarations.Where(this.HasDataMember).ToArray();

            if (!properties.Any())
            {
                return null;
            }

            IPropertyDeclaration[] reordered = ReorderNodes(properties);

            if (null == reordered)
            {
                return null;
            }

            IPropertyDeclaration anchor = properties[0];

            for (int i = properties.Length - 1; i >= 0; i--)
            {
                this.SetOrder(reordered[i], i);
                ModificationUtil.AddChildAfter(anchor, reordered[i]);
            }

            //remove old nodes
            foreach (IPropertyDeclaration node in properties)
            {
                ModificationUtil.DeleteChild(node);
            }

            return null;
        }

        private static IPropertyDeclaration[] ReorderNodes(IPropertyDeclaration[] properties)
        {
            var orderer = new DialogReorder(properties);
            return DialogResult.OK == orderer.ShowDialog() ? orderer.PropertiesInOrder : null;
        }

        private void SetOrder(IPropertyDeclaration propertyDeclaration, int i)
        {
            CSharpElementFactory factory = CSharpElementFactory.GetInstance(this._actionDataProvider.PsiModule);

            IAttribute attribute = propertyDeclaration.Attributes.SingleOrDefault(IsDataMemberAttribute);

            if (null == attribute)
            {
                return;
            }

            IPropertyAssignment orderProperty =
                attribute.PropertyAssignments.FirstOrDefault(p => p.PropertyNameIdentifier.Name == "Order");

            if (null != orderProperty)
            {
                attribute.RemovePropertyAssignment(orderProperty);
            }

            IPropertyAssignment replacement = factory.CreatePropertyAssignment(
                "Order",
                factory.CreateExpressionByConstantValue(
                    new ConstantValue(i, attribute.GetPsiModule(), attribute.GetResolveContext())));

            attribute.AddPropertyAssignmentAfter(replacement, null);

            //var anchor = attribute.Children().FirstOrDefault(n => n.NodeType == CSharpTokenType.LPARENTH);

            //if (null != anchor)
            //{
            //    var orderPara = attribute.PropertyAssignments.SingleOrDefault(a => a.Reference.GetName() == "Order");

            //    if (null != orderPara)
            //    {
            //        ModificationUtil.DeleteChild(orderPara);
            //    }

            //    var orderNew = factory.CreateExpression(String.Format("Order={0}", i)); //.CreateObjectCreationExpressionMemberInitializer("Order", factory.CreateExpression(""));

            //    ModificationUtil.AddChildAfter(anchor, orderNew);
            //}
        }

        public override string Text
        {
            get
            {
                return "Reorder DataMember in this class";
            }
        }

        public override bool IsAvailable(IUserDataHolder cache)
        {
            //reset
            this._class = null;

            ITreeNode element = this._actionDataProvider.SelectedElement;

            if (null == element)
            {
                return false;
            }

            this._class = element.Parent as IClassDeclaration;

            if (null == this._class)
            {
                return false;
            }

            return this.HasDataMembers(this._class);
        }

        private bool HasDataMembers(IClassDeclaration classDeclaration)
        {
            return classDeclaration.PropertyDeclarations.Any(this.HasDataMember);
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

    public static class CSharpElementFactoryExtensions
    {
        public static IPropertyAssignment CreatePropertyAssignment(
            this CSharpElementFactory factory,
            string name,
            ICSharpExpression arg)
        {
            return
                factory.CreateTypeMemberDeclaration("[A($0=$1)] class A {}", (object)name, (object)arg).Attributes[0]
                    .PropertyAssignments[0];
        }
    }
}