const api = "http://localhost:5050/"; // Eventually replace this with env variable resolution 

const createGameEndpoint = api + "CreateGame";
export const fetchCreateGame = (userId : string): string => {

    let gameID = "-1"; //Fail Check  

    fetch(createGameEndpoint, {
        method: "POST",
        headers: {
            "content-type": "application/json",
        },
        body: JSON.stringify({
            userId: userId,
        }),
    })
    .then((response => {
        if (!response.ok) {
            throw new Error("Network response was not ok");
        }
        return response.json(); // Parse JSON response
    }))
    .then((data) => {
        gameID = data.gameId; // Extract gameId from the response
        console.log("Response Content:", data); // Log the response content
    })
    .catch((error) => {
        console.error("Fetch error:", error);
    });

    return gameID;
}

const tryMakeMoveEndpoint = api + "TryMakeMove";
export const fetchTryMakeMove = (gameId : string, fromIndex : number, toIndex : number) => {
  
    const moveRequest = {
        GameId: gameId,
        FromIndex: fromIndex,
        ToIndex: toIndex,
    };

    fetch(tryMakeMoveEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify(moveRequest)
    })
    .catch((error) => {
        console.error("Fetch error:", error);
    });
}

const joinGameEndpoint = api + "JoinGame";
export const fetchJoinGame = (gameId : string, userId : string) => {
    fetch(joinGameEndpoint, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            GameId: gameId,
            UserId: userId,
        })
    })
    .catch((error) => {
        console.error("Fetch error:", error);
    });
}

