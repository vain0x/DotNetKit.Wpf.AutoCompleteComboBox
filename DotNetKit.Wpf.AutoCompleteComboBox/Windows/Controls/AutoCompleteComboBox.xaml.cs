using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DotNetKit.Misc.Disposables;
using DotNetKit.Windows.Media;

namespace DotNetKit.Windows.Controls
{
    /// <summary>
    /// AutoCompleteComboBox.xaml の相互作用ロジック
    /// </summary>
    public partial class AutoCompleteComboBox : ComboBox
    {
        readonly SerialDisposable disposable = new SerialDisposable();

        TextBox editableTextBoxCache;
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

        TimeSpan ResponseDelay
        {
            get { return TimeSpan.FromMilliseconds(500.0); }
        }

        /// <summary>
        /// Gets text to match with the query from an item.
        /// Never null.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        string TextFromItem(object item)
        {
            if (item == null) return "";

            var d = new DependencyVariable<string>();
            d.SetBinding(item, TextSearch.GetTextPath(this));
            return d.Value ?? "";
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
        #endregion

        #region OnTextChanged
        long revisionId;

        static int CountWithMax<X>(IEnumerable<X> xs, Func<X, bool> predicate, int maxCount)
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

        bool SeemsBackspacing(string text, int count)
        {
            return
                count == 1
                && SelectedItem != null
                && TextFromItem(SelectedItem) != text;
        }

        void Unselect()
        {
            var textBox = EditableTextBox;
            textBox.Select(textBox.SelectionStart + textBox.SelectionLength, 0);
        }

        void UpdateSuggestionList()
        {
            var text = Text;

            if (string.IsNullOrEmpty(text))
            {
                IsDropDownOpen = false;

                using (Items.DeferRefresh())
                {
                    Items.Filter = null;
                }

                SelectedItem = null;
            }
            else if (SelectedItem != null && TextFromItem(SelectedItem) == text)
            {
                // It seems the user selected an item.
                // Do nothing.
            }
            else
            {
                var setting = Setting ?? AutoCompleteComboBoxSetting.Default;
                var filter = setting.GetFilter(text, TextFromItem);
                var maxCount = setting.MaxSuggestionCount;
                var count = CountWithMax(ItemsSource.Cast<object>(), filter, maxCount);

                if (count > maxCount) return;
                if (SeemsBackspacing(text, count)) return;

                using (Items.DeferRefresh())
                {
                    Items.Filter = item => filter(item);
                }

                IsDropDownOpen = true;
                Unselect();
            }
        }

        void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var id = unchecked(++revisionId);

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
                    ResponseDelay,
                    Timeout.InfiniteTimeSpan
                );
        }
        #endregion

        public AutoCompleteComboBox()
        {
            InitializeComponent();

            AddHandler(
                TextBoxBase.TextChangedEvent,
                new TextChangedEventHandler(OnTextChanged)
            );
        }
    }
}
