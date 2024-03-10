using System.Windows;

namespace GKH;

public class App(MainWindow mainWindow) : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        mainWindow.Show();
        base.OnStartup(e);
    }
}