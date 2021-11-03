using System;
using System.Threading.Tasks;
using Microsoft.ClearScript;

// ReSharper disable UnusedMember.Global

namespace ClearScriptWorkerSample
{
    public class WorkerImpl : IDisposable
    {
        private readonly Func<ScriptEngine> _engineFactory;
        private readonly AsyncQueue<IMessage> _messageQueue = new AsyncQueue<IMessage>();
        private readonly TaskCompletionSource _exitSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public WorkerImpl(Func<ScriptEngine> engineFactory)
        {
            Engine = (_engineFactory = engineFactory)();
            Init(null);
        }

        private WorkerImpl(WorkerImpl parent)
        {
            Engine = (_engineFactory = parent._engineFactory)();
            Init(parent);
        }

        public ScriptEngine Engine { get; }

        public object ParseJson(string json) => Engine.Script.JSON.parse(json);
        public string GetCanonicalJson(string json) => (string)Engine.Evaluate($"JSON.stringify({json})");

        public void PostExecuteCode(string code) => _messageQueue.Enqueue(new ExecuteCodeMessage(code));
        public void PostExecuteDocument(string url) => _messageQueue.Enqueue(new ExecuteDocumentMessage(url));
        public void PostCommandString(string command) => _messageQueue.Enqueue(new CommandStringMessage(command));
        public void PostCommandObject(string json) => _messageQueue.Enqueue(new CommandObjectMessage(json));

        public void PostExit() => _messageQueue.Enqueue(new ExitMessage());
        public Task WaitForExitAsync() => _exitSource.Task;

        public WorkerImpl CreateChild() => new WorkerImpl(this);

        public void FireEvent(string name, params object[] args)
        {
            if (Engine.Script["on" + name] is ScriptObject function)
            {
                function.Invoke(false, args);
            }
        }

        private void Init(WorkerImpl parent)
        {
            Engine.DocumentSettings.AccessFlags |= DocumentAccessFlags.EnableAllLoading;
            ((ScriptObject)Engine.Evaluate(@"(function (impl) {
                Worker = function (url) {
                    const childImpl = impl.CreateChild();
                    this.postExecuteCode = childImpl.PostExecuteCode;
                    this.postExecuteDocument = childImpl.PostExecuteDocument;
                    this.postCommandString = childImpl.PostCommandString;
                    this.postCommandObject = obj => childImpl.PostCommandObject(JSON.stringify(obj));
                    this.terminate = childImpl.Dispose;
                    if (typeof(url) === 'string') {
                        this.postExecuteDocument(url);
                    }
                }
            })")).Invoke(false, this);

            if (parent != null)
            {
                Engine.Script.postCommandString = new Action<string>(parent.PostCommandString);
                Engine.Script.postCommandObject = new Action<object>(obj => parent.PostCommandObject(Engine.Script.JSON.stringify(obj)));
            }

            Engine.Script.postExit = new Action(PostExit);
            Task.Run(MessageLoop);
        }

        private async Task MessageLoop()
        {
            while (true)
            {
                var message = await _messageQueue.DequeueAsync();
                if (!message.Handle(this))
                {
                    break;
                }
            }

            _exitSource.SetResult();
        }

        public void Dispose()
        {
            PostExit();
            WaitForExitAsync().Wait();
            Engine.Dispose();
        }
    }
}
