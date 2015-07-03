using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace DataMemberOrderor
{
    namespace JetBrains.ReSharper.Psi.CSharp.Tree
    {
        public interface IPropertyAssignment : ICSharpTreeNode, ITreeNode
        {
            IReference Reference { get; }

            IAttribute ContainingAttribute { get; }

            ITokenNode Operator { get; }

            ICSharpIdentifier PropertyNameIdentifier { get; }

            ICSharpExpression Source { get; }

            void SetName(string name);

            ICSharpIdentifier SetPropertyNameIdentifier(ICSharpIdentifier param);

            ICSharpExpression SetSource(ICSharpExpression param);
        }
    }
}
