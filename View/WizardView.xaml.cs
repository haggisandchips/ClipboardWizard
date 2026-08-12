using ClipboardWizard.ViewModel;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace ClipboardWizard.View
{
    /// <summary>
    /// Interaction logic for WizardView.xaml
    /// </summary>
    public partial class WizardView
    {
        private readonly WizardViewModel _viewModel;

        public UniformGrid SnippetsUniformGrid { get; private set; }

        public WizardView(WizardViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            DataContext = _viewModel;
            RightWindowCommandsHost.DataContext = _viewModel;

            _viewModel.SnippetViewModels.CollectionChanged += Snippets_CollectionChanged;
        }

        private void Snippets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RecalculateColumns();
        }

        private void SnippetsUniformGrid_Loaded(object sender, RoutedEventArgs e)
        {
            SnippetsUniformGrid = sender as UniformGrid;
            SizeChanged += Window_SizedChangedEvent;

            RecalculateColumns();
        }

        private void Window_SizedChangedEvent(object sender, SizeChangedEventArgs e)
        {
            RecalculateColumns();
        }

        private void RecalculateColumns()
        {
            if (SnippetsUniformGrid == null)
            {
                return;
            }

            int columns = Math.Min((int)(SnippetsUniformGrid.ActualWidth / 200), _viewModel.SnippetViewModels.Count);

            SnippetsUniformGrid.Columns = columns > 0 ? columns : 1;
        }
    }
}
