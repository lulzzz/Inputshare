using InputshareLib.Input;

namespace InputshareLib.Output
{
    public interface IOutputManager
    {
        public bool Running { get; }

        public void Start();
        public void Stop();

        public void Send(ISInputData input);

        public void ResetKeyStates();
    }
}
