namespace ThirstyJoe.RPSChampions
{
    using PlayFab;
    using PlayFab.ClientModels;
    using UnityEngine;
    using PlayFab.Json;

    public static class RPSCommon
    {
        public static string InterpretCloudScriptData(JsonObject jsonResult, string dataName)
        {
            if (jsonResult == null)
                return "null";

            // interpret playerData
            object objValue;
            jsonResult.TryGetValue(dataName, out objValue);
            return (string)objValue;
        }

        public static void OnPlayFabError(PlayFabError obj)
        {
            Debug.Log(obj.ErrorMessage);
        }
    }
}