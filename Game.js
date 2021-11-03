// ReSharper disable UseOfImplicitGlobalInFunctionScope
// ReSharper disable AssignToImplicitGlobalInFunctionScope

shots = ['Ping', 'Pong'];
serverIndex = shotCount = pointCount = 0;
onCommandObject = async function (command) {
    if (command.begin) {
        players = [];
        for (let index = 0; index < 2; ++index) {
            players.push({ name: command.begin.playerNames[index], score: 0, worker: new Worker('Player.js') });
            players[index].worker.postCommandObject({ setPlayerId: { playerId: index + 1 } });
        }
        Console.WriteLine(`First server: ${players[serverIndex = (Math.random() < 0.5) ? 0 : 1].name}.`);
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
