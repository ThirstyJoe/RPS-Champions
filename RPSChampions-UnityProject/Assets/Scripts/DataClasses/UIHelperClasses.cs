namespace ThirstyJoe.RPSChampions
{
    public class TitleDescriptionButtonData
    {
        public string Title;
        public string Description;
        public string LinkID;

        public TitleDescriptionButtonData(string linkID, string title, string description = "")
        {
            Title = title;
            Description = description;
            LinkID = linkID;
        }
    }
}