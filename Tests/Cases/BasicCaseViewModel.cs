using Demo.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;
using Tests.Util;

namespace Tests.Cases
{
    public class BasicCaseViewModel : ViewModelBase
    {
        public BasicCaseViewModel()
        {
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());
        }

        List<Person> items = new(PersonModule.All);
        public List<Person> Items
        {
            get => items;
            set { SetField(ref items, value); }
        }

        string? text;
        public string? Text
        {
            get { return text; }
            set { SetField(ref text, value); }
        }

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
            newItems.Insert(0, new Person(
                Items.Count + 1,
                "Random person " + Random.Shared.NextInt64()
            ));
            Items = newItems;
            //SelectedValue = Items[0].Id;
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            Items = new List<Person>();
        }
    }
}
