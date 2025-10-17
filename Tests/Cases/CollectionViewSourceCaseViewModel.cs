using Demo.Data;
using DotNetKit.Windows.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
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
                var currentFilter = cvs.View.Filter;
                Debug.WriteLine($"filter: text='{Filter}'");
                cvs.View.Refresh();
                var newFilter = cvs.View.Filter;
                Debug.WriteLine($"  filterChanged: {currentFilter != newFilter}");
                AutoCompleteComboBox.FilterNames[newFilter] = $"vm({value})";
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    var deferFilter = cvs.View.Filter;
                    Debug.WriteLine($"  filterChanged(defer): {currentFilter != newFilter} {newFilter != deferFilter}");
                    AutoCompleteComboBox.FilterNames[deferFilter] = $"vm({value})";
                }));
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

            //SelectedValue = newItems[0].Id;
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
