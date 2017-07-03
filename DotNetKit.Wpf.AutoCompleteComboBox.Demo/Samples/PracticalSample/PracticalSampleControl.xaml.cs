using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DotNetKit.Demo.Samples.PracticalSample
{
    /// <summary>
    /// PracticalSampleControl.xaml の相互作用ロジック
    /// </summary>
    public partial class PracticalSampleControl : UserControl
    {
        public PracticalSampleControl()
        {
            InitializeComponent();

            DataContext = new PracticalSample();

            comboBox.SuggestionsUpdating += (sender, query) =>
            {
                foreach (var item in ((PracticalSample)DataContext).Items)
                {
                    item.Priority =
                        item.Name.StartsWith(query) ? 0 :
                        item.Name.Contains(query) ? 1 : 2;
                }
            };
        }
    }
}
