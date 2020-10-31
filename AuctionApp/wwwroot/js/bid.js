"use strict";


$(function () {
    const pendingBidsQueue = new Queue();
    let listOfBidsLoaded = false;
    let currentBid = { amount: 0 };
    const auction = { auctionId: null, buyNowPrice: null, buyNowThresholdPrice: null, expiry: null, winnerUserId: null };
    let userId = "";
    const apiBidsUrl = "/api/bids/";
    const apiAuctionsUrl = "/api/auctions/";

    const connection = new signalR.HubConnectionBuilder()
                            .withUrl("/bidhub")
                            .withAutomaticReconnect()
                            .build();
    signalrStart();

    connection.on("ReceiveBid", function (bid) {
        if (listOfBidsLoaded === true) {
            currentBid = bid;
            $("#amount").val(currentBid.amount + 1000);
            $("#bidsTable > tbody").prepend(createRowHtml(bid));
            bindCurrentBid();
            bindReserveAndBuyNowPrice();
        } else {
            pendingBidsQueue.enqueue(bid);
        }
    });

    connection.on("AuctionClosed", onAuctionClosed);

    connection.onreconnecting(error => {
        console.assert(connection.state === signalR.HubConnectionState.Reconnecting);
        disableBidding();
        $("#error-alert").text("Connection to auction is lost. we are trying to connect you.").show();

    });

    connection.onreconnected(connectionId => {
        console.assert(connection.state === signalR.HubConnectionState.Connected);
        enableBidding();
        $("#error-alert").hide();
        $("#reconnected-alert").show();
    });

    connection.onclose(error => {
        console.assert(connection.state === signalR.HubConnectionState.Disconnected);
        disableBidding();
        $("#error-alert").text("Connection to auction is lost. Try refreshing this page to restart the connection.").show();
    });

    $("#connectButton").click(function () {
        subscribeToAuction();
    });

    $("#placeButton").click(function () {

        const amount = $("#amount").val();
        auction.auctionId = $("#auctionId").val();

        if (amount <= currentBid.amount) {
            alert("amount should be more than max bid");
        } else if (userId === currentBid.userId) {
            alert("You are already the highest bidder");
        } else {
            const bid = {
                UserId: userId,
                Amount: Number(amount),
                AuctionId: auction.auctionId
            };

            $.ajax({
                url: apiBidsUrl,
                dataType: "json",
                type: "POST",
                data: JSON.stringify(bid),
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    console.log("successfully placed a bid", result);
                },
                error: function (err) {
                    console.error(err.responseText);
                    if (err.responseJSON && err.responseJSON.Error.StatusCode === 400) {
                        alert(err.responseJSON.Error.Message);
                    } else {
                        alert("failed to add request");
                    }
                }
            });

            $("#amount").val(currentBid.amount + 1000);
        }

        event.preventDefault();
    });

    $("#buyNowButton").click(function () {

        if (currentBid.amount >= auction.buyNowPrice) {
            alert("Auction cannot be buy now, as it already have a equal or greater bid");
        } else {
            disableBidding();
            auction.winnerUserId = userId;
            $.ajax({
                url: apiAuctionsUrl,
                dataType: "json",
                type: "POST",
                data: JSON.stringify(auction),
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    console.log("successful buy now", result);
                    $("#flipdown").hide();
                },
                error: function (err) {
                    enableBidding();
                    console.error(err.responseText);
                    if (err.responseJSON && err.responseJSON.Error.StatusCode === 400) {
                        alert(err.responseJSON.Error.Message);
                    } else {
                        alert("failed to add request");
                    }
                }
            });
        }
    });

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

    async function subscribeToAuction() {
        try {
            const selectedAuction = $("#auctionId option:selected");
            auction.auctionId = $(selectedAuction).val();
            await connection.invoke("subscribeToAuction", auction.auctionId);

            userId = $("#userId").prop("disabled", true).val();
            $("#auctionId").prop("disabled", true);
            auction.expiry = $(selectedAuction).data("expiry");
            const auctionExpiry = new Date(auction.expiry);
            const flipDown = new FlipDown(auctionExpiry.getTime() / 1000);
            flipDown.start();
            //flipDown.ifEnded(onAuctionClosed);
            getBids();

            
        } catch (err) {
            console.error(err);
            $("#error-alert").text("Connection to auction is lost. Try refreshing this page to restart the connection.").show();
        }
    }

    function getBids() {
        $("#loading").show();
        $.ajax({
            url: apiBidsUrl + auction.auctionId,
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

        bindCurrentBid();
        bindReserveAndBuyNowPrice();

        $("#bid-container").show();
        $("#connectButton").hide();

        enableBidding();
        //},5000);
    }

    function bindCurrentBid() {
        $("#current-bid").text(currentBid.amount);
        const currentBidContainer = $("#current-bid-container").val(currentBid.amount);
        if (userId === currentBid.userId) {
            currentBidContainer.children(":first").removeClass("badge-secondary").addClass("badge-success");
        } else {
            currentBidContainer.children(":first").removeClass("badge-success").addClass("badge-secondary");
        }
        currentBidContainer.show();
    }

    function bindReserveAndBuyNowPrice() {
        const selected = $("#auctionId option:selected");
        const reservePrice = selected.data("reserve-price");
        if (reservePrice) {
            $("#reserve-price").text(reservePrice);
            const reservePriceContainer = $("#reserve-price-container").val(reservePrice);
            if (currentBid.amount >= reservePrice) {
                reservePriceContainer.children(":first").removeClass("badge-secondary").addClass("badge-success");
            } else {
                reservePriceContainer.children(":first").removeClass("badge-success").addClass("badge-secondary");
            }
            reservePriceContainer.show();
        }

        // buy now
        auction.buyNowPrice = selected.data("buynow-price");
        if (auction.buyNowPrice) {
            auction.buyNowThresholdPrice = selected.data("buynow-threshold-price");
            if (currentBid.amount < auction.buyNowThresholdPrice) {
                $("#buyNowButton").show().children(":first").text(auction.buyNowPrice);
            } else {
                $("#buyNowButton").hide();
            }
        }

        
    }

    function createRowHtml(bid) {
        const css = userId === bid.userId ? "my-bid-row" : "";
        return `<tr class="${css}" >
                    <td>${userId === bid.userId ? "You" : bid.userId}</td>
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

    function onAuctionClosed(winnerUserId) {
        disableBidding();
        if (winnerUserId) {
            const u = userId === winnerUserId ? "You" : winnerUserId;
            alert(u+" won the auction");
        } else {
            alert("Auction is closed. No one placed a bid or reach the reserve price");
        }
    }

    function disableBidding() {
        $("#placeButton").prop("disabled", true);
    }

    function enableBidding() {
        $("#placeButton").prop("disabled", false);
    }
});




