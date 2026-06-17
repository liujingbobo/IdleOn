using System;
using System.Collections.Generic;
using IdleOn.Vault;

namespace IdleOn.Save
{
    /// <summary>
    /// Top-level account save. Holds account-shared data (Vault) and the list of
    /// per-character saves (Players) as siblings. One AccountSaveData == one save file.
    /// </summary>
    [Serializable]
    public class AccountSaveData
    {
        public int Version = 1;

        // ── Account-shared data ──────────────────────────────────────────────
        public VaultSaveData Vault = new VaultSaveData();

        // ── Per-character saves (siblings of Vault) ──────────────────────────
        public List<PlayerSaveData> Players = new List<PlayerSaveData>();

        // Id of the currently selected character.
        public string CurrentPlayerId = string.Empty;

        public PlayerSaveData FindPlayer(string playerId)
        {
            if (string.IsNullOrEmpty(playerId)) return null;
            foreach (var p in Players)
                if (p != null && p.PlayerId == playerId) return p;
            return null;
        }
    }
}
