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
    public sealed class AutoCompleteBehavior
    {
        #region UseProperty
        /// <summary>
        /// When this property is attached to a ComboBox,
        /// auto completion behavior is provided.
        ///
        /// The value of the property is either `null` or an instance of `AutoCompleteComboBoxSetting`.
        /// </summary>
        public static readonly DependencyProperty UseProperty = DependencyProperty.RegisterAttached(
            "Use",
            typeof(AutoCompleteComboBoxSetting),
            typeof(AutoCompleteBehavior),
            new PropertyMetadata(OnUsePropertyChanged)
        );

        private static void OnUsePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var comboBox = sender as ComboBox ?? throw new ArgumentException($"{nameof(AutoCompleteBehavior)}.Use canbe attached to only ComboBox.");

            var behavior = GetBehavior(comboBox);
            if (behavior == null)
            {
                behavior = new AutoCompleteBehavior(comboBox);
                SetBehavior(comboBox, behavior);
            }

            behavior.setting = (e.NewValue as AutoCompleteComboBoxSetting) ?? AutoCompleteComboBoxSetting.Default;
        }

        public static ComboBox GetUse(DependencyObject obj) => (ComboBox)obj.GetValue(UseProperty);
        public static void SetUse(DependencyObject obj, ComboBox value) => obj.SetValue(UseProperty, value);
        #endregion

        #region BehaviorProperty
        /// <summary>
        /// <b>[Internal use only]</b>:
        /// This property just provides a slot to store a behavior object to manage state.
        /// </para>
        /// </summary>
        public static readonly DependencyProperty BehaviorProperty = DependencyProperty.RegisterAttached(
            "Behavior",
            typeof(AutoCompleteBehavior),
            typeof(AutoCompleteBehavior)
        );

        public static AutoCompleteBehavior GetBehavior(DependencyObject obj) => (AutoCompleteBehavior)obj.GetValue(BehaviorProperty);
        public static void SetBehavior(DependencyObject obj, AutoCompleteBehavior value) => obj.SetValue(BehaviorProperty, value);
        #endregion

        public AutoCompleteBehavior(ComboBox owner)
        {
            this.owner = owner;

            debounceTimer = new DispatcherTimer(DispatcherPriority.Background, owner.Dispatcher);
            debounceTimer.Tick += (_sender, _e) =>
            {
                debounceTimer.Stop();
                UpdateSuggestionList();
            };

            owner.AddHandler(UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnPreviewKeyDown));
            owner.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(OnTextChanged));
        }

        readonly ComboBox owner;
        TextBox editableTextBox;
        readonly DispatcherTimer debounceTimer;

        Predicate<object> defaultItemsFilter;
        AutoCompleteComboBoxSetting setting;

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

        //#region ItemsSource
        //public static new readonly DependencyProperty ItemsSourceProperty =
        //    DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(AutoCompleteComboBox),
        //        new PropertyMetadata(null, ItemsSourcePropertyChanged));
        //public new IEnumerable ItemsSource
        //{
        //    get
        //    {
        //        return (IEnumerable)GetValue(ItemsSourceProperty);
        //    }
        //    set
        //    {
        //        SetValue(ItemsSourceProperty, value);
        //    }
        //}

        //private static void ItemsSourcePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dpcea)
        //{
        //    var comboBox = (ComboBox)dependencyObject;
        //    var previousSelectedItem = comboBox.SelectedItem;

        //    if (dpcea.NewValue is ICollectionView cv)
        //    {
        //        ((AutoCompleteComboBox)dependencyObject).defaultItemsFilter = cv.Filter;
        //        comboBox.ItemsSource = cv;
        //    }
        //    else
        //    {
        //        ((AutoCompleteComboBox)dependencyObject).defaultItemsFilter = null;
        //        IEnumerable newValue = dpcea.NewValue as IEnumerable;
        //        CollectionViewSource newCollectionViewSource = new CollectionViewSource
        //        {
        //            Source = newValue
        //        };
        //        comboBox.ItemsSource = newCollectionViewSource.View;
        //    }

        //    comboBox.SelectedItem = previousSelectedItem;

        //    // if ItemsSource doesn't contain previousSelectedItem
        //    if (comboBox.SelectedItem != previousSelectedItem)
        //    {
        //        comboBox.SelectedItem = null;
        //    }
        //}
        //#endregion ItemsSource

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
                var maxCount = setting.MaxSuggestionCount;
                var count = CountWithMax(owner.ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                if (0 < count && count <= maxCount)
                {
                    OpenDropDown(filter);
                }
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var delay = setting.Delay;

            if (delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList();
                return;
            }

            // Wait for debunce.
            debounceTimer.Interval = setting.Delay;
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
            var filter = setting.GetFilter(owner.Text, TextFromItem);

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }
    }
}
