<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Ticker.aspx.cs" Inherits="RabbitMQSignalR.Ticker" %>
<table id="FxPrices">
    <thead>
        <tr>
            <th>Currency pair</th>
            <th>Sell</th>
            <th>Buy</th>
        </tr>
    </thead>
    <tbody>
        <tr class="loading"><td colspan="5">loading...</td></tr>
    </tbody>
</table>

<script src="~/signalr/hubs"></script>
<script src='js/FxTicker.js'></script>
