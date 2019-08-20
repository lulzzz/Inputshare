using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Displays
{
    public class NullDisplayManager : DisplayManagerBase
    {
        public override void StartMonitoring()
        {
            Running = true;
        }

        public override void StopMonitoring()
        {
            Running = false;
        }

        public override void UpdateConfigManual()
        {
            CurrentConfig = new DisplayConfig(new System.Drawing.Rectangle(0, 0, 1024, 768), new List<Display>());
        }
    }
}
