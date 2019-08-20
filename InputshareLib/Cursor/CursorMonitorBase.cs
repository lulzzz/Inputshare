using System;
using System.Drawing;

namespace InputshareLib.Cursor
{
    public abstract class CursorMonitorBase
    {
        public event EventHandler<Edge> EdgeHit;
        protected Rectangle virtualDisplayBounds;
        public bool Running { get; protected set; }
        public abstract void StartMonitoring(Rectangle bounds);
        public abstract void StopMonitoring();
        public virtual void SetBounds(Rectangle bounds)
        {
            virtualDisplayBounds = bounds;
        }

        protected virtual void HandleEdgeHit(Edge edge)
        {
            EdgeHit?.Invoke(this, edge);
        }
    }
}
