using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Clipboard.DataTypes;

namespace InputshareLib.DragDrop
{
    public class NullDragDropManager : IDragDropManager
    {
        public bool Running { get; private set; }

        public bool LeftMouseState { get; } = false;

        public event EventHandler DragDropCancelled;
        public event EventHandler DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<ClipboardDataBase> DataDropped;

        public void CancelDrop()
        {

        }

        public void CheckForDrop()
        {

        }

        public void DoDragDrop(ClipboardDataBase data)
        {


        }

        public void Start()
        {
            Running = true;
        }

        public void Stop()
        {
            Running = false;
        }
    }
}
