using Demo.Data;
using System.Collections.Generic;
using System.Windows.Data;
using System.Windows.Input;
using Tests.Util;

namespace Tests.Cases
{
    class CollectionViewSourceCaseViewModel : ViewModelBase
    {
        public CollectionViewSourceCaseViewModel()
        {
            collectionView = new(Items);
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());
        }

        List<Person> items = new(PersonModule.All);
        public List<Person> Items
        {
            get => items;
            set { SetField(ref items, value); }
        }

        ListCollectionView collectionView;
        public ListCollectionView CollectionView => collectionView;

        Person? selectedItem;
        public Person? SelectedItem
        {
            get => selectedItem;
            set { SetField(ref selectedItem, value); }
        }

        long? selectedValue;
        public long? SelectedValue
        {
            get => selectedValue;
            set { SetField(ref selectedValue, value); }
        }

        //private string filter = "";
        //public string Filter
        //{
        //    get => filter;
        //    set { SetField(ref filter, value); }
        //}

        public ICommand ReloadCommand { get; init; }
        public void Reload()
        {
            var newItems = new List<Person>(PersonModule.All);
            newItems.Insert(0, GenerateRandomPerson());
            Items = items;

            SelectedValue = newItems[0].Id;

            CollectionView.Refresh();
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            Items = new List<Person>();
        }

        static Person GenerateRandomPerson()
        {
            sLastId++;
            var id = sLastId;
            return new Person(id, $"Person {id}");
        }

        static long sLastId = PersonModule.All.Count;
    }
}
