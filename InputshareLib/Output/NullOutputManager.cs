using System;
using System.Collections.Generic;
using System.Text;
using InputshareLib.Input;

namespace InputshareLib.Output
{
    public class NullOutputManager : IOutputManager
    {
        public bool Running { get; private set; }

        public void ResetKeyStates()
        {

        }

        public void Send(ISInputData input)
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
