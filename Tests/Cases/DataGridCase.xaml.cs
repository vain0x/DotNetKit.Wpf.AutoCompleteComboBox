using System.Windows.Controls;

namespace Tests.Cases
{
    /// <summary>
    /// Interaction logic for DataGridCase.xaml
    /// </summary>
    public partial class DataGridCase : UserControl
    {
        public DataGridCase()
        {
            InitializeComponent();

            DataContext = new DataGridCaseViewModel();
        }
    }
}
