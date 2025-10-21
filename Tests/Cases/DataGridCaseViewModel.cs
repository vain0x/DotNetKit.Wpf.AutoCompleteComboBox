using Demo.Data;
using System.Collections.ObjectModel;
using System.Linq;
using Tests.Util;

namespace Tests.Cases
{
    public class DataGridCaseViewModel : ViewModelBase
    {
        public DataGridCaseViewModel()
        {
            testItems = new ObservableCollection<TestItem>();

            for (int i = 1; i <= 14; i++)
            {
                testItems.Add(new TestItem()
                {
                    BaseName = "Base" + i.ToString(),
                    PersonId = 10,
                });
            }
        }

        private static readonly Person[] Persons = PersonModule.All.Take(50).ToArray();

        private Person? selectedItem;
        public Person? SelectedItem
        {
            get { return selectedItem; }
            set { SetField(ref selectedItem, value); }
        }

        private long? selectedValue;
        public long? SelectedValue
        {
            get { return selectedValue; }
            set { SetField(ref selectedValue, value); }
        }

        private ObservableCollection<TestItem> testItems;
        public ObservableCollection<TestItem> TestItems
        {
            get { return testItems; }
            set { SetField(ref testItems, value); }
        }

        public class TestItem : ViewModelBase
        {
            private long personId;
            public long PersonId
            {
                get { return personId; }
                set { SetField(ref personId, value); }
            }

            private string baseName = "";
            public string BaseName
            {
                get { return baseName; }
                set { SetField(ref baseName, value); }
            }

            // Use distinct ItemsSource instance for each row to avoid comboxes affecting each other of the same source. (#26)
            // ReadOnlyCollection is a readonly wrapper of list.
            private ReadOnlyCollection<Person> itemsSource = new ReadOnlyCollection<Person>(Persons);
            public ReadOnlyCollection<Person> ItemsSource
            {
                get { return itemsSource; }
            }
        }
    }
}
