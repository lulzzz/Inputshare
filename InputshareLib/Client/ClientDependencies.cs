using InputshareLib.Clipboard;
using InputshareLib.Cursor;
using InputshareLib.Displays;
using InputshareLib.DragDrop;
using InputshareLib.Output;
namespace InputshareLib.Client
{
    public class ClientDependencies
    {
        public IOutputManager outputManager { get; set; }
        public ClipboardManagerBase clipboardManager { get; set; }
        public CursorMonitorBase cursorMonitor { get; set; }
        public DisplayManagerBase displayManager { get; set; }
        public IDragDropManager dragDropManager { get; set; }
    }
}
