using System.Windows.Controls;

namespace Tests.Cases
{
    /// <summary>
    /// Interaction logic for CollectionViewSourceCase.xaml
    /// </summary>
    public partial class CollectionViewSourceCase : UserControl
    {
        public CollectionViewSourceCase()
        {
            InitializeComponent();

            DataContext = new CollectionViewSourceCaseViewModel();
        }
    }
}
