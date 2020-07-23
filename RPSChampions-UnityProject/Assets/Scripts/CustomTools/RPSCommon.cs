namespace ThirstyJoe.RPSChampions
{
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine;
    using PlayFab.Json;
    using System;
    using System.Collections.Generic;

    public static class RPSCommon
    {
        public static string InterpretCloudScriptData(JsonObject jsonResult, string dataName)
        {
            if (jsonResult == null)
            {
                Debug.Log("NULL JsonObject");
                return null;
            }

            // interpret playerData
            object objValue;
            jsonResult.TryGetValue(dataName, out objValue);

            if (objValue == null)
            {
                Debug.Log("NULL JsonObject value");
                return null;
            }
            return (string)objValue;
        }

        public static void OnPlayFabError(PlayFabError obj)
        {
            Debug.Log(obj.ErrorMessage);
        }

        // from: https://ourcodeworld.com/articles/read/865/how-to-convert-an-unixtime-to-datetime-class-and-viceversa-in-c-sharp
        public static DateTime UnixTimeToDateTime(long unixtime)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixtime).ToLocalTime();
            return dtDateTime;
        }

        public static Weapon ParseWeapon(string weaponName)
        {
            if (weaponName == null)
                return Weapon.None;

            return (Weapon)Enum.Parse(typeof(Weapon), weaponName);
        }


        public static string GetMatchTurnKey(string playerId, string matchIndex)
        {
            return "MatchTurn_" + matchIndex + "_" + playerId;
        }
        public static string GetMatchResultKey(string p1Id, string p2Id, string matchIndex)
        {
            List<string> playerIds = new List<string>() { p1Id, p2Id };
            playerIds.Sort();
            return "MatchResult_" + matchIndex + "_" + playerIds[0] + "_" + playerIds[1];
        }
        public static string GetPlayerKey(string playerId)
        {
            return "Player_" + playerId;
        }
        public static string GetStatsKey(string leagueId)
        {
            return "LeagueStats_" + leagueId;
        }
        public static string GetScheduleKey(string playerId)
        {
            return "PlayerSchedule_" + playerId;
        }
        public static int GetDataTimeFromMatchBrief(string matchBrief)
        {
            return Int32.Parse(matchBrief.Split('@')[0]);
        }
        public static string GetOpponentNameFromMatchBrief(string matchBrief)
        {
            return matchBrief.Split('@')[1];
        }
        public static string GetOpponentIdFromMatchBrief(string matchBrief)
        {
            return matchBrief.Split('@')[2];
        }
        public static string GetPlayerIdFromMatchBrief(string matchBrief)
        {
            return matchBrief.Split('@')[3];
        }
        public static string GetResultFromMatchBrief(string matchBrief)
        {
            return matchBrief.Split('@')[4];
        }
    }
}