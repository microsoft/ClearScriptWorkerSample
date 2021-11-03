// ReSharper disable UseOfImplicitGlobalInFunctionScope
// ReSharper disable AssignToImplicitGlobalInFunctionScope

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
