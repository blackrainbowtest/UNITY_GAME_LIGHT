//   ____  ____  _     ___ _____ ____  _   _    _    ____  ____    ____ _____ _   _ ____ ___ ___  
//  / ___||  _ \| |   |_ _|_   _/ ___|| | | |  / \  |  _ \|  _ \  / ___|_   _| | | |  _ \_ _/ _ \ 
//  \___ \| |_) | |    | |  | | \___ \| |_| | / _ \ | |_) | | | | \___ \ | | | | | | | | | | | | |
//   ___) |  __/| |___ | |  | |  ___) |  _  |/ ___ \|  _ <| |_| |  ___) || | | |_| | |_| | | |_| |
//  |____/|_|   |_____|___| |_| |____/|_| |_/_/   \_\_| \_\____/  |____/ |_|  \___/|____/___\___/ 
//
/* ******************************************************************************************************** */
/*                                                                                                          */
/*   File: Assets/Scripts/Battle/BattleLocationContext.cs                                                   */
/*                                                        /\_/\                                             */
/*                                                       ( •.• )                                            */
/*   By: unluckydungeonadventure.gmail.com                > ^ <                                             */
/*                                                                                                          */
/*   Created: 2026/01/24 by UDA                                                                             */
/*                                                                                                          */
/* ******************************************************************************************************** */

using UnityEngine;

/// <summary>
/// Контекст для передачи выбранной локации боя между сценами.
/// One-shot: сбрасывается после Consume.
/// </summary>
public static class BattleLocationContext
{
    private static Game.Battle.BattleLocationData selectedLocation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        selectedLocation = null;
    }

    public static void Set(Game.Battle.BattleLocationData location)
    {
        selectedLocation = location;
    }

    public static Game.Battle.BattleLocationData Consume()
    {
        var result = selectedLocation;
        selectedLocation = null;
        return result;
    }

    public static Game.Battle.BattleLocationData Peek() => selectedLocation;
}
