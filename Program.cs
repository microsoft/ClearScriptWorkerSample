using System;
using System.Threading.Tasks;
using Microsoft.ClearScript.V8;

namespace ClearScriptWorkerSample
{
    class Program
    {
        public static void Main()
        {
            V8ScriptEngine EngineFactory()
            {
                var engine = new V8ScriptEngine(V8ScriptEngineFlags.EnableTaskPromiseConversion);
                engine.AddHostType(typeof(Console));
                engine.Script.delay = new Func<int, Task>(Task.Delay);
                return engine;
            }

            using var gameImpl = new WorkerImpl(EngineFactory);
            gameImpl.PostExecuteDocument("Game.js");

            gameImpl.PostCommandObject(gameImpl.GetCanonicalJson("{ begin: { playerNames: [ 'Venus', 'Serena' ] } }"));
            gameImpl.WaitForExitAsync().Wait();
        }
    }
}
