using System;

namespace DataMemberOrderor
{
    using JetBrains.Application.Progress;
    using JetBrains.ProjectModel;
    using JetBrains.ReSharper.Feature.Services.Bulbs;
    using JetBrains.ReSharper.Feature.Services.CSharp.Bulbs;
    using JetBrains.ReSharper.Intentions.Extensibility;
    using JetBrains.TextControl;
    using JetBrains.Util;

    [ContextAction(Name = "CommentSync", Group = "C#", Description = "Sync comments from interface")]
    public class CommentSyncContextAction : ContextActionBase
    {
        private ICSharpContextActionDataProvider _actionDataProvider;

        public CommentSyncContextAction(ICSharpContextActionDataProvider actionDataProvider)
        {
            this._actionDataProvider = actionDataProvider;
        }

        /// <summary>
        /// Executes QuickFix or ContextAction. Returns post-execute method.
        /// </summary>
        /// <returns>
        /// Action to execute after document and PSI transaction finish. Use to open TextControls, navigate caret, etc.
        /// </returns>
        protected override Action<ITextControl> ExecutePsiTransaction(ISolution solution, IProgressIndicator progress)
        {
            return null;
        }

        /// <summary>
        /// Popup menu item text
        /// </summary>
        public override string Text
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Check if this action is available at the constructed context.
        ///             Actions could store precalculated info in <paramref name="cache"/> to share it between different actions
        /// </summary>
        /// <returns>
        /// true if this bulb action is available, false otherwise.
        /// </returns>
        public override bool IsAvailable(IUserDataHolder cache)
        {
            return false;
        }
    }
}
