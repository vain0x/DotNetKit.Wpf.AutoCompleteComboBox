using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DotNetKit.Demo.Data;

namespace DotNetKit.Demo.Samples.PracticalSample
{
    /// <summary>
    /// Represents a data context for the sample.
    /// </summary>
    public sealed class PracticalSample
        : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        void SetField<X>(ref X field, X value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<X>.Default.Equals(field, value)) return;

            field = value;

            var h = PropertyChanged;
            if (h != null) h(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public ObservableCollection<Person> Items { get; private set; }

        Person selectedItem;
        public Person SelectedItem
        {
            get { return selectedItem; }
            set { SetField(ref selectedItem, value); }
        }

        long? selectedValue;
        public long? SelectedValue
        {
            get { return selectedValue; }
            set { SetField(ref selectedValue, value); }
        }

        public PracticalSample()
        {
            Items = new ObservableCollection<Person>(PersonModule.All);
        }
    }
}
