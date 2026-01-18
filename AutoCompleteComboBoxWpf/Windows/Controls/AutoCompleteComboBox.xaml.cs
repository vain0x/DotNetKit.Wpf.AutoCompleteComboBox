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
                    editableTextBoxCache = (TextBox)FindDescendant(this, name);
                }
                return editableTextBoxCache;
            }
        }

        /// <summary>
        /// Gets the text used for matching against the query.
        /// Returns an empty string if the item is null.
        /// </summary>
        string GetItemText(object item)
        {
            if (item == null) return string.Empty;

            var d = new BindingEvaluator<string>();
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

        struct TextBoxStateSaver
            : IDisposable
        {
            readonly TextBox textBox;
            readonly int selectionStart;
            readonly int selectionLength;
            readonly string text;

            public void Dispose()
            {
                if (textBox != null)
                {
                    textBox.Text = text;
                    textBox.Select(selectionStart, selectionLength);
                }
            }

            public TextBoxStateSaver(TextBox textBox)
            {
                this.textBox = textBox;
                selectionStart = textBox?.SelectionStart ?? 0;
                selectionLength = textBox?.SelectionLength ?? 0;
                text = textBox?.Text ?? "";
            }
        }

        static int CountUpTo<T>(IEnumerable<T> xs, Predicate<T> predicate, int maxCount)
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

            // Setting Items.Filter can sometimes clear the TextBox unexpectedly.
            // The preserver is used as a workaround.
            using (new TextBoxStateSaver(textBox))
            using (Items.DeferRefresh())
            {
                Items.Filter = filter;
            }

            // Deselect the text.
            if (textBox != null)
            {
                textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
            }
        }

        void UpdateSuggestionList(bool controlOpen)
        {
            var text = Text;

            if (text == previousText) return;
            previousText = text;

            if (string.IsNullOrEmpty(text))
            {
                if (controlOpen)
                {
                    IsDropDownOpen = false;
                }

                SelectedItem = null;

                using (Items.DeferRefresh())
                {
                    Items.Filter = defaultItemsFilter;
                }
            }
            else if (SelectedItem != null && GetItemText(SelectedItem) == text)
            {
                // Some item is selected and therefore text is set. Keep the current filter.
                return;
            }
            else
            {
                using (new TextBoxStateSaver(EditableTextBox))
                {
                    SelectedItem = null;
                }

                UpdateFilter();

                // Automatically opens the dropdown when the number of filtered items is within the allowed range.
                if (controlOpen && !IsDropDownOpen && IsKeyboardFocusWithin)
                {
                    var filter = GetFilter();
                    var maxCount = SettingOrDefault.MaxSuggestionCount;
                    var count = CountUpTo(ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

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
                UpdateSuggestionList(controlOpen: true);
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
                UpdateSuggestionList(controlOpen: true);
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

            UpdateSuggestionList(controlOpen: false);

            // The text becomes fully selected when the dropdown opens; deselect it.
            var textBox = EditableTextBox;
            if (textBox != null)
            {
                textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
            }
        }

        void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Space
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                e.Handled = true;
                IsDropDownOpen = true;
            }
        }

        Predicate<object> GetFilter()
        {
            var filter = SettingOrDefault.GetFilter(Text ?? "", GetItemText);

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

        #region BindingEvaluator
        sealed class BindingEvaluator<T> : DependencyObject
        {
            public static readonly DependencyProperty ValueProperty =
                DependencyProperty.Register("Value", typeof(T), typeof(BindingEvaluator<T>));

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

        #region FindDescendant
        static FrameworkElement FindDescendant(DependencyObject obj, string childName)
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