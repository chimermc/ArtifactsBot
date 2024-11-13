namespace ArtifactsBot.Services.Models;

public readonly record struct FightSimulatorResult(bool IsWin, int Turns, int WithHealingTurns, int RemainingHp);
