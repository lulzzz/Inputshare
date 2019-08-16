using InputshareLib.Cursor;
using InputshareLib.Displays;
using InputshareLib.DragDrop;
using InputshareLib.Input;
using InputshareLib.Output;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Server
{
    /// <summary>
    /// OS specifid depedencies required to run an inputshare server
    /// </summary>
    public sealed class ISServerDependencies
    {
        public DisplayManagerBase DisplayManager { get; set; }
        public InputManagerBase InputManager { get; set; }
        public CursorMonitorBase CursorMonitor { get; set; }
        public IDragDropManager DragDropManager { get; set; }
        
        public IOutputManager OutputManager { get; set; }
    }
}
