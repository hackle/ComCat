using JetBrains.ReSharper.Psi.Tree;

namespace DataMemberOrderor
{
    internal static class ITreeNodeExtensions
    {
        public static T GetAncestor<T>(this ITreeNode node) where T : class, ITreeNode
        {
            ITreeNode parent = node.Parent;

            while (null != parent)
            {
                if (parent is T) return parent as T;

                parent = parent.Parent;
            }

            return null;
        }
    }
}