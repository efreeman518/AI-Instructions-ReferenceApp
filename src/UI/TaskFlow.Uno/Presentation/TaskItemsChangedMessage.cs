namespace TaskFlow.Uno.Presentation;

internal sealed record TaskItemsChangedMessage(bool ResetToFirstPage = false);

internal sealed record TaskFormResetMessage;