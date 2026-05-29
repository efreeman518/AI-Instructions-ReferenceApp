namespace TaskFlow.Uno.Presentation;

/// <summary>Drives task items changed state, navigation, and commands for the Uno presentation layer.</summary>
internal sealed record TaskItemsChangedMessage(bool ResetToFirstPage = false);

/// <summary>Drives task form reset state, navigation, and commands for the Uno presentation layer.</summary>
internal sealed record TaskFormResetMessage;