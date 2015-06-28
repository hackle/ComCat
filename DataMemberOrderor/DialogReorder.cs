using System.Collections.Generic;
using System.Windows.Forms;

namespace DataMemberOrderor
{
    using System.Linq;

    using JetBrains.ReSharper.Psi.CSharp.Tree;

    public partial class DialogReorder : Form
    {
        private readonly IPropertyDeclaration[] attributes;
        private List<PropertyInOrder> listOfOrder;

        public IPropertyDeclaration[] PropertiesInOrder
        {
            get
            {
                return listOfOrder.Select(o => o.Attribute).ToArray();
            }
        }

        public DialogReorder(IPropertyDeclaration[] attributes)
        {
            this.attributes = attributes;

            InitializeComponent();

            listOfOrder = new List<PropertyInOrder>();
            for (var i = 0; i < attributes.Length; i++)
            {
                listOfOrder.Add(new PropertyInOrder(attributes[i]) { Order = i });
            }

            this.dataGridViewOrders.DataSource = listOfOrder;
        }

        private void MoveUp()
        {
            var selectedAttribute = this.GetSelectedPropertyInOrder();

            var index = selectedAttribute.Order;
            if (index == 0) return;

            this.MoveOrderUp(index);
        }

        private void MoveDown()
        {
            var selectedAttribute = this.GetSelectedPropertyInOrder();

            var index = selectedAttribute.Order;
            if (index == listOfOrder.Max(o => o.Order)) return;

            this.MoveOrderUp(index + 1);
        }

        private void MoveOrderUp(int index)
        {
            this.MoveBy(index, -1);
            this.MoveBy(index - 1, 1);

            this.ReBind();
        }

        private void ReBind()
        {
            listOfOrder = listOfOrder.OrderBy(o => o.Order).ToList();
            this.dataGridViewOrders.DataSource = listOfOrder;
        }

        private void MoveBy(int order, int offset)
        {
            this.listOfOrder.Single(o => o.Order == order).Order += offset;
        }

        private PropertyInOrder GetSelectedPropertyInOrder()
        {
            if (dataGridViewOrders.SelectedRows.Count < 1) return null;

            return dataGridViewOrders.SelectedRows[0].DataBoundItem as PropertyInOrder;
        }

        private void buttonMoveUp_Click(object sender, System.EventArgs e)
        {
            this.MoveUp();
        }

        private void buttonMoveDown_Click(object sender, System.EventArgs e)
        {
            this.MoveDown();
        }
    }

    public class PropertyInOrder
    {
        public IPropertyDeclaration Attribute { get; set; }

        public PropertyInOrder(IPropertyDeclaration attribute)
        {
            this.Attribute = attribute;
        }

        public int Order { get; set; }

        public string Name
        {
            get
            {
                return this.Attribute.DeclaredName;
            }
        }
    }
}
