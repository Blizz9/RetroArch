using System;
using System.Diagnostics;
using System.Windows.Controls.Primitives;

namespace ParasiteDriver
{
    public class TextBoxTraceListener : TraceListener
    {
        private TextBoxBase _textBox;

        public TextBoxTraceListener(TextBoxBase textBox)
        {
            _textBox = textBox;
        }

        public override void Write(string message)
        {
            Action appendMessage = delegate ()
            {
                _textBox.AppendText(string.Format("[DEBUG] [{0}] ", DateTime.Now.ToString()));
                _textBox.AppendText(message);
            };

            if (!_textBox.Dispatcher.CheckAccess())
            {
                _textBox.Dispatcher.BeginInvoke(appendMessage);
            }
            else
            {
                appendMessage();
            }
        }

        public override void WriteLine(string message)
        {
            Write(message + Environment.NewLine);
        }
    }
}
