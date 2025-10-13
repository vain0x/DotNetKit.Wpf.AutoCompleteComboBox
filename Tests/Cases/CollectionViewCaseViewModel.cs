using Demo.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using Tests.Util;

namespace Tests.Cases
{
    class CollectionViewCaseViewModel : ViewModelBase
    {
        public CollectionViewCaseViewModel()
        {
            collectionView = new(Items);
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());
        }

        ObservableCollection<Person> items = new(PersonModule.All);
        public ObservableCollection<Person> Items
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

        private string filter = "";
        public string Filter
        {
            get => filter;
            set
            {
                SetField(ref filter, value);

                // Update filter.
                collectionView.Filter = obj =>
                {
                    var item = (Person)obj;
                    return item.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase);
                };
                collectionView.Refresh();
            }
        }

        public ICommand ReloadCommand { get; init; }
        public void Reload()
        {
            var newItems = new ObservableCollection<Person>(PersonModule.All);
            newItems.Insert(0, GenerateRandomPerson());

            Items.Clear();
            foreach (var item in newItems)
            {
                Items.Add(item);
            }

            //SelectedValue = newItems[0].Id;
            CollectionView.Refresh();
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            Items.Clear();
            //CollectionView.Refresh();
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
