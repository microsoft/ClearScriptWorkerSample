using System;
using System.Threading.Tasks;
using Microsoft.ClearScript;

namespace ClearScriptWorkerSample
{
    internal interface IMessage
    {
        Task<bool> HandleAsync(WorkerImpl impl);
    }

    internal class ExecuteCodeMessage : IMessage
    {
        private readonly string _code;

        public ExecuteCodeMessage(string code) => _code = code;

        Task<bool> IMessage.HandleAsync(WorkerImpl impl)
        {
            try
            {
                impl.Engine.Execute(_code);
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in ExecuteCode message handler: " + exception);
                return Task.FromResult(false);
            }
        }
    }

    internal class ExecuteDocumentMessage : IMessage
    {
        private readonly string _url;

        public ExecuteDocumentMessage(string url) => _url = url;

        Task<bool> IMessage.HandleAsync(WorkerImpl impl)
        {
            try
            {
                impl.Engine.ExecuteDocument(_url);
                return Task.FromResult(true);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in ExecuteDocument message handler: " + exception);
                return Task.FromResult(false);
            }
        }
    }

    internal class CommandStringMessage : IMessage
    {
        private readonly ScriptObject _sourceWorker;
        private readonly string _command;

        public CommandStringMessage(string command, ScriptObject sourceWorker)
        {
            _sourceWorker = sourceWorker;
            _command = command;
        }

        async Task<bool> IMessage.HandleAsync(WorkerImpl impl)
        {
            try
            {
                await impl.FireEventAsync(_sourceWorker, "CommandString", _command);
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
        private readonly ScriptObject _sourceWorker;
        private readonly string _json;

        public CommandObjectMessage(string json, ScriptObject sourceWorker)
        {
            _sourceWorker = sourceWorker;
            _json = json;
        }

        async Task<bool> IMessage.HandleAsync(WorkerImpl impl)
        {
            try
            {
                await impl.FireEventAsync(_sourceWorker, "CommandObject", impl.ParseJson(_json));
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
        async Task<bool> IMessage.HandleAsync(WorkerImpl impl)
        {
            try
            {
                await impl.FireEventAsync(null, "Exit");
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception in Exit message handler: " + exception);
            }

            return false;
        }
    }
}
