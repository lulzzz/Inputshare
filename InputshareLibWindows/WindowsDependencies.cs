using InputshareLib.Client;
using InputshareLib.Server;
using InputshareLibWindows.Clipboard;
using InputshareLibWindows.Cursor;
using InputshareLibWindows.Displays;
using InputshareLibWindows.DragDrop;
using InputshareLibWindows.Input;
using InputshareLibWindows.Output;

namespace InputshareLibWindows
{
    public static class WindowsDependencies
    {
        public static ISServerDependencies GetServerDependencies()
        {
            return new ISServerDependencies
            {
                DisplayManager = new WindowsDisplayManager(),
                CursorMonitor = new WindowsCursorMonitor(),
                DragDropManager = new WindowsDragDropManager(),
                InputManager = new WindowsInputManager(),
                OutputManager = new WindowsOutputManager()
            };
        }

        public static ClientDependencies GetClientDependencies()
        {
            return new ClientDependencies
            {
                clipboardManager = new WindowsClipboardManager(),
                cursorMonitor = new WindowsCursorMonitor(),
                displayManager = new WindowsDisplayManager(),
                dragDropManager = new WindowsDragDropManager(),
                outputManager = new WindowsOutputManager()
            };
        }
    }
}
