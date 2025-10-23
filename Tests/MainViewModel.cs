using System.Collections.Generic;
using System.Collections.ObjectModel;
using Tests.Util;

namespace Tests
{
    class MainViewModel : ViewModelBase
    {
        public ObservableCollection<object> Items { get; } = new();
    }
}
