"use strict";


$(function () {
    const pendingBidsQueue = new Queue();
    let listOfBidsLoaded = false;
    let currentBid = { amount: 0};
    let userId = "";
    const apiBaseUrl = "/api/bids/";
    const connection = new signalR.HubConnectionBuilder()
                            .withUrl("/bidhub")
                            .withAutomaticReconnect()
                            .build();
    signalrStart();

    async function signalrStart() {
        try {
            await connection.start();
            console.assert(connection.state === signalR.HubConnectionState.Connected);
            console.log("SignalR Connected.");
        } catch (err) {
            console.assert(connection.state === signalR.HubConnectionState.Disconnected);
            console.log(err);
            setTimeout(() => start(), 5000);
        }
    };

    connection.on("ReceiveBid", function (bid) {
        if (listOfBidsLoaded === true) {
            currentBid = bid;
            $("#amount").val(currentBid.amount + 1000);
            $("#bidsTable > tbody").prepend(createRowHtml(bid));
        } else {
            pendingBidsQueue.enqueue(bid);
        }
    });

    connection.onreconnecting(error => {
        console.assert(connection.state === signalR.HubConnectionState.Reconnecting);
        disableBidding();
        $("#reconnecting-alert").show();
    });

    connection.onreconnected(connectionId => {
        console.assert(connection.state === signalR.HubConnectionState.Connected);
        enableBidding();
        $("#reconnecting-alert").hide();
        $("#reconnected-alert").hide();

    });

    connection.onclose(error => {
        console.assert(connection.state === signalR.HubConnectionState.Disconnected);
        disableBidding();
        $("#disconnected-alert").show();
    });

    $("#connectButton").click(function () {

        userId = $("#userId").prop("disabled", true).val();
        $("#auctionId").prop("disabled", true);
        const selectedAuction = $("#auctionId option:selected");
        const auctionId = $(selectedAuction).val();
        const expiry = $(selectedAuction).data("expiry");;
        const auctionExpiry = new Date(expiry);
        const flipDown = new FlipDown(auctionExpiry.getTime() / 1000);
        flipDown.start();
        flipDown.ifEnded(onBidExpired);

        subscribeToAuction(auctionId);
        getBids(auctionId);
        $("#bid-container").show();
        $("#connectButton").hide();
    });

    $("#placeButton").click(function () {

        const amount = $("#amount").val();
        const auctionId = $("#auctionId").val();

        if (amount <= currentBid.amount) {
            alert("amount should be more than max bid");
        } else if (userId === currentBid.userId) {
            alert("You are the highest bidder");
        } else {
            const bid = {
                UserId: userId,
                Amount: Number(amount),
                AuctionId: auctionId
            };

            $.ajax({
                url: apiBaseUrl,
                dataType: "json",
                type: "POST",
                data: JSON.stringify(bid),
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    console.log("successfully placed a bid", result);
                },
                error: function (err) {
                    console.error(err.responseText);
                    alert("failed to add request");
                }
            });

            $("#amount").val(currentBid.amount + 1000);
        }

        event.preventDefault();
    });

    function subscribeToAuction(auctionId) {
        connection.invoke("subscribeToAuction", auctionId).catch(function (err) {
            return console.error(err.toString());
        });
    }

    function getBids(auctionId) {
        $("#loading").show();
        $.ajax({
            url: apiBaseUrl + auctionId,
            dataType: "json",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            success: function (response) {
                console.log("received list", response);
                bindBids(response);
            },
            error: function (err) {
                $("#loading").hide();
                alert("failed to get bids from server");
                console.error(err.responseText);
            }
        });
    }

    function bindBids(bids) {

        const rows = [];
        if (bids != null && bids.length > 0) {
            mergeAndPrepareBids(bids);
            bids.forEach(function (bid, index) {
                rows.push(createRowHtml(bid));
                if (index === 0) {
                    currentBid = bid;
                }
            });
        }
        //setTimeout(function () {

        // double checking if a element is pending during execution
        while (pendingBidsQueue.isEmpty() !== true) {
            const bid = pendingBidsQueue.dequeue();
            currentBid = bid;
            rows.unshift(createRowHtml(bid));
        }
        // using pure javascript to create list of elements as it is faster than jquery append https://howchoo.com/code/learn-the-slow-and-fast-way-to-append-elements-to-the-dom
        document.getElementById("bidsTable").getElementsByTagName("tbody")[0].innerHTML = rows.join("");
        listOfBidsLoaded = true;
        $("#loading").hide();
        $("#bidsTable").show();
        if (currentBid.amount) {
            $("#amount").val(currentBid.amount + 1000);
        }
        enableBidding();
        //},5000);
    }

    function createRowHtml(bid) {
        const css = userId === bid.userId ? "my-bid-row" : "";
        return `<tr class="${css}" >
                    <td>${bid.userId}</td>
                    <td>${(new Date(bid.createdAt)).toLocaleString()}</td>
                    <td>£ ${bid.amount}</td>
                </tr>`;
    }

    function mergeAndPrepareBids(bids) {
        // sort
        bids.sort(function (a, b) {
            return new Date(b.createdAt) - new Date(a.createdAt);
        });

        // append pending items
        while (pendingBidsQueue.isEmpty() !== true) {
            bids.unshift(pendingBidsQueue.dequeue());
        }

        // remove duplicate
        removeDuplicates(bids, "bidId");
    }

    function removeDuplicates(myArr, prop) {
        return myArr.filter((obj, pos, arr) => {
            return arr.map(mapObj => mapObj[prop]).indexOf(obj[prop]) === pos;
        });
    }

    function onBidExpired() {
        disableBidding();
    }

    function disableBidding() {
        $("#placeButton").prop("disabled", true);
    }

    function enableBidding() {
        $("#placeButton").prop("disabled", false);
    }
});




