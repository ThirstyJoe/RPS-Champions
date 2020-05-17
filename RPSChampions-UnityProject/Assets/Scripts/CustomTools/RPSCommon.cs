namespace ThirstyJoe.RPSChampions
{
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

    }
}