using Demo.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Input;
using Tests.Util;

namespace Tests.Cases
{
    class CollectionViewSourceCaseViewModel : ViewModelBase
    {
        public CollectionViewSourceCaseViewModel()
        {
            items = new(PersonModule.All);
            cvs = new CollectionViewSource
            {
                Source = items,
            };
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());

            cvs.Filter += (_, e) =>
            {
                var item = (Person)e.Item;
                e.Accepted = item.Name.Contains(Filter, StringComparison.OrdinalIgnoreCase);
            };
        }

        ObservableCollection<Person> items;
        public ObservableCollection<Person> Items
        {
            get => items;
            set { SetField(ref items, value); }
        }

        CollectionViewSource cvs;
        public CollectionViewSource CollectionViewSource => cvs;

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
                cvs.View.Refresh();
            }
        }

        public ICommand ReloadCommand { get; init; }
        public void Reload()
        {
            var newItems = new List<Person>(PersonModule.All);
            newItems.Insert(0, GenerateRandomPerson());
            using (cvs.DeferRefresh())
            {
                Items.Clear();
                foreach (var item in newItems)
                {
                    Items.Add(item);
                }
            }

            SelectedValue = newItems[0].Id;
            cvs.View.Refresh();
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            Items.Clear();
            cvs.View.Refresh();
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
