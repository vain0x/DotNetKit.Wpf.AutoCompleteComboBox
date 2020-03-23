using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using DotNetKit.Misc.Disposables;
using DotNetKit.Windows.Media;

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

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            defaultItemsFilter = newValue is ICollectionView cv ? cv.Filter : null;
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

        void UpdateFilter(Predicate<object> filter)
        {
            using (new TextBoxStatePreserver(EditableTextBox))
            using (Items.DeferRefresh())
            {
                // Can empty the text box. I don't why.
                Items.Filter = filter;
            }
        }

        void OpenDropDown(Predicate<object> filter)
        {
            UpdateFilter(filter);
            IsDropDownOpen = true;
            Unselect();
        }

        void OpenDropDown()
        {
            var filter = GetFilter();
            OpenDropDown(filter);
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

                using (Items.DeferRefresh())
                {
                    Items.Filter = defaultItemsFilter;
                }
            }
            else if (SelectedItem != null && TextFromItem(SelectedItem) == text)
            {
                // It seems the user selected an item.
                // Do nothing.
            }
            else
            {
                using (new TextBoxStatePreserver(EditableTextBox))
                {
                    SelectedItem = null;
                }

                var filter = GetFilter();
                var maxCount = SettingOrDefault.MaxSuggestionCount;
                var count = CountWithMax(ItemsSource.Cast<object>(), filter, maxCount);

                if (count > maxCount) return;

                OpenDropDown(filter);
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
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
            var filter = SettingOrDefault.GetFilter(Text, TextFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }

        public AutoCompleteComboBox()
        {
            InitializeComponent();

            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
        }
    }
}