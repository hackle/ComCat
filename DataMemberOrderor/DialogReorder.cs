using System.Collections.Generic;
using System.Windows.Forms;

namespace DataMemberOrderor
{
    using System.Linq;

    using JetBrains.ReSharper.Psi.CSharp.Tree;

    public partial class DialogReorder : Form
    {
        private readonly IAttribute[] attributes;
        private List<AttributeInOrder> listOfOrder;

        IAttribute[] AttributesInOrder
        {
            get
            {
                return listOfOrder.Select(o => o.Attribute).ToArray();
            }
        }

        public DialogReorder(IAttribute[] attributes)
        {
            this.attributes = attributes;

            InitializeComponent();

            listOfOrder = new List<AttributeInOrder>();
            for (var i = 0; i < attributes.Length; i++)
            {
                listOfOrder.Add(new AttributeInOrder(attributes[i]) { Order = i });
            }

            this.dataGridViewOrders.DataSource = listOfOrder;
        }

        private void MoveUp()
        {
            var selectedAttribute = this.GetSelectedAttributeInOrder();

            var index = selectedAttribute.Order;
            if (index == 0) return;

            this.MoveOrderUp(index);
        }

        private void MoveDown()
        {
            var selectedAttribute = this.GetSelectedAttributeInOrder();

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

        private AttributeInOrder GetSelectedAttributeInOrder()
        {
            if (dataGridViewOrders.SelectedRows.Count < 1) return null;

            return dataGridViewOrders.SelectedRows[0].DataBoundItem as AttributeInOrder;
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

    public class AttributeInOrder
    {
        public IAttribute Attribute { get; set; }

        public AttributeInOrder(IAttribute attribute)
        {
            this.Attribute = attribute;
        }

        public int Order { get; set; }

        public string Name
        {
            get
            {
                return this.Attribute.Name.QualifiedName;
            }
        }
    }
}
