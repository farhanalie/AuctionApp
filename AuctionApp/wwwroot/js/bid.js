"use strict";


$(function () {
    const pendingBidsQueue = new Queue();
    let listOfBidsLoaded = false;
    const apiBaseUrl = "/api/bids/";
    const connection = new signalR.HubConnectionBuilder().withUrl("/bidhub").build();

    //Disable send button until connection is established
    $("#placeButton").prop("disabled", true);

    connection.on("ReceiveBid", function (bid) {
        console.log(bid);
        if (listOfBidsLoaded === true) {
            $("#bidsTable > tbody").prepend(createRowHtml(bid));
        } else {
            pendingBidsQueue.enqueue(bid);
        }
    });

    connection.start().then(function () {
        console.log("connection established");
        $("#placeButton").prop("disabled", false);
    }).catch(function (err) {
        return console.error(err.toString());
    });

    $("#connectButton").click(function () {
        $("#loading").show();
        $("#userId").prop("disabled", true);
        const auctionId = $("#auctionId").prop("disabled", true).val();
        subscribeToAuction(auctionId);
        getBids(auctionId);
        $("#bid-container").show();
        $(this).hide();
    });

    $("#placeButton").click(function () {

        const userId = $("#userId").val();
        const amount = $("#amount").val();
        const auctionId = $("#auctionId").val();

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

        $("#amount").val("");
        event.preventDefault();
    });

    function subscribeToAuction(auctionId) {
        connection.invoke("subscribeToAuction", auctionId).catch(function (err) {
            return console.error(err.toString());
        });
    }

    function getBids(auctionId) {
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
            });
        }
        //setTimeout(function () {

        // double checking if a element is pending during execution
        while (pendingBidsQueue.isEmpty() !== true) {
            rows.unshift(createRowHtml(pendingBidsQueue.dequeue()));
        }
        // using pure javascript to create list of elements as it is faster than jquery append https://howchoo.com/code/learn-the-slow-and-fast-way-to-append-elements-to-the-dom
        document.getElementById("bidsTable").getElementsByTagName("tbody")[0].innerHTML = rows.join("");
        listOfBidsLoaded = true;
        $("#loading").hide();
        $("#bidsTable").show();

        //},5000);
    }

    function createRowHtml(bid) {
        return `<tr>
                    <td>£ ${bid.amount}</td>
                    <td>${bid.userId}</td>
                    <td>${bid.createdAt}</td>
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
});




