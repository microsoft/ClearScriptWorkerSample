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

            const string gameCode = @"
                shots = [ 'Ping', 'Pong' ];
                serverIndex = shotCount = pointCount = 0;
                onCommandObject = async function (command) {
                    if (command.begin) {
                        players = [];
                        for (let index = 0; index < 2; ++index) {
                            players.push({ name: command.begin.playerNames[index], score: 0, worker: new Worker() });
                            players[index].worker.postExecuteCode(playerCode);
                            players[index].worker.postCommandObject({ setPlayerId: { playerId: index + 1 } });
                        }
                        serverIndex = (Math.random() < 0.5) ? 0 : 1;
                        players[serverIndex].worker.postCommandObject({ serve: {} });
                    }
                    else if (command.serve) {
                        let index = command.serve.playerId - 1;
                        Console.WriteLine(`${players[index].name} serves: ${shots[shotCount = 0]}`);
                        players[index ^ 1].worker.postCommandObject({ handleShot: {} });
                    }
                    else if (command.makeShot) {
                        let index = command.makeShot.playerId - 1;
                        Console.WriteLine(`${players[index].name}: ${shots[++shotCount & 1]}`);
                        players[index ^ 1].worker.postCommandObject({ handleShot: {} });
                    }
                    else if (command.miss) {
                        let index = command.miss.playerId - 1;
                        Console.Write(`${players[index].name} misses.`);
                        const thisScore = players[index].score;
                        const otherScore = ++players[index ^ 1].score;
                        if ((otherScore >= 11) && ((otherScore - thisScore) >= 2)) {
                            Console.WriteLine(` ${players[index ^ 1].name} wins! Final score: ${players[index ^ 1].score}-${players[index].score}.`);
                            postExit();
                        } else {
                            if ((++pointCount % 2) === 0) {
                                Console.Write(` New server: ${players[serverIndex ^= 1].name}.`);
                            }
                            Console.WriteLine(` Score: ${players[serverIndex].score}-${players[serverIndex ^ 1].score}.`);
                            await delay(500);
                            players[serverIndex].worker.postCommandObject({ serve: {} });
                        }
                    }
                };
                onExit = function () {
                    for (let player of players) {
                        player.worker.terminate();
                    }
                };
            ";

            const string playerCode = @"
                onCommandObject = async function (command) {
                    if (command.setPlayerId) {
                        myPlayerId = command.setPlayerId.playerId;
                    }
                    else if (command.serve) {
                        postCommandObject({ serve: { playerId: myPlayerId } });
                    }
                    else if (command.handleShot) {
                        await delay(500);
                        if (Math.random() >= 0.25) {
                            postCommandObject({ makeShot: { playerId: myPlayerId } });
                        } else {
                            postCommandObject({ miss: { playerId: myPlayerId } });
                        }
                    }
                };
            ";

            using var gameImpl = new WorkerImpl(EngineFactory);
            gameImpl.Engine.Script.playerCode = playerCode;
            gameImpl.PostExecuteCode(gameCode);

            gameImpl.PostCommandObject(gameImpl.GetCanonicalJson("{ begin: { playerNames: [ 'Venus', 'Serena' ] } }"));
            gameImpl.WaitForExitAsync().Wait();
        }
    }
}
