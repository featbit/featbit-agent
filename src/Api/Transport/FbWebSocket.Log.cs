using System.Net.WebSockets;

namespace Api.Transport;

internal sealed partial class FbWebSocket
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Information, "Starting establish websocket connection with url: {Url}",
            EventName = "Starting")]
        public static partial void Starting(ILogger logger, Uri url);

        [LoggerMessage(2, LogLevel.Information, "Websocket connection established.",
            EventName = "Connected")]
        public static partial void Connected(ILogger logger);

        [LoggerMessage(3, LogLevel.Warning, "Websocket connection timed out after {timeoutInSeconds} seconds.",
            EventName = "TimedOut")]
        public static partial void TimedOut(ILogger logger, double timeoutInSeconds);

        [LoggerMessage(4, LogLevel.Error, "Exception occurred while establishing websocket connection.",
            EventName = "ErrorStarting")]
        public static partial void ErrorStarting(ILogger logger, Exception exception);

        [LoggerMessage(5, LogLevel.Debug, "Starting keep alive timer.", EventName = "StartingKeepAliveTimer")]
        public static partial void StartingKeepAliveTimer(ILogger logger);

        [LoggerMessage(6, LogLevel.Debug, "Invoking the {HandlerName} event handler.",
            EventName = "InvokingEventHandler")]
        public static partial void InvokingEventHandler(ILogger logger, string handlerName);

        [LoggerMessage(14, LogLevel.Debug, "Stopping keep alive timer.", EventName = "StoppingKeepAliveTimer")]
        public static partial void StoppingKeepAliveTimer(ILogger logger);

        [LoggerMessage(15, LogLevel.Warning,
            "FbWebSocket is trying to reconnect due to an exception. Flag evaluation results may be stale until reconnected.",
            EventName = "ReconnectingWithError")]
        public static partial void ReconnectingWithError(ILogger logger, Exception exception);

        [LoggerMessage(16, LogLevel.Warning,
            "FbWebSocket is trying to reconnect. Flag evaluation results may be stale until reconnected.",
            EventName = "Reconnecting")]
        public static partial void Reconnecting(ILogger logger);

        [LoggerMessage(17, LogLevel.Warning, "Give up reconnecting due to the close status {CloseStatus}.",
            EventName = "GiveUpReconnect")]
        public static partial void GiveUpReconnect(ILogger logger, WebSocketCloseStatus? closeStatus);

        [LoggerMessage(18, LogLevel.Information, "Reconnect attempt number {RetryTimes} will start in {RetryDelay}.",
            EventName = "AwaitingReconnectRetryDelay")]
        public static partial void AwaitingReconnectRetryDelay(ILogger logger, long retryTimes, TimeSpan retryDelay);

        [LoggerMessage(19, LogLevel.Warning, "Connection stopped during reconnect delay. Done reconnecting.",
            EventName = "ReconnectingStoppedDuringRetryDelay")]
        public static partial void ReconnectingStoppedDuringRetryDelay(ILogger logger);

        [LoggerMessage(20, LogLevel.Information,
            "FbWebSocket reconnected successfully after {RetryAttempt} attempts and {ElapsedSeconds}s elapsed.",
            EventName = "Reconnected")]
        public static partial void Reconnected(ILogger logger, long retryAttempt, double elapsedSeconds);

        [LoggerMessage(21, LogLevel.Debug, "Reconnect attempt failed.", EventName = "ReconnectAttemptFailed")]
        public static partial void ReconnectAttemptFailed(ILogger logger, Exception exception);

        [LoggerMessage(22, LogLevel.Warning, "Connection stopped during reconnect attempt. Done reconnecting.",
            EventName = "ReconnectingStoppedDuringReconnectAttempt")]
        public static partial void ReconnectingStoppedDuringReconnectAttempt(ILogger logger);

        [LoggerMessage(23, LogLevel.Information, "Shutting down connection.", EventName = "ShuttingDown")]
        public static partial void ShuttingDown(ILogger logger);

        [LoggerMessage(24, LogLevel.Error, "Connection is shutting down with an error.",
            EventName = "ShuttingDownWithError")]
        public static partial void ShuttingDownWithError(ILogger logger, Exception exception);

        [LoggerMessage(29, LogLevel.Warning,
            "FbWebSocket closed abnormally with status: {Status}, description: {Description}.",
            EventName = "AbnormallyClosed")]
        public static partial void AbnormallyClosed(ILogger logger, WebSocketCloseStatus? status, string? description);

        [LoggerMessage(30, LogLevel.Warning, "Cannot send message due to invalid websocket state: {State}",
            EventName = "CannotSend")]
        public static partial void CannotSend(ILogger logger, WebSocketState? state);

        [LoggerMessage(31, LogLevel.Error, "Exception occurred while sending message.",
            EventName = "SendError")]
        public static partial void SendError(ILogger logger, Exception exception);

        [LoggerMessage(32, LogLevel.Error, "Exception occurred while closing websocket connection.",
            EventName = "CloseError")]
        public static partial void CloseError(ILogger logger, Exception exception);
    }
}