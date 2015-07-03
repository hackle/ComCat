using System.Collections;
using System.Collections.Generic;
using System.Linq;

using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.ExpectedTypes;
using JetBrains.ReSharper.Psi.CSharp.Impl;
using JetBrains.ReSharper.Psi.CSharp.Impl.Completion;
using JetBrains.ReSharper.Psi.CSharp.Impl.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Impl.Tree;
using JetBrains.ReSharper.Psi.CSharp.Parsing;
using JetBrains.ReSharper.Psi.CSharp.Resolve;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Filters;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve.Managed;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Tree;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Resolve.Managed;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace DataMemberOrderor
{
    using global::JetBrains.Annotations;
    using global::JetBrains.Application;
    using global::JetBrains.ReSharper.Psi.Dependencies;
    using global::JetBrains.Util.Logging;

    public static class IAttributeExtensions
    {
        private static readonly NodeTypeDictionary<short> CHILD_ROLES = new NodeTypeDictionary<short>((IList<KeyValuePair<NodeType, short>>) new KeyValuePair<NodeType, short>[6]
        {
          new KeyValuePair<NodeType, short>((NodeType) ElementType.REFERENCE_NAME, (short) 25),
          new KeyValuePair<NodeType, short>((NodeType) TokenType.LPARENTH, (short) 6),
          new KeyValuePair<NodeType, short>((NodeType) ElementType.C_SHARP_ARGUMENT, (short) 30),
          new KeyValuePair<NodeType, short>((NodeType) TokenType.COMMA, (short) 12),
          new KeyValuePair<NodeType, short>((NodeType) ElementType.PROPERTY_ASSIGNMENT, (short) 106),
          new KeyValuePair<NodeType, short>((NodeType) TokenType.RPARENTH, (short) 7)
        });

        public static short GetChildRole(TreeElement child)
        {
            return CHILD_ROLES[child.NodeType];
        }


        public static IPropertyAssignment AddPropertyAssignmentBefore(this IAttribute attr, IPropertyAssignment param, IPropertyAssignment anchor)
        {
            using (WriteLockCookie.Create(attr.IsPhysical()))
            {
                if (attr.LPar == null)
                {
                    ModificationUtil.AddChild<LeafElementBase>((ITreeNode)attr, TreeElementFactory.CreateLeafElement(CSharpTokenType.LPARENTH));
                    ModificationUtil.AddChild<LeafElementBase>((ITreeNode)attr, TreeElementFactory.CreateLeafElement(CSharpTokenType.RPARENTH));
                }
                else if (attr.RPar == null)
                    ModificationUtil.AddChild<LeafElementBase>((ITreeNode)attr, TreeElementFactory.CreateLeafElement(CSharpTokenType.RPARENTH));
                Logger.Assert(attr.RPar != null, "The condition (RPar != null) is false.");
                if (anchor == null)
                {
                    if (attr.Arguments.Any() || attr.PropertyAssignments.Any())
                        return ModificationUtil.AddChildAfter<IPropertyAssignment>((ITreeNode)ModificationUtil.AddChildBefore<LeafElementBase>((ITreeNode)attr.RPar, TreeElementFactory.CreateLeafElement(CSharpTokenType.COMMA)), param);
                    return ModificationUtil.AddChildBefore<IPropertyAssignment>((ITreeNode)attr.RPar, param);
                }
                Logger.Assert(anchor.Parent == attr, "anchor.Parent == this");
                Logger.Assert((int)GetChildRole((TreeElement)anchor) == 106, "GetChildRole((TreeElement)anchor) == SPECIAL_ARGUMENT");
                return ModificationUtil.AddChildBefore<IPropertyAssignment>((ITreeNode)ModificationUtil.AddChildBefore<LeafElementBase>((ITreeNode)anchor, TreeElementFactory.CreateLeafElement(CSharpTokenType.COMMA)), param);
            }
        }

        public static IPropertyAssignment AddPropertyAssignmentAfter(this IAttribute attr, IPropertyAssignment param, IPropertyAssignment anchor)
        {
            using (WriteLockCookie.Create(attr.IsPhysical()))
            {
                if (attr.LPar == null)
                {
                    ModificationUtil.AddChild<LeafElementBase>((ITreeNode)attr, TreeElementFactory.CreateLeafElement(CSharpTokenType.LPARENTH));
                    ModificationUtil.AddChild<LeafElementBase>((ITreeNode)attr, TreeElementFactory.CreateLeafElement(CSharpTokenType.RPARENTH));
                }

                if (anchor == null)
                {
                    if (attr.Arguments.Any())
                        return ModificationUtil.AddChildAfter<IPropertyAssignment>((ITreeNode)ModificationUtil.AddChildAfter<LeafElementBase>((ITreeNode)attr.Arguments.Last(), TreeElementFactory.CreateLeafElement(CSharpTokenType.COMMA)), param);
                    if (attr.PropertyAssignments.Any())
                        return ModificationUtil.AddChildAfter<IPropertyAssignment>((ITreeNode)ModificationUtil.AddChildBefore<LeafElementBase>((ITreeNode)attr.PropertyAssignments.First(), TreeElementFactory.CreateLeafElement(CSharpTokenType.COMMA)), param);
                    return ModificationUtil.AddChildAfter<IPropertyAssignment>((ITreeNode)attr.LPar, param);
                }
                return ModificationUtil.AddChildAfter<IPropertyAssignment>((ITreeNode)ModificationUtil.AddChildAfter<LeafElementBase>((ITreeNode)anchor, TreeElementFactory.CreateLeafElement(CSharpTokenType.COMMA)), param);
            }
        }

        public static void RemovePropertyAssignment(this IAttribute attr, IPropertyAssignment param)
        {
            using (WriteLockCookie.Create(attr.IsPhysical()))
            {
                ITreeNode prevSibling = param.PrevSibling;
                while (prevSibling != null && (int)GetChildRole((TreeElement)prevSibling) != 12)
                    prevSibling = prevSibling.PrevSibling;
                if (prevSibling != null)
                {
                    ModificationUtil.DeleteChildRange(prevSibling, (ITreeNode)param);
                }
                else
                {
                    ITreeNode nextSibling = param.NextSibling;
                    while (nextSibling != null && (int)GetChildRole((TreeElement)nextSibling) != 12)
                        nextSibling = nextSibling.NextSibling;
                    if (nextSibling != null)
                        ModificationUtil.DeleteChildRange((ITreeNode)param, nextSibling);
                    else
                        ModificationUtil.DeleteChild((ITreeNode)param);
                }
            }
        }
    }
}
