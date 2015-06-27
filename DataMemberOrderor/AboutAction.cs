using System.Windows.Forms;
using JetBrains.ActionManagement;
using JetBrains.Application.DataContext;

namespace DataMemberOrderor
{
    [ActionHandler("DataMemberOrderor.About")]
    public class AboutAction : IActionHandler
    {
        public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
        {
            // return true or false to enable/disable this action
            return true;
        }

        public void Execute(IDataContext context, DelegateExecute nextExecute)
        {
            MessageBox.Show(
              "DataMember Orderer\nHackle Wayne\n\nOrder DataMember",
              "About DataMember Orderer",
              MessageBoxButtons.OK,
              MessageBoxIcon.Information);
        }
    }
}