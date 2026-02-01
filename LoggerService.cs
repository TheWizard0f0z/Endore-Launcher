using System;
using System.Windows.Controls;

namespace AktualizatorEME.Services
{
    public class LoggerService
    {
        private readonly TextBox _statusEditor;

        public LoggerService(TextBox statusEditor)
        {
            _statusEditor = statusEditor;
        }

        public void LogMessage(string message)
        {
            _statusEditor.Dispatcher.Invoke(() =>
            {
                _statusEditor.AppendText($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}\n");
                _statusEditor.ScrollToEnd();
            });
        }
    }
}
