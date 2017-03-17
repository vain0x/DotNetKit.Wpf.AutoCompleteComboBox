using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetKit.Windows.Controls
{
    /// <summary>
    /// Represents an object to configure <see cref="AutoCompleteComboBox"/>.
    /// </summary>
    public abstract class AutoCompleteComboBoxSetting
    {
        /// <summary>
        /// Gets a filter function which determines whether items should be suggested or not
        /// for the specified query.
        /// Default: Gets the filter which maps an item to <c>true</c>
        /// if its text contains the query (case insensitive).
        /// </summary>
        /// <param name="query">
        /// The string input by user.
        /// </param>
        /// <param name="stringFromItem">
        /// The function to get a string which identifies the specified item.
        /// </param>
        /// <returns></returns>
        public abstract Func<object, bool>
            GetFilter(string query, Func<object, string> stringFromItem);

        /// <summary>
        /// Gets an integer.
        /// The combobox opens the drop down
        /// if the number of suggested items is less than the value.
        /// Note that the value is larger, it's heavier to open the drop down.
        /// Default: 100.
        /// </summary>
        public abstract int MaxSuggestionCount { get; }

        /// <summary>
        /// Gets the duration to delay updating the suggestion list.
        /// Returns <c>Zero</c> if no delay.
        /// Default: 300ms.
        /// </summary>
        public abstract TimeSpan Delay { get; }

        #region Default
        /// <summary>
        /// Provides a default implementation of <see cref="AutoCompleteComboBoxSetting"/>.
        /// </summary>
        public class DefaultImplementation
            : AutoCompleteComboBoxSetting
        {
            /// <summary>
            /// Gets the default filter.
            /// </summary>
            /// <param name="query"></param>
            /// <param name="stringFromItem"></param>
            /// <returns></returns>
            public override Func<object, bool>
                GetFilter(string query, Func<object, string> stringFromItem)
            {
                return
                    item =>
                        stringFromItem(item).ToLowerInvariant()
                        .Contains(query.ToLowerInvariant());
            }

            /// <summary>
            /// Gets the default value.
            /// </summary>
            public override int MaxSuggestionCount
            {
                get { return 100; }
            }

            /// <summary>
            /// Gets the default delay.
            /// </summary>
            public override TimeSpan Delay
            {
                get { return TimeSpan.FromMilliseconds(300.0); }
            }
        }

        static readonly DefaultImplementation @default = new DefaultImplementation();

        /// <summary>
        /// Gets the default setting.
        /// </summary>
        public static DefaultImplementation Default
        {
            get { return @default; }
        }
        #endregion
    }
}
