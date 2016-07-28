// A simple templating method for replacing placeholders enclosed in curly braces.
if (!String.prototype.supplant) {
    String.prototype.supplant = function (o) {
        return this.replace(/{([^{}]*)}/g,
            function (a, b) {
                var r = o[b];
                return typeof r === 'string' || typeof r === 'number' ? r : a;
            }
        );
    };
}

$(function () {

    var ticker = $.connection.fxTickerMini, // the generated client-side hub proxy
        $priceTable = $('#FxPrices'),
        $priceTableBody = $priceTable.find('tbody'),
        rowTemplate = '<tr data-symbol="{Key}"><td>{Symbol}</td><td class="{BidDirectionClass}">{Bid}</td><td class="{AskDirectionClass}">{Ask}</td></tr>';

    function formatCurrency(currency) {
        return $.extend(currency, {
            Ask:
            	currency.Ask == 0 ? "<small>---</small>" :
            	currency.Ask > 1 ? parseFloat(currency.Ask).toPrecision(6) : parseFloat(currency.Ask).toPrecision(5),
            Bid:
            	currency.Bid == 0 ? "<small>---</small>" :
            	currency.Bid > 1 ? parseFloat(currency.Bid).toPrecision(6) : parseFloat(currency.Bid).toPrecision(5),
            AskDirectionClass: currency.AskPriceChange === 0 ? '' : currency.AskPriceChange >= 0 ? "price-up" : "price-down",
            BidDirectionClass: currency.BidPriceChange === 0 ? '' : currency.BidPriceChange >= 0 ? "price-up" : "price-down",
        });
    }

    function init() {
        ticker.server.GetAllPrices().done(function (prices) {
            $priceTableBody.empty();
            $.each(prices, function () {
                var currency = formatCurrency(this);
                $priceTableBody.append(rowTemplate.supplant(currency));
            });
        });
    }

    // Add a client-side hub method that the server will call
    ticker.client.updateFxPrice = function (currency) {
        var displayCurrency = formatCurrency(currency),
            $row = $(rowTemplate.supplant(displayCurrency));
        $priceTableBody.find('tr[data-symbol=' + currency.Key + ']')
            .replaceWith($row);
    }

    ticker.client.removeRows = function (currency) {
        $priceTableBody.find('tr[data-symbol=' + currency.Key + ']').remove();
    }

    // Start the connection
    $.connection.hub.start().done(init);

    $.connection.hub.disconnected(function () {
        setTimeout(function () {
            $.connection.hub.start();
        }, 5000); // Restart connection after 5 seconds.
    });
});