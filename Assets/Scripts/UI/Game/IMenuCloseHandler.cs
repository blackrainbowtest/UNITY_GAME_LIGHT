using System;

public interface IMenuCloseHandler
{
    /// <summary>
    /// Event that should be invoked when the menu is closed.
    /// </summary>
    event Action OnMenuClosed;
}
