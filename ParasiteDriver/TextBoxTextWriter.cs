using System;
using System.IO;
using System.Text;
using System.Windows.Controls.Primitives;

namespace ParasiteDriver
{
    public class TextBoxTextWriter : TextWriter
    {
        private TextBoxBase _textBox;

        public TextBoxTextWriter(TextBoxBase textBox)
        {
            _textBox = textBox;
        }

        public override void Write(char value)
        {
            Action appendMessage = delegate ()
            {
                _textBox.AppendText(value.ToString());
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

        public override void Write(string value)
        {
            Action appendMessage = delegate ()
            {
                _textBox.AppendText(string.Format("[CONSOLE] [{0}] ", DateTime.Now.ToString()));
                _textBox.AppendText(value + Environment.NewLine);
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

        public override Encoding Encoding
        {
            get { return Encoding.Unicode; }
        }
    }
}
