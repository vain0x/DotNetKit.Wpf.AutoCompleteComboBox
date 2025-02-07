using DotNetKit.Windows.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using System.Windows.Data;
using System.ComponentModel;

namespace DotNetKit.Windows.Controls
{
    public sealed class AutoCompleteBehavior : IDisposable
    {
        readonly ComboBox owner;
        TextBox editableTextBox;
        readonly DispatcherTimer debounceTimer;
        Action dispose = null;

        public Predicate<object> defaultItemsFilter;
        public Func<AutoCompleteComboBoxSetting> GetSetting { get; set; }

        public AutoCompleteBehavior(ComboBox owner)
        {
            this.owner = owner;
            debounceTimer = new DispatcherTimer(DispatcherPriority.Background, owner.Dispatcher);

            var onPreviewKeyDown = new KeyEventHandler(OnPreviewKeyDown);
            owner.AddHandler(UIElement.PreviewKeyDownEvent, onPreviewKeyDown);

            var onTextChanged = new TextChangedEventHandler(OnTextChanged);
            owner.AddHandler(TextBoxBase.TextChangedEvent, onTextChanged);

            dispose = () =>
            {
                owner.RemoveHandler(UIElement.PreviewKeyDownEvent, onPreviewKeyDown);
                owner.RemoveHandler(TextBoxBase.TextChangedEvent, onTextChanged);
            };
        }

        public void Dispose()
        {
            debounceTimer.Stop();
            dispose?.Invoke();
            dispose = null;
        }

        public AutoCompleteComboBoxSetting Setting
        {
            get
            {
                if (GetSetting != null)
                {
                    return GetSetting() ?? AutoCompleteComboBoxSetting.Default;
                }
                return AutoCompleteComboBoxSetting.Default;
            }
            set
            {
                var setting = value;
                GetSetting = () => setting;
            }
        }

        void OnDebounceTimerTick(object sender, EventArgs e)
        {
            debounceTimer.Stop();
            UpdateSuggestionList();
        }

        static TextBox FindEditableTextBox(ComboBox comboBox)
        {
            const string name = "PART_EditableTextBox";
            return (TextBox)VisualTreeModule.FindChild(comboBox, name);
        }

        TextBox EditableTextBox
        {
            get
            {
                if (editableTextBox == null)
                {
                    editableTextBox = FindEditableTextBox(owner);
                }
                return editableTextBox;
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
            d.SetBinding(item, TextSearch.GetTextPath(owner));
            return d.Value ?? string.Empty;
        }

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

        void Unselect()
        {
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateFilter(Predicate<object> filter)
        {
            using (new TextBoxStatePreserver(EditableTextBox))
            using (owner.Items.DeferRefresh())
            {
                // FIXME: This overwrites to the CollectionView.Filter,
                //        where the view is:
                var _view = owner.ItemsSource as ICollectionView ?? CollectionViewSource.GetDefaultView(owner.ItemsSource);
                //        it's undesirable for some of users who use CollectionView.Filter too.

                // Can empty the text box. I don't why.
                owner.Items.Filter = filter;
            }
        }

        void OpenDropDown(Predicate<object> filter)
        {
            UpdateFilter(filter);
            owner.IsDropDownOpen = true;
            Unselect();
        }

        void OpenDropDown()
        {
            var filter = GetFilter();
            OpenDropDown(filter);
        }

        void UpdateSuggestionList()
        {
            var text = owner.Text;

            if (text == previousText) return;
            previousText = text;

            if (string.IsNullOrEmpty(text))
            {
                owner.IsDropDownOpen = false;
                owner.SelectedItem = null;

                using (owner.Items.DeferRefresh())
                {
                    owner.Items.Filter = defaultItemsFilter;
                }
            }
            else if (owner.SelectedItem != null && TextFromItem(owner.SelectedItem) == text)
            {
                // It seems the user selected an item.
                // Do nothing.
            }
            else
            {
                using (new TextBoxStatePreserver(EditableTextBox))
                {
                    owner.SelectedItem = null;
                }

                var filter = GetFilter();
                var maxCount = Setting.MaxSuggestionCount;
                var count = CountWithMax(owner.ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                if (0 < count && count <= maxCount)
                {
                    OpenDropDown(filter);
                }
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var delay = Setting.Delay;

            if (delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList();
                return;
            }

            // Wait for debunce.
            debounceTimer.Interval = delay;
            debounceTimer.Stop();
            debounceTimer.Start();
        }
        #endregion

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Space
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                OpenDropDown();
                e.Handled = true;
            }
        }

        Predicate<object> GetFilter()
        {
            var filter = Setting.GetFilter(owner.Text, TextFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }
    }
}
