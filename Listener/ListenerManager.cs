using System.Collections.Generic;

namespace TownOfHost.Listener;

public class ListenerManager
{
    private static readonly List<IListener> Listeners = new();

    public static void RegisterListener(IListener listener)
    {
        Listeners.Add(listener);
    }

    public static List<IListener> GetListeners()
    {
        return Listeners;
    }

}