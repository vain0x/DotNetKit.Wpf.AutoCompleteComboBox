using System;
using System.Windows.Controls;

namespace DotNetKit.Windows.Controls
{
    public struct TextBoxStatePreserver : IDisposable
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
}
