using InputshareLib.Input;

namespace InputshareLib.Output
{
    public interface IOutputManager
    {
        public void Send(ISInputData input);

        public void ResetKeyStates();
    }
}
