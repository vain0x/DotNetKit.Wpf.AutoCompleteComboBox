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
        /// Gets a filter which determines whether items should be suggested or not
        /// for the specified query.
        /// </summary>
        /// <param name="query">
        /// The string the user input.
        /// </param>
        /// <param name="stringFromItem">
        /// The function to gets a string with which identifies the specified item.
        /// </param>
        /// <returns></returns>
        public abstract Func<object, bool>
            GetFilter(string query, Func<object, string> stringFromItem);

        #region Default
        /// <summary>
        /// Provides a default implementation of <see cref="AutoCompleteComboBoxSetting"/>.
        /// </summary>
        public class DefaultImplementation
            : AutoCompleteComboBoxSetting
        {
            /// <summary>
            /// Gets a filter.
            /// The function maps an item to <c>true</c>
            /// if its text contains the query (case insensitive, ignoring surrounding spaces).
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
