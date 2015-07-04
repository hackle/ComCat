using JetBrains.ReSharper.Psi.Tree;

namespace DataMemberOrderor
{
    internal class TreeNodeInheritance
    {
        public ITreeNode BaseTreeNode { get; set; }
        public ITreeNode ChildTreeNode { get; set; }
    }
}