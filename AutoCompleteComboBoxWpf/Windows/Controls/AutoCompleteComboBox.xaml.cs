using DotNetKit.Misc.Disposables;
using DotNetKit.Windows.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

namespace DotNetKit.Windows.Controls
{
    /// <summary>
    /// AutoCompleteComboBox.xaml
    /// </summary>
    public partial class AutoCompleteComboBox : ComboBox
    {
        readonly SerialDisposable disposable = new SerialDisposable();

        TextBox editableTextBoxCache;

        Predicate<object> defaultItemsFilter;
        Predicate<object> appliedItemsFilter;
        public static Dictionary<Predicate<object>, string> FilterNames = new Dictionary<Predicate<object>, string>();

        public TextBox EditableTextBox
        {
            get
            {
                if (editableTextBoxCache == null)
                {
                    const string name = "PART_EditableTextBox";
                    editableTextBoxCache = (TextBox)VisualTreeModule.FindChild(this, name);
                }
                return editableTextBoxCache;
            }
        }

        /// <summary>
        /// Gets text to match with the query from an item.
        /// Never null.
        /// </summary>
        /// <param name="item"/>
        string TextFromItem(object item)
        {
            if (item == null) return string.Empty;

            var d = new DependencyVariable<string>();
            d.SetBinding(item, TextSearch.GetTextPath(this));
            return d.Value ?? string.Empty;
        }

        #region Setting
        static readonly DependencyProperty settingProperty =
            DependencyProperty.Register(
                "Setting",
                typeof(AutoCompleteComboBoxSetting),
                typeof(AutoCompleteComboBox)
            );

        public static DependencyProperty SettingProperty
        {
            get { return settingProperty; }
        }

        public AutoCompleteComboBoxSetting Setting
        {
            get { return (AutoCompleteComboBoxSetting)GetValue(SettingProperty); }
            set { SetValue(SettingProperty, value); }
        }

        AutoCompleteComboBoxSetting SettingOrDefault
        {
            get { return Setting ?? AutoCompleteComboBoxSetting.Default; }
        }
        #endregion

        #region OnTextChanged
        long revisionId;
        string previousText;

        struct TextBoxStatePreserver
            : IDisposable
        {
            readonly TextBox textBox;
            readonly int selectionStart;
            readonly int selectionLength;
            readonly string text;

            public void Dispose()
            {
                textBox.Text = text;
                textBox.Select(selectionStart, selectionLength);
            }

            public TextBoxStatePreserver(TextBox textBox)
            {
                this.textBox = textBox;
                selectionStart = textBox.SelectionStart;
                selectionLength = textBox.SelectionLength;
                text = textBox.Text;
            }
        }

        static int CountWithMax<T>(IEnumerable<T> xs, Predicate<T> predicate, int maxCount)
        {
            var count = 0;
            foreach (var x in xs)
            {
                if (predicate(x))
                {
                    count++;
                    if (count > maxCount) return count;
                }
            }
            return count;
        }

        void Unselect()
        {
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateFilter()
        {
            // Assignment to Filter sometimes removes the text without the preserver.
            using (new TextBoxStatePreserver(EditableTextBox))
            using (Items.DeferRefresh())
            {
                // Capture the underlying filter if Items.Filter is modified.
                if (Items.Filter != appliedItemsFilter)
                {
                    Debug.WriteLine($"capturing");
                    defaultItemsFilter = Items.Filter;
                    if (!FilterNames.ContainsKey(defaultItemsFilter))
                    {
                        FilterNames[defaultItemsFilter] = "captured";
                    }
                }

                var filter = GetFilter();
                //if (defaultItemsFilter == null && Items.Filter != null)
                //{
                //    defaultItemsFilter = Items.Filter;
                //}
                if (filter != null && !FilterNames.ContainsKey(filter))
                {
                    var defaultName = defaultItemsFilter != null ? (FilterNames.TryGetValue(defaultItemsFilter, out var x) ? x : "default") : "null";
                    var name = $"input({Text})+{defaultName}";
                    FilterNames.Add(filter, name);
                }

                Items.Filter = filter;
                appliedItemsFilter = filter;
            }
        }

        void OpenDropDown()
        {
            //UpdateFilter();
            IsDropDownOpen = true;
            //Unselect();
        }

        protected override void OnDropDownOpened(EventArgs e)
        {
            UpdateFilter();

            base.OnDropDownOpened(e);

            var name = "";
            if (Items.Filter != null && FilterNames.ContainsKey(Items.Filter))
            {
                name = FilterNames[Items.Filter];
            }
            else if (Items.Filter != null)
            {
                name = "unknown";
            }
            else
            {
                name = "null";
            }
            Debug.WriteLine($"Dropdown open filter={name}");
        }

        void UpdateSuggestionList()
        {
            var text = Text;

            if (text == previousText) return;
            previousText = text;

            if (string.IsNullOrEmpty(text))
            {
                IsDropDownOpen = false;
                SelectedItem = null;

                // Remove filter.
                //using (Items.DeferRefresh())
                //{
                //    RemoveFilter();
                //    //Items.Filter = defaultItemsFilter;
                //}
                //UpdateFilter();
                if (IsDropDownOpen)
                {
                    UpdateFilter();
                }
            }
            else if (SelectedItem != null && TextFromItem(SelectedItem) == text)
            {
                // It seems the user selected an item.
                // Do nothing.
            }
            else if (IsDropDownOpen)
            {
                UpdateFilter();
            }
            else
            {
                //using (new TextBoxStatePreserver(EditableTextBox))
                //{
                //    SelectedItem = null;
                //}

                var textFilter = SettingOrDefault.GetFilter(Text ?? "", TextFromItem);
                //var filter = GetFilter();
                var filter = textFilter;
                var maxCount = SettingOrDefault.MaxSuggestionCount;
                var count = CountWithMax(ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                if (0 < count && count <= maxCount && IsKeyboardFocusWithin)
                {
                    OpenDropDown();
                }
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            //Debug.WriteLine($"TextChanged ('{Text}')");

            var id = unchecked(++revisionId);
            var setting = SettingOrDefault;

            if (setting.Delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList();
                return;
            }

            disposable.Content =
                new Timer(
                    state =>
                    {
                        Dispatcher.InvokeAsync(() =>
                        {
                            if (revisionId != id) return;
                            UpdateSuggestionList();
                        });
                    },
                    null,
                    setting.Delay,
                    Timeout.InfiniteTimeSpan
                );
        }
        #endregion

        void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                OpenDropDown();
                e.Handled = true;
            }
        }

        Predicate<object> GetFilter()
        {
            if (string.IsNullOrEmpty(Text))
            {
                return defaultItemsFilter;
            }

            var filter = SettingOrDefault.GetFilter(Text ?? "", TextFromItem);
            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }

        //public IEnumerable ItemsSourceBinder
        //{
        //    get { return (IEnumerable)GetValue(ItemsSourceBinderProperty); }
        //    set { SetValue(ItemsSourceBinderProperty, value); }
        //}
        //public static readonly DependencyProperty ItemsSourceBinderProperty =
        //    DependencyProperty.Register("ItemsSourceBinder", typeof(IEnumerable), typeof(AutoCompleteComboBox), new PropertyMetadata(null)
        //    {
        //        PropertyChangedCallback = OnItemsSourceBinderChanged,
        //    });
        //static void OnItemsSourceBinderChanged(object sender, DependencyPropertyChangedEventArgs e)
        //{
        //    Debug.WriteLine("ItemsSourceBinderChanged");
        //    AutoCompleteComboBox cb = (AutoCompleteComboBox)sender;
        //    cb.OnItemsSourceChangedShadowing();
        //}
        //private void OnItemsSourceChangedShadowing()
        //{
        //    defaultItemsFilter = null;
        //}

        public AutoCompleteComboBox()
        {
            InitializeComponent();

            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));

            // ItemsSourceBinder={Binding ItemsSource, Source=self}
            // to notify changing of ItemsSource itself
            //BindingOperations.SetBinding(this, ItemsSourceBinderProperty, new Binding("ItemsSource") { Source = this });
        }
    }
}