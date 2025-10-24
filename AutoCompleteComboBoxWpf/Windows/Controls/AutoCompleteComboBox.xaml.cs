using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DotNetKit.Windows.Controls
{
    /// <summary>
    /// AutoCompleteComboBox.xaml
    /// </summary>
    public partial class AutoCompleteComboBox : ComboBox
    {
        TextBox editableTextBoxCache;
        DispatcherTimer debounceTimer;
        Predicate<object> defaultItemsFilter;

        public TextBox EditableTextBox
        {
            get
            {
                if (editableTextBoxCache == null)
                {
                    const string name = "PART_EditableTextBox";
                    editableTextBoxCache = (TextBox)FindChild(this, name);
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

        void UpdateFilter()
        {
            var filter = GetFilter();
            var textBox = EditableTextBox;

            // Assignment to Items.Filter sometimes clears the TextBox for some reason. The preserver is used as a workaround.
            using (new TextBoxStatePreserver(textBox))
            using (Items.DeferRefresh())
            {
                Items.Filter = filter;
            }

            // Unselect text.
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateSuggestionList(bool autoOpen)
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
                // Some item is selected and therefore text is set. Keep the current filter.
                return;
            }
            else
            {
                using (new TextBoxStatePreserver(EditableTextBox))
                {
                    SelectedItem = null;
                }

                UpdateFilter();

                // When the number of filtered items is small enough, automatically open the dropdown.
                if (autoOpen && !IsDropDownOpen && IsKeyboardFocusWithin)
                {
                    var filter = GetFilter();
                    var maxCount = SettingOrDefault.MaxSuggestionCount;
                    var count = CountWithMax(ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                    if (0 < count && count <= maxCount)
                    {
                        IsDropDownOpen = true;
                    }
                }
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var setting = SettingOrDefault;

            if (setting.Delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList(autoOpen: true);
                return;
            }

            // Wait for delay (debounce pattern.)
            if (debounceTimer != null)
            {
                debounceTimer.Stop();
            }
            var onTick = new EventHandler((_sender, _e) =>
            {
                debounceTimer.Stop();
                debounceTimer = null;
                UpdateSuggestionList(autoOpen: true);
            });
            debounceTimer = new DispatcherTimer(setting.Delay, DispatcherPriority.Normal, onTick, Dispatcher);
            debounceTimer.Start();
        }
        #endregion

        protected override void OnDropDownOpened(EventArgs e)
        {
            base.OnDropDownOpened(e);

            // Update filter immediately.
            if (debounceTimer != null)
            {
                debounceTimer.Stop();
                debounceTimer = null;
            }

            UpdateSuggestionList(autoOpen: false);

            // Text is all-selected on dropdown opened, unselect it.
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                e.Handled = true;
                IsDropDownOpen = true;
            }
        }

        Predicate<object> GetFilter()
        {
            var filter = SettingOrDefault.GetFilter(Text ?? "", TextFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }

        public AutoCompleteComboBox()
        {
            InitializeComponent();

            AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
        }

        // Helpers

        #region DependencyVariable
        sealed class DependencyVariable<T> : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register(
                    "Value",
                    typeof(T),
                    typeof(DependencyVariable<T>)
                );

            public T Value
            {
                get { return (T)GetValue(ValueProperty); }
                set { SetValue(ValueProperty, value); }
            }

            public void SetBinding(Binding binding)
            {
                BindingOperations.SetBinding(this, ValueProperty, binding);
            }

            public void SetBinding(object dataContext, string propertyPath)
            {
                SetBinding(new Binding(propertyPath) { Source = dataContext });
            }
        }
        #endregion

        #region FindChild
        static FrameworkElement FindChild(DependencyObject obj, string childName)
        {
            if (obj == null) return null;

            var queue = new Queue<DependencyObject>();
            queue.Enqueue(obj);

            while (queue.Count > 0)
            {
                obj = queue.Dequeue();

                var childCount = VisualTreeHelper.GetChildrenCount(obj);
                for (var i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(obj, i);

                    var fe = child as FrameworkElement;
                    if (fe != null && fe.Name == childName)
                    {
                        return fe;
                    }

                    queue.Enqueue(child);
                }
            }

            return null;
        }
        #endregion
    }
}