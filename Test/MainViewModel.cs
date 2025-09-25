using DotNetKit.Demo.Data;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using Test.Util;

namespace Test
{
    sealed class MainViewModel
        : INotifyPropertyChanged
    {
        public MainViewModel()
        {
            ReloadCommand = new Command(_ => Reload());
            ClearCommand = new Command(_ => Clear());

            CollectionView = new ListCollectionView(Items);
            CollectionView.Filter = (item) => ((Person)item).Name.StartsWith("A");
        }

        public ListCollectionView CollectionView { get; }

        List<Person> items = new(PersonModule.All);
        public List<Person> Items
        {
            get => items;
            set { SetField(ref items, value); }
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
            Items = new List<Person>(PersonModule.All)
            {
                GenerateRandomPerson()
            };
            SelectedValue = Items.Last().Id;

            CollectionView.Refresh();
        }

        public ICommand ClearCommand { get; init; }
        public void Clear()
        {
            Items = new List<Person>();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return;

            field = value;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        static Person GenerateRandomPerson()
        {
            sLastId++;
            var id = sLastId;
            return new Person(id, $"Person {id}");
        }

        static long sLastId = PersonModule.All.Count;
    }
}
