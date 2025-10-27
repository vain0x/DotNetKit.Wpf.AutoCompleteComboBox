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
            source = new ObservableCollection<Person>(allItems);
            collectionView = new(source);
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());
        }

        List<Person> allItems = new(PersonModule.All);
        readonly ObservableCollection<Person> source;

        readonly ListCollectionView collectionView;
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

                // Do not this; changing Filter is overwritten by the library.
                //collectionView.Filter = obj =>
                //{
                //    var item = (Person)obj;
                //    return item.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase);
                //};
                //collectionView.Refresh();

                ResetSource();
            }
        }

        public ICommand ReloadCommand { get; init; }
        public void Reload()
        {
            // Regenerate items.
            var newItems = new List<Person>(PersonModule.All);
            newItems.Insert(0, GenerateRandomPerson());
            allItems = newItems;

            ResetSource();

            //SelectedValue = newItems[0].Id;
            //CollectionView.Refresh();
        }

        void ResetSource()
        {
            source.Clear();
            foreach (var item in allItems)
            {
                if (item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase))
                {
                    source.Add(item);
                }
            }
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            source.Clear();
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
