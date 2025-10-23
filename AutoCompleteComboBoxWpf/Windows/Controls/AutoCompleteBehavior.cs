using DotNetKit.Windows.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace DotNetKit.Windows.Controls
{
    public class AutoCompleteBehavior : IDisposable
    {
        readonly ComboBox owner;
        TextBox editableTextBox;
        string previousText;
        DispatcherTimer debounceTimer;
        Predicate<object> defaultItemsFilter;
        Action dispose;

        public DependencyPropertyChangedEventHandler ItemsSourceChanged;

        public Func<AutoCompleteComboBoxSetting> SettingFunc { get; set; }

        public AutoCompleteBehavior(ComboBox owner)
        {
            this.owner = owner;

            var isEditable = owner.IsEditable;
            owner.IsEditable = true;

            var isTextSearchEnabled = owner.IsTextSearchEnabled;
            owner.IsTextSearchEnabled = false;

            var onPreviewKeyDown = new KeyEventHandler(OnPreviewKeyDown);
            owner.AddHandler(UIElement.PreviewKeyDownEvent, onPreviewKeyDown);

            var onTextChanged = new TextChangedEventHandler(OnTextChanged);
            owner.AddHandler(TextBoxBase.TextChangedEvent, onTextChanged);

            var onDropDownOpened = new EventHandler(OnDropDownOpened);
            owner.DropDownOpened += onDropDownOpened;

            var onUnloaded = new RoutedEventHandler((_sender, _e) =>
            {
                Debug.WriteLine($"unloaded");
                debounceTimer?.Stop();
                debounceTimer = null;
            });
            owner.Unloaded += onUnloaded;

            // owner.(ItemsSourceWatcher)="{Binding ItemsSource, Source=owner}"
            var itemsSourceWatcherBinding = owner.SetBinding(ItemsSourceWatcherProperty, new Binding("ItemsSource") { Source = owner });
            ItemsSourceChanged += (_sender, e) =>
            {
                Debug.WriteLine($"ItemsSourceChanged invoked: {e.NewValue}");
                defaultItemsFilter = e.NewValue is ICollectionView cv ? cv.Filter : null;
            };

            dispose += () =>
            {
                Debug.WriteLine("behavior disposing");
                owner.IsEditable = isEditable;
                owner.IsTextSearchEnabled = isTextSearchEnabled;
                owner.RemoveHandler(UIElement.PreviewKeyDownEvent, onPreviewKeyDown);
                owner.RemoveHandler(TextBoxBase.TextChangedEvent, onTextChanged);
                owner.DropDownOpened -= onDropDownOpened;
                owner.Unloaded -= onUnloaded;
                BindingOperations.ClearBinding(owner, ItemsSourceWatcherProperty);
            };

            Debug.WriteLine($"behavior created");
        }

        #region IsAttached property
        public static bool GetIsAttached(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsAttachedProperty);
        }

        public static void SetIsAttached(DependencyObject obj, bool value)
        {
            obj.SetValue(IsAttachedProperty, value);
        }

        public static readonly DependencyProperty IsAttachedProperty =
            DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(AutoCompleteBehavior), new PropertyMetadata(false)
            {
                PropertyChangedCallback = OnIsAttachedChanged
            });

        public static AutoCompleteBehavior GetBehavior(DependencyObject obj)
        {
            return (AutoCompleteBehavior)obj.GetValue(BehaviorProperty);
        }

        public static void SetBehavior(DependencyObject obj, AutoCompleteBehavior value)
        {
            obj.SetValue(BehaviorProperty, value);
        }

        /// <summary>
        /// For internal use.
        /// </summary>
        public static readonly DependencyProperty BehaviorProperty =
            DependencyProperty.RegisterAttached("Behavior", typeof(AutoCompleteBehavior), typeof(AutoCompleteBehavior), new PropertyMetadata(null));
        #endregion

        static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;
            if (oldValue == newValue) return;

            Debug.WriteLine($"OnIsAttachedChanged: {e.OldValue} -> {e.NewValue}");
            var owner = (ComboBox)d;
            var old = GetBehavior(owner);
            old?.Dispose();

            if (newValue)
            {
                var behavior = new AutoCompleteBehavior(owner);
                SetBehavior(owner, behavior);
            }
            else
            {
                SetBehavior(owner, null);
            }
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

        public AutoCompleteComboBoxSetting Setting
        {
            get
            {
                if (SettingFunc != null)
                {
                    return SettingFunc() ?? AutoCompleteComboBoxSetting.Default;
                }
                return AutoCompleteComboBoxSetting.Default;
            }
            set
            {
                var setting = value;
                SettingFunc = () => setting;
            }
        }

        // Dispose is implemented just in case, this is unlikely invoked in basic use cases.
        public void Dispose()
        {
            dispose?.Invoke();
            dispose = null;

            debounceTimer?.Stop();
            debounceTimer = null;
        }

        void UpdateFilter()
        {
            var filter = GetFilter();
            var textBox = EditableTextBox;

            // Assignment to Items.Filter sometimes clears the TextBox for some reason. The preserver is used as a workaround.
            using (new TextBoxStatePreserver(textBox))
            using (owner.Items.DeferRefresh())
            {
                owner.Items.Filter = filter;
            }

            // Unselect text.
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateSuggestionList(bool autoOpen)
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
            else if (owner.SelectedItem != null && GetItemText(owner, owner.SelectedItem) == text)
            {
                // Some item is selected and therefore text is set. Keep the current filter.
                return;
            }
            else
            {
                using (new TextBoxStatePreserver(EditableTextBox))
                {
                    owner.SelectedItem = null;
                }

                UpdateFilter();

                // When the number of filtered items is small enough, automatically open the dropdown.
                if (autoOpen && !owner.IsDropDownOpen && owner.IsKeyboardFocusWithin)
                {
                    var filter = GetFilter();
                    var maxCount = Setting.MaxSuggestionCount;
                    var count = CountWithMax(owner.ItemsSource?.Cast<object>() ?? Enumerable.Empty<object>(), filter, maxCount);

                    if (0 < count && count <= maxCount)
                    {
                        owner.IsDropDownOpen = true;
                    }
                }
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var setting = Setting;
            if (setting.Delay <= TimeSpan.Zero)
            {
                UpdateSuggestionList(autoOpen: true);
                return;
            }

            // Wait for delay (debounce pattern.)
            debounceTimer?.Stop();
            var onTick = new EventHandler((_sender, _e) =>
            {
                debounceTimer?.Stop();
                debounceTimer = null;
                UpdateSuggestionList(autoOpen: true);
            });
            debounceTimer = new DispatcherTimer(setting.Delay, DispatcherPriority.Normal, onTick, owner.Dispatcher);
            debounceTimer.Start();
        }

        void OnDropDownOpened(object _sender, EventArgs _e)
        {
            // Update filter immediately.
            debounceTimer?.Stop();
            debounceTimer = null;
            UpdateSuggestionList(autoOpen: false);

            // Text is all-selected on dropdown opened, unselect it.
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Space)
            {
                e.Handled = true;
                owner.IsDropDownOpen = true;
            }
        }

        Predicate<object> GetFilter()
        {
            var filter = Setting.GetFilter(owner.Text ?? "", (object item) => GetItemText(owner, item));

            return defaultItemsFilter != null
                ? i => defaultItemsFilter(i) && filter(i)
                : filter;
        }

        #region ItemsSourceWatcher
        public static IEnumerable GetItemsSourceWatcher(DependencyObject obj)
        {
            return (IEnumerable)obj.GetValue(ItemsSourceWatcherProperty);
        }

        public static void SetItemsSourceWatcher(DependencyObject obj, IEnumerable value)
        {
            obj.SetValue(ItemsSourceWatcherProperty, value);
        }

        /// <summary>
        /// For internal use.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceWatcherProperty =
            DependencyProperty.RegisterAttached("ItemsSourceWatcher", typeof(IEnumerable), typeof(AutoCompleteBehavior), new PropertyMetadata(null)
            {
                PropertyChangedCallback = OnItemsSourceWatcherChanged,
            });

        static void OnItemsSourceWatcherChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine($"ItemsSourceWatcher.OnItemsSourceWatcherChanged: {e.NewValue}");

            var owner = (ComboBox)sender;
            var behavior = GetBehavior(owner);
            behavior?.ItemsSourceChanged?.Invoke(owner, e);
        }
        #endregion

        // Helpers

        public const string PART_EditableTextBox = "PART_EditableTextBox";

        public static TextBox FindEditableTextBox(ComboBox comboBox)
        {
            return (TextBox)VisualTreeModule.FindChild(comboBox, PART_EditableTextBox);
        }

        public static string GetItemText(ComboBox comboBox, object item)
        {
            if (item == null) return string.Empty;

            var d = new DependencyVariable<string>();
            d.SetBinding(item, TextSearch.GetTextPath(comboBox));
            return d.Value ?? string.Empty;
        }

        private static int CountWithMax<T>(IEnumerable<T> source, Predicate<T> predicate, int maxCount)
        {
            var count = 0;
            foreach (var item in source)
            {
                if (predicate(item))
                {
                    count++;
                    if (count > maxCount) return count;
                }
            }
            return count;
        }
    }
}
