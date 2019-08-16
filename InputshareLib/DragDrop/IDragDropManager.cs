using InputshareLib.Clipboard.DataTypes;
using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.DragDrop
{
    public interface IDragDropManager
    {
        public event EventHandler DragDropCancelled;
        public event EventHandler DragDropSuccess;
        public event EventHandler<Guid> DragDropComplete;
        public event EventHandler<ClipboardDataBase> DataDropped;
        public abstract bool Running { get; }
        public abstract bool LeftMouseState { get; }

        public abstract void CancelDrop();
        public abstract void Start();
        public abstract void Stop();
        public abstract void CheckForDrop();
        public abstract void DoDragDrop(ClipboardDataBase data);
    }
}
