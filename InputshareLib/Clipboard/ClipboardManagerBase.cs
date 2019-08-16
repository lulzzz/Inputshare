using InputshareLib.Clipboard.DataTypes;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Clipboard
{
    public abstract class ClipboardManagerBase
    {
        public event EventHandler<ClipboardDataBase> ClipboardContentChanged;

        public bool Running { get; protected set; }

        public abstract void Start();
        public abstract void Stop();

        public abstract void SetClipboardData(ClipboardDataBase data);

        protected void OnClipboardDataChanged(ClipboardDataBase data)
        {
            ClipboardContentChanged?.Invoke(this, data);
        }
    }
}
