using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using System.Collections.Generic;

namespace shaedyBounty;

public class ShaedyBounty : BasePlugin
{
    public override string ModuleName => "shaedy Bounty";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "shaedy";

    private const int KillsForBounty = 5;
    private const int BonusPoints = 5;

    private Dictionary<ulong, int> _killStreaks = new();

    // Hardcoded prefix
    private string _prefix = $" {ChatColors.White}[{ChatColors.Green}shaedy-Bounty{ChatColors.White}]";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        // Optionally reset streaks each round:
        // _killStreaks.Clear();
        return HookResult.Continue;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (victim == null || !victim.IsValid) return HookResult.Continue;

        // Check if the victim had a bounty
        if (_killStreaks.ContainsKey(victim.SteamID) && _killStreaks[victim.SteamID] >= KillsForBounty)
        {
            if (attacker != null && attacker.IsValid && attacker != victim && !attacker.IsBot)
            {
                Server.PrintToChatAll($"{_prefix} {ChatColors.Green}{attacker.PlayerName} {ChatColors.White}claimed the bounty on {ChatColors.Red}{victim.PlayerName}!");
                Server.ExecuteCommand($"css_ranks_addpoints {attacker.SteamID} {BonusPoints}");
            }
            else
            {
                Server.PrintToChatAll($"{_prefix} {ChatColors.White}The bounty on {ChatColors.Red}{victim.PlayerName} {ChatColors.White}was lost.");
            }
        }

        _killStreaks[victim.SteamID] = 0;

        // Increment attacker streak
        if (attacker != null && attacker.IsValid && !attacker.IsBot && attacker != victim)
        {
            if (!_killStreaks.ContainsKey(attacker.SteamID)) _killStreaks[attacker.SteamID] = 0;

            _killStreaks[attacker.SteamID]++;
            int currentStreak = _killStreaks[attacker.SteamID];

            if (currentStreak == KillsForBounty)
            {
                Server.PrintToChatAll($"{_prefix} {ChatColors.Red}{attacker.PlayerName} {ChatColors.White}is on a {ChatColors.Red}{currentStreak} kill streak! {ChatColors.Gold}KILL HIM FOR BONUS MMR!");
            }
        }

        return HookResult.Continue;
    }
}