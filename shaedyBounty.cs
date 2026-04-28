using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Utils;
using ShaedyHudManager;

namespace shaedyBounty;

public class ShaedyBounty : BasePlugin
{
    private const int KillsForBounty = 5;
    private const int BonusPoints = 5;

    private Dictionary<ulong, int> _killStreaks = new();
    private HashSet<ulong> _hasBounty = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _hudTimer;

    private string _prefix = string.Concat(" ", ChatColors.White, "[", ChatColors.Green, "shaedy-Bounty", ChatColors.White, "]");

    public override string ModuleName => "shaedy Bounty";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "shaedy";

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);

        Console.WriteLine("[shaedyBounty] v1.2.0 loaded.");
    }

    private HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
    {
        _killStreaks.Clear();
        _hasBounty.Clear();
        StopHudTimer();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event, GameEventInfo info)
    {
        StopHudTimer();
        return HookResult.Continue;
    }

    private void StartHudTimer()
    {
        StopHudTimer();
        _hudTimer = AddTimer(1.0f, OnHudTick, CounterStrikeSharp.API.Modules.Timers.TimerFlags.REPEAT);
    }

    private void StopHudTimer()
    {
        _hudTimer?.Kill();
        _hudTimer = null;
    }

    private void OnHudTick()
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player == null || !player.IsValid || !player.PawnIsAlive || player.IsBot)
                continue;

            if (_killStreaks.ContainsKey(player.SteamID))
            {
                int streak = _killStreaks[player.SteamID];
                if (streak >= 3)
                {
                    ShowStreakCounter(player, streak);
                }
            }

            if (_hasBounty.Contains(player.SteamID))
            {
                ShowBountyWarning(player);
            }
        }
    }

    private void ShowStreakCounter(CCSPlayerController player, int streak)
    {
        int needed = KillsForBounty - streak;

        string dotsHtml = "";
        for (int i = 0; i < KillsForBounty; i++)
        {
            string dotColor = i < streak ? "#ff4444" : "#333";
            dotsHtml += "<span style='font-size:16px;color:" + dotColor + ";margin:0 2px;'>*</span>";
        }

        string color = streak >= KillsForBounty ? "#ff4444" : streak >= 4 ? "#ff8800" : "#ffcc00";
        string label = streak >= KillsForBounty ? "BOUNTY ACTIVE" : streak >= 4 ? "ALMOST BOUNTY" : "STREAK";

        string extraInfo = "";
        if (_hasBounty.Contains(player.SteamID))
            extraInfo = " - <span style='color:#ff4444;'>BOUNTY ON YOU</span>";
        else if (needed > 0)
            extraInfo = " (" + needed + " to bounty)";

        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:12px;color:#888;letter-spacing:2px;'>" + label + "</div>";
        html += "<div style='font-size:24px;font-weight:bold;color:" + color + ";text-shadow:0 0 10px " + color + ";margin-top:2px;'>" + dotsHtml + "</div>";
        html += "<div style='font-size:14px;color:#ccc;margin-top:2px;'>" + streak + "/" + KillsForBounty + " kills" + extraInfo + "</div>";
        html += "</div></body></html>";

        HudManager.Show(player.SteamID, html, HudPriority.Low, 1);
    }

    private void ShowBountyWarning(CCSPlayerController player)
    {
        var players = Utilities.GetPlayers();
        foreach (var other in players)
        {
            if (other == null || !other.IsValid || !other.PawnIsAlive || other.IsBot || other == player || other.TeamNum == player.TeamNum)
                continue;

            string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'><div style='font-size:14px;color:#ff4444;text-shadow:0 0 10px #ff4444;'>!! " + player.PlayerName + " has a bounty! (+" + BonusPoints + " MMR)</div></div></body></html>";

            HudManager.Show(other.SteamID, html, HudPriority.High, 1);
        }
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        var victim = @event.Userid;
        var attacker = @event.Attacker;

        if (victim == null || !victim.IsValid) return HookResult.Continue;

        if (_killStreaks.ContainsKey(victim.SteamID) && _killStreaks[victim.SteamID] >= KillsForBounty)
        {
            if (attacker != null && attacker.IsValid && attacker != victim && !attacker.IsBot)
            {
                Server.PrintToChatAll(_prefix + " " + ChatColors.Green + attacker.PlayerName + " " + ChatColors.White + "claimed the bounty on " + ChatColors.Red + victim.PlayerName + "!");
                Server.ExecuteCommand("css_ranks_addpoints " + attacker.SteamID + " " + BonusPoints);

                ShowBountyClaimedFlash(attacker, victim.PlayerName);
            }
            else
            {
                Server.PrintToChatAll(_prefix + " " + ChatColors.White + "The bounty on " + ChatColors.Red + victim.PlayerName + " " + ChatColors.White + "was lost.");
            }

            _hasBounty.Remove(victim.SteamID);
        }

        _killStreaks[victim.SteamID] = 0;
        _hasBounty.Remove(victim.SteamID);

        if (attacker != null && attacker.IsValid && !attacker.IsBot && attacker != victim)
        {
            if (!_killStreaks.ContainsKey(attacker.SteamID)) _killStreaks[attacker.SteamID] = 0;

            _killStreaks[attacker.SteamID]++;
            int currentStreak = _killStreaks[attacker.SteamID];

            if (currentStreak == KillsForBounty)
            {
                Server.PrintToChatAll(_prefix + " " + ChatColors.Red + attacker.PlayerName + " " + ChatColors.White + "is on a " + ChatColors.Red + currentStreak + " kill streak! " + ChatColors.Gold + "KILL HIM FOR BONUS MMR!");

                _hasBounty.Add(attacker.SteamID);

                if (_hudTimer == null)
                    StartHudTimer();
            }
            else if (currentStreak >= 3 && _hudTimer == null)
            {
                StartHudTimer();
            }
        }

        if (_killStreaks.Values.All(s => s < 3) && _hasBounty.Count == 0)
        {
            StopHudTimer();
        }

        return HookResult.Continue;
    }

    private void ShowBountyClaimedFlash(CCSPlayerController attacker, string victimName)
    {
        string html = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'>";
        html += "<div style='font-size:28px;font-weight:bold;color:#ffd700;text-shadow:0 0 20px #ffd700;'>* BOUNTY CLAIMED *</div>";
        html += "<div style='font-size:16px;color:#ccc;margin-top:4px;'>Eliminated " + victimName + " - +" + BonusPoints + " MMR</div>";
        html += "</div></body></html>";

        HudManager.Show(attacker.SteamID, html, HudPriority.High, 3);

        var allPlayers = Utilities.GetPlayers();
        foreach (var p in allPlayers)
        {
            if (p != attacker && p.IsValid && !p.IsBot)
            {
                string notifyHtml = "<html><body style='margin:0;padding:0;'><div style='text-align:center;font-family:Arial;'><div style='font-size:16px;color:#ffd700;'>* " + attacker.PlayerName + " claimed the bounty!</div></div></body></html>";
                HudManager.Show(p.SteamID, notifyHtml, HudPriority.Low, 2);
            }
        }
    }
}
