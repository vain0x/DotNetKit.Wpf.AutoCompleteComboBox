using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
        public AutoCompleteBehavior Behavior { get; set; }

        public AutoCompleteComboBox()
        {
            InitializeComponent();

            Behavior = new AutoCompleteBehavior(this)
            {
                GetSetting = () => Setting,
            };
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

        // see #26
        #region ItemsSourceExtended
        public static readonly DependencyProperty ItemsSourceExtendedProperty =
            DependencyProperty.Register(nameof(ItemsSourceExtended), typeof(IEnumerable), typeof(AutoCompleteComboBox),
                new PropertyMetadata(null, ItemsSourceExtendedPropertyChanged));
        public IEnumerable ItemsSourceExtended
        {
            get
            {
                return (IEnumerable)GetValue(ItemsSourceExtendedProperty);
            }
            set
            {
                SetValue(ItemsSourceExtendedProperty, value);
            }
        }

        private static void ItemsSourceExtendedPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs dpcea)
        {
            var comboBox = (ComboBox)dependencyObject;
            var previousSelectedItem = comboBox.SelectedItem;

            if (dpcea.NewValue is ICollectionView cv)
            {
                //((AutoCompleteComboBox)dependencyObject).defaultItemsFilter = cv.Filter;
                ((AutoCompleteComboBox)dependencyObject).Behavior.defaultItemsFilter = cv.Filter;
                comboBox.ItemsSource = cv;
            }
            else
            {
                //((AutoCompleteComboBox)dependencyObject).defaultItemsFilter = null;
                ((AutoCompleteComboBox)dependencyObject).Behavior.defaultItemsFilter = null;
                IEnumerable newValue = dpcea.NewValue as IEnumerable;
                CollectionViewSource newCollectionViewSource = new CollectionViewSource
                {
                    Source = newValue
                };
                comboBox.ItemsSource = newCollectionViewSource.View;
            }

            comboBox.SelectedItem = previousSelectedItem;

            // if ItemsSource doesn't contain previousSelectedItem
            if (comboBox.SelectedItem != previousSelectedItem)
            {
                comboBox.SelectedItem = null;
            }
        }
        #endregion ItemsSourceExtended
    }
}
