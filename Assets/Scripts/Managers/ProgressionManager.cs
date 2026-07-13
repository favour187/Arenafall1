using UnityEngine;
using ArenaFall.Core;
using ArenaFall.Data;
using ArenaFall.Events;

namespace ArenaFall.Managers
{
    /// <summary>
    /// Manages player progression including XP, levels, battle pass, and missions.
    /// </summary>
    public class ProgressionManager : SaveManager // Inherits save behavior
    {
        private SaveManager _saveManager;

        private void Start()
        {
            _saveManager = ServiceLocator.Get<SaveManager>();
            ServiceLocator.Register<ProgressionManager>(this);
        }

        /// <summary>
        /// Add XP to the player's total.
        /// </summary>
        public void AddXP(int amount, string source = "match")
        {
            if (_saveManager?.CurrentSave == null) return;

            _saveManager.AddXP(amount);

            EventBus.Raise(new XPAddedEvent
            {
                Amount = amount,
                TotalXP = _saveManager.CurrentSave.totalXP,
                Source = source
            });
        }

        /// <summary>
        /// Complete a mission.
        /// </summary>
        public void CompleteMission(string missionId)
        {
            var save = _saveManager?.CurrentSave;
            if (save == null) return;

            // Mark mission as completed
            var missions = save.dailyMissions;
            var mission = missions.Find(m => m.missionId == missionId);
            if (mission != null)
            {
                mission.isCompleted = true;
                _saveManager.MarkDirty();
            }

            EventBus.Raise(new MissionCompletedEvent
            {
                MissionId = missionId,
                MissionName = missionId,
                Rewards = new MissionReward[] { new MissionReward { Type = RewardType.XP, Amount = 100 } }
            });
        }

        /// <summary>
        /// Update mission progress.
        /// </summary>
        public void UpdateMissionProgress(string missionId, int progress)
        {
            var save = _saveManager?.CurrentSave;
            if (save == null) return;

            var mission = save.dailyMissions.Find(m => m.missionId == missionId);
            if (mission != null)
            {
                mission.currentProgress = progress;
                if (mission.currentProgress >= mission.targetProgress)
                {
                    CompleteMission(missionId);
                }
                _saveManager.MarkDirty();
            }
        }

        /// <summary>
        /// Add battle pass XP.
        /// </summary>
        public void AddBattlePassXP(int amount)
        {
            var save = _saveManager?.CurrentSave;
            if (save == null) return;

            save.battlePass.currentXP += amount;
            save.battlePass.totalXP += amount;

            // Check tier up
            int xpPerTier = 1000;
            while (save.battlePass.currentXP >= xpPerTier)
            {
                save.battlePass.currentXP -= xpPerTier;
                save.battlePass.currentTier++;
            }

            _saveManager.MarkDirty();
        }

        /// <summary>
        /// Unlock an achievement.
        /// </summary>
        public void UnlockAchievement(string achievementId)
        {
            var save = _saveManager?.CurrentSave;
            if (save == null) return;

            if (!save.completedAchievements.Contains(achievementId))
            {
                save.completedAchievements.Add(achievementId);
                _saveManager.MarkDirty();

                EventBus.Raise(new AchievementUnlockedEvent
                {
                    AchievementId = achievementId,
                    AchievementName = achievementId
                });
            }
        }

        /// <summary>
        /// Process match end rewards.
        /// </summary>
        public void ProcessMatchRewards(int placement, int kills, int damage, float survivalTime)
        {
            int xpReward = 50; // Base XP
            
            // Placement bonus
            if (placement == 1) xpReward += 500;
            else if (placement <= 5) xpReward += 300;
            else if (placement <= 10) xpReward += 200;
            else if (placement <= 25) xpReward += 100;

            // Kill bonus
            xpReward += kills * 100;

            // Survival bonus
            xpReward += Mathf.FloorToInt(survivalTime / 60f) * 10;

            int creditReward = xpReward / 2;

            AddXP(xpReward, "match");
            
            var save = _saveManager?.CurrentSave;
            if (save != null)
            {
                save.credits += creditReward;
                _saveManager.MarkDirty();
            }
        }
    }
}
