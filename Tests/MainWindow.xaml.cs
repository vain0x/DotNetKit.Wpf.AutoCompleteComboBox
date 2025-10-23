using System.Windows;

namespace Tests
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainViewModel();
            DataContext = vm;
            expanded.Collapsed += (_, _) =>
            {
                vm.Items.Clear();
            };
            expanded.Expanded += (_, _) =>
            {
                vm.Items.Add(new object());
            };
        }
    }
}
