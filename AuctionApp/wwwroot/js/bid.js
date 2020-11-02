"use strict";
$(function () {
    const pendingBidsQueue = new Queue();
    let listOfBidsLoaded = false;
    let userMaxBidAmount = null;
    let currentBid = { amount: 0 };
    const auction = { auctionId: null, buyNowPrice: null, buyNowThresholdPrice: null, expiry: null, winnerUserId: null };
    let userId = "";
    const apiBidsUrl = "/api/bids/";
    const apiAuctionsUrl = "/api/auctions/";
    let countDownTimer = null;

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/bidhub")
        .withAutomaticReconnect()
        .build();
    signalrStart();

    connection.on("ReceiveBid", function (bid, extendedExpiredAt) {
        if (listOfBidsLoaded === true) {
            currentBid = bid;
            currentBid["extendedExpiredAt"] = extendedExpiredAt;
            $("#amount").val(currentBid.amount + 1000);
            $("#bidsTable > tbody").prepend(createRowHtml(bid));
            bindControls();
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

    $("#maxBidButton").click(function () {

        const amount = $("#amount").val();
        auction.auctionId = $("#auctionId").val();

        if (amount <= currentBid.amount) {
            alert("amount should be more than max bid");
        } else if (amount <= userMaxBidAmount) {
            alert("Please enter amount greater than your current max amount");
        }
        // Todo: discuss with client
        //else if (userId === currentBid.userId) {
        //    alert("You are already the highest bidder");
        //}
        else {
            const userMaxBid = {
                UserId: userId,
                MaxBid: Number(amount),
                AuctionId: auction.auctionId
            };

            $.ajax({
                url: apiAuctionsUrl + "SetMaxBid",
                dataType: "json",
                type: "POST",
                data: JSON.stringify(userMaxBid),
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    console.log("successfully placed a max bid", result);
                    // users max amount
                    userMaxBidAmount = result;
                    $("#user-max-bid").text(userMaxBidAmount).parent().removeClass("badge-danger").addClass("badge-success").closest("h2").show();
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
                url: apiAuctionsUrl + "BuyNow",
                dataType: "json",
                type: "POST",
                data: JSON.stringify(auction),
                contentType: "application/json; charset=utf-8",
                success: function (result) {
                    console.log("successful buy now", result);
                    //$("#flipdown").hide();
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
            const openingBid = $(selectedAuction).data("opening-bid");
            if (openingBid) {
                $("#amount").val(openingBid);
            }
            auction.auctionId = $(selectedAuction).val();

            await connection.invoke("subscribeToAuction", auction.auctionId);

            userId = $("#userId").prop("disabled", true).val();

            getMyMaxBid();

            $("#auctionId").prop("disabled", true);
            auction.expiry = $(selectedAuction).data("expiry");
            countDownTimer = new Date(auction.expiry);
            initializeCountDown(countDownEnds);
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

    function getMyMaxBid() {
        $.ajax({
            url: apiAuctionsUrl + "GetMaxBid" + "/" + auction.auctionId + "/" + userId,
            dataType: "json",
            type: "GET",
            contentType: "application/json; charset=utf-8",
            success: function (response) {
                console.log("receive user max bid", response);
                userMaxBidAmount = response;
                $("#user-max-bid").text(userMaxBidAmount).parent().removeClass("badge-danger").addClass("badge-success").closest("h2").show();
            },
            error: function (err) {
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

        bindControls();

        $("#bid-container").show();
        $("#connectButton").hide();

        enableBidding();
        //},5000);
    }

    function bindControls() {

        $("#current-bid").text(currentBid.amount);
        const currentBidContainer = $("#current-bid-container").val(currentBid.amount);
        if (userId === currentBid.userId) {
            currentBidContainer.children(":first").removeClass("badge-danger").addClass("badge-success");
        } else {
            currentBidContainer.children(":first").removeClass("badge-success").addClass("badge-danger");
        }
        currentBidContainer.show();

        const selected = $("#auctionId option:selected");
        const reservePrice = selected.data("reserve-price");
        if (reservePrice) {
            $("#reserve-price").text(reservePrice);
            const reservePriceContainer = $("#reserve-price-container").val(reservePrice);
            if (currentBid.amount >= reservePrice) {
                reservePriceContainer.children(":first").removeClass("badge-danger").addClass("badge-success");
            } else {
                reservePriceContainer.children(":first").removeClass("badge-success").addClass("badge-danger");
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

        // users max amount
        if (currentBid.amount > userMaxBidAmount) {
            $("#user-max-bid").parent().removeClass("badge-success").addClass("badge-danger").closest("h2").show();
        } else {
            $("#user-max-bid").parent().removeClass("badge-danger").addClass("badge-success").closest("h2").show();
        }

        // extended expiry
        if (currentBid.extendedExpiredAt) {
            if (getSecondsDiff(new Date(currentBid.extendedExpiredAt), new Date(auction.expiry))>0) {
                countDownTimer = new Date(currentBid.extendedExpiredAt);
                $("#extension-time-container").show();
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
            alert(u + " won the auction");
        } else {
            alert("Auction is closed. No one placed a bid or reach the reserve price");
        }
    }

    function disableBidding() {
        $("#placeButton").prop("disabled", true);
        $("#buyNowButton").prop("disabled", true);
        $("#maxBidButton").prop("disabled", true);
    }

    function enableBidding() {
        $("#placeButton").prop("disabled", false);
        $("#buyNowButton").prop("disabled", false);
        $("#maxBidButton").prop("disabled", false);
    }

    function countDownEnds() {
        console.log("time ends");
        //var extendedDate = new Date(currentBid.extendedExpiredAt);
        //if (getSecondsDiff(new Date(), extendedDate )<=60) {
        //    // extension period
        //    countDownTimer = extendedDate;
        //    initializeCountDown(countDownEnds);
        //}
    }

    /*count down*/
    function getTimeRemaining(endtime) {
        const total = Date.parse(endtime) - Date.parse(new Date());
        const seconds = Math.floor((total / 1000) % 60);
        const minutes = Math.floor((total / 1000 / 60) % 60);
        const hours = Math.floor((total / (1000 * 60 * 60)) % 24);
        const days = Math.floor(total / (1000 * 60 * 60 * 24));

        return {
            total,
            days,
            hours,
            minutes,
            seconds
        };
    }

    function initializeCountDown(callback) {
        $("#count-down-container").show();
        const clock = document.getElementById("count-down-container");
        const daysSpan = clock.querySelector('.days');
        const hoursSpan = clock.querySelector('.hours');
        const minutesSpan = clock.querySelector('.minutes');
        const secondsSpan = clock.querySelector('.seconds');

        function updateClock() {
            const t = getTimeRemaining(countDownTimer);

            daysSpan.innerHTML = t.days;
            hoursSpan.innerHTML = ("0" + t.hours).slice(-2);
            minutesSpan.innerHTML = ("0" + t.minutes).slice(-2);
            secondsSpan.innerHTML = ("0" + t.seconds).slice(-2);

            if (t.total <= 0) {
                callback();
                clearInterval(timeInterval);
            }
        }

        updateClock();
        const timeInterval = setInterval(updateClock, 1000);
    }

    function getSecondsDiff(date1, date2) {
        const dif = date1 - date2;
        return Math.floor(Math.abs((dif / 1000)));
    }
});




