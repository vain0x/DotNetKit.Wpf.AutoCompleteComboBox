using System;
using System.Collections.Generic;
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

        public IReadOnlyList<PersonItem> Items { get; private set; }

        PersonItem selectedItem;
        public PersonItem SelectedItem
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
            Items = PersonModule.All.Select(p => new PersonItem(p)).ToArray();
        }
    }

    public class PersonItem
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

        readonly Person person;

        public long Id => person.Id;
        public string Name => person.Name;

        double priority;
        public double Priority
        {
            get { return priority; }
            set { SetField(ref priority, value); }
        }

        public override string ToString()
        {
            return person.ToString();
        }

        public PersonItem(Person person)
        {
            this.person = person;
        }
    }
}
