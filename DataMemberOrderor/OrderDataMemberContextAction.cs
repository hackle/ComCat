using System;
using System.Linq;
using JetBrains.Application.Progress;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.Util;

namespace DataMemberOrderor
{
    using System.Windows.Forms;

    using global::JetBrains.ReSharper.Feature.Services.Bulbs;
    using global::JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
    using global::JetBrains.ReSharper.Intentions.Extensibility;
    using global::JetBrains.ReSharper.Psi;

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

            if (null == reordered) return null;

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
            return DialogResult.OK == orderer.ShowDialog() ? orderer.PropertiesInOrder : null;
        }

        private void SetOrder(IPropertyDeclaration propertyDeclaration, int i)
        {
            var factory = CSharpElementFactory.GetInstance(this._actionDataProvider.PsiModule);

            var attribute = propertyDeclaration.Attributes.SingleOrDefault(IsDataMemberAttribute);

            if (null == attribute) return;

            var orderProperty = attribute.PropertyAssignments.FirstOrDefault(p => p.PropertyNameIdentifier.Name == "Order");

            if (null != orderProperty) attribute.RemovePropertyAssignment(orderProperty);

            var replacement = factory.CreatePropertyAssignment(
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

    public static class CSharpElementFactoryExtensions
    {
        public static IPropertyAssignment CreatePropertyAssignment(this CSharpElementFactory factory, string name, ICSharpExpression arg)
        {
            return factory.CreateTypeMemberDeclaration("[A($0=$1)] class A {}", (object)name, (object)arg).Attributes[0].PropertyAssignments[0];
        }

    }
}
