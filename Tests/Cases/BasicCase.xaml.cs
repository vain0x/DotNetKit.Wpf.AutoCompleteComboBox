using System.Windows.Controls;

namespace Tests.Cases
{
    /// <summary>
    /// Interaction logic for BasicCase.xaml
    /// </summary>
    public partial class BasicCase : UserControl
    {
        public BasicCase()
        {
            InitializeComponent();

            DataContext = new BasicCaseViewModel();
        }
    }
}
