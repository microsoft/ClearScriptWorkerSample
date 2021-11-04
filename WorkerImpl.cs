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
        private readonly ScriptObject _worker;

        public WorkerImpl(Func<ScriptEngine> engineFactory)
        {
            Engine = (_engineFactory = engineFactory)();
            Init(null);
        }

        private WorkerImpl(WorkerImpl parent, ScriptObject worker)
        {
            Engine = (_engineFactory = parent._engineFactory)();
            _worker = worker;
            Init(parent);
        }

        public ScriptEngine Engine { get; }

        public object ParseJson(string json) => Engine.Script.JSON.parse(json);
        public string GetCanonicalJson(string json) => (string)Engine.Evaluate($"JSON.stringify({json})");

        public void PostExecuteCode(string code) => _messageQueue.Enqueue(new ExecuteCodeMessage(code));
        public void PostExecuteDocument(string url) => _messageQueue.Enqueue(new ExecuteDocumentMessage(url));
        public void PostCommandString(string command, ScriptObject sourceWorker = null) => _messageQueue.Enqueue(new CommandStringMessage(command, sourceWorker));
        public void PostCommandObject(string json, ScriptObject sourceWorker = null) => _messageQueue.Enqueue(new CommandObjectMessage(json, sourceWorker));

        public void PostExit() => _messageQueue.Enqueue(new ExitMessage());
        public Task WaitForExitAsync() => _exitSource.Task;

        public WorkerImpl CreateChild(ScriptObject worker) => new WorkerImpl(this, worker);

        public async Task FireEventAsync(ScriptObject sourceWorker, string name, params object[] args)
        {
            var funcName = "on" + name;
            if (sourceWorker?.GetProperty(funcName) is ScriptObject workerFunc)
            {
                await InvokeFuncAsync(workerFunc, args);
            }
            else if (Engine.Script[funcName] is ScriptObject globalFunc)
            {
                await InvokeFuncAsync(globalFunc, args);
            }
        }

        private void Init(WorkerImpl parent)
        {
            Engine.DocumentSettings.AccessFlags |= DocumentAccessFlags.EnableAllLoading;
            ((ScriptObject)Engine.Evaluate(@"(function (impl) {
                Worker = function (url) {
                    const childImpl = impl.CreateChild(this);
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
                Engine.Script.postCommandString = new Action<string>(command => parent.PostCommandString(command, _worker));
                Engine.Script.postCommandObject = new Action<object>(obj => parent.PostCommandObject(Engine.Script.JSON.stringify(obj), _worker));
            }

            Engine.Script.postExit = new Action(PostExit);
            Task.Run(MessageLoop);
        }

        private async Task MessageLoop()
        {
            while (true)
            {
                var message = await _messageQueue.DequeueAsync();
                if (!await message.HandleAsync(this))
                {
                    break;
                }
            }

            _exitSource.SetResult();
        }

        private async Task InvokeFuncAsync(ScriptObject func, params object[] args)
        {
            var result = func.Invoke(false, args);
            if (result is Task task)
            {
                await task;
            }
        }

        public void Dispose()
        {
            PostExit();
            WaitForExitAsync().Wait();
            Engine.Dispose();
        }
    }
}
