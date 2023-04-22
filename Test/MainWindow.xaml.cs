using System.Windows;
using System.Windows.Data;

namespace Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainViewModel();
        }

        //public CollectionViewSource Items = new CollectionViewSource();
    }
}
