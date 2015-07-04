using JetBrains.ReSharper.Feature.Services.Bulbs;
using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;

namespace DataMemberOrderor
{
    [ContextAction(Name = "Push comments to base types", Group = "C#", Description = "Push comments to base types", Priority = -20)]
    public class PushCommentToBaseContextAction : SyncCommentContextActionBase
    {
        public PushCommentToBaseContextAction(ICSharpContextActionDataProvider provider)
            : base(provider, "Push comments to base types")
        {
            this.Direction = SyncDirection.ToBaseTypes;
        }
    }
}