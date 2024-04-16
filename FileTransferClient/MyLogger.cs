using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Windows.Forms;


namespace FileTransferClient
{
    public class LogWindow : Form
    {
        private TextBox logTextBox;

        public LogWindow()
        {
            Text = "Application Logs";
            Size = new System.Drawing.Size(400, 300);

            logTextBox = new TextBox();
            logTextBox.Multiline = true;
            logTextBox.ScrollBars = ScrollBars.Vertical;
            logTextBox.Dock = DockStyle.Fill;

            Controls.Add(logTextBox);
        }

        public void LogMessage(string message)
        {
            logTextBox.AppendText(message + Environment.NewLine);
        }
    }

    public class FormTarget : TargetWithLayout
    {
        private LogWindow _form = new LogWindow();

        public void Show(string title)
        {
            if (_form == null || _form.IsDisposed)
            {
                _form = new LogWindow();
            }
            _form.Text = title;
            _form.Show();
        }

        

        protected override void Write(LogEventInfo logEvent)
        {
            if (_form != null && !_form.IsDisposed && _form.IsHandleCreated)
            {
                _form.Invoke((Action)(() =>
                {
                    try
                    {
                        // Write to the form logEvent using the layout
                        _form.LogMessage(this.Layout.Render(logEvent));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error logging message: {ex.Message}");
                    }
                }));
            }
        }

    }

    class MyLogger
    {
        public static Logger Log { get; } = LogManager.GetLogger(" - ");
        public static FormTarget logWindow;
        private static LoggingConfiguration config = new LoggingConfiguration();
        private static string layoutTemplate = "${longdate} - ${level:uppercase=true} - ${message:withexception=true}";

        static MyLogger()
        {
            logWindow = new FormTarget();
            // Targets where to log to: File and logWindow
            FileTarget Infologfile = new FileTarget("logfile") { FileName = "Info.log" };
            Infologfile.Layout = layoutTemplate;

            FileTarget Errorlogfile = new FileTarget("logfile") { FileName = "Error.log" };
            Errorlogfile.Layout = layoutTemplate;

            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Info, Infologfile);
            config.AddRule(LogLevel.Error, LogLevel.Fatal, Errorlogfile);

            LoggingRule rule = new LoggingRule("*", LogLevel.Trace, LogLevel.Fatal, logWindow);
            logWindow.Layout = layoutTemplate;
            config.LoggingRules.Add(rule);

            // Apply config           
            LogManager.Configuration = config;
        }
    }

}















