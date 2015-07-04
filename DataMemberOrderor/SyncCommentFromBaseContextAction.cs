using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;

namespace DataMemberOrderor
{
    [ContextAction(Name = "Use base type comment", Group = "C#", Description = "Use comment from base type", Priority = -20)]
    public class SyncCommentFromBaseContextAction : SyncCommentContextActionBase
    {
        public SyncCommentFromBaseContextAction(ICSharpContextActionDataProvider provider) : base(provider, "Take comments from base types")
        {
        }
    }
}