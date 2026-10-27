namespace Yahtzee;

/// <summary>Application entry point. Standard WinForms bootstrap.</summary>
internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new UI.MainForm());
    }
}