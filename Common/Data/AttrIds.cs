namespace MikuSB.Data;

public static class AttrIds
{
    public const uint CurrencyGid = Currency.GroupId;

    public static class Currency
    {
        public const uint GroupId = 1;
        public const uint Money = 1;
        public const uint Gold = 2;
        public const uint Silver = 3;
        public const uint Vigor = 4;
        public const uint Token = 5;
        public const uint PayGold = 8;
        public const uint Repression = 12;
        public const uint Furniture = 13;
        public const uint StarWish = 61;
        public const uint Rmb = 100;

        public static uint GetSid(uint moneyType) => checked(moneyType * 2 + 1);
    }

    public static class CashExchange
    {
        public const uint GroupId = 70;
        public const uint VigorLimitSid = 1;
        public const uint SilverLimitSid = 2;
    }

    public static class Achievement
    {
        public const uint GroupId = 2;
        public const uint QuestGroupId = 7;
        public const uint DlcGroupId = 14;
        public const uint VirCaptureGroupId = 133;
        public const uint DailyPointGroupId = 8;
        public const uint DailyPointSid = 1;
        public const uint DailyRewardStartSid = 10;
        public const uint DailyRewardEndSid = 19;
        public const uint RedDotGroupId = 8;
        public const uint RedDotStartSid = 200;
        public const uint RedDotEndSid = 299;
        public const uint PopGroupId = 111;
        public const uint PopStepEndSid = 2200;
    }

    public static class Activity
    {
        public const uint GroupId = 50;
        public const uint FaceGroupId = 60;
        public const uint BirthdayMailGroupId = 155;
        public const uint GiftboxSuggestGroupId = 182;
        public const uint JigsawPuzzleGroupId = 34;
        public const uint JigsawPuzzleOverVigourSid = 0;
        public const uint JigsawPuzzleHelpTimeStartSid = 10;
        public const uint JigsawPuzzleHelpTimeEndSid = 50;
        public const uint JigsawPuzzleEnterTimeStartSid = 51;
        public const uint JigsawPuzzleEnterTimeEndSid = 90;
        public const uint JigsawPuzzlePieceStateStartSid = 100;
        public const uint JigsawPuzzlePieceStateEndSid = 200;
        public const uint PlayerReturnGroupId = 0;
        public const uint PlayerReturnActSid = 15;
        public const uint PlayerReturnLastActiveTimeSid = 16;
        public const uint PlayerReturnStartTimeSid = 1;
        public const uint PlayerReturnSignSid = 2;
        public const uint PlayerReturnStartLevelSid = 3;
        public const uint PlayerReturnFirstGetSid = 4;
        public const uint PlayerReturnProtectAddSid = 5;
        public const uint RoleFestivalGroupId = 72;
        public const uint RoleFestivalActSid = 1;
        public const uint RoleFestivalLoginDaysSid = 2;
        public const uint RoleFestivalLevelRewardStartSid = 100;
        public const uint RoleFestivalContentStartSid = 1000;
        public const uint RoleFestivalPreReadStartSid = 2000;
        public const uint RoleFestivalReadStateStartSid = 3000;
        public const uint EnergyExchangeStringGroupId = 54;
        public const uint EnergyExchangeStringSid = 1;
        public const uint VigourSupplyGroupId = 105;
        public const uint NitaMonopolyGroupId = 159;
        public const uint NitaMonopolyStringGroupId = 102;
        public const uint NitaMonopolyTreeExpSid = 1;
        public const uint NitaMonopolySumRoundSid = 2;
        public const uint NitaMonopolyTreeRewardStartSid = 10;
        public const uint NitaMonopolyStoryChoiceStartSid = 100;
        public const uint NitaMonopolyBaseGameDataSid = 1;
        public const uint NitaMonopolyCellDataStartSid = 10;
    }

    public static class BattlePass
    {
        public const uint Gid = 25;
        public const uint CurrentIdSid = 1;
        public const uint StatusSid = 2;
        public const uint ExpSid = 3;
        public const uint DailyTaskExpSid = 4;
        public const uint WeeklyTaskExpSid = 5;
        public const uint FirstOpenSid = 11;
        public const uint NormalAwardTaskSid = 100;
        public const uint AdvanceAwardTaskSid = 200;
    }

    public static class Boss
    {
        public const uint Gid = 50;
        public const uint ActivitySid = 0;
        public const uint DifficultyRecordSid = 101;
        public const uint LevelStartSid = 100;
        public const uint LevelSidStride = 10;
    }

    public static class BossPvp
    {
        public const uint Gid = 51;
        public const uint ActivitySid = 0;
        public const uint ChallengeNumSid = 1;
        public const uint DifficultyStartSid = 10;
        public const uint LevelStartSid = 100;
        public const uint LevelSidStride = 10;
    }

    public static class Chess
    {
        public const uint Gid = 12;
        public const uint ActivitySid = 0;
        public const uint RewardGetSid = 1;
        public const uint MapEnterSid = 2;
        public const uint FightPassStartSid = 10;
        public const uint FightPassEndSid = 98;
        public const uint MapDataStartSid = 99;
        public const uint MapDataEndSid = 200;
        public const uint MapDataStringStartSid = 0;
        public const uint MapDataStringEndSid = 50;
        public const uint TeamInfoSid = 51;
        public const uint TeamIdSid = 19;
        public const uint OperationFinishSid = 202;
        public const uint RichmanGid = 52;
        public const uint RichmanMapGid = 121;
        public const uint RichmanMapSidStride = 10;
        public const uint RichmanFansGid = 118;
        public const uint RichmanFansSidStride = 2;
    }

    public static class DarkZone
    {
        public const uint Gid = 16;
        public const uint ActivitySid = 0;
        public const uint CurrentLevelSid = 1;
        public const uint RoundSid = 2;
        public const uint TriggeredPointCountSid = 3;
        public const uint EnterTimeSid = 4;
        public const uint ExtraExpSid = 5;
        public const uint ExtraLevelSid = 6;
        public const uint AwardFlagStartSid = 7;
        public const uint AwardFlagEndSid = 10;
        public const uint TrackingMissionSid = 11;
        public const uint EntrustMissionTimeSid = 12;
        public const uint MaxDailyExpSid = 23;
        public const uint InnerGid = 17;
        public const uint ExploreMissionGroupId = 98;
        public const uint HandbookGroupId = 108;
    }

    public static class Defend
    {
        public const uint Gid = 10;
        public const uint ActivitySid = 0;
        public const uint DifficultySid = 1;
        public const uint PassWaveSid = 2;
        public const uint AwardsGotSid = 3;
        public const uint AwardsGotAdvancedSid = 4;
        public const uint CurrentLevelScoreSid = 5;
        public const uint CurrentLevelSid = 6;
        public const uint MaxLevelScoreSid = 7;
        public const uint CurrentDifficultySid = 8;
        public const uint RoundSid = 11;
        public const uint AwardFlagStartSid = 20;
        public const uint AwardFlagEndSid = 30;
        public const uint LevelPassStartSid = 40;
        public const uint LevelPassEndSid = 100;
        public const uint CurrentActivityLevelPassStartSid = 140;
        public const uint CurrentActivityLevelPassEndSid = 200;
    }

    public static class SeasonBoss
    {
        public const uint Gid = 18;
        public const uint ActivitySid = 1;
        public const uint TermSid = 2;
        public const uint LevelIndexSid = 3;
        public const uint MaxZone1DifficultySid = 4;
        public const uint MaxZone2DifficultySid = 5;
        public const uint HistoryMaxZone1DifficultySid = 6;
        public const uint HistoryMaxZone2DifficultySid = 7;
        public const uint StringGid = 26;
        public const uint LevelStartSid = 100;
        public const uint LevelSidStride = 10;
    }

    public static class TargetShoot
    {
        public const uint GroupId = 106;
        public const uint HighestScoreSid = 1;
        public const uint ShownInfoSid = 0;
    }

    public static class Tower
    {
        public const uint Gid = 3;
        public const uint TimeSid = 1;
        public const uint LevelSid = 2;
        public const uint LevelSidAdvanced = 3;
        public const uint DifficultySid = 4;
        public const uint HistoryDifficultySid = 5;
        public const uint RewardStateStartSid = 100;
        public const uint LevelStateStartSid = 10000;
        public const uint LevelStateGid = 21;
        public const uint PassGid = 22;
        public const uint ProgressBasicSid = 2;
        public const uint ProgressAdvancedSid = 3;
        public const uint DiffSid = DifficultySid;
        public const uint HistoryDiffSid = HistoryDifficultySid;
        public const uint BasicProgressSid = ProgressBasicSid;
        public const uint AdvancedProgressSid = ProgressAdvancedSid;
        public const uint RewardStateSidBase = RewardStateStartSid;
        public const uint LevelStateSidBase = LevelStateStartSid;
    }

    public static class TowerEvent
    {
        public const uint Gid = 13;
        public const uint LevelStateStartSid = 0;
    }

    public static class WorldBoss
    {
        public const uint Gid = 179;
        public const uint RewardGid = 181;
        public const uint ActivitySid = 0;
        public const uint ActivityIdSid = 1;
        public const uint CurrentDataSid = 2;
        public const uint ActivitySidStride = 1000;
        public const uint MemberSidStride = 10;
    }

    public static class Chat
    {
        public const uint Gid = 126;
        public const uint EmojiGid = 144;
    }

    public static class Dlc
    {
        public const uint Gid = 15;
        public const uint ActIdSid = 1;
        public const uint OperationFinishSid = 9;
        public const uint DailyMissionStartSid = 10;
        public const uint WeeklyMissionStartSid = 100;
        public const uint DonoDuelGroupId = 185;
        public const uint DonoDuelTaskGroupId = 186;
        public const uint RmolGroupId = 172;
        public const uint RmolLevelGroupId = 173;
    }

    public static class AlienDefense
    {
        public const uint Gid = 143;
        public const uint ActivitySid = 1;
        public const uint ScoreSid = 2;
        public const uint AwardGotSid = 3;
        public const uint AwardGotAdvancedSid = 4;
        public const uint FightPalSid = 5;
        public const uint MaxScoreSid = 6;
        public const uint FightGirlSid = 7;
        public const uint GirlSkillNumStartSid = 601;
    }

    public static class BagRogue
    {
        public const uint GroupId = 142;
        public const uint ActivitySid = 1;
        public const uint LevelOpenNumSid = 2;
        public const uint BagMaxIndexSid = 3;
        public const uint CurrentLevelNodeSid = 4;
        public const uint BagItemUniqueSid = 5;
        public const uint CurrentWalkNodeSid = 6;
        public const uint CurrentStockroomNumSid = 7;
        public const uint FirstEnterSid = 8;
        public const uint TrialCard1Sid = 9;
        public const uint TrialCard2Sid = 10;
        public const uint TrialCard3Sid = 11;
        public const uint AwardRefreshTimesSid = 13;
        public const uint UnlockEndingStoryInfoSid = 15;
        public const uint ChosenExternalItemUniqueSid = 16;
        public const uint CurrentCraftNumSid = 17;
        public const uint CurrentReforgeNumSid = 18;
        public const uint SavedItemIdBackupSid = 19;
        public const uint StringGroupId = 61;
        public const uint StringLevelNodeSid = 1;
        public const uint StringEquipBagSid = 2;
        public const uint StringMapNodeRewardSid = 3;
        public const uint StringBestEndNodeRecordSid = 5;
    }

    public static class ClimbTowerDlc
    {
        public const uint GroupId = 174;
        public const uint ActivitySid = 1;
        public const uint AwardStartSid = 10;
        public const uint AwardEndSid = 30;
    }

    public static class Decrypt
    {
        public const uint Gid = 156;
        public const uint SubIdStride = 3;
    }

    public static class DreamCard
    {
        public const uint DataGid = 62;
        public const uint LevelGid = 152;
        public const uint RewardGid = 153;
        public const uint UnlockGid = 154;
        public const uint LevelSidStride = 10;
    }

    public static class Element
    {
        public const uint Gid = 33;
        public const uint StringGid = 53;
        public const uint DrawListSid = 1;
        public const uint BuyGoodsListSid = 2;
        public const uint HarvestNumSid = 3;
    }

    public static class Party
    {
        public const uint Gid = 141;
        public const uint MapGid = 103;
        public const uint FailGid = 145;
        public const uint TeachGid = 146;
        public const uint TitleGid = 147;
        public const uint TitleShownGid = 148;
        public const uint ExpSid = 0;
        public const uint SidStride = 10;
    }

    public static class RichmanLove
    {
        public const uint Gid = 52;
        public const uint MapGid = 121;
        public const uint DiceGid = 130;
        public const uint EndGid = 131;
        public const uint MapSidStride = 10;
    }

    public static class Rogue
    {
        public const uint Gid = 25;
        public const uint BaseInfoSid = 1;
        public const uint PathInfoSid = 2;
        public const uint RoleInfoSid = 3;
        public const uint BuffInfoSid = 4;
        public const uint FormationInfoSid = 5;
        public const uint ShopGoodsListSid = 10;
        public const uint BuyListSid = 11;
        public const uint DailyRefreshFlagSid = 9997;
        public const uint VisitShopNodeSid = 9998;
        public const uint OpenStorySid = 9999;
    }

    public static class Rogue3D
    {
        public const uint Gid = 124;
        public const uint ActivitySid = 1;
        public const uint CurrentExpSid = 2;
        public const uint CurrentLevelSid = 3;
        public const uint LevelOpenNumSid = 4;
        public const uint CurDiffSid = 5;
        public const uint GameplayIdSid = 6;
        public const uint TalentIdSid = 7;
        public const uint EnterFlagSid = 8;
        public const uint DailyCountSid = 9;
        public const uint TechLevelStartSid = 100;
        public const uint TechLevelEndSid = 300;
        public const uint TechRestrictStartSid = 500;
        public const uint TechRestrictEndSid = 700;
        public const uint TechPointCurrencyType = 25;
        public const uint SeasonActivitySid = 1001;
        public const uint SeasonLevelOpenNumSid = 1004;
        public const uint SeasonGameplayIdSid = 1006;
        public const uint SeasonTalentIdSid = 1007;
        public const uint SeasonEnterFlagSid = 1008;
        public const uint SeasonDailyTicketNumSid = 1011;
        public const uint SeasonPoolStartSid = 1100;
        public const uint JourneyItemFlagStartSid = 2201;
        public const uint JourneySanNumSid = 2301;
        public const uint JourneyKeyDropNumStartSid = 2304;
        public const uint MonsterTotalRikiSid = 3001;
        public const uint UavIdSid = 3101;
        public const uint UavCoreIdSid = 3102;
        public const uint UseCountSid = 3104;
        public const uint StringGroupId = 56;
        public const uint StringTaskSid = 1;
        public const uint StringSeasonTaskSid = 2;
        public const uint StringJourneyTrialSid = 3;
        public const uint StringJourneyBuffSid = 6;
        public const uint BuffGroupId = 137;
        public const uint JourneyKeyIdStartSid = 31;
        public const uint LevelPassStartSid = TechLevelStartSid;
        public const uint DailyBuffStartSid = 51;
        public const uint DailyBuffEndSid = 65;
    }

    public static class StarWish
    {
        public const uint Gid = 180;
        public const uint ActivitySid = 1;
        public const uint ScoreAwardGotSid = 2;
        public const uint HouseTreeJigsawStepSid = 3;
        public const uint RepeatQuestExtraAwardSid = 4;
        public const uint EquipJigsawOffsetSid = 70;
        public const uint JigsawIdOffsetSid = 80;
        public const uint StoryQuestPageStartSid = 90;
        public const uint StoryQuestIdOffsetSid = 100;
        public const uint SidStride = 3;
    }

    public static class VirCapture
    {
        public const uint Gid = 128;
        public const uint ActivitySid = 1;
        public const uint CurrentExpSid = 2;
        public const uint CurrentLevelSid = 3;
        public const uint BagRedFlagSid = 4;
        public const uint BagNumSid = 5;
        public const uint TrialActivitySid = 6;
        public const uint TrialPointsSid = 7;
        public const uint DailyExpSid = 8;
        public const uint SeasonActivitySid = 9;
        public const uint ColorMaxStartSid = 11;
        public const uint ColorMaxEndSid = 20;
        public const uint LevelAwardFlagStartSid = 101;
        public const uint LevelAwardFlagEndSid = 120;
        public const uint RikiAwardFlagStartSid = 121;
        public const uint RikiAwardFlagEndSid = 140;
        public const uint TowerStarNormalFlagStartSid = 141;
        public const uint TowerStarNormalFlagEndSid = 160;
        public const uint MapDataStartSid = 10000;
        public const uint MapDataEndSid = 19000;
        public const uint MaxMapDataLength = 3000;
        public const uint FormationStringGid = 57;
        public const uint FormationSid = 1;
        public const uint TrialFormationSid = 2;
        public const uint RikiGid = 135;
        public const uint BuildGroupId = 161;
        public const uint StaffGroupId = 162;
        public const uint TaskGroupId = 163;
        public const uint TrialActIdSid = TrialActivitySid;
        public const uint SeasonActIdSid = SeasonActivitySid;
    }

    public static class FarmWar
    {
        public const uint Gid = 164;
        public const uint ActivitySid = 1;
        public const uint SkillGid = 165;
        public const uint BuffGid = 169;
        public const uint StringGid = 104;
    }

    public static class Fishing
    {
        public const uint Gid = 32;
        public const uint NodeBaseSid = 10000;
        public const uint FishBaseSid = 20000;
        public const uint FoodBaseSid = 30000;
        public const uint FishingExpSid = 1;
        public const uint ActivitySid = 3;
        public const uint TodayExpSid = 5;
        public const uint LevelStartSid = 10;
        public const uint LevelEndSid = 19;
        public const uint RikiAwardStartSid = 20;
        public const uint RikiAwardEndSid = 29;
        public const uint NodeSuccessTimesSid = 1;
        public const uint FishCaughtNumSid = 1;
        public const uint FishMaxLengthSid = 2;
        public const uint FishMaxWeightSid = 3;
        public const uint FishNewWeightRecordSid = 4;
        public const uint FishNewLengthRecordSid = 5;
        public const uint FishMaxScoreSid = 6;
        public const uint FoodAvailableTimeSid = 1;
        public const uint FoodExploreAvailableTimeSid = 2;
    }

    public static class FragmentStory
    {
        public const uint Gid = 11;
    }

    public static class Gacha
    {
        public const uint Gid = 5;
        public const uint StringGid = 42;
        public const uint TotalTimeSid = 1;
        public const uint DailyTotalTimeSid = 2;
        public const uint TimeInheritStartSid = 20000;
        public const uint TimeNotInheritStartSid = 10;
        public const uint AddTimeItemSid = 1;
        public const uint AddTimeProbSid = 2;
        public const uint AddProtectTypeSid = 3;
        public const uint AddBigGuaranteeTimeItemSid = 4;
        public const uint AddSmallGuaranteeTimeItemSid = 5;
        public const uint AddFirstTriggeredSid = 6;
        public const uint AddTotalTimeSid = 7;
        public const uint AddPurpleFlagSid = 8;
        public const uint AddBitValue1Sid = 9;
        public const uint UpSelectGetFlagSid = 2;
    }

    public static class Girl
    {
        public const uint SpineStringGid = 30;
        public const uint RushGid = 158;
        public const uint RushAwardGid = 160;
        public const uint RoleCardVoicesGid = 136;
        public const uint RoleCardBreakGid = 80;
        public const uint RoleCardBreakSid = 1;
    }

    public static class Guide
    {
        public const uint BeginnerGroupId = 187;
        public const uint GroupId = 4;
        public const uint LegacyGroupId = 40;
        public const uint CreateTimeGroupId = 178;
    }

    public static class House
    {
        public const uint Gid = 101;
        public const uint TaskGuideGroupId = 175;
        public const uint ThrowMiniGameGroupId = 170;
        public const uint BedroomStartSid = 2550;
        public const uint PlayerRingInfoSidBase = 3174;
        public const uint HouseInfoStartSid = 3000;
        public const uint MassageRoomInfoStartSid = 10000;
        public const uint HotSpringInfoStartSid = 15000;
        public const uint PubInfoStartSid = 16000;
        public const uint BeachInfoStartSid = 17000;
        public const uint ArcadeInfoStartSid = 18000;
        public const uint CustomWallInfoStartSid = 19000;
        public const uint HasOpenedPuzzleThisWeekSid = 60050;
        public const uint HasOpenedHouseLoveUiSid = 60101;
        public const uint HasOpenedClipInSceneUiSid = 60102;
        public const uint HasOpenedWishClipUiSid = 60103;
        public const uint CurrentShopVersionSid = 60104;
        public const uint SuitViewedStartSid = 60105;
        public const uint RingViewedSid = 60116;
        public const uint GirlRingViewedSid = 60117;
        public const uint PubExpSid = 1;
        public const uint PubLevelRewardGot1Sid = 2;
        public const uint PubLevelRewardGot2Sid = 3;
        public const uint PubBlindRiki1Sid = 4;
        public const uint PubBlindRiki2Sid = 5;
        public const uint PubLevelSeedSid = 6;
        public const uint PubFurnitureStartSid = 101;
        public const uint PubFurnitureEndSid = 110;
        public const uint PubHistoryDataStartSid = 10;
        public const uint PubHistoryDataEndSid = 100;
        public const uint ArcadeLevelSeedSid = 1;
        public const uint ArcadeExpSid = 2;
        public const uint ArcadeLevelRewardGotSid = 3;
        public const uint ArcadeEndlessScoreSid = 4;
        public const uint ArcadeGirlEndlessModeStateSid = 5;
        public const uint ArcadeGirlNormalModeStateStartSid = 10;
        public const uint ArcadeGirlNormalModeStateEndSid = 35;
        public const uint ArcadeConditionValueStartSid = 36;
        public const uint ArcadeConditionValueEndSid = 55;
        public const uint ArcadePropUseTimeStartSid = 56;
        public const uint ArcadePropUseTimeEndSid = 250;
    }

    public static class Item
    {
        public const uint FashionGid = 58;
        public const uint FashionSid = 2;
        public const uint LogisticsGroupId = 150;
        public const uint SpineGid = 30;
        public const uint WeaponPartLockGid = 112;
    }

    public static class Role
    {
        public const uint GroupId = 9;
        public const uint LevelIdStartSid = 10000;
        public const uint MoneySid = 7;
    }

    public static class Launch
    {
        public const uint TemporaryGid = 0;
        public const uint TemporaryMaskSid = 3;
        public const uint TemporaryLevelSid = 4;
        public const uint TemporaryTimeSid = 5;
        public const uint TemporarySeedSid = 6;
        public const uint LevelStateGid = 21;
        public const uint LevelPassGid = 22;
        public const uint LevelRecordGid = 20;
        public const uint ChapterMaskGid = 20;
        public const uint DailyLevelGid = 21;
        public const uint TowerEventLevelGid = 21;
    }

    public static class Online
    {
        public const uint GroupId = 23;
        public const uint WeeklyPointTaskSid = 1;
        public const uint WeeklyAwardTaskSid = 2;
        public const uint PreviousIdTaskSid = 3;
        public const uint JoinIdTaskSid = 4;
        public const uint VigorTaskSid = 5;
        public const uint LevelTaskSid = 6;
        public const uint EnterTimeTaskSid = 7;
        public const uint FightNumTaskSid = 8;
        public const uint DoubleNumTaskSid = 12;
        public const uint CostVigorTaskSid = 13;
        public const uint CheatFlagTaskSid = 14;
        public const uint TrialIdSid = 15;
        public const uint PartyCardIdSid = 16;
        public const uint PartyMatchIdSid = 17;
        public const uint PartySkillIdSid = 18;
        public const uint PartyMatchModeSid = 19;
        public const uint FirstPopTaskStartSid = 20;
        public const uint FirstPopTaskEndSid = 29;
        public const uint RmolRoleSid = 30;
        public const uint RmolFailuresCounterSid = 35;
        public const uint StringGroupId = 43;
        public const uint StringRecentTaskSid = 1;
        public const uint ActivityLevelGid = 24;
        public const uint AssaultOpsGroupId = 183;
        public const uint AssaultOpsBattlePassGroupId = 184;
        public const uint BattlePassGroupId = 125;
        public const uint CombineBattlePassGroupId = 138;
        public const uint SeaGroupId = 171;
        public const uint SpeedScoreGroupId = 176;
        public const uint TowerRushRikiGroupId = 166;
        public const uint BossGroupId = 120;
        public const uint BossDamageGroupId = 119;
        public const uint ForbidzoneGroupId = 140;
    }

    public static class Mail
    {
        public const uint RecycleGid = 59;
        public const uint RecycleSid = 1;
    }

    public static class Misc
    {
        public const uint AdjustGroupId = 107;
        public const uint AdjustGachaWeaponSid = 1;
        public const uint AdjustMallBuySid = 2;
        public const uint AdjustMallBuyMoneySid = 3;
        public const uint AdjustMallBuySkinSid = 4;
        public const uint QuestionnaireGroupId = 57;
        public const uint QuestionnaireCurrentSid = 0;
        public const uint ResourceAmendGid = 117;
        public const uint ResourceAmendBlackSid = 17;
        public const uint LoadingTaskGroupId = 167;
        public const uint ColorFilterGid = 177;
        public const uint RikiTaskGroupId = 103;
    }

    public static class Adjust
    {
        public const uint Gid = Misc.AdjustGroupId;
    }

    public static class Player
    {
        public const uint Gid = 0;
        public const uint IsNewPlayerInDlc32Sid = 201;
        public const uint FriendGroupId = 71;
        public const uint FriendStringGroupId = 55;
        public const uint LoginStateGroupId = 99;
        public const uint LoginStateSid = 3;
        public const uint RenameCountSid = 6;
        public const uint AccountBirthdayGroupId = 151;
        public const uint AccountBirthdaySid = 1;
        public const uint MainRedDotGroupId = 110;
        public const uint LogisticsStoryStringGroupId = 41;
        public const uint LogisticsStoryStringSid = 1;
        public const uint PhotoStudioGroupId = 157;
    }

    public static class Preview
    {
        public const uint MainSceneGid = 132;
        public const uint MainSceneSid = 1;
        public const uint SkinStringGid = 58;
        public const uint SkinSid = 1;
        public const uint RandomShowGid = 139;
        public const uint RandomShowFormGid = 188;
        public const uint RandomGirlStartSid = 0;
        public const uint RandomGirlEndSid = 300;
        public const uint RandomMainSkinStartSid = 401;
        public const uint RandomMainSkinEndSid = 500;
        public const uint RandomGirlSkinInLevelStartSid = 600;
        public const uint RandomGirlSkinInLevelEndSid = 900;
        public const uint RandomBackgroundModeSid = 901;
        public const uint RandomCgStartSid = 1000;
        public const uint RandomCgEndSid = 2000;
    }

    public static class Purchase
    {
        public const uint BuyGroupId = 26;
        public const uint RedDotGroupId = 113;
    }

    public static class Settings
    {
        public const uint Gid = 44;
        public const uint StringGid = 44;
        public const uint LegacyGid = 40;
        public const uint OperationSid = 1;
        public const uint FrameSid = 2;
        public const uint SoundSid = 3;
        public const uint OtherSid = 4;
        public const uint KeyboardSid = 5;
        public const uint NotificationSid = 6;
        public const uint LanguageSid = 7;
        public const uint HandleSid = 8;
        public const uint GirlRushSid = 9;
        public const uint PlayTimeSid = 51;
        public const uint HandIndexSid = 52;
        public const uint PlotSid = 101;
        public const uint NoticeSid = 100;
        public const uint CustomizeSid = 102;
    }

    public static class Shop
    {
        public const uint Gid = 1;
        public const uint GoodsStartSid = 1000;
        public const uint PurchaseGid = 26;
        public const uint RedDotGid = 113;
    }

    public static class Survey
    {
        public const uint GroupId = 104;
        public const uint LastPopupTimeSid = 1;
        public const uint FailCountSid = 2;
        public const uint TaskFlagSid = 3;
        public const uint SumCountSid = 4;
    }

    public static class DonoDuel
    {
        public const uint GroupId = 185;
        public const uint TaskGroupId = 186;
        public const uint ActivitySid = 1;
        public const uint CurrentStockroomNumSid = 3;
        public const uint LeaderSkillSid = 4;
        public const uint RogueCountSid = 5;
        public const uint RogueFormationStartSid = 10;
        public const uint RogueFormationEndSid = 13;
        public const uint ArenaFormationStartSid = 14;
        public const uint ArenaFormationEndSid = 17;
    }

    public static class Riki
    {
        public const uint TaskGroupId = 103;
    }

    public static class Rmol
    {
        public const uint AttributeGid = 172;
        public const uint LevelGid = 173;
    }

    public static class Quest
    {
        public const uint LevelStateGid = Launch.LevelStateGid;
        public const uint LevelPassGid = Launch.LevelPassGid;
        public const uint SettlementSeedGid = 23;
        public const uint ChapterStarAwardGid = Launch.ChapterMaskGid;
        public const uint ChapterStarAwardMaskVersionSid = 0;
    }

    public static class Scene
    {
        public const uint MainGid = Preview.MainSceneGid;
        public const uint MainSid = Preview.MainSceneSid;
    }

    public static class SupporterCard
    {
        public const uint Gid = 150;
        public const uint FixedResetSid = 1;
    }
}
