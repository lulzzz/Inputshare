using InputshareLib.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace InputshareLib.Output
{
    public interface IOutputManager
    {
        public void Send(ISInputData input);

        public void ResetKeyStates();
    }
}
