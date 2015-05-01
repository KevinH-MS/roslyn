using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class Dashboard : UserControl, IDisposable
    {
        private readonly DashboardViewModel model;
        private readonly IWpfTextView textView;
        private readonly IAdornmentLayer findAdornmentLayer;
        private PresentationSource presentationSource;
        private DependencyObject rootDependencyObject;
        private IInputElement rootInputElement;
        private UIElement focusedElement = null;
        private readonly List<UIElement> tabNavigableChildren;
        internal bool ShouldReceiveKeyboardNavigation { get; set; }

        private IEnumerable<string> renameAccessKeys = new[]
            {
                RenameShortcutKey.RenameOverloads,
                RenameShortcutKey.SearchInComments,
                RenameShortcutKey.SearchInStrings,
                RenameShortcutKey.Apply,
                RenameShortcutKey.PreviewChanges
            };

        public Dashboard(
            DashboardViewModel model,
            IWpfTextView textView)
        {
            this.model = model;
            InitializeComponent();

            tabNavigableChildren = new UIElement[] { this.OverloadsCheckbox, this.CommentsCheckbox, this.StringsCheckbox, this.PreviewChangesCheckbox, this.ApplyButton, this.CloseButton }.ToList();

            this.textView = textView;
            this.DataContext = model;

            this.Visibility = textView.HasAggregateFocus ? Visibility.Visible : Visibility.Collapsed;

            this.textView.GotAggregateFocus += OnTextViewGotAggregateFocus;
            this.textView.LostAggregateFocus += OnTextViewLostAggregateFocus;
            this.textView.VisualElement.SizeChanged += OnElementSizeChanged;
            this.SizeChanged += OnElementSizeChanged;

            PresentationSource.AddSourceChangedHandler(this, OnPresentationSourceChanged);

            try
            {
                this.findAdornmentLayer = textView.GetAdornmentLayer("FindUIAdornmentLayer");
                ((UIElement)findAdornmentLayer).LayoutUpdated += FindAdornmentCanvas_LayoutUpdated;
            }
            catch (ArgumentOutOfRangeException)
            {
                // Find UI doesn't exist in ETA.
            }

            this.Focus();
            textView.Caret.IsHidden = false;
            ShouldReceiveKeyboardNavigation = true;
        }

        private void ShowCaret()
        {
            // We actually want the caret visible even though the view isn't explicitly focused.
            ((UIElement)textView.Caret).Visibility = Visibility.Visible;
        }

        private void FocusElement(UIElement firstElement, Func<int, int> selector)
        {
            if (focusedElement == null)
            {
                focusedElement = firstElement;
            }
            else
            {
                var current = tabNavigableChildren.IndexOf(focusedElement);
                current = selector(current);
                focusedElement = tabNavigableChildren.ElementAt(current);
            }
            
            focusedElement.Focus();
            ShowCaret();
        }

        internal void FocusNextElement()
        {
            FocusElement(tabNavigableChildren.First(), i => i == tabNavigableChildren.Count - 1 ? 0 : i + 1);
        }

        internal void FocusPreviousElement()
        {
            FocusElement(tabNavigableChildren.Last(), i => i == 0 ? tabNavigableChildren.Count - 1 : i - 1);
        }

        private void OnPresentationSourceChanged(object sender, SourceChangedEventArgs args)
        {
            if (args.NewSource == null)
            {
                this.DisonnectFromPresentationSource();
            }
            else
            {
                this.ConnectToPresentationSource(args.NewSource);
            }
        }

        private void ConnectToPresentationSource(PresentationSource presentationSource)
        {
            if (presentationSource == null)
            {
                throw new ArgumentNullException("presentationSource");
            }

            this.presentationSource = presentationSource;

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                rootDependencyObject = Application.Current.MainWindow as DependencyObject;
            }
            else
            {
                rootDependencyObject = this.presentationSource.RootVisual as DependencyObject;
            }

            rootInputElement = rootDependencyObject as IInputElement;

            if (rootDependencyObject != null && rootInputElement != null)
            {
                foreach (string accessKey in renameAccessKeys)
                {
                    AccessKeyManager.Register(accessKey, rootInputElement);
                }

                AccessKeyManager.AddAccessKeyPressedHandler(rootDependencyObject, OnAccessKeyPressed);
            }
        }

        private void OnAccessKeyPressed(object sender, AccessKeyPressedEventArgs args)
        {
            foreach (string accessKey in renameAccessKeys)
            {
                if (string.Compare(accessKey, args.Key, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    args.Target = this;
                    args.Handled = true;
                    return;
                }
            }
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            if (e != null)
            {
                if (string.Compare(e.Key, RenameShortcutKey.RenameOverloads, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.OverloadsCheckbox.IsChecked = !this.OverloadsCheckbox.IsChecked;
                }
                else if (string.Compare(e.Key, RenameShortcutKey.SearchInComments, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.CommentsCheckbox.IsChecked = !this.CommentsCheckbox.IsChecked;
                }
                else if (string.Compare(e.Key, RenameShortcutKey.SearchInStrings, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    this.StringsCheckbox.IsChecked = !this.StringsCheckbox.IsChecked;
                }
                else if (string.Compare(e.Key, RenameShortcutKey.PreviewChanges) == 0)
                {
                    this.PreviewChangesCheckbox.IsChecked = !this.PreviewChangesCheckbox.IsChecked;
                }
                else if (string.Compare(e.Key, RenameShortcutKey.Apply) == 0)
                {
                    this.Commit();
                }
            }
        }

        private void DisonnectFromPresentationSource()
        {
            if (rootInputElement != null)
            {
                foreach (string registeredKey in renameAccessKeys)
                {
                    AccessKeyManager.Unregister(registeredKey, rootInputElement);
                }

                AccessKeyManager.RemoveAccessKeyPressedHandler(rootDependencyObject, OnAccessKeyPressed);
            }

            presentationSource = null;
            rootDependencyObject = null;
            rootInputElement = null;
        }

        private void FindAdornmentCanvas_LayoutUpdated(object sender, EventArgs e)
        {
            PositionDashboard();
        }

        public string RenameOverloads { get { return EditorFeaturesResources.RenameOverloads; } }
        public Visibility RenameOverloadsVisibility { get { return this.model.RenameOverloadsVisibility; } }
        public string SearchInComments { get { return EditorFeaturesResources.SearchInComments; } }
        public string SearchInStrings { get { return EditorFeaturesResources.SearchInStrings; } }
        public string ApplyRename { get { return EditorFeaturesResources.ApplyRename; } }
        public string PreviewChanges { get { return EditorFeaturesResources.RenamePreviewChanges; } }
        public string ApplyToolTip { get { return EditorFeaturesResources.RenameApplyToolTip + " (Enter)"; } }
        public string CancelToolTip { get { return EditorFeaturesResources.RenameCancelToolTip + " (Esc)"; } }

        private void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                PositionDashboard();
            }
        }

        private void PositionDashboard()
        {
            const int Padding = 10;
            double top = 0;
            if (findAdornmentLayer != null && findAdornmentLayer.Elements.Count != 0)
            {
                var adornment = findAdornmentLayer.Elements[0].Adornment;
                top += adornment.RenderSize.Height;
            }

            Canvas.SetTop(this, top + Padding);
            Canvas.SetLeft(this, textView.ViewportLeft + textView.VisualElement.RenderSize.Width - this.RenderSize.Width - Padding);
        }

        private void OnTextViewGotAggregateFocus(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Visible;
            PositionDashboard();
        }

        private void OnTextViewLostAggregateFocus(object sender, EventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.model.Session.Cancel();
            this.textView.VisualElement.Focus();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Commit();
        }

        private void Commit()
        {
            this.model.Session.Commit();
            this.textView.VisualElement.Focus();
        }

        public void Dispose()
        {
            this.textView.GotAggregateFocus -= OnTextViewGotAggregateFocus;
            this.textView.LostAggregateFocus -= OnTextViewLostAggregateFocus;
            this.textView.VisualElement.SizeChanged -= OnElementSizeChanged;
            this.SizeChanged -= OnElementSizeChanged;

            if (findAdornmentLayer != null)
            {
                ((UIElement)findAdornmentLayer).LayoutUpdated -= FindAdornmentCanvas_LayoutUpdated;
            }

            this.model.Dispose();
            PresentationSource.RemoveSourceChangedHandler(this, OnPresentationSourceChanged);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = false;
            e.Handled = true;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = true;
            e.Handled = true;
        }

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = true;
            e.Handled = true;
        }

        protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            ShouldReceiveKeyboardNavigation = false;
            e.Handled = true;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            // Don't send clicks into the text editor below.
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        protected override void OnIsKeyboardFocusWithinChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnIsKeyboardFocusWithinChanged(e);

            ShouldReceiveKeyboardNavigation = (bool)e.NewValue;
        }
    }
}
