using System;

namespace ClearScriptWorkerSample
{
    internal interface IMessage
    {
        bool Handle(WorkerImpl impl);
    }

    internal class ExecuteCodeMessage : IMessage
    {
        private readonly string _code;

        public ExecuteCodeMessage(string code) => _code = code;

        bool IMessage.Handle(WorkerImpl impl)
        {
            try
            {
                impl.Engine.Execute(_code);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in ExecuteCode message handler: " + exception);
                return false;
            }
        }
    }

    internal class CommandStringMessage : IMessage
    {
        private readonly string _command;

        public CommandStringMessage(string command) => _command = command;

        bool IMessage.Handle(WorkerImpl impl)
        {
            try
            {
                impl.FireEvent("CommandString", _command);
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in CommandString message handler: " + exception);
                return false;
            }
        }
    }

    internal class CommandObjectMessage : IMessage
    {
        private readonly string _json;

        public CommandObjectMessage(string json) => _json = json;

        bool IMessage.Handle(WorkerImpl impl)
        {
            try
            {
                impl.FireEvent("CommandObject", impl.ParseJson(_json));
                return true;
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in CommandObject message handler: " + exception);
                return false;
            }
        }
    }

    internal class ExitMessage : IMessage
    {
        bool IMessage.Handle(WorkerImpl impl)
        {
            try
            {
                impl.FireEvent("Exit");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in Exit message handler: " + exception);
            }

            return false;
        }
    }

}
