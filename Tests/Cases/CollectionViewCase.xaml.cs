using System.Windows.Controls;

namespace Tests.Cases
{
    /// <summary>
    /// Interaction logic for CollectionViewCase.xaml
    /// </summary>
    public partial class CollectionViewCase : UserControl
    {
        public CollectionViewCase()
        {
            InitializeComponent();

            DataContext = new CollectionViewCaseViewModel();
        }
    }
}
