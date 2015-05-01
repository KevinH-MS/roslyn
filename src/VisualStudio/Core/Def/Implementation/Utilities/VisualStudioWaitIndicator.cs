using System;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    [Export(typeof(IWaitIndicator))]
    internal sealed class VisualStudioWaitIndicator : IWaitIndicator
    {
        private readonly SVsServiceProvider serviceProvider;

        private static readonly Func<string, string, string> messageGetter = (t, m) => string.Format("{0} : {1}", t, m);
        private readonly VisualStudioWorkspace visualStudioWorkspace;

        [ImportingConstructor]
        public VisualStudioWaitIndicator(SVsServiceProvider serviceProvider, VisualStudioWorkspace visualStudioWorkspace)
        {
            this.serviceProvider = serviceProvider;
            this.visualStudioWorkspace = visualStudioWorkspace;
        }

        public WaitIndicatorResult Wait(string title, string message, bool allowCancel, Action<IWaitContext> action)
        {
            using (Logger.LogBlock(FunctionId.Misc_VisualStudioWaitIndicator_Wait, messageGetter, title, message, CancellationToken.None))
            using (var waitContext = StartWait(title, message, allowCancel))
            {
                try
                {
                    action(waitContext);

                    return WaitIndicatorResult.Completed;
                }
                catch (OperationCanceledException)
                {
                    return WaitIndicatorResult.Canceled;
                }
                catch (AggregateException e)
                {
                    var operationCanceledException = e.InnerExceptions[0] as OperationCanceledException;
                    if (operationCanceledException != null)
                    {
                        return WaitIndicatorResult.Canceled;
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        private VisualStudioWaitContext StartWait(string title, string message, bool allowCancel)
        {
            var componentModel = (IComponentModel)serviceProvider.GetService(typeof(SComponentModel));
            Contract.ThrowIfNull(visualStudioWorkspace);

            var notificationService = visualStudioWorkspace.Services.GetService<IGlobalOperationNotificationService>();
            Contract.ThrowIfNull(notificationService);

            var dialogFactory = (IVsThreadedWaitDialogFactory)this.serviceProvider.GetService(typeof(SVsThreadedWaitDialogFactory));
            Contract.ThrowIfNull(dialogFactory);

            return new VisualStudioWaitContext(notificationService, dialogFactory, title, message, allowCancel);
        }

        IWaitContext IWaitIndicator.StartWait(string title, string message, bool allowCancel)
        {
            return StartWait(title, message, allowCancel);
        }
    }
}